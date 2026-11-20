// Parts panel. Click a row to turn that paint on or off.
Texture1D<float4> IniParams : register(t120);
Texture2D<float4> FontTex : register(t101);

struct VsOut {
    float4 pos : SV_Position0;
    float2 uv : TEXCOORD0;
};

#ifdef VERTEX_SHADER
void main(out VsOut output, uint vertex : SV_VertexID) {
    float4 place = IniParams[90];
    float2 offset = float2(place.x * 2.0 - 1.0, (1.0 - place.y) * 2.0 - 1.0);
    float2 far = float2(2.0 * place.z, 2.0 * (-place.w));
    output.pos.zw = float2(0.0, 1.0);
    if (vertex == 0) {
        output.pos.xy = float2(far.x + offset.x, far.y + offset.y);
        output.uv = float2(1.0, 1.0);
    } else if (vertex == 1) {
        output.pos.xy = float2(far.x + offset.x, offset.y);
        output.uv = float2(1.0, 0.0);
    } else if (vertex == 2) {
        output.pos.xy = float2(offset.x, far.y + offset.y);
        output.uv = float2(0.0, 1.0);
    } else {
        output.pos.xy = float2(offset.x, offset.y);
        output.uv = float2(0.0, 0.0);
    }
}
#endif

#ifdef PIXEL_SHADER
int Advance(int ch) {
    if (ch < 32 || ch > 126) return 10;
    return (int)(FontTex.Load(int3(ch - 32, 255, 0)).r * 255.0 + 0.5);
}

float GlyphAlpha(int ch, int lx, int ly) {
    if (ch < 32 || ch > 126) return 0.0;
    if (lx < 0 || ly < 0 || lx >= 32 || ly >= 39) return 0.0;
    int idx = ch - 32;
    int x = (idx % 16) * 32 + lx;
    int y = (idx / 16) * 39 + ly;
    return FontTex.Load(int3(x, y, 0)).a;
}

int CharAt(int i, int4 a, int4 b, int4 c, int4 d) {
    if (i < 4) return i == 0 ? a.x : i == 1 ? a.y : i == 2 ? a.z : a.w;
    if (i < 8) {
        int j = i - 4;
        return j == 0 ? b.x : j == 1 ? b.y : j == 2 ? b.z : b.w;
    }
    if (i < 12) {
        int j = i - 8;
        return j == 0 ? c.x : j == 1 ? c.y : j == 2 ? c.z : c.w;
    }
    int j = i - 12;
    return j == 0 ? d.x : j == 1 ? d.y : j == 2 ? d.z : d.w;
}

float TextAt(float2 pix, float2 origin, int count, int4 a, int4 b, int4 c, int4 d) {
    float2 rel = pix - origin;
    if (rel.x < 0.0 || rel.y < 0.0 || rel.y >= 39.0) return 0.0;
    float x = 0.0;
    [loop]
    for (int i = 0; i < 16; i++) {
        if (i >= count) return 0.0;
        int ch = CharAt(i, a, b, c, d);
        int adv = Advance(ch);
        if (rel.x < x + (float)adv) return GlyphAlpha(ch, (int)(rel.x - x), (int)rel.y);
        x += (float)adv;
    }
    return 0.0;
}

float StringWidth(int count, int4 a, int4 b, int4 c, int4 d) {
    float w = 0.0;
    [loop]
    for (int i = 0; i < 16; i++) {
        if (i >= count) break;
        w += (float)Advance(CharAt(i, a, b, c, d));
    }
    return w;
}

void Cover(inout float4 result, float cov, float3 color) {
    if (cov <= 0.004) return;
    float a = saturate(cov);
    result.rgb = lerp(result.rgb, color, a);
    result.a = max(result.a, a);
}

void DrawStr(float2 pix, float2 origin, int count, int4 a, int4 b, int4 c, int4 d, float3 color, inout float4 result) {
    Cover(result, TextAt(pix, origin, count, a, b, c, d), color);
}

