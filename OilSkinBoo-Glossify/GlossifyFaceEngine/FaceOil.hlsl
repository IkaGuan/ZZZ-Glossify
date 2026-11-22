// Experimental face-only sheen. Reuses the native vertex shader and geometry.
// Only colour target 0 is bound for this pass; alpha is preserved by blend state.
Texture2D<float4> FaceNormals : register(t119);
Texture1D<float4> IniParams : register(t120);
Texture2D<float4> OilMatcap : register(t125);

float3 ReadMatcap(float2 uv)
{
    uint width, height;
    OilMatcap.GetDimensions(width, height);
    if (width == 0 || height == 0)
        return 0.0;

    float2 p = saturate(uv) * float2(width - 1, height - 1);
    int2 p0 = int2(p);
    int2 p1 = min(p0 + 1, int2(width - 1, height - 1));
    float2 f = frac(p);
    float3 a = OilMatcap.Load(int3(p0, 0)).rgb;
    float3 b = OilMatcap.Load(int3(p1.x, p0.y, 0)).rgb;
    float3 c = OilMatcap.Load(int3(p0.x, p1.y, 0)).rgb;
    float3 d = OilMatcap.Load(int3(p1, 0)).rgb;
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

float4 main(float4 position : SV_Position) : SV_Target0
{
    uint width, height;
    FaceNormals.GetDimensions(width, height);
    if (width == 0 || height == 0)
        return 0.0;

    int2 pixel = clamp(int2(position.xy), int2(0, 0), int2(width - 1, height - 1));
    float3 encodedNormal = FaceNormals.Load(int3(pixel, 0)).rgb;
    float3 normal = encodedNormal * 2.0 - 1.0;
    float lengthSquared = dot(normal, normal);
    normal = lengthSquared > 0.01 ? normal * rsqrt(lengthSquared) : float3(0, 0, 1);

    // Same world-normal matcap orientation as the currently working body port.
    float2 uv = float2(normal.x * 0.5 + 0.5, 0.5 - normal.y * 0.5);
    float strength = saturate(IniParams.Load(int2(121, 0)).y);
    float3 sheen = ReadMatcap(uv) * strength * 0.35;
    return float4(sheen, 0.0);
}
