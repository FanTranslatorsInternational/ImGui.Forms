cbuffer UBO : register(b0, space1)
{
    float4x4 World : packoffset(c0);
    float4x4 ViewProjection : packoffset(c4);
    float4x4 WorldViewProjection : packoffset(c8);
    float4 RenderParams : packoffset(c12);
    float4 VertexDotColor : packoffset(c13);
    float4 CameraRight : packoffset(c14);
    float4 CameraUp : packoffset(c15);
};

struct Input
{
    float3 Position : TEXCOORD0;
    float4 Color : TEXCOORD1;
    float2 Uv : TEXCOORD2;
    float3 Barycentric : TEXCOORD3;
};

struct Output
{
    float4 Color : TEXCOORD0;
    float2 Uv : TEXCOORD1;
    float3 WorldPos : TEXCOORD2;
    float3 Barycentric : TEXCOORD3;
    float WireframeEnabled : TEXCOORD4;
    float GridEnabled : TEXCOORD5;
    float RenderPass : TEXCOORD6;
    float4 DotColor : TEXCOORD7;
    float4 Position : SV_Position;
};

Output main(Input input)
{
    Output output;

    float4 worldPos;
    if (RenderParams.z > 1.5f)
    {
        float halfSize = max(0.0001f, RenderParams.w * 0.0025f);
        float2 cornerOffset = input.Barycentric.xy;
        float3 billboardOffset = (CameraRight.xyz * cornerOffset.x + CameraUp.xyz * cornerOffset.y) * halfSize;
        worldPos = mul(World, float4(input.Position, 1.0f));
        worldPos.xyz += billboardOffset;
        output.Position = mul(ViewProjection, worldPos);
    }
    else
    {
        worldPos = mul(World, float4(input.Position, 1.0f));
        output.Position = mul(WorldViewProjection, float4(input.Position, 1.0f));
    }

    output.WorldPos = worldPos.xyz;
    output.Color = input.Color;
    output.Uv = input.Uv;
    output.Barycentric = input.Barycentric;
    output.WireframeEnabled = RenderParams.x;
    output.GridEnabled = RenderParams.y;
    output.RenderPass = RenderParams.z;
    output.DotColor = VertexDotColor;

    return output;
}
