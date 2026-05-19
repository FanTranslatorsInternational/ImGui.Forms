using Hexa.NET.SDL3;
using ImGui.Forms.Controls;
using ImGui.Forms.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace ImGui.Forms.Support;

internal unsafe class SdlGpuMeshRenderer3D : IDisposable
{
    private static readonly Vector4 LightBillboardColor = new(180f, 140f, 40f, 255f);
    private const float MinLightBillboardDistance = 0.5f;
    private const float LightBillboardPaddingFactor = 0.1f;
    private const float MinLightBillboardPadding = 0.25f;
    private static readonly Rgba32 TransparentPixel = new(0, 0, 0, 0);

    private Mesh3DVertex[] _vertices = [];
    private Mesh3DVertex[] _pointCenters = [];
    private Mesh3DVertex[] _lightVertices = [];
    private Image<Rgba32>? _sourceTexture;

    private SDLTexture* _rasterTexture;
    private int _rasterWidth;
    private int _rasterHeight;
    private Rgba32[] _colorBuffer = [];
    private float[] _depthBuffer = [];
    private readonly List<WireTriangle> _wireTriangles = [];
    private readonly List<Rectangle> _additionalScissorExclusions = [];
    private readonly List<Rectangle> _scissorRenderRects = [];

    public SceneConfiguration SceneConfiguration { get; } = new();

    public SdlGpuMeshRenderer3D(Mesh3D? mesh = null)
    {
        SetMesh(mesh);
    }

    public void SetMesh(Mesh3D? mesh)
    {
        if (mesh == null || mesh.Vertices.Count == 0)
        {
            _vertices = [];
            _pointCenters = [];
            _lightVertices = [];
            return;
        }

        var points = new Mesh3DVertex[mesh.Vertices.Count];
        for (int i = 0; i < mesh.Vertices.Count; i++)
        {
            MeshVertex3D pointVertex = mesh.Vertices[i];
            points[i] = new Mesh3DVertex(pointVertex.Position, pointVertex.Color, pointVertex.UvCoordinate);
        }

        var expandedVertices = new List<Mesh3DVertex>(mesh.Faces.Count * 3);
        foreach (var face in mesh.Faces)
        {
            if (face.AIndex < 0 || face.AIndex >= mesh.Vertices.Count)
                continue;
            if (face.BIndex < 0 || face.BIndex >= mesh.Vertices.Count)
                continue;
            if (face.CIndex < 0 || face.CIndex >= mesh.Vertices.Count)
                continue;

            MeshVertex3D vertexA = mesh.Vertices[face.AIndex];
            MeshVertex3D vertexB = mesh.Vertices[face.BIndex];
            MeshVertex3D vertexC = mesh.Vertices[face.CIndex];
            expandedVertices.Add(new Mesh3DVertex(vertexA.Position, vertexA.Color, vertexA.UvCoordinate));
            expandedVertices.Add(new Mesh3DVertex(vertexB.Position, vertexB.Color, vertexB.UvCoordinate));
            expandedVertices.Add(new Mesh3DVertex(vertexC.Position, vertexC.Color, vertexC.UvCoordinate));
        }

        _pointCenters = points;
        _vertices = [.. expandedVertices];
        UpdateLightBillboardVertices();
    }

    public void SetTexture(Image<Rgba32>? texture)
    {
        _sourceTexture = texture;
    }

    public void SetAdditionalScissorExclusions(IEnumerable<Rectangle>? exclusions)
    {
        _additionalScissorExclusions.Clear();
        if (exclusions == null)
            return;

        foreach (Rectangle exclusion in exclusions)
        {
            if (exclusion.Width <= 0f || exclusion.Height <= 0f)
                continue;

            _additionalScissorExclusions.Add(exclusion);
        }
    }

    public void Prepare(SDLRenderer* renderer)
    {
        UpdateLightBillboardVertices();
    }

