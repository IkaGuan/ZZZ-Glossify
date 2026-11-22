// Glossify Toggles 1.0. Segoe UI Semibold atlas in Font.dds, one panel quad.
Texture1D<float4> IniParams : register(t120);
Texture2D<float4> LogoTex : register(t100);
Texture2D<float4> FontTex : register(t101);

struct VsOut {
    float4 pos : SV_Position0;
    float2 uv : TEXCOORD0;
    nointerpolation float4 styleKey : TEXCOORD1;
    nointerpolation float4 upKey : TEXCOORD2;
    nointerpolation float4 downKey : TEXCOORD3;
    nointerpolation float4 menuKey : TEXCOORD4;
    nointerpolation float4 faceKey : TEXCOORD5;
    nointerpolation float4 bodyKey : TEXCOORD6;
    nointerpolation float4 spanA : TEXCOORD7;
    nointerpolation float4 spanB : TEXCOORD8;
};

#ifdef VERTEX_SHADER
Buffer<uint> ToggleText : register(t102);
Buffer<uint> BaseText : register(t103);

int Raw(Buffer<uint> b, int i) {
    return (int)(b[i] & 255u);
}

int Up(int c) {
    if (c >= 97 && c <= 122) return c - 32;
    return c;
}

int Sid(Buffer<uint> b, int i) {
    if (Raw(b, i) != 91 || Raw(b, i + 1) != 75 || Raw(b, i + 2) != 101 || Raw(b, i + 3) != 121) return -1;
    int c4 = Raw(b, i + 4);
    int c6 = Raw(b, i + 6);
    if (c4 == 83 && c6 == 121) return 0;
    if (c4 == 83 && c6 == 114) return 1;
    if (c4 == 87) return 2;
    if (c4 == 77) return 3;
    if (c4 == 70) return 4;
    if (c4 == 66) return 5;
    return -1;
}

int AtW(int w0, int w1, int w2, int w3, int w4, int w5, int w6, int w7, int i) {
    int w = w0;
    int n = i;
    if (n >= 28) { w = w7; n -= 28; }
    else if (n >= 24) { w = w6; n -= 24; }
    else if (n >= 20) { w = w5; n -= 20; }
    else if (n >= 16) { w = w4; n -= 16; }
    else if (n >= 12) { w = w3; n -= 12; }
    else if (n >= 8) { w = w2; n -= 8; }
    else if (n >= 4) { w = w1; n -= 4; }
    return (w >> (n * 8)) & 255;
}

void Push(inout int n, inout int p0, inout int p1, inout int p2, inout int p3, int ch) {
    if (n >= 12 || ch <= 0) return;
    int r = n;
    int slot = 0;
    if (r >= 9) { slot = 3; r -= 9; }
    else if (r >= 6) { slot = 2; r -= 6; }
    else if (r >= 3) { slot = 1; r -= 3; }
    int sh = 0;
    if (r == 1) sh = 8;
    else if (r == 2) sh = 16;
    if (slot == 0) p0 += ch << sh;
    else if (slot == 1) p1 += ch << sh;
    else if (slot == 2) p2 += ch << sh;
    else p3 += ch << sh;
    n++;
}

