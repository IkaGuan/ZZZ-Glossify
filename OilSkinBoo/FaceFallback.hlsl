// Used only when a face draw has no normal target. The shared OilSkin face
// engine refuses that pass. This keeps the sheen inside GlossifyBase.
Texture2D<float4> OilMatcap : register(t125);
Texture1D<float4> IniParams : register(t120);

float4 main(float4 position : SV_Position) : SV_Target0
{
    float strength = saturate(IniParams.Load(int2(121, 0)).y);
    uint width, height;
    OilMatcap.GetDimensions(width, height);
    float3 sheen = float3(0.65, 0.65, 0.65);
    if (width > 0 && height > 0)
        sheen = OilMatcap.Load(int3(width / 2, height / 2, 0)).rgb;
    return float4(sheen * strength * 0.45, 0.0);
}