    public void Render(SDLRenderer* renderer, Rectangle contentSize, ObjectState state)
    {
        if (_vertices.Length == 0 && _pointCenters.Length == 0)
            return;

        BuildScissorRenderRects(contentSize, _scissorRenderRects);
        if (_scissorRenderRects.Count == 0)
            return;

        Matrix4x4 worldViewProjection = state.Transformation * state.View * state.Projection;
        float vertexDotSize = MathF.Max(1f, SceneConfiguration.VertexDotSize);

        RasterizeFaces(renderer, contentSize, worldViewProjection);

        foreach (Rectangle scissorRect in _scissorRenderRects)
        {
            if (!TryConvertToSdlRect(scissorRect, out SDLRect sdlScissor))
                continue;

            SDL.SetRenderClipRect(renderer, sdlScissor);
            DrawFaceRegion(renderer, contentSize, scissorRect);
            DrawGrid(renderer, scissorRect, state.View * state.Projection);
            DrawVertices(renderer, scissorRect, worldViewProjection, _pointCenters, Vector4.One, vertexDotSize);
            DrawVertices(renderer, scissorRect, worldViewProjection, _lightVertices, new Vector4(SceneConfiguration.LightColor, 1f), vertexDotSize * 1.2f);
        }

        SDL.SetRenderClipRect(renderer, (SDLRect*)0);
    }

    public void Dispose()
    {
        if (_rasterTexture != null)
        {
            SDL.DestroyTexture(_rasterTexture);
            _rasterTexture = null;
        }
    }

    private void RasterizeFaces(SDLRenderer* renderer, Rectangle contentRect, Matrix4x4 worldViewProjection)
    {
        if (_vertices.Length < 3)
            return;

        int width = Math.Max(1, (int)contentRect.Width);
        int height = Math.Max(1, (int)contentRect.Height);
        EnsureRasterTarget(renderer, width, height);
        Array.Fill(_colorBuffer, TransparentPixel);
        Array.Fill(_depthBuffer, float.PositiveInfinity);
        _wireTriangles.Clear();

        bool hasTexture = _sourceTexture != null;

        for (int i = 0; i + 2 < _vertices.Length; i += 3)
        {
            if (!TryProjectTriangle(width, height, worldViewProjection, _vertices[i], _vertices[i + 1], _vertices[i + 2], out ProjectedTriangle triangle))
                continue;

            _wireTriangles.Add(new WireTriangle(triangle.A.Position, triangle.B.Position, triangle.C.Position));
            RasterizeTriangle(triangle, hasTexture);
        }

        fixed (Rgba32* colorBufferPtr = _colorBuffer)
            SDL.UpdateTexture(_rasterTexture, (SDLRect*)0, colorBufferPtr, _rasterWidth * sizeof(uint));
    }

    private void DrawFaceRegion(SDLRenderer* renderer, Rectangle contentRect, Rectangle scissorRect)
    {
        if (_rasterTexture == null)
            return;

        SDLFRect src = new()
        {
            X = scissorRect.X - contentRect.X,
            Y = scissorRect.Y - contentRect.Y,
            W = scissorRect.Width,
            H = scissorRect.Height
        };

        SDLFRect dst = new()
        {
            X = scissorRect.X,
            Y = scissorRect.Y,
            W = scissorRect.Width,
            H = scissorRect.Height
        };

        SDL.RenderTexture(renderer, _rasterTexture, src, dst);

        if (!SceneConfiguration.ShowWireFrame || _wireTriangles.Count == 0)
            return;

        Vector4 wireColor = NormalizeColor(SceneConfiguration.WireColor);
        float wireThickness = MathF.Max(1f, SceneConfiguration.WireThickness);
        foreach (WireTriangle triangle in _wireTriangles)
        {
            SDLFPoint a = triangle.A;
            SDLFPoint b = triangle.B;
            SDLFPoint c = triangle.C;
            a.X += contentRect.X; a.Y += contentRect.Y;
            b.X += contentRect.X; b.Y += contentRect.Y;
            c.X += contentRect.X; c.Y += contentRect.Y;
            DrawLine(renderer, a, b, wireColor, wireThickness);
            DrawLine(renderer, b, c, wireColor, wireThickness);
            DrawLine(renderer, c, a, wireColor, wireThickness);
        }
    }

    private void EnsureRasterTarget(SDLRenderer* renderer, int width, int height)
    {
        if (_rasterTexture != null && _rasterWidth == width && _rasterHeight == height)
            return;

        if (_rasterTexture != null)
            SDL.DestroyTexture(_rasterTexture);

        _rasterTexture = SDL.CreateTexture(renderer, SDLPixelFormat.Rgba32, SDLTextureAccess.Streaming, width, height);
        if (_rasterTexture == null)
            throw new InvalidOperationException($"Failed to create raster texture: {SDL.GetErrorS()}");

        SDL.SetTextureBlendMode(_rasterTexture, (uint)SDLBlendMode.Blend);
        SDL.SetTextureScaleMode(_rasterTexture, SDLScaleMode.Nearest);
        _rasterWidth = width;
        _rasterHeight = height;
        _colorBuffer = new Rgba32[width * height];
        _depthBuffer = new float[width * height];
    }

