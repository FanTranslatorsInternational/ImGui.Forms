cbuffer UBO : register(b0, space1)
{
    float4x4 World : packoffset(c0);
    float4x4 WorldViewProjection : packoffset(c4);
};

struct Input
{
    float3 Position : TEXCOORD0;
    float4 Color : TEXCOORD1;
};

struct Output
{
    float4 Color : TEXCOORD0;
    float3 WorldPos : TEXCOORD1;
    float4 Position : SV_Position;
};

Output main(Input input)
{
    Output output;

    float4 worldPos = mul(World, float4(input.Position, 1.0f));
    output.WorldPos = worldPos.xyz;
    output.Color = input.Color;
    output.Position = mul(WorldViewProjection, float4(input.Position, 1.0f));

    return output;
}