float4 Label(int w0, int w1, int w2, int w3, int w4, int w5, int w6, int w7, int len) {
    int n = 0;
    int p0 = 0, p1 = 0, p2 = 0, p3 = 0;
    int i = 0;
    bool word = true;
    bool skip = false;
    bool gap = false;
    [loop]
    for (int step = 0; step < 32; step++) {
        if (i >= len || n >= 12) break;
        int ch = AtW(w0, w1, w2, w3, w4, w5, w6, w7, i);
        if (ch == 32 || ch == 9 || ch == 43 || ch == 10 || ch == 13) {
            if (gap) Push(n, p0, p1, p2, p3, 32);
            gap = false;
            word = true;
            skip = false;
            i++;
            continue;
        }
        if (word) {
            int c1 = Up(AtW(w0, w1, w2, w3, w4, w5, w6, w7, i + 1));
            int c2 = Up(AtW(w0, w1, w2, w3, w4, w5, w6, w7, i + 2));
            int u0 = Up(ch);
            if (u0 == 78 && c1 == 79 && c2 == 95) skip = true;
            if (u0 == 86 && c1 == 75 && c2 == 95) { i += 3; continue; }
        }
        if (skip) { i++; continue; }
        int u = Up(ch);
        int outc = ch;
        if (ch == 95) {
            outc = 32;
            word = true;
        } else if (u >= 65 && u <= 90) {
            outc = word ? u : u + 32;
            word = false;
        } else {
            word = false;
        }
        Push(n, p0, p1, p2, p3, outc);
        gap = outc != 32;
        i++;
    }
    return float4((float)p0, (float)p1, (float)p2, (float)p3);
}
void Take(int which, int w0, int w1, int w2, int w3, int w4, int w5, int w6, int w7, int n, inout int a0, inout int a1, inout int a2, inout int a3, inout int a4, inout int a5, inout int a6, inout int a7, inout int an, inout int b0, inout int b1, inout int b2, inout int b3, inout int b4, inout int b5, inout int b6, inout int b7, inout int bn, inout int c0, inout int c1, inout int c2, inout int c3, inout int c4, inout int c5, inout int c6, inout int c7, inout int cn, inout int d0, inout int d1, inout int d2, inout int d3, inout int d4, inout int d5, inout int d6, inout int d7, inout int dn) {
    if (which == 0) { a0 = w0; a1 = w1; a2 = w2; a3 = w3; a4 = w4; a5 = w5; a6 = w6; a7 = w7; an = n; }
    else if (which == 1) { b0 = w0; b1 = w1; b2 = w2; b3 = w3; b4 = w4; b5 = w5; b6 = w6; b7 = w7; bn = n; }
    else if (which == 2) { c0 = w0; c1 = w1; c2 = w2; c3 = w3; c4 = w4; c5 = w5; c6 = w6; c7 = w7; cn = n; }
    else if (which == 3) { d0 = w0; d1 = w1; d2 = w2; d3 = w3; d4 = w4; d5 = w5; d6 = w6; d7 = w7; dn = n; }
}

void Scan(Buffer<uint> b, int limit, int baseId, out float4 r0, out float4 r1, out float4 r2, out float4 r3) {
    int a0 = 0, a1 = 0, a2 = 0, a3 = 0, a4 = 0, a5 = 0, a6 = 0, a7 = 0, an = 0;
    int b0 = 0, b1 = 0, b2 = 0, b3 = 0, b4 = 0, b5 = 0, b6 = 0, b7 = 0, bn = 0;
    int c0 = 0, c1 = 0, c2 = 0, c3 = 0, c4 = 0, c5 = 0, c6 = 0, c7 = 0, cn = 0;
    int d0 = 0, d1 = 0, d2 = 0, d3 = 0, d4 = 0, d5 = 0, d6 = 0, d7 = 0, dn = 0;
    int state = 0;
    int which = -1;
    int n = 0;
    int w0 = 0, w1 = 0, w2 = 0, w3 = 0, w4 = 0, w5 = 0, w6 = 0, w7 = 0;
    [loop]
    for (int i = 0; i < limit; i++) {
        int ch = Raw(b, i);
        bool nl = i == 0 || Raw(b, i - 1) == 10;
        if (state == 0) {
            if (nl) {
                int id = Sid(b, i);
                if (id >= baseId && id < baseId + 4) { which = id - baseId; state = 1; }
            }
        } else if (state == 1) {
            if (ch == 61) {
                state = 2;
                n = 0;
                w0 = 0; w1 = 0; w2 = 0; w3 = 0; w4 = 0; w5 = 0; w6 = 0; w7 = 0;
            } else if (nl && ch == 91) {
                int id = Sid(b, i);
                if (id >= baseId && id < baseId + 4) which = id - baseId;
                else { state = 0; which = -1; }
            }
        } else if (ch == 10 || ch == 13 || ch == 0) {
            Take(which, w0, w1, w2, w3, w4, w5, w6, w7, n, a0, a1, a2, a3, a4, a5, a6, a7, an, b0, b1, b2, b3, b4, b5, b6, b7, bn, c0, c1, c2, c3, c4, c5, c6, c7, cn, d0, d1, d2, d3, d4, d5, d6, d7, dn);
            state = 0;
            which = -1;
        } else if (state == 2 && (ch == 32 || ch == 9)) {
        } else {
            state = 3;
            if (n < 32) {
                int sh = (n & 3) * 8;
                if (n < 4) w0 += ch << sh;
                else if (n < 8) w1 += ch << sh;
                else if (n < 12) w2 += ch << sh;
                else if (n < 16) w3 += ch << sh;
                else if (n < 20) w4 += ch << sh;
                else if (n < 24) w5 += ch << sh;
                else if (n < 28) w6 += ch << sh;
                else w7 += ch << sh;
                n++;
            }
        }
    }
    if (state == 3) Take(which, w0, w1, w2, w3, w4, w5, w6, w7, n, a0, a1, a2, a3, a4, a5, a6, a7, an, b0, b1, b2, b3, b4, b5, b6, b7, bn, c0, c1, c2, c3, c4, c5, c6, c7, cn, d0, d1, d2, d3, d4, d5, d6, d7, dn);
    r0 = float4(0, 0, 0, 0);
    r1 = float4(0, 0, 0, 0);
    r2 = float4(0, 0, 0, 0);
    r3 = float4(0, 0, 0, 0);
    int rounds = (int)IniParams[84].z;
    [loop]
    for (int part = 0; part < rounds; part++) {
        int u0 = a0, u1 = a1, u2 = a2, u3 = a3, u4 = a4, u5 = a5, u6 = a6, u7 = a7, un = an;
        if (part == 1) { u0 = b0; u1 = b1; u2 = b2; u3 = b3; u4 = b4; u5 = b5; u6 = b6; u7 = b7; un = bn; }
        else if (part == 2) { u0 = c0; u1 = c1; u2 = c2; u3 = c3; u4 = c4; u5 = c5; u6 = c6; u7 = c7; un = cn; }
        else if (part == 3) { u0 = d0; u1 = d1; u2 = d2; u3 = d3; u4 = d4; u5 = d5; u6 = d6; u7 = d7; un = dn; }
        float4 lab = Label(u0, u1, u2, u3, u4, u5, u6, u7, un);
        if (part == 0) r0 = lab;
        else if (part == 1) r1 = lab;
        else if (part == 2) r2 = lab;
        else r3 = lab;
    }
}

