using Hexa.NET.SDL3;
using ImGui.Forms.Controls;
using ImGui.Forms.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ImGui.Forms.Support;

internal unsafe class SdlGpuMeshRenderer3D : IDisposable
{
    private readonly Mesh3DVertex[] _gridVertices = CreateGridVertices();

    private Mesh3DVertex[] _vertices;
    private Mesh3DVertex[] _pointCenters;
    private Mesh3DVertex[] _pointVertices;
    private bool _vertexDataDirty = true;
    private bool _pointVertexDataDirty = true;
    private bool _gridVertexDataDirty = true;

    private SDLGPUBuffer* _vertexBuffer;
    private uint _vertexBufferSize;
    private SDLGPUBuffer* _pointVertexBuffer;
    private uint _pointVertexBufferSize;
    private SDLGPUBuffer* _gridVertexBuffer;
    private uint _gridVertexBufferSize;

    private SDLGPUGraphicsPipeline* _pipeline;
    private SDLGPUShader* _vertexShader;
    private SDLGPUShader* _fragmentShader;
    private SDLGPUSampler* _textureSampler;
    private SDLGPUTexture* _texture;
    private SDLGPUTexture* _fallbackTexture;
    private Image<Rgba32>? _sourceTexture;
    private bool _textureDirty = true;
    private readonly List<Rectangle> _additionalScissorExclusions = [];
    private readonly List<Rectangle> _scissorRenderRects = [];

    public SceneConfiguration SceneConfiguration { get; } = new();

    public SdlGpuMeshRenderer3D(Mesh3D? mesh = null)
    {
        _vertices = [];
        _pointCenters = [];
        _pointVertices = [];
        SetMesh(mesh);
    }

    public void SetMesh(Mesh3D? mesh)
    {
        if (mesh == null || mesh.Vertices.Count == 0)
        {
            _vertices = [];
            _pointCenters = [];
            _pointVertices = [];
            _vertexDataDirty = true;
            _pointVertexDataDirty = true;
            return;
        }

        var points = new Mesh3DVertex[mesh.Vertices.Count];
        for (int i = 0; i < mesh.Vertices.Count; i++)
        {
            MeshVertex3D pointVertex = mesh.Vertices[i];
            points[i] = new Mesh3DVertex(pointVertex.Position, pointVertex.Color, pointVertex.UvCoordinate, Vector3.Zero);
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
            expandedVertices.Add(new Mesh3DVertex(vertexA.Position, vertexA.Color, vertexA.UvCoordinate, new Vector3(1f, 0f, 0f)));
            expandedVertices.Add(new Mesh3DVertex(vertexB.Position, vertexB.Color, vertexB.UvCoordinate, new Vector3(0f, 1f, 0f)));
            expandedVertices.Add(new Mesh3DVertex(vertexC.Position, vertexC.Color, vertexC.UvCoordinate, new Vector3(0f, 0f, 1f)));
        }

        _pointCenters = points;
        RebuildPointVertices();
        _vertices = [.. expandedVertices];
        _vertexDataDirty = true;
    }

    public void SetTexture(Image<Rgba32>? texture)
    {
        _sourceTexture = texture;
        _textureDirty = true;
    }

    private void RebuildPointVertices()
    {
        if (_pointCenters.Length == 0)
        {
            _pointVertices = [];
            _pointVertexDataDirty = true;
            return;
        }

        var dotVertices = new List<Mesh3DVertex>(_pointCenters.Length * 6);

        foreach (Mesh3DVertex center in _pointCenters)
        {
            AddBillboardDot(dotVertices, center);
        }

        _pointVertices = [.. dotVertices];
        _pointVertexDataDirty = true;
    }

