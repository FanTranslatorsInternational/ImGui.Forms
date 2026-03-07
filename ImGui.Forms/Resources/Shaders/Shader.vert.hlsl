cbuffer UBO : register(b0, space1)
{
    float4x4 World : packoffset(c0);
    float4x4 WorldViewProjection : packoffset(c4);
    float4 RenderParams : packoffset(c8);
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
    float GridPass : TEXCOORD6;
    float4 Position : SV_Position;
};

Output main(Input input)
{
    Output output;

    float4 worldPos = mul(World, float4(input.Position, 1.0f));
    output.WorldPos = worldPos.xyz;
    output.Color = input.Color;
    output.Uv = input.Uv;
    output.Barycentric = input.Barycentric;
    output.WireframeEnabled = RenderParams.x;
    output.GridEnabled = RenderParams.y;
    output.GridPass = RenderParams.z;
    output.Position = mul(WorldViewProjection, float4(input.Position, 1.0f));

    return output;
}
