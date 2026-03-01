using Hexa.NET.SDL3;
using ImGui.Forms.Models;
using System;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ImGui.Forms.Support;

internal unsafe class SdlGpuMeshRenderer3D : IDisposable
{
    private readonly Mesh3DVertex[] _emptyVertices = [];

    private Mesh3DVertex[] _vertices;
    private bool _vertexDataDirty = true;

    private SDLGPUBuffer* _vertexBuffer;
    private uint _vertexBufferSize;

    private SDLGPUGraphicsPipeline* _pipeline;
    private SDLGPUShader* _vertexShader;
    private SDLGPUShader* _fragmentShader;

    public SdlGpuMeshRenderer3D(Mesh3D? mesh = null)
    {
        _vertices = _emptyVertices;
        SetMesh(mesh);
    }

    public void SetMesh(Mesh3D? mesh)
    {
        if (mesh == null || mesh.Faces.Count == 0)
        {
            _vertices = _emptyVertices;
            _vertexDataDirty = true;
            return;
        }

        _vertices = new Mesh3DVertex[mesh.Faces.Count * 3];

        int index = 0;
        foreach (var face in mesh.Faces)
        {
            _vertices[index++] = new Mesh3DVertex(face.A.Position, face.A.Color);
            _vertices[index++] = new Mesh3DVertex(face.B.Position, face.B.Color);
            _vertices[index++] = new Mesh3DVertex(face.C.Position, face.C.Color);
        }

        _vertexDataDirty = true;
    }