int SpanAt(int i, int4 a, int4 b, int4 c, int4 d) {
    if (i < 4) return i == 0 ? a.x : i == 1 ? a.y : i == 2 ? a.z : a.w;
    if (i < 8) {
        int j = i - 4;
        return j == 0 ? b.x : j == 1 ? b.y : j == 2 ? b.z : b.w;
    }
    int j = i - 8;
    return j == 0 ? c.x : j == 1 ? c.y : j == 2 ? c.z : c.w;
}

float SpanWidth(int count, int4 a, int4 b, int4 c, int4 d) {
    float w = 0.0;
    [loop]
    for (int i = 0; i < 12; i++) {
        if (i >= count) break;
        int ch = SpanAt(i, a, b, c, d) - 32;
        int adv = 10;
        if (ch >= 0 && ch < 128) adv = (int)(FontTex.Load(int3(ch, 700, 0)).r * 255.0 + 0.5);
        w += (float)adv;
    }
    return w;
}

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
    int toggleLimit = (int)IniParams[84].x;
    int baseLimit = (int)IniParams[84].y;
    if (toggleLimit < 256) toggleLimit = 256;
    if (baseLimit < 256) baseLimit = 256;
    float4 s0, s1, s2, s3;
    Scan(ToggleText, toggleLimit, 0, s0, s1, s2, s3);
    output.styleKey = s0;
    output.upKey = s1;
    output.downKey = s2;
    output.menuKey = s3;
    float4 f0, f1, f2, f3;
    Scan(BaseText, baseLimit, 4, f0, f1, f2, f3);
    output.faceKey = f0;
    output.bodyKey = f1;
    int4 blank = int4(0, 0, 0, 0);
    output.spanA = float4(
        SpanWidth(2, int4(141, 142, 0, 0), blank, blank, blank),
        SpanWidth(2, int4(143, 144, 0, 0), blank, blank, blank),
        SpanWidth(1, int4(150, 0, 0, 0), blank, blank, blank),
        SpanWidth(1, int4(151, 0, 0, 0), blank, blank, blank));
    output.spanB = float4(
        SpanWidth(3, int4(147, 141, 142, 0), blank, blank, blank),
        SpanWidth(2, int4(153, 154, 0, 0), blank, blank, blank),
        SpanWidth(2, int4(152, 133, 0, 0), blank, blank, blank),
        SpanWidth(2, int4(148, 149, 0, 0), blank, blank, blank));
}
#endif