    private void RasterizeTriangle(ProjectedTriangle triangle, bool hasTexture)
    {
        float minX = MathF.Min(triangle.A.Position.X, MathF.Min(triangle.B.Position.X, triangle.C.Position.X));
        float minY = MathF.Min(triangle.A.Position.Y, MathF.Min(triangle.B.Position.Y, triangle.C.Position.Y));
        float maxX = MathF.Max(triangle.A.Position.X, MathF.Max(triangle.B.Position.X, triangle.C.Position.X));
        float maxY = MathF.Max(triangle.A.Position.Y, MathF.Max(triangle.B.Position.Y, triangle.C.Position.Y));

        int startX = Math.Max(0, (int)MathF.Floor(minX));
        int startY = Math.Max(0, (int)MathF.Floor(minY));
        int endX = Math.Min(_rasterWidth - 1, (int)MathF.Ceiling(maxX));
        int endY = Math.Min(_rasterHeight - 1, (int)MathF.Ceiling(maxY));

        float area = Edge(triangle.A.Position, triangle.B.Position, triangle.C.Position);
        if (MathF.Abs(area) < float.Epsilon)
            return;

        for (int y = startY; y <= endY; y++)
        {
            for (int x = startX; x <= endX; x++)
            {
                var p = new SDLFPoint { X = x + 0.5f, Y = y + 0.5f };
                float w0 = Edge(triangle.B.Position, triangle.C.Position, p);
                float w1 = Edge(triangle.C.Position, triangle.A.Position, p);
                float w2 = Edge(triangle.A.Position, triangle.B.Position, p);
                bool hasNegative = (w0 < 0f) || (w1 < 0f) || (w2 < 0f);
                bool hasPositive = (w0 > 0f) || (w1 > 0f) || (w2 > 0f);
                if (hasNegative && hasPositive)
                    continue;

                w0 /= area;
                w1 /= area;
                w2 /= area;
                float depth = (w0 * triangle.A.Depth) + (w1 * triangle.B.Depth) + (w2 * triangle.C.Depth);
                int pixelIndex = (y * _rasterWidth) + x;
                if (depth >= _depthBuffer[pixelIndex])
                    continue;

                _depthBuffer[pixelIndex] = depth;
                Rgba32 color = InterpolateColor(triangle, w0, w1, w2);
                if (hasTexture)
                {
                    Vector2 uv = PerspectiveCorrectUv(triangle, w0, w1, w2);
                    color = MultiplyColor(color, SampleTexture(uv));
                }

                _colorBuffer[pixelIndex] = color;
            }
        }
    }

    private static float Edge(SDLFPoint a, SDLFPoint b, SDLFPoint c)
    {
        return ((c.X - a.X) * (b.Y - a.Y)) - ((c.Y - a.Y) * (b.X - a.X));
    }

    private static Rgba32 InterpolateColor(ProjectedTriangle triangle, float w0, float w1, float w2)
    {
        float r = (triangle.A.Color.R * w0) + (triangle.B.Color.R * w1) + (triangle.C.Color.R * w2);
        float g = (triangle.A.Color.G * w0) + (triangle.B.Color.G * w1) + (triangle.C.Color.G * w2);
        float b = (triangle.A.Color.B * w0) + (triangle.B.Color.B * w1) + (triangle.C.Color.B * w2);
        float a = (triangle.A.Color.A * w0) + (triangle.B.Color.A * w1) + (triangle.C.Color.A * w2);
        return new Rgba32(ClampByte(r), ClampByte(g), ClampByte(b), ClampByte(a));
    }

    private static byte ClampByte(float value)
    {
        return (byte)Math.Clamp((int)MathF.Round(value), 0, 255);
    }