    public void Prepare(SDLGPUDevice* gpuDevice, SDLGPUCommandBuffer* commandBuffer)
    {
        if (_vertices.Length == 0)
            return;

        CreatePipelineIfRequired(gpuDevice);
        EnsureVertexBuffer(gpuDevice);

        if (!_vertexDataDirty)
            return;

        uint bytesToUpload = (uint)(_vertices.Length * Marshal.SizeOf<Mesh3DVertex>());
        SDLGPUTransferBuffer* transferBuffer = SDL.CreateGPUTransferBuffer(gpuDevice, new SDLGPUTransferBufferCreateInfo
        {
            Usage = SDLGPUTransferBufferUsage.Upload,
            Size = bytesToUpload
        });

        void* mapped = SDL.MapGPUTransferBuffer(gpuDevice, transferBuffer, true);
        fixed (Mesh3DVertex* verticesPtr = _vertices)
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
                Buffer = _vertexBuffer,
                Offset = 0,
                Size = bytesToUpload
            },
            false);
        SDL.EndGPUCopyPass(copyPass);
        SDL.ReleaseGPUTransferBuffer(gpuDevice, transferBuffer);

        _vertexDataDirty = false;
    }

    public void Render(SDLGPUDevice* gpuDevice, SDLGPUCommandBuffer* commandBuffer, SDLGPURenderPass* renderPass, Rectangle contentRect, Matrix4x4 transformation)
    {
        if (_vertices.Length == 0 || _pipeline == null || _vertexBuffer == null)
            return;

        int viewportWidth = Math.Max(1, (int)contentRect.Width);
        int viewportHeight = Math.Max(1, (int)contentRect.Height);

        SDLGPUViewport viewport = new()
        {
            X = contentRect.X,
            Y = contentRect.Y,
            W = viewportWidth,
            H = viewportHeight,
            MinDepth = 0f,
            MaxDepth = 1f
        };

        SDL.SetGPUViewport(renderPass, viewport);
        SDL.SetGPUScissor(renderPass, new SDLRect
        {
            X = (int)contentRect.X,
            Y = (int)contentRect.Y,
            W = viewportWidth,
            H = viewportHeight
        });

        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 3f, viewportWidth / (float)viewportHeight, 0.1f, 100f);
        Matrix4x4 view = Matrix4x4.CreateLookAt(new Vector3(0f, 0f, 3f), Vector3.Zero, Vector3.UnitY);
        Matrix4x4 transform = transformation * view * projection;

        SDL.BindGPUGraphicsPipeline(renderPass, _pipeline);

        SDLGPUBufferBinding vertexBinding = new()
        {
            Buffer = _vertexBuffer,
            Offset = 0
        };

        SDL.BindGPUVertexBuffers(renderPass, 0, &vertexBinding, 1);
        SDL.PushGPUVertexUniformData(commandBuffer, 0, &transform, (uint)sizeof(Matrix4x4));
        SDL.DrawGPUPrimitives(renderPass, (uint)_vertices.Length, 1, 0, 0);
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

        if (_vertexBuffer != null)
        {
            SDL.ReleaseGPUBuffer(gpuDevice, _vertexBuffer);
            _vertexBuffer = null;
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

    private void CreatePipelineIfRequired(SDLGPUDevice* gpuDevice)
    {
        if (_pipeline != null)
            return;

        (byte[] vertexCode, byte[] fragmentCode, SDLGPUShaderFormat format) = LoadShadersForCurrentBackend(gpuDevice);
        _vertexShader = CreateShader(gpuDevice, vertexCode, format, SDLGPUShaderStage.Vertex, 1);
        _fragmentShader = CreateShader(gpuDevice, fragmentCode, format, SDLGPUShaderStage.Fragment, 0);

        SDLGPUVertexBufferDescription vertexBufferDescription = new()
        {
            Slot = 0,
            InputRate = SDLGPUVertexInputRate.Vertex,
            InstanceStepRate = 0,
            Pitch = (uint)Marshal.SizeOf<Mesh3DVertex>()
        };

        SDLGPUVertexAttribute* attributes = stackalloc SDLGPUVertexAttribute[2];
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

        SDLGPUColorTargetDescription colorTargetDescription = new()
        {
            Format = Application.Instance.SwapchainFormat
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
                NumVertexAttributes = 2,
                VertexAttributes = attributes
            },
            RasterizerState = new SDLGPURasterizerState
            {
                CullMode = SDLGPUCullMode.Back
            },
            MultisampleState = new SDLGPUMultisampleState
            {
                SampleCount = SDLGPUSampleCount.Samplecount1
            },
            TargetInfo = new SDLGPUGraphicsPipelineTargetInfo
            {
                NumColorTargets = 1,
                ColorTargetDescriptions = &colorTargetDescription
            }
        });
    }

    private static SDLGPUShader* CreateShader(SDLGPUDevice* gpuDevice, byte[] code, SDLGPUShaderFormat format, SDLGPUShaderStage stage, uint uniformBufferCount)
    {
        fixed (byte* shaderCode = code)
        {
            return SDL.CreateGPUShader(gpuDevice, new SDLGPUShaderCreateInfo
            {
                Code = shaderCode,
                CodeSize = (nuint)code.Length,
                Format = (uint)format,
                Stage = stage,
                NumSamplers = 0,
                NumStorageTextures = 0,
                NumStorageBuffers = 0,
                NumUniformBuffers = uniformBufferCount
            });
        }
    }

    private static (byte[] vertexShader, byte[] fragmentShader, SDLGPUShaderFormat format) LoadShadersForCurrentBackend(SDLGPUDevice* gpuDevice)
    {
        SDLGPUShaderFormat format = (SDLGPUShaderFormat)SDL.GetGPUShaderFormats(gpuDevice);

        if ((format & SDLGPUShaderFormat.Dxil) != 0)
        {
            return (
                ReadEmbeddedFile("ImGui.Forms.Resources.Shaders.DXIL.PositionColorTransform.vert.dxil"),
                ReadEmbeddedFile("ImGui.Forms.Resources.Shaders.DXIL.SolidColor.frag.dxil"),
                SDLGPUShaderFormat.Dxil);
        }

        if ((format & SDLGPUShaderFormat.Spirv) != 0)
        {
            return (
                ReadEmbeddedFile("ImGui.Forms.Resources.Shaders.SPIRV.PositionColorTransform.vert.spv"),
                ReadEmbeddedFile("ImGui.Forms.Resources.Shaders.SPIRV.SolidColor.frag.spv"),
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

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct Mesh3DVertex(Vector3 position, Vector4 color)
    {
        public readonly Vector3 Position = position;
        public readonly Vector4 Color = color;
    }
}
