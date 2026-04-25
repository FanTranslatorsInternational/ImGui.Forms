struct Input
{
    float4 Color : TEXCOORD0;
    float2 Uv : TEXCOORD1;
    float3 WorldPos : TEXCOORD2;
    float3 Barycentric : TEXCOORD3;
    float WireframeEnabled : TEXCOORD4;
    float GridEnabled : TEXCOORD5;
    float RenderPass : TEXCOORD6;
    float4 WireColor : TEXCOORD7;
    float3 LightDirection : TEXCOORD8;
    float3 LightColor : TEXCOORD9;
    float WireThickness : TEXCOORD10;
    float LightIntensity : TEXCOORD11;
    float TextureEnabled : TEXCOORD12;
    float4 Position : SV_Position;
};

Texture2D FaceTexture : register(t0, space2);
SamplerState FaceSampler : register(s0, space2);

float4 main(Input input) : SV_Target0
{
    // Dedicated pass for independent vertex markers.
    if (input.RenderPass > 1.5f)
    {
        float2 centeredUv = (input.Uv * 2.0f) - 1.0f;
        float distanceToCenter = length(centeredUv);
        float edgeWidth = max(fwidth(distanceToCenter), 0.0001f);
        float alpha = 1.0f - smoothstep(1.0f - edgeWidth, 1.0f + edgeWidth, distanceToCenter);
        clip(alpha - 0.001f);
        return float4(input.Color.rgb, input.Color.a * alpha);
    }

    if (input.GridEnabled > 0.5f && input.RenderPass > 0.5f)
    {
        float2 gridUv = input.WorldPos.xz;

        float2 minorWidth = max(fwidth(gridUv), float2(0.0001f, 0.0001f));
        float2 minorCell = abs(frac(gridUv) - 0.5f) / minorWidth;
        float minorLine = 1.0f - saturate(min(minorCell.x, minorCell.y));

        float2 majorUv = gridUv / 5.0f;
        float2 majorWidth = max(fwidth(majorUv), float2(0.0001f, 0.0001f));
        float2 majorCell = abs(frac(majorUv) - 0.5f) / majorWidth;
        float majorLine = 1.0f - saturate(min(majorCell.x, majorCell.y));

        float axisX = 1.0f - saturate(abs(input.WorldPos.x) / max(fwidth(input.WorldPos.x), 0.0001f));
        float axisZ = 1.0f - saturate(abs(input.WorldPos.z) / max(fwidth(input.WorldPos.z), 0.0001f));

        float baseMask = saturate(minorLine * 0.30f + majorLine * 0.70f);
        float axisMask = saturate(max(axisX, axisZ));
        float minorAlpha = saturate(minorLine * 0.30f);
        float majorAlpha = saturate(majorLine * 0.60f);
        float lineMask = saturate(max(max(minorAlpha, majorAlpha), axisMask));

        float3 gridColor = lerp(float3(0.08f, 0.08f, 0.10f), float3(0.75f, 0.85f, 1.0f), baseMask);
        gridColor = lerp(gridColor, float3(1.0f, 0.45f, 0.35f), axisMask);
        float gridAlpha = lineMask;

        // Remove non-line fragments entirely so the grid is fully transparent
        // between lines and does not write depth there.
        clip(gridAlpha - 0.001f);

        return float4(gridColor, gridAlpha);
    }

    // Flat face normal reconstructed from derivatives.
    float3 dpdx = ddx_fine(input.WorldPos);
    float3 dpdy = ddy_fine(input.WorldPos);
    float3 faceNormal = normalize(cross(dpdx, dpdy));

    // Directional lighting with configurable color and direction.
    // Keep legacy intensity response when lightColor is white.
    // LightDirection points from origin toward the light marker.
    // For shading we need the incoming light vector at the surface.
    float3 lightDir = normalize(-input.LightDirection);
    float ndotl = saturate(dot(faceNormal, lightDir));
    float3 lightColor = max(input.LightColor, float3(0.0f, 0.0f, 0.0f));
    float lightingIntensity = (1.25f + 0.25f * ndotl) * max(0.0f, input.LightIntensity);
    float3 lighting = lightColor * lightingIntensity;

    float4 sampled = FaceTexture.Sample(FaceSampler, input.Uv);
    float3 albedo = sampled.rgb;
    if (input.TextureEnabled < 0.5f)
        albedo = float3(0.7f, 0.7f, 0.7f);
    float3 shaded = input.Color.rgb * albedo * lighting;
    
    // Shader wireframe overlay from barycentric coordinates.
    float wireThickness = max(0.01f, input.WireThickness);
    float3 wireColor = input.WireColor.rgb;
    float3 baryWidth = fwidth(input.Barycentric) * wireThickness;
    float3 baryBlend = smoothstep(0.0f, baryWidth, input.Barycentric);
    float edgeFactor = min(min(baryBlend.x, baryBlend.y), baryBlend.z);
    float wireMask = 1.0f - edgeFactor;
    float3 finalColor = lerp(shaded, wireColor, wireMask * saturate(input.WireframeEnabled));

    return float4(finalColor, input.Color.a * sampled.a);
}