    private static Vector2 PerspectiveCorrectUv(ProjectedTriangle triangle, float w0, float w1, float w2)
    {
        float invW = (w0 * triangle.A.InvW) + (w1 * triangle.B.InvW) + (w2 * triangle.C.InvW);
        if (MathF.Abs(invW) < float.Epsilon)
            return Vector2.Zero;

        float u = ((w0 * triangle.A.Uv.X * triangle.A.InvW) + (w1 * triangle.B.Uv.X * triangle.B.InvW) + (w2 * triangle.C.Uv.X * triangle.C.InvW)) / invW;
        float v = ((w0 * triangle.A.Uv.Y * triangle.A.InvW) + (w1 * triangle.B.Uv.Y * triangle.B.InvW) + (w2 * triangle.C.Uv.Y * triangle.C.InvW)) / invW;
        return new Vector2(u, v);
    }

    private Rgba32 SampleTexture(Vector2 uv)
    {
        if (_sourceTexture == null || _sourceTexture.Width <= 0 || _sourceTexture.Height <= 0)
            return new Rgba32(255, 255, 255, 255);

        float wrappedU = uv.X - MathF.Floor(uv.X);
        float wrappedV = uv.Y - MathF.Floor(uv.Y);
        int x = Math.Clamp((int)(wrappedU * (_sourceTexture.Width - 1)), 0, _sourceTexture.Width - 1);
        int y = Math.Clamp((int)(wrappedV * (_sourceTexture.Height - 1)), 0, _sourceTexture.Height - 1);
        return _sourceTexture[x, y];
    }

    private static Rgba32 MultiplyColor(Rgba32 vertexColor, Rgba32 textureColor)
    {
        byte r = (byte)((vertexColor.R * textureColor.R) / 255);
        byte g = (byte)((vertexColor.G * textureColor.G) / 255);
        byte b = (byte)((vertexColor.B * textureColor.B) / 255);
        byte a = (byte)((vertexColor.A * textureColor.A) / 255);
        return new Rgba32(r, g, b, a);
    }

    private void DrawGrid(SDLRenderer* renderer, Rectangle contentRect, Matrix4x4 viewProjection)
    {
        if (!SceneConfiguration.ShowGrid)
            return;

        Vector4 gridColor = NormalizeColor(new Vector4(150f, 150f, 150f, 200f));
        const int halfCells = 15;
        const float step = 1f;

        for (int i = -halfCells; i <= halfCells; i++)
        {
            float offset = i * step;
            DrawProjectedLine(renderer, contentRect, viewProjection, new Vector3(offset, 0f, -halfCells * step), new Vector3(offset, 0f, halfCells * step), gridColor);
            DrawProjectedLine(renderer, contentRect, viewProjection, new Vector3(-halfCells * step, 0f, offset), new Vector3(halfCells * step, 0f, offset), gridColor);
        }
    }

    private void DrawVertices(SDLRenderer* renderer, Rectangle contentRect, Matrix4x4 worldViewProjection, Mesh3DVertex[] points, Vector4 color, float size)
    {
        if (!SceneConfiguration.ShowVertices && !ReferenceEquals(points, _lightVertices))
            return;

        Vector4 normalizedColor = NormalizeColor(color);
        float halfSize = size * 0.5f;
        foreach (Mesh3DVertex point in points)
        {
            if (!TryProjectPoint((int)contentRect.Width, (int)contentRect.Height, worldViewProjection, point.Position, out SDLFPoint projected, out _, out _))
                continue;

            projected.X += contentRect.X;
            projected.Y += contentRect.Y;

            SDL.SetRenderDrawColorFloat(renderer, normalizedColor.X, normalizedColor.Y, normalizedColor.Z, normalizedColor.W);
            SDLFRect dotRect = new()
            {
                X = projected.X - halfSize,
                Y = projected.Y - halfSize,
                W = size,
                H = size
            };
            SDL.RenderFillRect(renderer, dotRect);
        }
    }

    private static void DrawLine(SDLRenderer* renderer, SDLFPoint start, SDLFPoint end, Vector4 color, float thickness)
    {
        SDL.SetRenderDrawColorFloat(renderer, color.X, color.Y, color.Z, color.W);
        if (thickness <= 1f)
        {
            SDL.RenderLine(renderer, start.X, start.Y, end.X, end.Y);
            return;
        }

        float halfThickness = thickness * 0.5f;
        for (float offset = -halfThickness; offset <= halfThickness; offset += 1f)
            SDL.RenderLine(renderer, start.X + offset, start.Y, end.X + offset, end.Y);
    }

