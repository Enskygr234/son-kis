using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace WinterDeveloper.SonKis
{
    // Oyunun renk paleti: gece mavisi + kor sarısı + kırmızı kaban
    static class P
    {
        public static Color C(int rgb) { return Color.FromArgb(255, (rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255); }
        public static int I(int rgb) { return unchecked((int)0xFF000000) | rgb; }

        public static readonly Color Ink = C(0x0B0F24);
        public static readonly Color Night0 = C(0x050716), Night1 = C(0x0C1230), Night2 = C(0x18234C), Night3 = C(0x2A3C70);
        public static readonly Color Snow0 = C(0xF4F8FF), Snow1 = C(0xCBDCF5), Snow2 = C(0x9DB3DD), Snow3 = C(0x6F86BC), Snow4 = C(0x4A5C92);
        public static readonly Color Ice = C(0x8FD3E8), IceD = C(0x4F8FB8);
        public static readonly Color Fire0 = C(0xFFF6D0), Fire1 = C(0xFFD166), Fire2 = C(0xFF9A3C), Fire3 = C(0xE8552D), Fire4 = C(0x9C2A2A);
        public static readonly Color Red = C(0xD8433B), RedD = C(0x9B2733), RedL = C(0xF2705A);
        public static readonly Color Cream = C(0xF6ECD6), Amber = C(0xF2C46D), AmberD = C(0xB5803A), Bronze = C(0x7A5630);
        public static readonly Color Panel = C(0x0B0E2A), PanelHi = C(0x161C44);
        public static readonly Color Dim = C(0x93A5CF), Narr = C(0xBFD2F2);
        public static readonly Color Teal = C(0x2F7F86), Green = C(0x6FCF97);
    }

    static class Fx
    {
        static readonly Dictionary<int, SolidBrush> brushes = new Dictionary<int, SolidBrush>();

        public static Brush B(Color c)
        {
            SolidBrush b;
            int key = c.ToArgb();
            if (!brushes.TryGetValue(key, out b))
            {
                if (brushes.Count > 6000)
                {
                    foreach (var old in brushes.Values) old.Dispose();
                    brushes.Clear();
                }
                b = new SolidBrush(c);
                brushes[key] = b;
            }
            return b;
        }

        public static Brush B(int a, Color c) { return B(Color.FromArgb(a < 0 ? 0 : (a > 255 ? 255 : a), c)); }

        public static float Clamp(float v, float lo, float hi) { return v < lo ? lo : (v > hi ? hi : v); }
        public static float Lerp(float a, float b, float t) { return a + (b - a) * t; }
        public static float Smooth(float t) { t = Clamp(t, 0, 1); return t * t * (3 - 2 * t); }
        public static float Dist(float ax, float ay, float bx, float by) { float dx = ax - bx, dy = ay - by; return (float)Math.Sqrt(dx * dx + dy * dy); }

        public static Color Mix(Color a, Color b, float t)
        {
            t = Clamp(t, 0, 1);
            return Color.FromArgb(255, (int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
        }

        public static void Rect(Graphics g, Color c, float x, float y, float w, float h) { g.FillRectangle(B(c), x, y, w, h); }

        // 1 piksellik çerçeve (kalem kullanmadan, piksel hizalı)
        public static void Frame(Graphics g, Color c, int x, int y, int w, int h)
        {
            Brush b = B(c);
            g.FillRectangle(b, x, y, w, 1);
            g.FillRectangle(b, x, y + h - 1, w, 1);
            g.FillRectangle(b, x, y + 1, 1, h - 2);
            g.FillRectangle(b, x + w - 1, y + 1, 1, h - 2);
        }

        public static void Line(Graphics g, Color c, int x0, int y0, int x1, int y1)
        {
            Brush b = B(c);
            int dx = Math.Abs(x1 - x0), dy = -Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx + dy;
            for (int n = 0; n < 2000; n++)
            {
                g.FillRectangle(b, x0, y0, 1, 1);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        // Her çağrıda aynı sonucu veren ucuz gürültü
        public static float Hash(int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }
    }

    // Küçük piksel tuvali: sprite'lar ve arka plan katmanları bununla üretilir
    sealed class Pix
    {
        public readonly int W, H;
        public readonly int[] D;
        public bool OnlyOpaque; // true iken sadece dolu piksellerin üstüne boyar (gölgelendirme için)

        public Pix(int w, int h) { W = w; H = h; D = new int[w * h]; }

        public int Get(int x, int y) { return ((uint)x < (uint)W && (uint)y < (uint)H) ? D[y * W + x] : 0; }

        public void Set(int x, int y, int c)
        {
            if ((uint)x >= (uint)W || (uint)y >= (uint)H) return;
            int i = y * W + x;
            if (OnlyOpaque && ((uint)D[i] >> 24) == 0) return;
            D[i] = c;
        }

        public void Blend(int x, int y, int c, float a)
        {
            if ((uint)x >= (uint)W || (uint)y >= (uint)H) return;
            int i = y * W + x;
            int o = D[i];
            if (((uint)o >> 24) == 0) { if (!OnlyOpaque) D[i] = ((int)(a * 255) << 24) | (c & 0xFFFFFF); return; }
            int r = (int)(((o >> 16) & 255) * (1 - a) + ((c >> 16) & 255) * a);
            int gg = (int)(((o >> 8) & 255) * (1 - a) + ((c >> 8) & 255) * a);
            int b = (int)((o & 255) * (1 - a) + (c & 255) * a);
            D[i] = (o & unchecked((int)0xFF000000)) | (r << 16) | (gg << 8) | b;
        }

        public void Rect(int x, int y, int w, int h, int c)
        {
            for (int j = y; j < y + h; j++)
                for (int i = x; i < x + w; i++) Set(i, j, c);
        }

        public void Ellipse(float cx, float cy, float rx, float ry, int c)
        {
            for (int y = (int)(cy - ry) - 1; y <= (int)(cy + ry) + 1; y++)
                for (int x = (int)(cx - rx) - 1; x <= (int)(cx + rx) + 1; x++)
                {
                    float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                    if (dx * dx + dy * dy <= 1f) Set(x, y, c);
                }
        }

        public void Line(int x0, int y0, int x1, int y1, int c)
        {
            int dx = Math.Abs(x1 - x0), dy = -Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx + dy;
            for (int n = 0; n < 4000; n++)
            {
                Set(x0, y0, c);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        // Çokgen doldurma (piksel merkezlerine göre)
        public void Poly(int c, params float[] xy)
        {
            int n = xy.Length / 2;
            float minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < n; i++) { minY = Math.Min(minY, xy[i * 2 + 1]); maxY = Math.Max(maxY, xy[i * 2 + 1]); }
            var xs = new List<float>();
            for (int y = (int)Math.Floor(minY); y <= (int)Math.Ceiling(maxY); y++)
            {
                float py = y + 0.5f;
                xs.Clear();
                for (int i = 0; i < n; i++)
                {
                    float x0 = xy[i * 2], y0 = xy[i * 2 + 1];
                    float x1 = xy[((i + 1) % n) * 2], y1 = xy[((i + 1) % n) * 2 + 1];
                    if ((y0 <= py && y1 > py) || (y1 <= py && y0 > py))
                        xs.Add(x0 + (py - y0) / (y1 - y0) * (x1 - x0));
                }
                xs.Sort();
                for (int k = 0; k + 1 < xs.Count; k += 2)
                    for (int x = (int)Math.Ceiling(xs[k] - 0.5f); x + 0.5f < xs[k + 1]; x++) Set(x, y, c);
            }
        }

        public void Rows(int ox, int oy, string[] rows, Func<char, int> key)
        {
            for (int y = 0; y < rows.Length; y++)
                for (int x = 0; x < rows[y].Length; x++)
                {
                    int c = key(rows[y][x]);
                    if (((uint)c >> 24) != 0) Set(ox + x, oy + y, c);
                }
        }

        public void Stamp(Pix src, int ox, int oy)
        {
            for (int y = 0; y < src.H; y++)
                for (int x = 0; x < src.W; x++)
                {
                    int c = src.D[y * src.W + x];
                    if (((uint)c >> 24) != 0) Set(ox + x, oy + y, c);
                }
        }

        // Dolu piksellerin çevresine dış hat çizer
        public void Outline(int c)
        {
            var src = (int[])D.Clone();
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (((uint)src[y * W + x] >> 24) != 0) continue;
                    bool near = (x > 0 && ((uint)src[y * W + x - 1] >> 24) != 0) || (x < W - 1 && ((uint)src[y * W + x + 1] >> 24) != 0)
                             || (y > 0 && ((uint)src[(y - 1) * W + x] >> 24) != 0) || (y < H - 1 && ((uint)src[(y + 1) * W + x] >> 24) != 0);
                    if (near) D[y * W + x] = c;
                }
        }

        public Pix Flipped()
        {
            var p = new Pix(W, H);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++) p.D[y * W + (W - 1 - x)] = D[y * W + x];
            return p;
        }

        public Pix Clone()
        {
            var p = new Pix(W, H);
            Array.Copy(D, p.D, D.Length);
            return p;
        }

        public Bitmap Bmp()
        {
            var bm = new Bitmap(W, H, PixelFormat.Format32bppPArgb);
            var pre = new int[D.Length];
            for (int i = 0; i < D.Length; i++)
            {
                int c = D[i];
                int a = (int)((uint)c >> 24);
                if (a == 255) pre[i] = c;
                else if (a == 0) pre[i] = 0;
                else pre[i] = (a << 24) | ((((c >> 16) & 255) * a / 255) << 16) | ((((c >> 8) & 255) * a / 255) << 8) | ((c & 255) * a / 255);
            }
            var bd = bm.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
            for (int y = 0; y < H; y++) Marshal.Copy(pre, y * W, bd.Scan0 + y * bd.Stride, W);
            bm.UnlockBits(bd);
            return bm;
        }
    }

    // Oyunun kendi piksel yazı tipi (Türkçe karakterlerle)
    static class Font5
    {
        public const int LineH = 12;
        const int Above = 3, GH = 12;

        sealed class Glyph { public int W; public bool[] Bits; }

        static readonly Dictionary<char, Glyph> glyphs = new Dictionary<char, Glyph>();

        static Font5()
        {
            Def('A', ".###./#...#/#...#/#####/#...#/#...#/#...#");
            Def('B', "####./#...#/#...#/####./#...#/#...#/####.");
            Def('C', ".###./#...#/#..../#..../#..../#...#/.###.");
            Def('D', "####./#...#/#...#/#...#/#...#/#...#/####.");
            Def('E', "#####/#..../#..../####./#..../#..../#####");
            Def('F', "#####/#..../#..../####./#..../#..../#....");
            Def('G', ".###./#...#/#..../#.###/#...#/#...#/.###.");
            Def('H', "#...#/#...#/#...#/#####/#...#/#...#/#...#");
            Def('I', "###/.#./.#./.#./.#./.#./###");
            Def('J', "..###/...#./...#./...#./...#./#..#./.##..");
            Def('K', "#...#/#..#./#.#../##.../#.#../#..#./#...#");
            Def('L', "#..../#..../#..../#..../#..../#..../#####");
            Def('M', "#...#/##.##/#.#.#/#.#.#/#...#/#...#/#...#");
            Def('N', "#...#/##..#/#.#.#/#..##/#...#/#...#/#...#");
            Def('O', ".###./#...#/#...#/#...#/#...#/#...#/.###.");
            Def('P', "####./#...#/#...#/####./#..../#..../#....");
            Def('Q', ".###./#...#/#...#/#...#/#.#.#/#..#./.##.#");
            Def('R', "####./#...#/#...#/####./#.#../#..#./#...#");
            Def('S', ".####/#..../#..../.###./....#/....#/####.");
            Def('T', "#####/..#../..#../..#../..#../..#../..#..");
            Def('U', "#...#/#...#/#...#/#...#/#...#/#...#/.###.");
            Def('V', "#...#/#...#/#...#/#...#/#...#/.#.#./..#..");
            Def('W', "#...#/#...#/#...#/#.#.#/#.#.#/##.##/#...#");
            Def('X', "#...#/#...#/.#.#./..#../.#.#./#...#/#...#");
            Def('Y', "#...#/#...#/.#.#./..#../..#../..#../..#..");
            Def('Z', "#####/....#/...#./..#../.#.../#..../#####");

            Def('a', ".###./....#/.####/#...#/.####", 2);
            Def('b', "#..../#..../####./#...#/#...#/#...#/####.");
            Def('c', ".###/#.../#.../#.../.###", 2);
            Def('d', "....#/....#/.####/#...#/#...#/#...#/.####");
            Def('e', ".###./#...#/#####/#..../.###.", 2);
            Def('f', "..##/.#../###./.#../.#../.#../.#..");
            Def('g', ".####/#...#/#...#/#...#/.####/....#/.###.", 2);
            Def('h', "#..../#..../####./#...#/#...#/#...#/#...#");
            Def('i', "#/./#/#/#/#/#");
            Def('ı', "#/#/#/#/#", 2);
            Def('j', "..#/.../..#/..#/..#/..#/..#/..#/##.");
            Def('k', "#.../#.../#..#/#.#./##../#.#./#..#");
            Def('l', "#/#/#/#/#/#/#");
            Def('m', "##.#./#.#.#/#.#.#/#.#.#/#.#.#", 2);
            Def('n', "####./#...#/#...#/#...#/#...#", 2);
            Def('o', ".###./#...#/#...#/#...#/.###.", 2);
            Def('p', "####./#...#/#...#/#...#/####./#..../#....", 2);
            Def('q', ".####/#...#/#...#/#...#/.####/....#/....#", 2);
            Def('r', "#.##/##../#.../#.../#...", 2);
            Def('s', ".###/#.../.##./...#/###.", 2);
            Def('t', ".#../.#../###./.#../.#../.#../..##");
            Def('u', "#...#/#...#/#...#/#...#/.####", 2);
            Def('v', "#...#/#...#/#...#/.#.#./..#..", 2);
            Def('w', "#...#/#...#/#.#.#/#.#.#/.#.#.", 2);
            Def('x', "#...#/.#.#./..#../.#.#./#...#", 2);
            Def('y', "#...#/#...#/#...#/#...#/.####/....#/.###.", 2);
            Def('z', "####/...#/.##./#.../####", 2);

            Def('0', ".###./#...#/#..##/#.#.#/##..#/#...#/.###.");
            Def('1', ".#./##./.#./.#./.#./.#./###");
            Def('2', ".###./#...#/....#/...#./..#../.#.../#####");
            Def('3', "####./....#/....#/.###./....#/....#/####.");
            Def('4', "...#./..##./.#.#./#..#./#####/...#./...#.");
            Def('5', "#####/#..../####./....#/....#/#...#/.###.");
            Def('6', ".###./#..../#..../####./#...#/#...#/.###.");
            Def('7', "#####/....#/...#./..#../..#../..#../..#..");
            Def('8', ".###./#...#/#...#/.###./#...#/#...#/.###.");
            Def('9', ".###./#...#/#...#/.####/....#/....#/.###.");

            Def(' ', "...");
            Def('.', "#", 6);
            Def(',', ".#/#.", 6);
            Def('!', "#/#/#/#/#/./#");
            Def('?', ".###./#...#/....#/...#./..#../...../..#..");
            Def(':', "#/././#", 3);
            Def(';', ".#/../../.#/#.", 3);
            Def('-', "###", 3);
            Def('—', "#####", 3);
            Def('\'', "#/#");
            Def('"', "#.#/#.#");
            Def('…', "#.#.#", 6);
            Def('_', "####", 7);
            Def('(', ".#/#./#./#./#./#./.#");
            Def(')', "#./.#/.#/.#/.#/.#/#.");
            Def('[', "##/#./#./#./#./#./##");
            Def(']', "##/.#/.#/.#/.#/.#/##");
            Def('/', "..#/..#/.#./.#./.#./#../#..");
            Def('+', ".#./###/.#.", 2);
            Def('=', "###/.../###", 2);
            Def('*', "#.#/.#./#.#", 1);
            Def('×', "#.#/.#./#.#", 3);
            Def('%', "##..#/##.#./...#./..#../.#.../.#.##/#..##");
            Def('<', "..#/.#./#../.#./..#", 1);
            Def('>', "#../.#./..#/.#./#..", 1);
            Def('~', ".#.../#.#.#/...#.", 2);
            Def('►', "#../##./###/##./#..", 1);
            Def('◄', "..#/.##/###/.##/..#", 1);

            // Türkçe karakterler: taban harf + işaret
            Acc('Ç', 'C', "..#../.##..", 7);
            Acc('Ş', 'S', "..#../.##..", 7);
            Acc('Ö', 'O', ".#.#.", -2);
            Acc('Ü', 'U', ".#.#.", -2);
            Acc('İ', 'I', ".#.", -2);
            Acc('Ğ', 'G', "#...#/.###.", -3);
            Acc('Â', 'A', "..#../.#.#.", -3);
            Acc('ç', 'c', "..#./.##.", 7);
            Acc('ş', 's', ".#../##..", 7);
            Acc('ö', 'o', ".#.#.", 0);
            Acc('ü', 'u', ".#.#.", 0);
            Acc('ğ', 'g', "#...#/.###.", -1);
            Acc('â', 'a', "..#../.#.#.", -1);
            Acc('û', 'u', "..#../.#.#.", -1);
            Def('î', ".#./#.#/.../.#./.#./.#./.#./.#.", -1);
        }

        static void Def(char ch, string rows, int startRow = 0)
        {
            string[] r = rows.Split('/');
            int w = 0;
            foreach (var s in r) w = Math.Max(w, s.Length);
            var gl = new Glyph { W = w, Bits = new bool[w * GH] };
            for (int y = 0; y < r.Length; y++)
                for (int x = 0; x < r[y].Length; x++)
                {
                    int gy = y + startRow + Above;
                    if (r[y][x] == '#' && gy >= 0 && gy < GH) gl.Bits[gy * w + x] = true;
                }
            glyphs[ch] = gl;
        }

        static void Acc(char ch, char baseCh, string rows, int startRow)
        {
            Glyph b = glyphs[baseCh];
            var gl = new Glyph { W = b.W, Bits = (bool[])b.Bits.Clone() };
            string[] r = rows.Split('/');
            for (int y = 0; y < r.Length; y++)
                for (int x = 0; x < r[y].Length && x < b.W; x++)
                {
                    int gy = y + startRow + Above;
                    if (r[y][x] == '#' && gy >= 0 && gy < GH) gl.Bits[gy * b.W + x] = true;
                }
            glyphs[ch] = gl;
        }

        static Glyph Get(char ch)
        {
            Glyph gl;
            if (glyphs.TryGetValue(ch, out gl)) return gl;
            if (ch == '’' || ch == '‘') return glyphs['\''];
            if (ch == '“' || ch == '”') return glyphs['"'];
            if (ch == '–') return glyphs['-'];
            return glyphs['?'];
        }

        public static int Width(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            int w = 0;
            foreach (char ch in s) w += Get(ch).W + 1;
            return w - 1;
        }

        // Yazılar satır satır önbelleğe alınır: her karede harf harf çizmek yerine tek bitmap basılır
        static readonly Dictionary<string, Bitmap> lineCache = new Dictionary<string, Bitmap>();

        static Bitmap Line(string s, Color c)
        {
            int argb = c.ToArgb();
            string key = argb.ToString("X8") + s;
            Bitmap bm;
            if (lineCache.TryGetValue(key, out bm)) return bm;
            if (lineCache.Count > 700)
            {
                foreach (var old in lineCache.Values) old.Dispose();
                lineCache.Clear();
            }
            var px = new Pix(Math.Max(1, Width(s)), GH);
            int x = 0;
            foreach (char ch in s)
            {
                Glyph gl = Get(ch);
                for (int gy = 0; gy < GH; gy++)
                    for (int gx = 0; gx < gl.W; gx++)
                        if (gl.Bits[gy * gl.W + gx]) px.D[gy * px.W + x + gx] = argb;
                x += gl.W + 1;
            }
            bm = px.Bmp();
            lineCache[key] = bm;
            return bm;
        }

        // y: büyük harflerin üst kenarı
        public static void Draw(Graphics g, string s, float fx, float fy, Color c, int scale = 1)
        {
            if (string.IsNullOrEmpty(s) || c.A < 9) return;
            Bitmap bm = Line(s, c);
            g.DrawImage(bm, new Rectangle((int)Math.Round(fx), (int)Math.Round(fy) - Above * scale, bm.Width * scale, GH * scale));
        }

        public static void DrawS(Graphics g, string s, float x, float y, Color c, Color shadow, int scale = 1)
        {
            Draw(g, s, x + scale, y + scale, shadow, scale);
            Draw(g, s, x, y, c, scale);
        }

        public static void Center(Graphics g, string s, float cx, float y, Color c, int scale = 1)
        {
            Draw(g, s, cx - Width(s) * scale / 2f, y, c, scale);
        }

        public static void CenterS(Graphics g, string s, float cx, float y, Color c, Color shadow, int scale = 1)
        {
            DrawS(g, s, cx - Width(s) * scale / 2f, y, c, shadow, scale);
        }

        public static List<string> Wrap(string text, int maxW)
        {
            var lines = new List<string>();
            foreach (string para in text.Split('\n'))
            {
                string cur = "";
                foreach (string word in para.Split(' '))
                {
                    string test = cur.Length == 0 ? word : cur + " " + word;
                    if (Width(test) <= maxW || cur.Length == 0) cur = test;
                    else { lines.Add(cur); cur = word; }
                }
                lines.Add(cur);
            }
            return lines;
        }

        // Bir harfin piksellerini dışarı verir (logo çizimi için)
        public static bool[] Bits(char ch, out int w)
        {
            Glyph gl = Get(ch);
            w = gl.W;
            return gl.Bits;
        }
    }
}