void Row(float2 pix, float y0, float y1, float hot, float on, int count, int4 a, int4 b, int4 c, int4 d, inout float4 result) {
    float3 ink = float3(0.98, 0.98, 0.99);
    float3 onCol = float3(0.42, 0.95, 0.60);
    float3 offCol = float3(0.70, 0.73, 0.78);
    if (pix.y >= y0 && pix.y < y1) {
        result.rgb = hot > 0.5 ? float3(0.16, 0.20, 0.26) : float3(0.10, 0.11, 0.14);
    }
    int4 z = int4(0, 0, 0, 0);
    DrawStr(pix, float2(16, y0 + 4), count, a, b, c, d, ink, result);
    int4 word = on > 0.5 ? int4(79, 78, 0, 0) : int4(79, 70, 70, 0);
    int n = on > 0.5 ? 2 : 3;
    float x = 404.0 - StringWidth(n, word, z, z, z);
    DrawStr(pix, float2(x, y0 + 4), n, word, z, z, z, on > 0.5 ? onCol : offCol, result);
}

void main(VsOut input, out float4 result : SV_Target0) {
    float2 pix = floor(input.uv * float2(420.0, 400.0));
    float4 rows = IniParams[91];
    float4 more = IniParams[92];
    float hot = more.w;
    result = float4(0.06, 0.07, 0.09, 0.94);
    if (pix.y < 48.0) result.rgb = float3(0.12, 0.14, 0.18);
    if (input.uv.x < 0.006 || input.uv.x > 0.994 || input.uv.y < 0.004 || input.uv.y > 0.996)
        result.rgb = float3(0.55, 0.68, 0.82);

    int4 z = int4(0, 0, 0, 0);
    DrawStr(pix, float2(16, 6), 5, int4(80, 65, 82, 84), int4(83, 0, 0, 0), z, z, float3(0.98, 0.98, 0.99), result);

    float closeHot = hot > 7.5 && hot < 8.5 ? 1.0 : 0.0;
    if (pix.x >= 372.0 && pix.x < 412.0 && pix.y >= 8.0 && pix.y < 44.0)
        result.rgb = lerp(float3(0.42, 0.16, 0.16), float3(0.78, 0.24, 0.22), closeHot);
    float d1 = abs((pix.x - 380.0) - (pix.y - 14.0));
    float d2 = abs((pix.x - 380.0) + (pix.y - 38.0));
    if ((d1 < 2.2 || d2 < 2.2) && pix.x > 376.0 && pix.x < 404.0 && pix.y > 12.0 && pix.y < 40.0)
        result.rgb = float3(0.98, 0.98, 0.99);

    Row(pix, 52, 96, hot > 0.5 && hot < 1.5, rows.x, 5, int4(85, 80, 80, 69), int4(82, 0, 0, 0), z, z, result);
    Row(pix, 100, 144, hot > 1.5 && hot < 2.5, rows.y, 5, int4(76, 79, 87, 69), int4(82, 0, 0, 0), z, z, result);
    Row(pix, 148, 192, hot > 2.5 && hot < 3.5, rows.z, 4, int4(65, 82, 77, 83), z, z, z, result);
    Row(pix, 208, 252, hot > 3.5 && hot < 4.5, rows.w, 3, int4(78, 80, 67, 0), z, z, z, result);
    Row(pix, 256, 300, hot > 4.5 && hot < 5.5, more.x, 5, int4(69, 78, 69, 77), int4(89, 0, 0, 0), z, z, result);
    Row(pix, 304, 348, hot > 5.5 && hot < 6.5, more.y, 7, int4(66, 65, 78, 71), int4(66, 79, 79, 0), z, z, result);
    Row(pix, 352, 396, hot > 6.5 && hot < 7.5, more.z, 8, int4(76, 69, 71, 32), int4(68, 82, 65, 87), z, z, result);
}
#endif