    private static void DrawProjectedLine(SDLRenderer* renderer, Rectangle contentRect, Matrix4x4 viewProjection, Vector3 start, Vector3 end, Vector4 color)
    {
        if (!TryProjectPoint((int)contentRect.Width, (int)contentRect.Height, viewProjection, start, out SDLFPoint projectedStart, out _, out _))
            return;
        if (!TryProjectPoint((int)contentRect.Width, (int)contentRect.Height, viewProjection, end, out SDLFPoint projectedEnd, out _, out _))
            return;

        projectedStart.X += contentRect.X;
        projectedStart.Y += contentRect.Y;
        projectedEnd.X += contentRect.X;
        projectedEnd.Y += contentRect.Y;
        DrawLine(renderer, projectedStart, projectedEnd, color, 1f);
    }

    private static bool TryProjectTriangle(int width, int height, Matrix4x4 mvp, Mesh3DVertex a, Mesh3DVertex b, Mesh3DVertex c, out ProjectedTriangle triangle)
    {
        triangle = default;
        if (!TryProjectVertex(width, height, mvp, a, out RasterVertex pa))
            return false;
        if (!TryProjectVertex(width, height, mvp, b, out RasterVertex pb))
            return false;
        if (!TryProjectVertex(width, height, mvp, c, out RasterVertex pc))
            return false;

        triangle = new ProjectedTriangle(pa, pb, pc);
        return true;
    }

    private static bool TryProjectVertex(int width, int height, Matrix4x4 mvp, Mesh3DVertex input, out RasterVertex vertex)
    {
        vertex = default;
        if (!TryProjectPoint(width, height, mvp, input.Position, out SDLFPoint projected, out float ndcDepth, out float invW))
            return false;

        SDLFColor color = ToSdlColor(input.Color);
        vertex = new RasterVertex(
            projected,
            ndcDepth,
            invW,
            input.UvCoordinate,
            new Rgba32(ClampByte(color.R * 255f), ClampByte(color.G * 255f), ClampByte(color.B * 255f), ClampByte(color.A * 255f)));
        return true;
    }

    private static bool TryProjectPoint(int width, int height, Matrix4x4 mvp, Vector3 position, out SDLFPoint projected, out float ndcDepth, out float invW)
    {
        projected = default;
        ndcDepth = 0f;
        invW = 0f;

        Vector4 clip = Vector4.Transform(new Vector4(position, 1f), mvp);
        if (Math.Abs(clip.W) < float.Epsilon || clip.W <= 0f)
            return false;

        invW = 1f / clip.W;
        float ndcX = clip.X * invW;
        float ndcY = clip.Y * invW;
        ndcDepth = clip.Z * invW;
        projected = new SDLFPoint
        {
            X = (ndcX * 0.5f + 0.5f) * width,
            Y = (1f - (ndcY * 0.5f + 0.5f)) * height
        };
        return true;
    }

    private void BuildScissorRenderRects(Rectangle contentRect, List<Rectangle> output)
    {
        output.Clear();
        if (contentRect.Width <= 0f || contentRect.Height <= 0f)
            return;

        output.Add(contentRect);

        foreach (Rectangle exclusion in _additionalScissorExclusions)
        {
            Rectangle? clippedExclusion = Intersect(contentRect, exclusion);
            if (clippedExclusion == null)
                continue;

            var next = new List<Rectangle>(output.Count * 4);
            foreach (Rectangle candidate in output)
            {
                Rectangle? clippedCandidateExclusion = Intersect(candidate, clippedExclusion.Value);
                if (clippedCandidateExclusion == null)
                {
                    next.Add(candidate);
                    continue;
                }

                Subtract(candidate, clippedCandidateExclusion.Value, next);
            }

            output.Clear();
            output.AddRange(next);
            if (output.Count == 0)
                return;
        }
    }

    private static Rectangle? Intersect(Rectangle a, Rectangle b)
    {
        float left = Math.Max(a.X, b.X);
        float top = Math.Max(a.Y, b.Y);
        float right = Math.Min(a.X + a.Width, b.X + b.Width);
        float bottom = Math.Min(a.Y + a.Height, b.Y + b.Height);
        float width = right - left;
        float height = bottom - top;
        if (width <= 0f || height <= 0f)
            return null;

        return new Rectangle(new Vector2(left, top), new Vector2(width, height));
    }