    private static void AddBillboardDot(List<Mesh3DVertex> output, Mesh3DVertex center)
    {
        var color = center.Color;
        Vector3 position = center.Position;

        output.Add(new Mesh3DVertex(position, color, new Vector2(0f, 0f), new Vector3(-1f, -1f, 0f)));
        output.Add(new Mesh3DVertex(position, color, new Vector2(1f, 0f), new Vector3(1f, -1f, 0f)));
        output.Add(new Mesh3DVertex(position, color, new Vector2(1f, 1f), new Vector3(1f, 1f, 0f)));
        output.Add(new Mesh3DVertex(position, color, new Vector2(0f, 0f), new Vector3(-1f, -1f, 0f)));
        output.Add(new Mesh3DVertex(position, color, new Vector2(1f, 1f), new Vector3(1f, 1f, 0f)));
        output.Add(new Mesh3DVertex(position, color, new Vector2(0f, 1f), new Vector3(-1f, 1f, 0f)));
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

    public void Prepare(SDLGPUDevice* gpuDevice, SDLGPUCommandBuffer* commandBuffer)
    {
        if (_vertices.Length == 0 && _pointVertices.Length == 0)
            return;

        CreatePipelineIfRequired(gpuDevice);
        if (_vertices.Length > 0)
            EnsureVertexBuffer(gpuDevice);
        if (_pointVertices.Length > 0)
            EnsurePointVertexBuffer(gpuDevice);
        EnsureGridVertexBuffer(gpuDevice, commandBuffer);
        EnsureSampler(gpuDevice);
        EnsureFallbackTexture(gpuDevice, commandBuffer);
        UploadTextureIfRequired(gpuDevice, commandBuffer);

        if (_vertexDataDirty && _vertexBuffer != null && _vertices.Length > 0)
        {
            UploadVertices(gpuDevice, commandBuffer, _vertexBuffer, _vertices);
            _vertexDataDirty = false;
        }

        if (_pointVertexDataDirty && _pointVertexBuffer != null && _pointVertices.Length > 0)
        {
            UploadVertices(gpuDevice, commandBuffer, _pointVertexBuffer, _pointVertices);
            _pointVertexDataDirty = false;
        }
    }

    public void Render(SDLGPUDevice* gpuDevice, SDLGPUCommandBuffer* commandBuffer, SDLGPURenderPass* renderPass, Rectangle contentSize, ObjectState state)
    {
        bool hasFaceData = _vertices.Length > 0 && _pipeline != null && _vertexBuffer != null;
        bool hasPointData = SceneConfiguration.ShowVertices && _pointVertices.Length > 0 && _pipeline != null && _pointVertexBuffer != null;
        if (!hasFaceData && !hasPointData)
            return;

        int viewportWidth = Math.Max(1, (int)contentSize.Width);
        int viewportHeight = Math.Max(1, (int)contentSize.Height);

        SDLGPUViewport viewport = new()
        {
            X = contentSize.X,
            Y = contentSize.Y,
            W = viewportWidth,
            H = viewportHeight,
            MinDepth = 0f,
            MaxDepth = 1f
        };

        SDL.SetGPUViewport(renderPass, viewport);
        BuildScissorRenderRects(contentSize, _scissorRenderRects);
        if (_scissorRenderRects.Count == 0)
            return;

        GetCameraBasisFromView(state.View, out Vector3 cameraRight, out Vector3 cameraUp);

        MeshTransformUniform uniform = new()
        {
            World = state.Transformation,
            ViewProjection = state.View * state.Projection,
            WorldViewProjection = state.Transformation * state.View * state.Projection,
            // x: wireframe, y: Y=0 grid enabled, z: render pass (0=mesh), w: unused
            RenderParams = new Vector4(SceneConfiguration.ShowWireFrame ? 1f : 0f, SceneConfiguration.ShowGrid ? 1f : 0f, 0f, 0f),
            VertexDotColor = SceneConfiguration.VertexDotColor,
            WireColor = NormalizeColor(SceneConfiguration.WireColor),
            LightDirection = new Vector4(Vector3.Normalize(SceneConfiguration.LightDirection == Vector3.Zero ? new Vector3(1f, 0f, -1f) : SceneConfiguration.LightDirection), 0f),
            LightColor = new Vector4(SceneConfiguration.LightColor, 1f),
            StyleParams = new Vector4(MathF.Max(0.01f, SceneConfiguration.WireThickness), MathF.Max(0f, SceneConfiguration.LightIntensity), 0f, 0f),
            CameraRight = Vector4.Zero,
            CameraUp = Vector4.Zero
        };

        SDLGPUBufferBinding vertexBinding = new()
        {
            Buffer = _vertexBuffer,
            Offset = 0
        };

        SDLGPUTextureSamplerBinding textureBinding = new()
        {
            Texture = _texture != null ? _texture : _fallbackTexture,
            Sampler = _textureSampler
        };
        SDL.BindGPUFragmentSamplers(renderPass, 0, &textureBinding, 1);

        MeshTransformUniform gridUniform = new()
        {
            World = Matrix4x4.Identity,
            ViewProjection = state.View * state.Projection,
            WorldViewProjection = state.View * state.Projection,
            // Disable wireframe, keep grid enabled, mark this as dedicated grid pass (z=1).
            RenderParams = new Vector4(0f, 1f, 1f, 0f),
            VertexDotColor = SceneConfiguration.VertexDotColor,
            WireColor = NormalizeColor(SceneConfiguration.WireColor),
            LightDirection = new Vector4(Vector3.Normalize(SceneConfiguration.LightDirection == Vector3.Zero ? new Vector3(1f, 0f, -1f) : SceneConfiguration.LightDirection), 0f),
            LightColor = new Vector4(SceneConfiguration.LightColor, 1f),
            StyleParams = new Vector4(MathF.Max(0.01f, SceneConfiguration.WireThickness), MathF.Max(0f, SceneConfiguration.LightIntensity), 0f, 0f),
            CameraRight = Vector4.Zero,
            CameraUp = Vector4.Zero
        };

        SDLGPUBufferBinding gridBinding = new()
        {
            Buffer = _gridVertexBuffer,
            Offset = 0
        };

        float vertexDotSize = MathF.Max(1f, SceneConfiguration.VertexDotSize);

        MeshTransformUniform pointUniform = new()
        {
            World = state.Transformation,
            ViewProjection = state.View * state.Projection,
            WorldViewProjection = state.Transformation * state.View * state.Projection,
            // Dedicated vertex-marker pass (z=2).
            RenderParams = new Vector4(0f, 0f, 2f, vertexDotSize),
            VertexDotColor = SceneConfiguration.VertexDotColor,
            WireColor = NormalizeColor(SceneConfiguration.WireColor),
            LightDirection = new Vector4(Vector3.Normalize(SceneConfiguration.LightDirection == Vector3.Zero ? new Vector3(1f, 0f, -1f) : SceneConfiguration.LightDirection), 0f),
            LightColor = new Vector4(SceneConfiguration.LightColor, 1f),
            StyleParams = new Vector4(MathF.Max(0.01f, SceneConfiguration.WireThickness), MathF.Max(0f, SceneConfiguration.LightIntensity), 0f, 0f),
            CameraRight = new Vector4(cameraRight, 0f),
            CameraUp = new Vector4(cameraUp, 0f)
        };
        SDLGPUBufferBinding pointBinding = new()
        {
            Buffer = _pointVertexBuffer,
            Offset = 0
        };

        foreach (Rectangle scissorRect in _scissorRenderRects)
        {
            if (!TryConvertToSdlRect(scissorRect, out SDLRect sdlScissor))
                continue;

            SDL.SetGPUScissor(renderPass, sdlScissor);
            if (hasFaceData)
            {
                SDL.BindGPUGraphicsPipeline(renderPass, _pipeline);
                SDL.BindGPUVertexBuffers(renderPass, 0, &vertexBinding, 1);
                SDL.BindGPUFragmentSamplers(renderPass, 0, &textureBinding, 1);
                SDL.PushGPUVertexUniformData(commandBuffer, 0, &uniform, (uint)sizeof(MeshTransformUniform));
                SDL.DrawGPUPrimitives(renderPass, (uint)_vertices.Length, 1, 0, 0);
            }

            if (SceneConfiguration.ShowGrid && _gridVertexBuffer != null)
            {
                SDL.BindGPUGraphicsPipeline(renderPass, _pipeline);
                SDL.BindGPUVertexBuffers(renderPass, 0, &gridBinding, 1);
                SDL.BindGPUFragmentSamplers(renderPass, 0, &textureBinding, 1);
                SDL.PushGPUVertexUniformData(commandBuffer, 0, &gridUniform, (uint)sizeof(MeshTransformUniform));
                SDL.DrawGPUPrimitives(renderPass, (uint)_gridVertices.Length, 1, 0, 0);
            }

            if (hasPointData)
            {
                SDL.BindGPUGraphicsPipeline(renderPass, _pipeline);
                SDL.BindGPUVertexBuffers(renderPass, 0, &pointBinding, 1);
                SDL.PushGPUVertexUniformData(commandBuffer, 0, &pointUniform, (uint)sizeof(MeshTransformUniform));
                SDL.DrawGPUPrimitives(renderPass, (uint)_pointVertices.Length, 1, 0, 0);
            }
        }
    }

    public void Dispose()
    {
        SDLGPUDevice* gpuDevice = Application.Instance.GpuDevice;
        if (gpuDevice == null)
            return;

        if (_pipeline != null)
        {
            SDL.ReleaseGPUGraphicsPipeline(gpuDevice, _pipeline);
            _pipeline = null;
        }

        if (_vertexShader != null)
        {
            SDL.ReleaseGPUShader(gpuDevice, _vertexShader);
            _vertexShader = null;
        }

        if (_fragmentShader != null)
        {
            SDL.ReleaseGPUShader(gpuDevice, _fragmentShader);
            _fragmentShader = null;
        }

        if (_textureSampler != null)
        {
            SDL.ReleaseGPUSampler(gpuDevice, _textureSampler);
            _textureSampler = null;
        }

        if (_texture != null)
        {
            SDL.ReleaseGPUTexture(gpuDevice, _texture);
            _texture = null;
        }

        if (_fallbackTexture != null)
        {
            SDL.ReleaseGPUTexture(gpuDevice, _fallbackTexture);
            _fallbackTexture = null;
        }

        //if (DepthTexture != null)
        //{
        //    SDL.ReleaseGPUTexture(gpuDevice, DepthTexture);
        //    DepthTexture = null;
        //    DepthTextureWidth = DepthTextureHeight = 0;
        //}

        if (_vertexBuffer != null)
        {
            SDL.ReleaseGPUBuffer(gpuDevice, _vertexBuffer);
            _vertexBuffer = null;
        }

        if (_gridVertexBuffer != null)
        {
            SDL.ReleaseGPUBuffer(gpuDevice, _gridVertexBuffer);
            _gridVertexBuffer = null;
        }

        if (_pointVertexBuffer != null)
        {
            SDL.ReleaseGPUBuffer(gpuDevice, _pointVertexBuffer);
            _pointVertexBuffer = null;
        }
    }

    private void EnsureVertexBuffer(SDLGPUDevice* gpuDevice)
    {
        uint requiredSize = (uint)(_vertices.Length * Marshal.SizeOf<Mesh3DVertex>());
        if (_vertexBuffer != null && _vertexBufferSize >= requiredSize)
            return;

        if (_vertexBuffer != null)
            SDL.ReleaseGPUBuffer(gpuDevice, _vertexBuffer);

        _vertexBuffer = SDL.CreateGPUBuffer(gpuDevice, new SDLGPUBufferCreateInfo
        {
            Usage = (int)SDLGPUBufferUsageFlags.Vertex,
            Size = requiredSize
        });
        _vertexBufferSize = requiredSize;
        _vertexDataDirty = true;
    }

    private void EnsurePointVertexBuffer(SDLGPUDevice* gpuDevice)
    {
        uint requiredSize = (uint)(_pointVertices.Length * Marshal.SizeOf<Mesh3DVertex>());
        if (_pointVertexBuffer != null && _pointVertexBufferSize >= requiredSize)
            return;

        if (_pointVertexBuffer != null)
            SDL.ReleaseGPUBuffer(gpuDevice, _pointVertexBuffer);

        _pointVertexBuffer = SDL.CreateGPUBuffer(gpuDevice, new SDLGPUBufferCreateInfo
        {
            Usage = (int)SDLGPUBufferUsageFlags.Vertex,
            Size = requiredSize
        });
        _pointVertexBufferSize = requiredSize;
        _pointVertexDataDirty = true;
    }

    private static void UploadVertices(SDLGPUDevice* gpuDevice, SDLGPUCommandBuffer* commandBuffer, SDLGPUBuffer* destinationBuffer, Mesh3DVertex[] vertices)
    {
        uint bytesToUpload = (uint)(vertices.Length * Marshal.SizeOf<Mesh3DVertex>());
        SDLGPUTransferBuffer* transferBuffer = SDL.CreateGPUTransferBuffer(gpuDevice, new SDLGPUTransferBufferCreateInfo
        {
            Usage = SDLGPUTransferBufferUsage.Upload,
            Size = bytesToUpload
        });

        void* mapped = SDL.MapGPUTransferBuffer(gpuDevice, transferBuffer, true);
        fixed (Mesh3DVertex* verticesPtr = vertices)
            Buffer.MemoryCopy(verticesPtr, mapped, bytesToUpload, bytesToUpload);
        SDL.UnmapGPUTransferBuffer(gpuDevice, transferBuffer);

        SDLGPUCopyPass* copyPass = SDL.BeginGPUCopyPass(commandBuffer);
        SDL.UploadToGPUBuffer(copyPass,
            new SDLGPUTransferBufferLocation
            {
                TransferBuffer = transferBuffer,
                Offset = 0
            },
            new SDLGPUBufferRegion
            {
                Buffer = destinationBuffer,
                Offset = 0,
                Size = bytesToUpload
            },
            false);
        SDL.EndGPUCopyPass(copyPass);
        SDL.ReleaseGPUTransferBuffer(gpuDevice, transferBuffer);
    }

    private void EnsureGridVertexBuffer(SDLGPUDevice* gpuDevice, SDLGPUCommandBuffer* commandBuffer)
    {
        uint requiredSize = (uint)(_gridVertices.Length * Marshal.SizeOf<Mesh3DVertex>());
        if (_gridVertexBuffer == null || _gridVertexBufferSize < requiredSize)
        {
            if (_gridVertexBuffer != null)
                SDL.ReleaseGPUBuffer(gpuDevice, _gridVertexBuffer);

            _gridVertexBuffer = SDL.CreateGPUBuffer(gpuDevice, new SDLGPUBufferCreateInfo
            {
                Usage = (int)SDLGPUBufferUsageFlags.Vertex,
                Size = requiredSize
            });
            _gridVertexBufferSize = requiredSize;
            _gridVertexDataDirty = true;
        }

        if (!_gridVertexDataDirty || _gridVertexBuffer == null)
            return;

        SDLGPUTransferBuffer* transferBuffer = SDL.CreateGPUTransferBuffer(gpuDevice, new SDLGPUTransferBufferCreateInfo
        {
            Usage = SDLGPUTransferBufferUsage.Upload,
            Size = requiredSize
        });

        void* mapped = SDL.MapGPUTransferBuffer(gpuDevice, transferBuffer, true);
        fixed (Mesh3DVertex* verticesPtr = _gridVertices)
            Buffer.MemoryCopy(verticesPtr, mapped, requiredSize, requiredSize);
        SDL.UnmapGPUTransferBuffer(gpuDevice, transferBuffer);

        SDLGPUCopyPass* copyPass = SDL.BeginGPUCopyPass(commandBuffer);
        SDL.UploadToGPUBuffer(copyPass,
            new SDLGPUTransferBufferLocation
            {
                TransferBuffer = transferBuffer,
                Offset = 0
            },
            new SDLGPUBufferRegion
            {
                Buffer = _gridVertexBuffer,
                Offset = 0,
                Size = requiredSize
            },
            false);
        SDL.EndGPUCopyPass(copyPass);
        SDL.ReleaseGPUTransferBuffer(gpuDevice, transferBuffer);

        _gridVertexDataDirty = false;
    }

    private void CreatePipelineIfRequired(SDLGPUDevice* gpuDevice)
    {
        if (_pipeline != null)
            return;

        (byte[] vertexCode, byte[] fragmentCode, SDLGPUShaderFormat format) = LoadShadersForCurrentBackend(gpuDevice);
        if (_vertexShader == null)
            _vertexShader = CreateShader(gpuDevice, vertexCode, format, SDLGPUShaderStage.Vertex, 1);
        if (_fragmentShader == null)
            _fragmentShader = CreateShader(gpuDevice, fragmentCode, format, SDLGPUShaderStage.Fragment, 0, 1);

        SDLGPUVertexBufferDescription vertexBufferDescription = new()
        {
            Slot = 0,
            InputRate = SDLGPUVertexInputRate.Vertex,
            InstanceStepRate = 0,
            Pitch = (uint)Marshal.SizeOf<Mesh3DVertex>()
        };

        SDLGPUVertexAttribute* attributes = stackalloc SDLGPUVertexAttribute[4];
        attributes[0] = new SDLGPUVertexAttribute
        {
            BufferSlot = 0,
            Format = SDLGPUVertexElementFormat.Float3,
            Location = 0,
            Offset = 0
        };
        attributes[1] = new SDLGPUVertexAttribute
        {
            BufferSlot = 0,
            Format = SDLGPUVertexElementFormat.Float4,
            Location = 1,
            Offset = 12
        };
        attributes[2] = new SDLGPUVertexAttribute
        {
            BufferSlot = 0,
            Format = SDLGPUVertexElementFormat.Float2,
            Location = 2,
            Offset = 28
        };
        attributes[3] = new SDLGPUVertexAttribute
        {
            BufferSlot = 0,
            Format = SDLGPUVertexElementFormat.Float3,
            Location = 3,
            Offset = 36
        };

        SDLGPUColorTargetDescription colorTargetDescription = new()
        {
            Format = Application.Instance.SwapchainFormat,
            BlendState = new SDLGPUColorTargetBlendState
            {
                SrcColorBlendfactor = SDLGPUBlendFactor.SrcAlpha,
                DstColorBlendfactor = SDLGPUBlendFactor.OneMinusSrcAlpha,
                ColorBlendOp = SDLGPUBlendOp.Add,
                SrcAlphaBlendfactor = SDLGPUBlendFactor.One,
                DstAlphaBlendfactor = SDLGPUBlendFactor.OneMinusSrcAlpha,
                AlphaBlendOp = SDLGPUBlendOp.Add,
                EnableBlend = 1
            }
        };

        _pipeline = SDL.CreateGPUGraphicsPipeline(gpuDevice, new SDLGPUGraphicsPipelineCreateInfo
        {
            VertexShader = _vertexShader,
            FragmentShader = _fragmentShader,
            PrimitiveType = SDLGPUPrimitiveType.Trianglelist,
            VertexInputState = new SDLGPUVertexInputState
            {
                NumVertexBuffers = 1,
                VertexBufferDescriptions = &vertexBufferDescription,
                NumVertexAttributes = 4,
                VertexAttributes = attributes
            },
            RasterizerState = new SDLGPURasterizerState
            {
                CullMode = SDLGPUCullMode.None
            },
            DepthStencilState = new SDLGPUDepthStencilState
            {
                CompareOp = SDLGPUCompareOp.LessOrEqual,
                EnableDepthTest = 1,
                EnableDepthWrite = 1,
                EnableStencilTest = 0
            },
            MultisampleState = new SDLGPUMultisampleState
            {
                SampleCount = SDLGPUSampleCount.Samplecount1
            },
            TargetInfo = new SDLGPUGraphicsPipelineTargetInfo
            {
                NumColorTargets = 1,
                ColorTargetDescriptions = &colorTargetDescription,
                HasDepthStencilTarget = 1,
                DepthStencilFormat = SelectDepthFormat(gpuDevice)
            }
        });
    }

    private static SDLGPUShader* CreateShader(SDLGPUDevice* gpuDevice, byte[] code, SDLGPUShaderFormat format, SDLGPUShaderStage stage, uint uniformBufferCount, uint samplerCount = 0)
    {
        fixed (byte* shaderCode = code)
        {
            return SDL.CreateGPUShader(gpuDevice, new SDLGPUShaderCreateInfo
            {
                Code = shaderCode,
                CodeSize = (nuint)code.Length,
                Format = (uint)format,
                Stage = stage,
                NumSamplers = samplerCount,
                NumStorageTextures = 0,
                NumStorageBuffers = 0,
                NumUniformBuffers = uniformBufferCount
            });
        }
    }

    private static (byte[] vertexShader, byte[] fragmentShader, SDLGPUShaderFormat format) LoadShadersForCurrentBackend(SDLGPUDevice* gpuDevice)
    {
        SDLGPUShaderFormat format = (SDLGPUShaderFormat)SDL.GetGPUShaderFormats(gpuDevice);

        if (format.HasFlag(SDLGPUShaderFormat.Dxil))
        {
            return (
                ReadEmbeddedFile("ImGui.Forms.Resources.Shaders.DXIL.Shader.vert.dxil"),
                ReadEmbeddedFile("ImGui.Forms.Resources.Shaders.DXIL.Shader.frag.dxil"),
                SDLGPUShaderFormat.Dxil);
        }

        if (format.HasFlag(SDLGPUShaderFormat.Spirv))
        {
            return (
                ReadEmbeddedFile("ImGui.Forms.Resources.Shaders.SPIRV.Shader.vert.spv"),
                ReadEmbeddedFile("ImGui.Forms.Resources.Shaders.SPIRV.Shader.frag.spv"),
                SDLGPUShaderFormat.Spirv);
        }

        throw new InvalidOperationException($"The current GPU backend does not support supported shader formats. Available: {format}");
    }

    private static byte[] ReadEmbeddedFile(string resourceName)
    {
        using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new InvalidOperationException($"Shader resource not found: {resourceName}");

        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private void EnsureSampler(SDLGPUDevice* gpuDevice)
    {
        if (_textureSampler != null)
            return;

        _textureSampler = SDL.CreateGPUSampler(gpuDevice, new SDLGPUSamplerCreateInfo
        {
            MinFilter = SDLGPUFilter.Nearest,
            MagFilter = SDLGPUFilter.Nearest,
            MipmapMode = SDLGPUSamplerMipmapMode.Nearest,
            AddressModeU = SDLGPUSamplerAddressMode.Repeat,
            AddressModeV = SDLGPUSamplerAddressMode.Repeat,
            AddressModeW = SDLGPUSamplerAddressMode.Repeat
        });
    }

    private void EnsureFallbackTexture(SDLGPUDevice* gpuDevice, SDLGPUCommandBuffer* commandBuffer)
    {
        if (_fallbackTexture != null)
            return;

        using var image = new Image<Rgba32>(1, 1, new Rgba32(255, 255, 255, 255));
        _fallbackTexture = CreateGpuTexture(gpuDevice, image);
        UploadImageToTexture(gpuDevice, commandBuffer, _fallbackTexture, image);
    }

    private void UploadTextureIfRequired(SDLGPUDevice* gpuDevice, SDLGPUCommandBuffer* commandBuffer)
    {
        if (!_textureDirty)
            return;

        if (_texture != null)
        {
            SDL.ReleaseGPUTexture(gpuDevice, _texture);
            _texture = null;
        }

        if (_sourceTexture != null)
        {
            _texture = CreateGpuTexture(gpuDevice, _sourceTexture);
            UploadImageToTexture(gpuDevice, commandBuffer, _texture, _sourceTexture);
        }

        _textureDirty = false;
    }

    private static SDLGPUTexture* CreateGpuTexture(SDLGPUDevice* gpuDevice, Image<Rgba32> image)
    {
        return SDL.CreateGPUTexture(gpuDevice, new SDLGPUTextureCreateInfo
        {
            Width = (uint)image.Width,
            Height = (uint)image.Height,
            Format = SDLGPUTextureFormat.R8G8B8A8Unorm,
            Type = SDLGPUTextureType.Texturetype2D,
            LayerCountOrDepth = 1,
            NumLevels = 1,
            SampleCount = SDLGPUSampleCount.Samplecount1,
            Usage = (int)SDLGPUTextureUsageFlags.Sampler
        });
    }

    private static void UploadImageToTexture(SDLGPUDevice* gpuDevice, SDLGPUCommandBuffer* commandBuffer, SDLGPUTexture* texture, Image<Rgba32> image)
    {
        uint size = (uint)(image.Width * image.Height * 4);
        SDLGPUTransferBuffer* transferBuffer = SDL.CreateGPUTransferBuffer(gpuDevice, new SDLGPUTransferBufferCreateInfo
        {
            Usage = SDLGPUTransferBufferUsage.Upload,
            Size = size
        });

        void* mapped = SDL.MapGPUTransferBuffer(gpuDevice, transferBuffer, true);
        var pixels = new Rgba32[image.Width * image.Height];
        image.CopyPixelDataTo(pixels);
        fixed (Rgba32* pixelPtr = pixels)
            Buffer.MemoryCopy(pixelPtr, mapped, size, size);
        SDL.UnmapGPUTransferBuffer(gpuDevice, transferBuffer);

        SDLGPUCopyPass* copyPass = SDL.BeginGPUCopyPass(commandBuffer);
        SDL.UploadToGPUTexture(copyPass,
            new SDLGPUTextureTransferInfo
            {
                TransferBuffer = transferBuffer,
                Offset = 0,
                PixelsPerRow = (uint)image.Width,
                RowsPerLayer = (uint)image.Height
            },
            new SDLGPUTextureRegion
            {
                Texture = texture,
                X = 0,
                Y = 0,
                W = (uint)image.Width,
                H = (uint)image.Height,
                D = 1
            },
            false);
        SDL.EndGPUCopyPass(copyPass);
        SDL.ReleaseGPUTransferBuffer(gpuDevice, transferBuffer);
    }

    private static SDLGPUTextureFormat SelectDepthFormat(SDLGPUDevice* gpuDevice)
    {
        SDLGPUTextureType textureType = SDLGPUTextureType.Texturetype2D;
        uint usage = (uint)SDLGPUTextureUsageFlags.DepthStencilTarget;

        if (SDL.GPUTextureSupportsFormat(gpuDevice, SDLGPUTextureFormat.D32Float, textureType, usage))
            return SDLGPUTextureFormat.D32Float;

        if (SDL.GPUTextureSupportsFormat(gpuDevice, SDLGPUTextureFormat.D24Unorm, textureType, usage))
            return SDLGPUTextureFormat.D24Unorm;

        throw new InvalidOperationException("No supported depth format was found for this GPU backend.");
    }

    private static Mesh3DVertex[] CreateGridVertices()
    {
        const float gridHalfSize = 1024f;
        Vector4 color = new(255f, 255f, 255f, 255f);
        Vector3 normalBarycentric = new(1f, 0f, 0f);

        // Two triangles making a large XZ plane at Y=0.
        return
        [
            new Mesh3DVertex(new Vector3(-gridHalfSize, 0f, -gridHalfSize), color, new Vector2(0f, 0f), normalBarycentric),
            new Mesh3DVertex(new Vector3( gridHalfSize, 0f, -gridHalfSize), color, new Vector2(1f, 0f), normalBarycentric),
            new Mesh3DVertex(new Vector3( gridHalfSize, 0f,  gridHalfSize), color, new Vector2(1f, 1f), normalBarycentric),
            new Mesh3DVertex(new Vector3(-gridHalfSize, 0f, -gridHalfSize), color, new Vector2(0f, 0f), normalBarycentric),
            new Mesh3DVertex(new Vector3( gridHalfSize, 0f,  gridHalfSize), color, new Vector2(1f, 1f), normalBarycentric),
            new Mesh3DVertex(new Vector3(-gridHalfSize, 0f,  gridHalfSize), color, new Vector2(0f, 1f), normalBarycentric)
        ];
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

        sdlRect = new SDLRect
        {
            X = x,
            Y = y,
            W = width,
            H = height
        };

        return width > 0 && height > 0;
    }

    private static void GetCameraBasisFromView(Matrix4x4 view, out Vector3 right, out Vector3 up)
    {
        if (!Matrix4x4.Invert(view, out Matrix4x4 inverseView))
        {
            right = Vector3.UnitX;
            up = Vector3.UnitY;
            return;
        }

        right = Vector3.Normalize(new Vector3(inverseView.M11, inverseView.M12, inverseView.M13));
        up = Vector3.Normalize(new Vector3(inverseView.M21, inverseView.M22, inverseView.M23));
    }

    private static Vector4 NormalizeColor(Vector4 color)
    {
        if (color.X > 1f || color.Y > 1f || color.Z > 1f || color.W > 1f)
            return color / 255f;

        return color;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct Mesh3DVertex
    {
        public readonly Vector3 Position;
        public readonly Vector4 Color;
        public readonly Vector2 UvCoordinate;
        public readonly Vector3 Barycentric;

        public Mesh3DVertex(Vector3 position, Vector4 color, Vector2 uvCoordinate, Vector3 barycentric)
        {
            Position = position;
            Color = NormalizeColor(color);
            UvCoordinate = uvCoordinate;
            Barycentric = barycentric;
        }

        private static Vector4 NormalizeColor(Vector4 color)
        {
            if (color.X > 1f || color.Y > 1f || color.Z > 1f || color.W > 1f)
                return color / 255f;

            return color;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MeshTransformUniform
    {
        public Matrix4x4 World;
        public Matrix4x4 ViewProjection;
        public Matrix4x4 WorldViewProjection;
        public Vector4 RenderParams;
        public Vector4 VertexDotColor;
        public Vector4 WireColor;
        public Vector4 LightDirection;
        public Vector4 LightColor;
        public Vector4 StyleParams;
        public Vector4 CameraRight;
        public Vector4 CameraUp;
    }
}
