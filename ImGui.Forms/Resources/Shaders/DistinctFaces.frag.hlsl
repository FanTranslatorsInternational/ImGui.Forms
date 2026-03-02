struct Input
{
    float4 Color : TEXCOORD0;
    float3 WorldPos : TEXCOORD1;
    float4 Position : SV_Position;
};

float4 main(Input input) : SV_Target0
{
    // Flat face normal reconstructed from derivatives.
    float3 dpdx = ddx_fine(input.WorldPos);
    float3 dpdy = ddy_fine(input.WorldPos);
    float3 faceNormal = normalize(cross(dpdx, dpdy));

    // Two-sided diffuse term keeps both winding directions visible.
    float3 lightDir = normalize(float3(0.35f, 0.80f, 0.45f));
    float ndotl = abs(dot(faceNormal, lightDir));

    // Strong orientation tint so neighboring faces are clearly distinct.
    float3 orientation = abs(faceNormal);
    float3 faceTint = 0.30f + 0.70f * orientation;

    // Quantized lighting emphasizes planar faces.
    float diffuseBands = floor(ndotl * 4.0f) / 3.0f;
    float lighting = 0.25f + 0.75f * saturate(diffuseBands);

    float3 shaded = input.Color.rgb * faceTint;

    return float4(abs(faceNormal), input.Color.a);
}
