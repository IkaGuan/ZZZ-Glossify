// Glossify Toggles 1.0. Segoe UI Semibold atlas in Font.dds, one panel quad.
Texture1D<float4> IniParams : register(t120);
Texture2D<float4> FontTex : register(t101);

struct VsOut {
    float4 pos : SV_Position0;
    float2 uv : TEXCOORD0;
};

#ifdef VERTEX_SHADER
void main(out VsOut output, uint vertex : SV_VertexID) {
    float4 place = IniParams[80];
    float2 offset;
    offset.x = place.x * 2.0 - 1.0;
    offset.y = (1.0 - place.y) * 2.0 - 1.0;
    float2 far;
    far.x = 2.0 * place.z;
    far.y = 2.0 * (-place.w);
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
        if (rel.x < x + (float)adv) {
            return GlyphAlpha(ch, (int)(rel.x - x), (int)rel.y);
        }
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

float CharCov(float2 pix, float2 origin, int ch) {
    float2 rel = pix - origin;
    int adv = Advance(ch);
    if (rel.x < 0.0 || rel.y < 0.0 || rel.x >= (float)adv || rel.y >= 39.0) return 0.0;
    return GlyphAlpha(ch, (int)rel.x, (int)rel.y);
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

void main(VsOut input, out float4 result : SV_Target0) {
    float2 pix = floor(input.uv * float2(500.0, 540.0));
    float3 paper = float3(0.06, 0.07, 0.09);
    float3 ink = float3(0.98, 0.98, 0.99);
    float3 key = float3(0.82, 0.90, 0.98);
    float3 on = float3(0.42, 0.95, 0.60);
    float3 off = float3(0.70, 0.73, 0.78);
    result = float4(paper, 0.94);
    if (pix.y < 56.0) result.rgb = float3(0.12, 0.14, 0.18);
    if (input.uv.x < 0.004 || input.uv.x > 0.996 || input.uv.y < 0.003 || input.uv.y > 0.997)
        result.rgb = float3(0.55, 0.68, 0.82);

    float4 state = IniParams[81];
    float4 meter = IniParams[82];
    float hot = (meter.z > 0.5 && meter.z < 1.5) ? 1.0 : 0.0;
    float saveHot = meter.z > 1.5 ? 1.0 : 0.0;
    if (pix.x >= 448.0 && pix.x < 488.0 && pix.y >= 8.0 && pix.y < 48.0)
        result.rgb = lerp(float3(0.42, 0.16, 0.16), float3(0.78, 0.24, 0.22), hot);
    float d1 = abs((pix.x - 456.0) - (pix.y - 16.0));
    float d2 = abs((pix.x - 456.0) + (pix.y - 40.0));
    if ((d1 < 2.4 || d2 < 2.4) && pix.x > 454.0 && pix.x < 482.0 && pix.y > 14.0 && pix.y < 42.0)
        result.rgb = float3(0.98, 0.98, 0.99);

    int4 z = int4(0, 0, 0, 0);
    DrawStr(pix, float2(16, 10), 16, int4(71, 76, 79, 83), int4(83, 73, 70, 89), int4(32, 84, 79, 71), int4(71, 76, 69, 83), ink, result);

    float patched = meter.w > 0.5 ? 1.0 : 0.0;
    float saved = meter.w > 1.5 ? 1.0 : 0.0;
    float yFace = 68.0;
    float yBody = 114.0;
    float yStatus = 160.0;
    float yModFace = 206.0;
    float yModBody = 252.0;
    float yStyle = patched > 0.5 ? 298.0 : 206.0;
    float yLevel = yStyle + 46.0;
    float yDown = yStyle + 92.0;
    float yPanel = yStyle + 138.0;
    float ySave = 486.0;

    DrawStr(pix, float2(16, yFace), 9, int4(66, 65, 83, 69), int4(32, 70, 65, 67), int4(69, 0, 0, 0), z, ink, result);
    DrawStr(pix, float2(196, yFace), 11, int4(67, 84, 82, 76), int4(32, 65, 76, 84), int4(32, 70, 56, 0), z, key, result);
    DrawStr(pix, float2(16, yBody), 9, int4(66, 65, 83, 69), int4(32, 66, 79, 68), int4(89, 0, 0, 0), z, ink, result);
    DrawStr(pix, float2(196, yBody), 11, int4(67, 84, 82, 76), int4(32, 65, 76, 84), int4(32, 70, 57, 0), z, key, result);
    DrawStr(pix, float2(16, yStatus), 9, int4(79, 78, 32, 83), int4(67, 82, 69, 69), int4(78, 0, 0, 0), z, ink, result);
    if (patched > 0.5) {
        DrawStr(pix, float2(16, yModFace), 8, int4(77, 79, 68, 32), int4(70, 65, 67, 69), z, z, ink, result);
        DrawStr(pix, float2(196, yModFace), 9, int4(65, 76, 84, 32), int4(78, 85, 77, 32), int4(42, 0, 0, 0), z, key, result);
        DrawStr(pix, float2(16, yModBody), 8, int4(77, 79, 68, 32), int4(66, 79, 68, 89), z, z, ink, result);
        DrawStr(pix, float2(196, yModBody), 5, int4(78, 85, 77, 32), int4(42, 0, 0, 0), z, z, key, result);
    }
    DrawStr(pix, float2(16, yStyle), 9, int4(79, 73, 76, 32), int4(83, 84, 89, 76), int4(69, 0, 0, 0), z, ink, result);
    DrawStr(pix, float2(196, yStyle), 11, int4(83, 72, 73, 70), int4(84, 32, 78, 85), int4(77, 32, 42, 0), z, key, result);
    DrawStr(pix, float2(16, yLevel), 9, int4(79, 73, 76, 32), int4(76, 69, 86, 69), int4(76, 0, 0, 0), z, ink, result);
    DrawStr(pix, float2(196, yLevel), 8, int4(85, 80, 32, 78), int4(85, 77, 32, 43), z, z, key, result);
    DrawStr(pix, float2(196, yDown), 10, int4(68, 79, 87, 78), int4(32, 78, 85, 77), int4(32, 45, 0, 0), z, key, result);
    DrawStr(pix, float2(16, yPanel), 5, int4(80, 65, 78, 69), int4(76, 0, 0, 0), z, z, ink, result);
    DrawStr(pix, float2(196, yPanel), 11, int4(67, 84, 82, 76), int4(32, 65, 76, 84), int4(32, 70, 55, 0), z, key, result);
    if (patched > 0.5) {
        float3 savePaper = saveHot > 0.5 ? float3(0.20, 0.34, 0.24) : float3(0.14, 0.22, 0.18);
        if (pix.x >= 12.0 && pix.x < 230.0 && pix.y >= 482.0 && pix.y < 526.0)
            result.rgb = savePaper;
        DrawStr(pix, float2(24, ySave), 9, int4(83, 65, 86, 69), int4(32, 76, 79, 79), int4(75, 0, 0, 0), z, ink, result);
    }

    float right = 484.0;
    float face = state.x > 0.5 ? 1.0 : 0.0;
    float body = state.y > 0.5 ? 1.0 : 0.0;
    float modFace = state.z > 0.5 ? 1.0 : 0.0;
    float modBody = state.w > 0.5 ? 1.0 : 0.0;
    int4 onWord = int4(79, 78, 0, 0);
    int4 offWord = int4(79, 70, 70, 0);
    float onX = right - StringWidth(2, onWord, z, z, z);
    float offX = right - StringWidth(3, offWord, z, z, z);
    if (face > 0.5) DrawStr(pix, float2(onX, yFace), 2, onWord, z, z, z, on, result);
    else DrawStr(pix, float2(offX, yFace), 3, offWord, z, z, z, off, result);
    if (body > 0.5) DrawStr(pix, float2(onX, yBody), 2, onWord, z, z, z, on, result);
    else DrawStr(pix, float2(offX, yBody), 3, offWord, z, z, z, off, result);
    int4 patchedWord = int4(80, 65, 84, 67);
    int4 patchedRest = int4(72, 69, 68, 0);
    int4 stockWord = int4(83, 84, 79, 67);
    int4 stockRest = int4(75, 0, 0, 0);
    if (patched > 0.5) {
        float patchedX = right - StringWidth(7, patchedWord, patchedRest, z, z);
        DrawStr(pix, float2(patchedX, yStatus), 7, patchedWord, patchedRest, z, z, on, result);
        if (modFace > 0.5) DrawStr(pix, float2(onX, yModFace), 2, onWord, z, z, z, on, result);
        else DrawStr(pix, float2(offX, yModFace), 3, offWord, z, z, z, off, result);
        if (modBody > 0.5) DrawStr(pix, float2(onX, yModBody), 2, onWord, z, z, z, on, result);
        else DrawStr(pix, float2(offX, yModBody), 3, offWord, z, z, z, off, result);
        int4 savedWord = int4(83, 65, 86, 69);
        int4 savedRest = int4(68, 0, 0, 0);
        if (saved > 0.5) {
            float savedX = right - StringWidth(5, savedWord, savedRest, z, z);
            DrawStr(pix, float2(savedX, ySave), 5, savedWord, savedRest, z, z, on, result);
        } else {
            float followX = right - StringWidth(5, stockWord, stockRest, z, z);
            DrawStr(pix, float2(followX, ySave), 5, stockWord, stockRest, z, z, off, result);
        }
    } else {
        float stockX = right - StringWidth(5, stockWord, stockRest, z, z);
        DrawStr(pix, float2(stockX, yStatus), 5, stockWord, stockRest, z, z, off, result);
    }

    int style = (int)(meter.x + 0.5);
    if (style < 0) style = 0;
    if (style > 9) style = 9;
    int styleCh = 48 + style;
    float3 styleColor = style == 0 ? off : ink;
    Cover(result, CharCov(pix, float2(right - (float)Advance(styleCh), yStyle), styleCh), styleColor);

    int level = (int)(saturate(meter.y) * 10.0 + 0.5);
    if (level < 0) level = 0;
    if (level > 10) level = 10;
    if (level >= 10) {
        int w1 = Advance(49);
        int w0 = Advance(48);
        float x = right - (float)(w1 + w0);
        Cover(result, CharCov(pix, float2(x, yLevel), 49), ink);
        Cover(result, CharCov(pix, float2(x + (float)w1, yLevel), 48), ink);
    } else {
        int levelCh = 48 + level;
        Cover(result, CharCov(pix, float2(right - (float)Advance(levelCh), yLevel), levelCh), ink);
    }
}
#endif