#ifdef PIXEL_SHADER
float4 SamplePx(Texture2D<float4> tex, float2 p) {
    float2 f = frac(p);
    int2 i = int2(p);
    float4 a = tex.Load(int3(i, 0));
    float4 b = tex.Load(int3(i + int2(1, 0), 0));
    float4 c = tex.Load(int3(i + int2(0, 1), 0));
    float4 d = tex.Load(int3(i + int2(1, 1), 0));
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

int Advance(int ch) {
    int idx = ch - 32;
    if (idx < 0 || idx >= 128) return 10;
    return (int)(FontTex.Load(int3(idx, 700, 0)).r * 255.0 + 0.5);
}

float GlyphAlpha(int ch, float lx, float ly) {
    int idx = ch - 32;
    if (idx < 0 || idx >= 128 || lx < 0.0 || ly < 0.0 || lx >= 32.0 || ly >= 39.0) return 0.0;
    float2 local = clamp(float2(lx, ly) * 2.0, 0.0, float2(61.0, 74.0));
    float2 origin = float2(float((idx % 16) * 64), float((idx / 16) * 78));
    return FontTex.Load(int3(int2(origin + local), 0)).a;
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
    if (rel.x < 0.0 || rel.y < 0.0 || rel.y >= 39.0 || rel.x >= 220.0) return 0.0;
    float x = 0.0;
    [loop]
    for (int i = 0; i < 16; i++) {
        if (i >= count) return 0.0;
        int ch = CharAt(i, a, b, c, d);
        int adv = Advance(ch);
        if (rel.x < x + (float)adv) {
            return GlyphAlpha(ch, rel.x - x, rel.y);
        }
        x += (float)adv;
    }
    return 0.0;
}

float CharCov(float2 pix, float2 origin, int ch) {
    float2 rel = pix - origin;
    if (rel.x < 0.0 || rel.y < 0.0 || rel.x >= 32.0 || rel.y >= 39.0) return 0.0;
    int adv = Advance(ch);
    if (rel.x >= (float)adv) return 0.0;
    return GlyphAlpha(ch, rel.x, rel.y);
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

void DrawCh(float2 pix, float y, int ch, float3 color, inout float4 result, inout float x) {
    Cover(result, CharCov(pix, float2(x, y), ch), color);
    x += (float)Advance(ch);
}

void DrawLab(float2 pix, float2 origin, float4 packed, float3 color, inout float4 result) {
    if (pix.y < origin.y || pix.y >= origin.y + 39.0) return;
    if (pix.x < origin.x || pix.x >= origin.x + 220.0) return;
    float x = origin.x;
    float y = origin.y;
    [loop]
    for (int i = 0; i < 12; i++) {
        int slot = 0;
        int sub = i;
        if (i >= 9) { slot = 3; sub = i - 9; }
        else if (i >= 6) { slot = 2; sub = i - 6; }
        else if (i >= 3) { slot = 1; sub = i - 3; }
        float comp = slot == 0 ? packed.x : slot == 1 ? packed.y : slot == 2 ? packed.z : packed.w;
        int bits = (int)(comp + 0.5);
        int sh = 0;
        if (sub == 1) sh = 8;
        else if (sub == 2) sh = 16;
        int ch = (bits >> sh) & 255;
        if (ch == 0) return;
        DrawCh(pix, y, ch, color, result, x);
    }
}

void main(VsOut input, out float4 result : SV_Target0) {
    float2 pix = input.uv * float2(500.0, 540.0);
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
    float saveHot = (meter.z > 1.5 && meter.z < 2.5) ? 1.0 : 0.0;
    float clearHot = meter.z > 2.5 ? 1.0 : 0.0;
    if (pix.x >= 448.0 && pix.x < 488.0 && pix.y >= 8.0 && pix.y < 48.0)
        result.rgb = lerp(float3(0.42, 0.16, 0.16), float3(0.78, 0.24, 0.22), hot);
    float d1 = abs((pix.x - 456.0) - (pix.y - 16.0));
    float d2 = abs((pix.x - 456.0) + (pix.y - 40.0));
    if ((d1 < 2.4 || d2 < 2.4) && pix.x > 454.0 && pix.x < 482.0 && pix.y > 14.0 && pix.y < 42.0)
        result.rgb = float3(0.98, 0.98, 0.99);

    float2 logoAt = float2(6.0, 4.0);
    float2 logoRel = pix - logoAt;
    if (logoRel.x >= 0.0 && logoRel.x < 48.0 && logoRel.y >= 0.0 && logoRel.y < 48.0) {
        float4 mark = SamplePx(LogoTex, clamp(logoRel / 48.0 * 256.0, 0.0, 254.0));
        result.rgb = result.rgb * (1.0 - mark.a) + mark.rgb;
    }

    int4 z = int4(0, 0, 0, 0);
    DrawStr(pix, float2(62, 10), 3, int4(127, 128, 129, 0), z, z, z, ink, result);

    float patched = 0.0;
    float saved = 0.0;
    if (meter.w > 2.5) {
        patched = 1.0;
        saved = 1.0;
    } else if (meter.w > 1.5) {
        saved = 1.0;
    } else if (meter.w > 0.5) {
        patched = 1.0;
    }
    float yFace = 68.0;
    float yBody = 114.0;
    float yStyle = 160.0;
    float yLevel = yStyle + 46.0;
    float yDown = yStyle + 92.0;
    float yPanel = yStyle + 138.0;
    float ySave = 486.0;
    float yLook = 400.0;

    DrawStr(pix, float2(16, yFace), 1, int4(130, 0, 0, 0), z, z, z, ink, result);
    DrawLab(pix, float2(196, yFace), input.faceKey, key, result);
    DrawStr(pix, float2(16, yBody), 2, int4(131, 132, 0, 0), z, z, z, ink, result);
    DrawLab(pix, float2(196, yBody), input.bodyKey, key, result);
    DrawStr(pix, float2(16, yStyle), 2, int4(135, 136, 0, 0), z, z, z, ink, result);
    DrawLab(pix, float2(196, yStyle), input.styleKey, key, result);
    DrawStr(pix, float2(16, yLevel), 2, int4(137, 138, 0, 0), z, z, z, ink, result);
    DrawLab(pix, float2(196, yLevel), input.upKey, key, result);
    DrawLab(pix, float2(196, yDown), input.downKey, key, result);
    DrawStr(pix, float2(16, yPanel), 2, int4(139, 140, 0, 0), z, z, z, ink, result);
    DrawLab(pix, float2(196, yPanel), input.menuKey, key, result);
    float3 savePaper = saveHot > 0.5 ? float3(0.20, 0.34, 0.24) : float3(0.14, 0.22, 0.18);
    float3 clearPaper = clearHot > 0.5 ? float3(0.42, 0.16, 0.16) : float3(0.24, 0.12, 0.12);
    if (pix.x >= 12.0 && pix.x < 230.0 && pix.y >= 482.0 && pix.y < 526.0)
        result.rgb = savePaper;
    if (pix.x >= 250.0 && pix.x < 488.0 && pix.y >= 482.0 && pix.y < 526.0)
        result.rgb = clearPaper;
    int4 saveWordBtn = int4(141, 142, 0, 0);
    int4 resetWord = int4(143, 144, 0, 0);
    float saveW = input.spanA.x;
    float resetW = input.spanA.y;
    DrawStr(pix, float2(12.0 + (218.0 - saveW) * 0.5, ySave), 2, saveWordBtn, z, z, z, ink, result);
    DrawStr(pix, float2(250.0 + (238.0 - resetW) * 0.5, ySave), 2, resetWord, z, z, z, ink, result);
    if (patched < 0.5) {
        if (saved > 0.5 && pix.x >= 12.0 && pix.x < 488.0 && pix.y >= 396.0 && pix.y < 442.0)
            result.rgb = float3(0.10, 0.24, 0.16);
        DrawStr(pix, float2(16, yLook), 2, int4(145, 146, 0, 0), z, z, z, ink, result);
    }

    float right = 484.0;
    float face = state.x > 0.5 ? 1.0 : 0.0;
    float body = state.y > 0.5 ? 1.0 : 0.0;
    int4 onWord = int4(150, 0, 0, 0);
    int4 offWord = int4(151, 0, 0, 0);
    float onX = right - input.spanA.z;
    float offX = right - input.spanA.w;
    if (face > 0.5) DrawStr(pix, float2(onX, yFace), 1, onWord, z, z, z, on, result);
    else DrawStr(pix, float2(offX, yFace), 1, offWord, z, z, z, off, result);
    if (body > 0.5) DrawStr(pix, float2(onX, yBody), 1, onWord, z, z, z, on, result);
    else DrawStr(pix, float2(offX, yBody), 1, offWord, z, z, z, off, result);
    int4 savedWord = int4(147, 141, 142, 0);
    int4 defaultWord = int4(148, 149, 0, 0);
    if (patched < 0.5) {
        if (saved > 0.5) {
            float savedX = right - input.spanB.x;
            DrawStr(pix, float2(savedX, yLook), 3, savedWord, z, z, z, on, result);
        } else {
            float defaultX = right - input.spanB.w;
            DrawStr(pix, float2(defaultX, yLook), 2, defaultWord, z, z, z, off, result);
        }
    }

    if (pix.y >= yStyle && pix.y < yStyle + 39.0) {
        int style = (int)(meter.x + 0.5);
        if (style < 0) style = 0;
        if (style > 9) style = 9;
        int styleCh = 48 + style;
        float3 styleColor = style == 0 ? off : ink;
        Cover(result, CharCov(pix, float2(right - (float)Advance(styleCh), yStyle), styleCh), styleColor);
    }

    if (pix.y >= yLevel && pix.y < yLevel + 39.0) {
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
}
#endif