    private static void Subtract(Rectangle source, Rectangle cut, List<Rectangle> output)
    {
        float sourceRight = source.X + source.Width;
        float sourceBottom = source.Y + source.Height;
        float cutRight = cut.X + cut.Width;
        float cutBottom = cut.Y + cut.Height;

        AddRect(source.X, source.Y, source.Width, cut.Y - source.Y, output);
        AddRect(source.X, cutBottom, source.Width, sourceBottom - cutBottom, output);
        AddRect(source.X, cut.Y, cut.X - source.X, cut.Height, output);
        AddRect(cutRight, cut.Y, sourceRight - cutRight, cut.Height, output);
    }

    private static void AddRect(float x, float y, float width, float height, List<Rectangle> output)
    {
        if (width <= 0f || height <= 0f)
            return;
        output.Add(new Rectangle(new Vector2(x, y), new Vector2(width, height)));
    }

    private static bool TryConvertToSdlRect(Rectangle rect, out SDLRect sdlRect)
    {
        int x = (int)MathF.Floor(rect.X);
        int y = (int)MathF.Floor(rect.Y);
        int right = (int)MathF.Ceiling(rect.X + rect.Width);
        int bottom = (int)MathF.Ceiling(rect.Y + rect.Height);
        int width = right - x;
        int height = bottom - y;
        sdlRect = new SDLRect { X = x, Y = y, W = width, H = height };
        return width > 0 && height > 0;
    }

    private void UpdateLightBillboardVertices()
    {
        Vector3 lightDirection = Vector3.Normalize(SceneConfiguration.LightDirection == Vector3.Zero ? new Vector3(1f, 0f, -1f) : SceneConfiguration.LightDirection);
        float lightDistance = GetLightBillboardDistance(lightDirection);
        Vector3 lightPosition = lightDirection * lightDistance;
        _lightVertices = [new Mesh3DVertex(lightPosition, LightBillboardColor, Vector2.Zero)];
    }

    private float GetLightBillboardDistance(Vector3 lightDirection)
    {
        if (_pointCenters.Length == 0)
            return MinLightBillboardDistance;

        float maxProjection = float.NegativeInfinity;
        Vector3 minBounds = new(float.PositiveInfinity);
        Vector3 maxBounds = new(float.NegativeInfinity);
        foreach (Mesh3DVertex vertex in _pointCenters)
        {
            maxProjection = MathF.Max(maxProjection, Vector3.Dot(vertex.Position, lightDirection));
            minBounds = Vector3.Min(minBounds, vertex.Position);
            maxBounds = Vector3.Max(maxBounds, vertex.Position);
        }

        float diagonalLength = Vector3.Distance(minBounds, maxBounds);
        float padding = MathF.Max(MinLightBillboardPadding, diagonalLength * LightBillboardPaddingFactor);
        return MathF.Max(MinLightBillboardDistance, maxProjection + padding);
    }

    private static Vector4 NormalizeColor(Vector4 color)
    {
        if (color.X > 1f || color.Y > 1f || color.Z > 1f || color.W > 1f)
            return color / 255f;
        return color;
    }

    private static SDLFColor ToSdlColor(Vector4 color)
    {
        Vector4 normalizedColor = NormalizeColor(color);
        return new SDLFColor { R = normalizedColor.X, G = normalizedColor.Y, B = normalizedColor.Z, A = normalizedColor.W };
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct Mesh3DVertex(Vector3 position, Vector4 color, Vector2 uvCoordinate)
    {
        public readonly Vector3 Position = position;
        public readonly Vector4 Color = NormalizeColor(color);
        public readonly Vector2 UvCoordinate = uvCoordinate;
    }

    private readonly struct RasterVertex(SDLFPoint position, float depth, float invW, Vector2 uv, Rgba32 color)
    {
        public SDLFPoint Position { get; } = position;
        public float Depth { get; } = depth;
        public float InvW { get; } = invW;
        public Vector2 Uv { get; } = uv;
        public Rgba32 Color { get; } = color;
    }

    private readonly struct ProjectedTriangle(RasterVertex a, RasterVertex b, RasterVertex c)
    {
        public RasterVertex A { get; } = a;
        public RasterVertex B { get; } = b;
        public RasterVertex C { get; } = c;
    }

    private readonly struct WireTriangle(SDLFPoint a, SDLFPoint b, SDLFPoint c)
    {
        public SDLFPoint A { get; } = a;
        public SDLFPoint B { get; } = b;
        public SDLFPoint C { get; } = c;
    }
}
