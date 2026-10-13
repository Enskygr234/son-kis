using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace WinterDeveloper.SonKis
{
    sealed class Prop
    {
        public float X, Y, R;            // R: çarpışma yarıçapı (0 = yok)
        public Spr S;
        public Action<Graphics, float, float> Draw; // özel çizim (ekran koordinatı)
        public Action Use;               // etkileşim
        public float IR = 26, PromptH = 30;
        public bool Flat;                // yerde, her şeyin altında
        public float LightR, LightDY;    // ışık kaynağı
    }

    sealed class Theme
    {
        public string Name, Track;
        public int Width, SkyTop, SkyMid, SkyLow, FarC, NearC, AkH, Ground, TreeMode, SnowN, Stars;
        public float Aurora, Wind;
    }

    sealed class Backdrop
    {
        public Bitmap Sky, Far, Near, Trees, Ground;
        public int AkX, AkY; // Akdağ zirvesinin uzak katmandaki konumu
    }

    sealed partial class Game
    {
        const int Horizon = 112, BandTop = 134, BandBot = 204;
        const float FarP = 0.08f, NearP = 0.22f, TreeP = 0.5f;

        static readonly Theme[] themes =
        {
            new Theme { Name = "SÖNMEZ KÖYÜ", Track = "ocak", Width = 1000, SkyTop = 0x060818, SkyMid = 0x141C46, SkyLow = 0x30437C, FarC = 0x222E60, NearC = 0x172048, AkH = 46, Ground = 0, TreeMode = 0, SnowN = 90, Stars = 60, Aurora = 0.8f, Wind = 6 },
            new Theme { Name = "KAYIN ORMANI", Track = "yol", Width = 1300, SkyTop = 0x040A18, SkyMid = 0x0E2440, SkyLow = 0x215066, FarC = 0x1B3558, NearC = 0x122844, AkH = 58, Ground = 0, TreeMode = 1, SnowN = 70, Stars = 45, Aurora = 1.25f, Wind = 4 },
            new Theme { Name = "TURNA GÖLÜ", Track = "gol", Width = 1300, SkyTop = 0x080822, SkyMid = 0x1E2258, SkyLow = 0x4E4E94, FarC = 0x2A2C68, NearC = 0x1C1E50, AkH = 74, Ground = 1, TreeMode = 2, SnowN = 60, Stars = 70, Aurora = 1.0f, Wind = 10 },
            new Theme { Name = "KURT GEÇİDİ", Track = "gecit", Width = 1300, SkyTop = 0x05060F, SkyMid = 0x121832, SkyLow = 0x2A3156, FarC = 0x1E2548, NearC = 0x141936, AkH = 104, Ground = 2, TreeMode = 3, SnowN = 140, Stars = 40, Aurora = 0.4f, Wind = 38 },
            new Theme { Name = "AKDAĞ ZİRVESİ", Track = "ocak", Width = 920, SkyTop = 0x030410, SkyMid = 0x0C1232, SkyLow = 0x22326A, FarC = 0x1A2452, NearC = 0x141C44, AkH = 0, Ground = 2, TreeMode = 4, SnowN = 50, Stars = 120, Aurora = 1.5f, Wind = 14 }
        };

        Theme theme = themes[0];
        Backdrop bd;
        readonly Dictionary<int, Backdrop> bdCache = new Dictionary<int, Backdrop>();
        readonly List<Prop> props = new List<Prop>();
        readonly List<Prop> drawList = new List<Prop>();
        readonly List<PointF> prints = new List<PointF>();
        readonly Prop playerProp = new Prop();
        Prop spiritProp;
        int areaW = 1000, facing = 1;
        float px, py, camX, walkT, stepT, bannerT;
        readonly List<int> areaSpirits = new List<int>(); // bu bölgedeki ruhlar, yol sırasıyla
        readonly float[] spX = new float[8];
        bool moving, showPlayer = true;
        bool descent; // gün doğduktan sonra köye dönüş yürüyüşü
        float dawn; int dawnKind;
        bool gunLit;

        // ---------- Kar ----------
        const int SnowMax = 150;
        readonly float[] snX = new float[SnowMax], snY = new float[SnowMax], snV = new float[SnowMax], snP = new float[SnowMax];
        readonly int[] snS = new int[SnowMax];

        void InitSnow()
        {
            for (int i = 0; i < SnowMax; i++)
            {
                snX[i] = rnd.Next(0, W); snY[i] = rnd.Next(0, H);
                snS[i] = rnd.Next(0, 4) == 0 ? 2 : 1;
                snV[i] = snS[i] == 2 ? 30 + rnd.Next(0, 14) : 14 + rnd.Next(0, 14);
                snP[i] = (float)(rnd.NextDouble() * 6.28);
            }
        }

        void UpdateSnow(float dt)
        {
            float wind = theme.Wind;
            for (int i = 0; i < SnowMax; i++)
            {
                snY[i] += snV[i] * (1f + wind * 0.012f) * dt;
                snX[i] += (wind * (snS[i] == 2 ? 1.5f : 1f) + (float)Math.Sin(time * 0.8f + snP[i]) * 9f) * dt;
                if (snY[i] > H) { snY[i] = -2; snX[i] = rnd.Next(-30, W); }
                if (snX[i] > W + 2) snX[i] -= W + 4;
                if (snX[i] < -4) snX[i] += W + 4;
            }
        }

        void DrawSnow(Graphics g)
        {
            int n = Math.Min(SnowMax, (int)(theme.SnowN * (1f - dawn * 0.8f)));
            Brush small = Fx.B(170, P.Snow0), big = Fx.B(235, P.Snow0);
            for (int i = 0; i < n; i++)
                g.FillRectangle(snS[i] == 2 ? big : small, (int)snX[i], (int)snY[i], snS[i], snS[i]);
        }

        // =====================================================
        //  ARKA PLAN KATMANLARI
        // =====================================================
        static readonly float[] bayer = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
        static float Bayer(int x, int y) { return (bayer[(y & 3) * 4 + (x & 3)] + 0.5f) / 16f; }

        static int MixI(int a, int b, float t)
        {
            t = Fx.Clamp(t, 0, 1);
            int r = (int)(((a >> 16) & 255) + (((b >> 16) & 255) - ((a >> 16) & 255)) * t);
            int g = (int)(((a >> 8) & 255) + (((b >> 8) & 255) - ((a >> 8) & 255)) * t);
            int bl = (int)((a & 255) + ((b & 255) - (a & 255)) * t);
            return P.I((r << 16) | (g << 8) | bl);
        }

        Backdrop GetBackdrop(int idx)
        {
            Backdrop b;
            if (bdCache.TryGetValue(idx, out b)) return b;
            Theme t = themes[idx];
            b = new Backdrop();
            int seed = idx * 17 + 3;

            // --- gökyüzü: çizgili (dither) geçiş ---
            var sky = new Pix(W, Horizon + 12);
            for (int y = 0; y < sky.H; y++)
                for (int x = 0; x < W; x++)
                {
                    float tt = y / (float)(sky.H - 1);
                    float q = (float)Math.Floor(tt * 11 + Bayer(x, y) - 0.5f) / 11f;
                    int c = q < 0.55f ? MixI(t.SkyTop, t.SkyMid, q / 0.55f) : MixI(t.SkyMid, t.SkyLow, (q - 0.55f) / 0.45f);
                    sky.D[y * W + x] = c;
                }
            b.Sky = sky.Bmp();

            // --- uzak dağlar + Akdağ ---
            int fw = W + (int)((t.Width - W) * FarP) + 8, fh = Horizon + 4;
            var far = new Pix(fw, fh);
            float axc = fw * 0.70f, hw = t.AkH * 1.15f;
            int farBody = P.I(t.FarC), farShade = MixI(t.FarC, 0x050716, 0.35f), farSnow = MixI(t.FarC, 0xCBDCF5, 0.55f);
            int akBody = MixI(t.FarC, 0x8FA6D6, 0.22f), akShade = MixI(t.FarC, 0x050716, 0.12f);
            // sivri tepeler: sol yüz aydınlık, sağ yüz gölge, tepede testere dişli kar
            var peaks = new List<float[]>(); // cx, yükseklik, yarı genişlik, ana tepe mi
            for (int k = 0; k < fw / 44 + 3; k++)
            {
                float ph = 13 + Fx.Hash(k, seed + 2) * 19;
                if (t.TreeMode == 4) ph *= 0.5f; // zirvede: aşağıda kalan tepeler
                peaks.Add(new[] { k * 44 - 30 + Fx.Hash(k, seed + 1) * 38, ph, ph * (1.25f + Fx.Hash(k, seed + 3) * 0.9f), 0f });
            }
            if (t.AkH > 0)
            {
                peaks.Add(new[] { axc, t.AkH, hw, 1f });
                peaks.Add(new[] { axc - hw * 0.62f, t.AkH * 0.56f, hw * 0.55f, 1f });
                peaks.Add(new[] { axc + hw * 0.7f, t.AkH * 0.42f, hw * 0.5f, 1f });
            }
            int akSnowL = P.I(0xDCE8FA), akSnowR = P.I(0x9DB3DD), farSnowR = MixI(t.FarC, 0xCBDCF5, 0.3f);
            for (int x = 0; x < fw; x++)
            {
                float r = 5 + (Fx.Hash(x / 5, seed) - 0.5f) * 2;
                float[] pk = null;
                foreach (float[] q in peaks)
                {
                    float v = q[1] * (1 - Math.Abs(x - q[0]) / q[2]) + (Fx.Hash(x / 2, seed + (int)q[0]) - 0.5f) * (q[3] > 0 ? 1.6f : 1.0f);
                    if (v > r) { r = v; pk = q; }
                }
                int top = fh - (int)r;
                for (int y = Math.Max(0, top); y < fh; y++)
                {
                    int c = farShade;
                    if (pk != null)
                    {
                        bool main = pk[3] > 0;
                        bool right = x + Math.Sin(y * 0.33 + seed) * 1.6 > pk[0];
                        float zig = Math.Abs(((x + (int)pk[0]) % 8) - 4) - 2;
                        float snowBottom = fh - pk[1] + (main ? pk[1] * 0.44f : Math.Max(0, (pk[1] - 17) * 0.7f)) + zig * (main ? 1.6f : 1f);
                        bool snow = y < snowBottom && (main || pk[1] > 17);
                        if (snow) c = main ? (right ? akSnowR : akSnowL) : (right ? farSnowR : farSnow);
                        else c = main ? (right ? akShade : akBody) : (right ? farShade : farBody);
                    }
                    far.D[y * fw + x] = c;
                }
            }
            if (t.AkH > 0)
            {
                b.AkX = (int)axc; b.AkY = fh - t.AkH;
                far.Rect(b.AkX - 1, b.AkY - 2, 3, 3, P.I(0x0B0F24));
                far.Set(b.AkX, b.AkY - 1, P.I(0x1A2450));
            }
            // ufuk sisi
            for (int y = fh - 14; y < fh; y++)
                for (int x = 0; x < fw; x++)
                    if (((uint)far.D[y * fw + x] >> 24) != 0 && Bayer(x, y) < (y - (fh - 14)) / 22f)
                        far.D[y * fw + x] = MixI(far.D[y * fw + x] & 0xFFFFFF, t.SkyLow, 0.5f);
            b.Far = far.Bmp();

            // --- yakın tepeler ---
            int nw = W + (int)((t.Width - W) * NearP) + 8, nh = Horizon + 4;
            var near = new Pix(nw, nh);
            int nearC = P.I(t.NearC), nearHi = MixI(t.NearC, 0x9DB3DD, 0.35f), nearD = MixI(t.NearC, 0x050716, 0.4f);
            if (t.TreeMode != 4)
            {
                for (int x = 0; x < nw; x++)
                {
                    float r = 11 + 5 * (float)Math.Sin(x * 0.030 + seed * 1.3) + 4 * (float)Math.Sin(x * 0.071 + seed * 0.4);
                    if (t.TreeMode == 3) r = 26 + 14 * (float)Math.Abs(Math.Sin(x * 0.024 + seed)) + 8 * (float)Math.Sin(x * 0.09 + seed) + (Fx.Hash(x / 2, seed + 2) - 0.5f) * 5;
                    if (t.TreeMode == 2) r = 5 + 2 * (float)Math.Sin(x * 0.02 + seed);
                    int top = nh - (int)r;
                    for (int y = top; y < nh; y++) near.D[y * nw + x] = (y - top) < (t.TreeMode == 3 ? 3 : 1) ? nearHi : nearC;
                    if (t.TreeMode <= 1 && Fx.Hash(x, seed + 21) < 0.16f)
                    {
                        int th = 4 + (int)(Fx.Hash(x, seed + 22) * 6);
                        for (int k = 0; k < th; k++)
                            for (int j = -(k / 2); j <= k / 2; j++) near.Set(x + j, top - th + k, nearD);
                    }
                }
            }
            b.Near = near.Bmp();

            // --- ağaç / kaya / bulut hattı ---
            int tw = W + (int)((t.Width - W) * TreeP) + 8, th2 = Horizon + 4;
            var tr = new Pix(tw, th2);
            int dark = P.I(0x0E2434), darkHi = P.I(0x4E6AA0);
            if (t.TreeMode == 0 || t.TreeMode == 1)
            {
                int x = 0, k = 0;
                while (x < tw)
                {
                    int h = t.TreeMode == 0 ? 14 + (int)(Fx.Hash(k, seed + 31) * 18) : 26 + (int)(Fx.Hash(k, seed + 31) * 34);
                    int half = h / 4 + 2;
                    int tiers = 3 + h / 16;
                    for (int ti = 0; ti < tiers; ti++)
                    {
                        float y0 = th2 - h + ti * (h * 0.8f / tiers), y1 = y0 + h * 0.9f / tiers + 3;
                        float hwid = half * (0.45f + 0.55f * (ti + 1) / tiers);
                        tr.Poly(dark, x, y0, x + hwid, y1, x - hwid, y1);
                        if (ti == 0 && h > 20) { tr.Set(x, (int)y0 + 1, darkHi); tr.Set(x - 1, (int)y0 + 2, darkHi); tr.Set(x, (int)y0 + 2, darkHi); }
                    }
                    tr.Rect(x, th2 - 3, 1, 3, dark);
                    if (t.TreeMode == 1 && k % 2 == 0)
                    {
                        int bx = x + 6, bh = 36 + (int)(Fx.Hash(k, seed + 33) * 34);
                        tr.Rect(bx, th2 - bh, 2, bh, P.I(0x7486B2));
                        for (int m = 0; m < 5; m++) tr.Set(bx, th2 - 6 - (int)(Fx.Hash(k * 5 + m, 3) * (bh - 8)), P.I(0x1A2440));
                        tr.Line(bx, th2 - bh + 8, bx - 5, th2 - bh, P.I(0x3C4A70));
                        tr.Line(bx + 1, th2 - bh + 14, bx + 6, th2 - bh + 4, P.I(0x3C4A70));
                    }
                    x += t.TreeMode == 0 ? 9 + (int)(Fx.Hash(k, seed + 32) * 12) : 7 + (int)(Fx.Hash(k, seed + 32) * 8);
                    k++;
                }
            }
            else if (t.TreeMode == 2)
            {
                tr.Rect(0, th2 - 5, tw, 5, P.I(0x232A64));
                tr.Rect(0, th2 - 6, tw, 1, P.I(0x6272B4));
                for (int k = 0; k < tw / 16; k++)
                {
                    int x = (int)(k * 16 + Fx.Hash(k, seed + 41) * 14), h = 5 + (int)(Fx.Hash(k, seed + 42) * 6);
                    if (Fx.Hash(k, seed + 43) < 0.45f) continue;
                    tr.Poly(P.I(0x161C4C), x, th2 - 5 - h, x + 3, th2 - 5, x - 3, th2 - 5);
                }
            }
            else if (t.TreeMode == 3)
            {
                int x = -10, k = 0;
                while (x < tw)
                {
                    int wd = 34 + (int)(Fx.Hash(k, seed + 51) * 40), h = 34 + (int)(Fx.Hash(k, seed + 52) * 58);
                    float peak = x + wd * (0.35f + Fx.Hash(k, seed + 53) * 0.3f);
                    tr.Poly(P.I(0x151A34), x, th2, peak - 4, th2 - h + 6, peak, th2 - h, peak + 5, th2 - h + 9, x + wd, th2);
                    tr.Poly(P.I(0x2C355C), x, th2, peak - 4, th2 - h + 6, peak, th2 - h, peak - 2, th2);
                    tr.Poly(P.I(0x9DB3DD), peak - 4, th2 - h + 6, peak, th2 - h, peak + 5, th2 - h + 9, peak + 1, th2 - h + 12, peak - 2, th2 - h + 9);
                    x += wd - 10 + (int)(Fx.Hash(k, seed + 54) * 30);
                    k++;
                }
            }
            else
            {
                // bulut denizi
                for (int layer = 0; layer < 3; layer++)
                {
                    int col = layer == 0 ? P.I(0x26366C) : (layer == 1 ? P.I(0x3A4C88) : P.I(0x566AA8));
                    for (int k = 0; k < tw / 28 + 2; k++)
                    {
                        float cx = k * 28 + Fx.Hash(k, seed + 60 + layer) * 26, cy = th2 - 20 + layer * 8 + Fx.Hash(k, seed + 70 + layer) * 6;
                        float rx = 24 + Fx.Hash(k, seed + 80 + layer) * 30, ry = 6 + Fx.Hash(k, seed + 90 + layer) * 6;
                        tr.Ellipse(cx, cy, rx, ry, col);
                    }
                }
                tr.Rect(0, th2 - 5, tw, 5, P.I(0x566AA8));
                for (int x = 0; x < tw; x++)
                    for (int y = 1; y < th2; y++)
                        if (((uint)tr.D[y * tw + x] >> 24) != 0 && ((uint)tr.D[(y - 1) * tw + x] >> 24) == 0) tr.D[y * tw + x] = P.I(0x93A8DA);
            }
            b.Trees = tr.Bmp();

            // --- zemin ---
            int gh = H - Horizon + 8;
            var gr = new Pix(t.Width, gh);
            int gA = t.Ground == 1 ? 0x5A90BC : (t.Ground == 2 ? 0x8496C6 : 0x96ACDC);
            int gB = t.Ground == 1 ? 0x2E5088 : (t.Ground == 2 ? 0x4C5C92 : 0x5C74AE);
            for (int x = 0; x < t.Width; x++)
            {
                int edge = 8 - (int)(3.5f + 2 * Math.Sin(x * 0.05 + seed) + 2 * Math.Sin(x * 0.013));
                for (int y = edge; y < gh; y++)
                {
                    float tt = (y - 8) / (float)(gh - 8);
                    float q = (float)Math.Floor(tt * 7 + Bayer(x, y) - 0.5f) / 7f;
                    gr.D[y * t.Width + x] = MixI(gA, gB, q);
                }
                // üst kenarda parlak kar çizgisi
                gr.Set(x, edge, P.I(0xD6E4F8)); gr.Set(x, edge + 1, P.I(0xB4C8EC));
            }
            gr.OnlyOpaque = true;
            if (t.Ground == 1)
            {
                // buz: kıyı karı, parlak çizgiler, çatlaklar
                for (int x = 0; x < t.Width; x++)
                {
                    int sh = 20 + (int)(3 * Math.Sin(x * 0.03) + 2 * Math.Sin(x * 0.11));
                    for (int y = 0; y < sh; y++) if (y < sh - 2 || Bayer(x, y) < 0.5f) gr.Set(x, y, MixI(0xB4C8EC, 0x8FA6D6, y / 22f));
                }
                for (int k = 0; k < t.Width / 9; k++)
                {
                    int x = (int)(Fx.Hash(k, seed + 100) * t.Width), y = 26 + (int)(Fx.Hash(k, seed + 101) * (gh - 30)), len = 8 + (int)(Fx.Hash(k, seed + 102) * 30);
                    gr.Rect(x, y, len, 1, P.I(0x86BCE0));
                    if (k % 3 == 0) gr.Rect(x + 3, y + 1, len / 2, 1, P.I(0x3C669C));
                }
                for (int k = 0; k < t.Width / 60; k++)
                {
                    int x = (int)(Fx.Hash(k, seed + 110) * t.Width), y = 30 + (int)(Fx.Hash(k, seed + 111) * (gh - 40));
                    for (int s = 0; s < 22; s++)
                    {
                        gr.Set(x, y, P.I(0xB8E4F6));
                        x += 1 + (int)(Fx.Hash(k * 31 + s, 7) * 2);
                        y += (int)(Fx.Hash(k * 31 + s, 8) * 3) - 1;
                    }
                }
                for (int k = 0; k < t.Width / 70; k++)
                    gr.Ellipse(Fx.Hash(k, seed + 120) * t.Width, 34 + Fx.Hash(k, seed + 121) * (gh - 44), 12 + Fx.Hash(k, seed + 122) * 16, 2.5f + Fx.Hash(k, seed + 123) * 2, P.I(0xA9C0E6));
            }
            else
            {
                // kar: tümsekler, gölgeler ve çiğnenmiş yol
                for (int k = 0; k < t.Width / 26; k++)
                {
                    float cx = Fx.Hash(k, seed + 130) * t.Width, cy = 12 + Fx.Hash(k, seed + 131) * (gh - 16);
                    float rx = 14 + Fx.Hash(k, seed + 132) * 26, ry = 2 + Fx.Hash(k, seed + 133) * 3;
                    float tt = cy / gh;
                    gr.Ellipse(cx, cy + ry * 0.9f, rx, ry, MixI(gA, gB, tt + 0.22f));
                    gr.Ellipse(cx, cy, rx, ry, MixI(MixI(gA, gB, tt), 0xE4EEFC, 0.38f));
                }
                for (int x = 0; x < t.Width; x++)
                {
                    float yc = 62 + 10 * (float)Math.Sin(x * 0.008 + seed) + 5 * (float)Math.Sin(x * 0.021);
                    float half = 13 + 3 * (float)Math.Sin(x * 0.05);
                    for (int y = (int)(yc - half); y <= (int)(yc + half); y++)
                    {
                        float e = Math.Abs(y - yc) / half;
                        if (e < 0.7f || Bayer(x, y) > (e - 0.7f) / 0.3f)
                        {
                            int cur = gr.Get(x, y) & 0xFFFFFF;
                            gr.Set(x, y, MixI(cur, 0x3E4E86, 0.20f));
                        }
                    }
                }
                if (t.Ground == 2)
                {
                    // kayalık: karın arasından çıkan taşlar ve rüzgâr izleri
                    for (int k = 0; k < t.Width / 34; k++)
                    {
                        float cx = Fx.Hash(k, seed + 140) * t.Width;
                        bool topSide = Fx.Hash(k, seed + 141) < 0.55f;
                        float cy = topSide ? 12 + Fx.Hash(k, seed + 142) * 16 : gh - 4 - Fx.Hash(k, seed + 142) * 14;
                        float rx = 8 + Fx.Hash(k, seed + 143) * 14, ry = 2.5f + Fx.Hash(k, seed + 144) * 3;
                        gr.Ellipse(cx, cy, rx, ry, P.I(0x2E3558));
                        gr.Ellipse(cx - 1, cy - 1, rx - 2, ry - 1, P.I(0x485080));
                        gr.Ellipse(cx - 2, cy - ry * 0.6f, rx * 0.6f, 1.2f, P.I(0xCBDCF5));
                    }
                    for (int k = 0; k < t.Width / 14; k++)
                    {
                        int x = (int)(Fx.Hash(k, seed + 150) * t.Width), y = 10 + (int)(Fx.Hash(k, seed + 151) * (gh - 12));
                        gr.Rect(x, y, 6 + (int)(Fx.Hash(k, seed + 152) * 14), 1, P.I(0xB4C8EC));
                    }
                }
            }
            // kar pırıltısı tanecikleri
            for (int k = 0; k < t.Width * 2; k++)
            {
                int x = (int)(Fx.Hash(k, seed + 160) * t.Width), y = 10 + (int)(Fx.Hash(k, seed + 161) * (gh - 10));
                gr.Blend(x, y, 0xFFFFFF, 0.28f);
            }
            gr.OnlyOpaque = false;
            b.Ground = gr.Bmp();

            bdCache[idx] = b;
            return b;
        }

        const int AW = W / 4, AH = 72, AY = 6;
        readonly Bitmap auroraBmp = new Bitmap(AW, AH, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        readonly int[] auroraBuf = new int[AW * AH];

        Color DawnCol(int kind, int which)
        {
            int[][] cols =
            {
                new[] { 0x2B4A8C, 0xF0A08C, 0xFFE2A8, 0xFFD9A0 },
                new[] { 0x3A4468, 0x9C9AB0, 0xD8CFC8, 0xC8C8D8 },
                new[] { 0x501828, 0xD8482E, 0xFFB040, 0xFF7040 }
            };
            return P.C(cols[kind][which]);
        }

        void DrawBackdrop(Graphics g, float cam)
        {
            g.DrawImage(bd.Sky, 0, 0, bd.Sky.Width, bd.Sky.Height);
            float night = 1f - dawn;

            if (dawn > 0)
            {
                int a = (int)(dawn * 255);
                for (int y = 0; y < Horizon + 12; y += 4)
                {
                    float t = y / (float)(Horizon + 12);
                    Color c = t < 0.55f ? Fx.Mix(DawnCol(dawnKind, 0), DawnCol(dawnKind, 1), t / 0.55f) : Fx.Mix(DawnCol(dawnKind, 1), DawnCol(dawnKind, 2), (t - 0.55f) / 0.45f);
                    g.FillRectangle(Fx.B(a, c), 0, y, W, 4);
                }
            }

            // yıldızlar
            if (night > 0.05f)
                for (int i = 0; i < theme.Stars; i++)
                {
                    float sx = Fx.Hash(i, 901) * (W + 40) - cam * 0.02f, sy = Fx.Hash(i, 902) * (Horizon - 22);
                    sx = ((sx % W) + W) % W;
                    float tw = 0.5f + 0.5f * (float)Math.Sin(time * (1.2f + Fx.Hash(i, 903) * 2.5f) + i);
                    int a = (int)((70 + 150 * tw) * night) / 24 * 24;
                    if (a <= 0) continue;
                    g.FillRectangle(Fx.B(a, P.Snow0), (int)sx, (int)sy, 1, 1);
                    if (i % 9 == 0 && tw > 0.7f)
                    {
                        g.FillRectangle(Fx.B(a / 2, P.Snow0), (int)sx - 1, (int)sy, 3, 1);
                        g.FillRectangle(Fx.B(a / 2, P.Snow0), (int)sx, (int)sy - 1, 1, 3);
                    }
                }

            // hilal
            if (night > 0.3f)
            {
                int mx = 300 - (int)(cam * 0.03f), my = area == 2 ? 26 : 16;
                g.FillEllipse(Fx.B((int)(12 * night), P.Snow1), mx - 8, my - 8, 38, 38);
                g.DrawImage(moonBmp, mx, my, 22, 22);
            }

            // kuzey ışıkları
            float aur = theme.Aurora * night;
            if (aur > 0.02f)
            {
                // perde perde inen ışık: 4 piksellik sütunlar, tek bitmap'e yazılıp gerilir
                Array.Clear(auroraBuf, 0, auroraBuf.Length);
                for (int cx = 0; cx < AW; cx++)
                {
                    float wx = cx * 4 + cam * 0.05f;
                    float t = (((wx / 34f) % 12f) + 12f) % 12f / 11f;
                    Color col = t < 0.5f ? Fx.Mix(P.C(0x50FFAA), P.C(0x5ADCFF), t * 2f) : Fx.Mix(P.C(0x5ADCFF), P.C(0xBE78FF), (t - 0.5f) * 2f);
                    float wave = (float)(Math.Sin(wx * 0.03 + time * 0.55) * 9 + Math.Sin(wx * 0.071 - time * 0.8) * 5);
                    float top = 34 + wave - 9 - AY;
                    float shimmer = 0.6f + 0.4f * (float)Math.Sin(wx * 0.05 + time * 1.1) * (float)Math.Sin(wx * 0.013 - time * 0.3);
                    float peak = aur * 78 * shimmer;
                    for (int k = 0; k < 34; k++)
                    {
                        int y = (int)top + k;
                        if (y < 0 || y >= AH) continue;
                        float fall = k < 7 ? (k + 1) / 7f : 1f - (k - 7) / 27f;
                        int a = (int)(peak * fall) / 8 * 8;
                        if (a <= 0) continue;
                        if (a > 255) a = 255;
                        auroraBuf[y * AW + cx] = (a << 24) | ((col.R * a / 255) << 16) | ((col.G * a / 255) << 8) | (col.B * a / 255);
                    }
                }
                var ad = auroraBmp.LockBits(new Rectangle(0, 0, AW, AH), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                System.Runtime.InteropServices.Marshal.Copy(auroraBuf, 0, ad.Scan0, auroraBuf.Length);
                auroraBmp.UnlockBits(ad);
                g.DrawImage(auroraBmp, new Rectangle(0, AY, W, AH), 0, 0, AW, AH, GraphicsUnit.Pixel);
            }

            // doğan güneş
            if (dawn > 0)
            {
                float sx = 232 - cam * 0.03f, sy = Horizon + 22 - Fx.Smooth(dawn) * 66;
                float r = dawnKind == 2 ? 22 : (dawnKind == 1 ? 12 : 16);
                Color core = dawnKind == 2 ? P.C(0xFF5A30) : (dawnKind == 1 ? P.C(0xE8E4E0) : P.Fire0);
                Color halo = dawnKind == 2 ? P.C(0xFF3020) : (dawnKind == 1 ? P.C(0xC8C8D8) : P.Fire1);
                for (int i = 4; i >= 1; i--)
                {
                    float rr = r + i * (dawnKind == 1 ? 5 : 9) + (float)Math.Sin(time * 2 + i) * 1.5f;
                    g.FillEllipse(Fx.B((int)(26 * dawn), halo), sx - rr, sy - rr, rr * 2, rr * 2);
                }
                if (dawnKind == 0)
                    for (int k = 0; k < 12; k++)
                    {
                        double an = k * Math.PI / 6 + time * 0.15;
                        float r0 = r + 5, r1 = r + 22 + (k % 2) * 10;
                        g.FillPolygon(Fx.B((int)(60 * dawn), P.Fire1), new[]
                        {
                            new PointF(sx + (float)Math.Cos(an - 0.05) * r0, sy + (float)Math.Sin(an - 0.05) * r0),
                            new PointF(sx + (float)Math.Cos(an) * r1, sy + (float)Math.Sin(an) * r1),
                            new PointF(sx + (float)Math.Cos(an + 0.05) * r0, sy + (float)Math.Sin(an + 0.05) * r0)
                        });
                    }
                g.FillEllipse(Fx.B(halo), sx - r, sy - r, r * 2, r * 2);
                g.FillEllipse(Fx.B(core), sx - r + 2, sy - r + 2, r * 2 - 4, r * 2 - 4);
            }

            int fx = -(int)(cam * FarP);
            g.DrawImage(bd.Far, fx, 0, bd.Far.Width, bd.Far.Height);
            if (theme.AkH > 0)
            {
                // zirvedeki Gün Ocağı'nın uzaktan görünen kıvılcımı
                float tw = 0.5f + 0.5f * (float)Math.Sin(time * 2.2f);
                if (gunLit || dawn > 0) g.FillEllipse(Fx.B(120, P.Fire1), fx + bd.AkX - 3, bd.AkY - 4, 7, 7);
                else if (tw > 0.75f) g.FillRectangle(Fx.B(110, P.Ice), fx + bd.AkX, bd.AkY - 3, 1, 1);
            }
            g.DrawImage(bd.Near, -(int)(cam * NearP), 0, bd.Near.Width, bd.Near.Height);
            g.DrawImage(bd.Trees, -(int)(cam * TreeP), 0, bd.Trees.Width, bd.Trees.Height);
            if (dawn > 0) g.FillRectangle(Fx.B((int)(dawn * 60), DawnCol(dawnKind, 3)), 0, 60, W, Horizon - 56);

            int gh = bd.Ground.Height;
            g.DrawImage(bd.Ground, new Rectangle(0, Horizon - 8, W, gh), (int)cam, 0, W, gh, GraphicsUnit.Pixel);

            // göl: gökyüzünün buzdaki yansıması
            if (theme.Ground == 1 && night > 0.1f)
                for (int x = 0; x < W; x += 6)
                {
                    float wx = x + cam * 0.6f;
                    float t = (((wx / 40f) % 12f) + 12f) % 12f / 11f;
                    Color col = t < 0.5f ? Fx.Mix(P.C(0x50FFAA), P.C(0x5ADCFF), t * 2f) : Fx.Mix(P.C(0x5ADCFF), P.C(0xBE78FF), (t - 0.5f) * 2f);
                    int a = (int)(14 * night * (0.6f + 0.4f * Math.Sin(wx * 0.04 + time * 0.6)));
                    if (a > 2) g.FillRectangle(Fx.B(a, col), x, Horizon + 26, 6, 60);
                }

            // kar pırıltıları
            if (theme.Ground != 1)
                for (int i = 0; i < 46; i++)
                {
                    float wx = Fx.Hash(i, 911) * areaW, wy = Horizon + 6 + Fx.Hash(i, 912) * (H - Horizon - 8);
                    float sx = wx - cam;
                    if (sx < 0 || sx >= W) continue;
                    if (Math.Sin(time * 3 + i * 1.7f) > 0.82) g.FillRectangle(Fx.B(230, P.Snow0), (int)sx, (int)wy, 1, 1);
                }
        }

        // =====================================================
        //  IŞIK
        // =====================================================
        struct Light { public float X, Y, R; }
        readonly List<Light> lights = new List<Light>();

        // Gece karanlığı ve sıcak ışık tek geçişte: yarı çözünürlüklü, noktalı (dither) bir ışık haritası.
        // Işıkların çevresi basamak basamak aydınlanır; gökyüzüne dokunulmaz.
        const int LW = W / 2, LH = H / 2;
        readonly Bitmap lightBmp = new Bitmap(LW, LH, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        readonly int[] lightBuf = new int[LW * LH];

        void DrawLighting(Graphics g, float night)
        {
            if (night <= 0.02f && lights.Count == 0) return;
            const int levels = 7;
            float maxDark = 0.60f * night, maxWarm = 0.26f * (0.45f + 0.55f * night);
            int n = lights.Count;
            for (int by = 0; by < LH; by++)
            {
                float y = by * 2 + 1;
                float skyFade = Fx.Clamp((y - 48) / 30f, 0, 1);
                for (int bx = 0; bx < LW; bx++)
                {
                    float x = bx * 2 + 1;
                    float light = 0;
                    for (int i = 0; i < n; i++)
                    {
                        Light l = lights[i];
                        float rr = l.R + 36;
                        float dx = (x - l.X) / rr, dy = (y - l.Y) / (rr * 0.8f);
                        float d2 = dx * dx + dy * dy;
                        if (d2 >= 1) continue;
                        float v = (1f - (float)Math.Sqrt(d2)) * rr / 44f;
                        if (v > 1) v = 1;
                        light = light + v - light * v;
                    }
                    // ışığı basamaklara böl (dither ile yumuşat)
                    int lv = (int)Math.Floor(light * levels + Bayer(bx, by) - 0.5f);
                    if (lv < 0) lv = 0; else if (lv > levels) lv = levels;
                    float lq = lv / (float)levels;

                    float ad = maxDark * (1f - lq) * skyFade;
                    float aw = maxWarm * lq * lq;
                    float a = 1f - (1f - ad) * (1f - aw);
                    if (a <= 0.004f) { lightBuf[by * LW + bx] = 0; continue; }
                    // önce karanlık, üstüne sıcak ışık (önceden çarpılmış alfa)
                    float kd = ad * (1f - aw);
                    int r = (int)((255 * aw + 5 * kd) * 255f / 255f), gg = (int)(150 * aw + 7 * kd), b = (int)(64 * aw + 22 * kd);
                    lightBuf[by * LW + bx] = ((int)(a * 255) << 24) | (r << 16) | (gg << 8) | b;
                }
            }
            var data = lightBmp.LockBits(new Rectangle(0, 0, LW, LH), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            System.Runtime.InteropServices.Marshal.Copy(lightBuf, 0, data.Scan0, lightBuf.Length);
            lightBmp.UnlockBits(data);
            g.DrawImage(lightBmp, new Rectangle(0, 0, W, H), 0, 0, LW, LH, GraphicsUnit.Pixel);
        }

        // =====================================================
        //  NESNE ÇİZİMLERİ
        // =====================================================
        void DrawShadow(Graphics g, float x, float y, float rx, float ry = 2.4f)
        {
            g.FillEllipse(Fx.B(70, P.Night1), x - rx, y - ry, rx * 2, ry * 2);
        }

        void DrawFire(Graphics g, float x, float y, float s)
        {
            float f1 = (float)Math.Sin(time * 13 + x), f2 = (float)Math.Sin(time * 17 + 1 + x), f3 = (float)Math.Sin(time * 11 + 2 + x);
            float h = 15 * s, w = 7 * s;
            g.FillPolygon(Fx.B(P.Fire3), new[]
            {
                new PointF(x - w, y), new PointF(x - w * 0.8f, y - h * 0.5f), new PointF(x - w * 0.4f, y - h * (0.8f + 0.12f * f1)),
                new PointF(x - w * 0.12f, y - h * 0.55f), new PointF(x + w * 0.12f, y - h * (1.12f + 0.14f * f2)), new PointF(x + w * 0.42f, y - h * 0.62f),
                new PointF(x + w * 0.66f, y - h * (0.86f + 0.1f * f3)), new PointF(x + w * 0.9f, y - h * 0.42f), new PointF(x + w, y)
            });
            g.FillPolygon(Fx.B(P.Fire2), new[]
            {
                new PointF(x - w * 0.72f, y), new PointF(x - w * 0.45f, y - h * (0.55f + 0.1f * f3)), new PointF(x - w * 0.1f, y - h * 0.4f),
                new PointF(x + w * 0.1f, y - h * (0.86f + 0.1f * f1)), new PointF(x + w * 0.45f, y - h * 0.45f), new PointF(x + w * 0.72f, y)
            });
            g.FillPolygon(Fx.B(P.Fire1), new[]
            {
                new PointF(x - w * 0.45f, y), new PointF(x - w * 0.2f, y - h * 0.4f), new PointF(x, y - h * (0.62f + 0.08f * f2)),
                new PointF(x + w * 0.25f, y - h * 0.36f), new PointF(x + w * 0.45f, y)
            });
            g.FillPolygon(Fx.B(P.Fire0), new[] { new PointF(x - w * 0.22f, y), new PointF(x, y - h * 0.3f), new PointF(x + w * 0.22f, y) });
            for (int i = 0; i < 5; i++)
            {
                float t = (time * 0.8f + i * 0.37f) % 1f;
                float ex = x + (float)Math.Sin(i * 3 + time * 2) * 5 * s * t, ey = y - h * 0.6f - t * 22 * s;
                int a = (int)(255 * (1f - t)) / 32 * 32;
                if (a > 0) g.FillRectangle(Fx.B(a, P.Fire1), (int)ex, (int)ey, 1, 1);
            }
        }

        // Dallara bağlı, rüzgârda uçuşan çaputlar
        void DrawRibbons(Graphics g, float x, float y, int[][] pts, bool mine)
        {
            int[] cols = { 0xB8A07A, 0x8CA0B8, 0xA890A8, 0x9AA88A, 0xB89088 };
            for (int i = 0; i < pts.Length; i++)
            {
                float fl = (float)Math.Sin(time * (3 + theme.Wind * 0.08f) + i * 1.7f);
                int rx = (int)(x + pts[i][0]), ry = (int)(y + pts[i][1]);
                Color c = P.C(cols[i % cols.Length]);
                if (mine && i == pts.Length - 1) c = P.Red;
                g.FillRectangle(Fx.B(c), rx, ry, 2, 1);
                g.FillRectangle(Fx.B(c), rx + 1 + (fl > 0 ? 1 : 0), ry + 1, 2, 1);
                g.FillRectangle(Fx.B(c), rx + 2 + (fl > 0.3f ? 1 : 0) + (fl > -0.3f ? 1 : 0), ry + 2, 2, 1);
            }
        }

        void DrawLantern(Graphics g, float lx, float ly)
        {
            int x = (int)Math.Round(lx), y = (int)Math.Round(ly);
            g.FillRectangle(Fx.B(P.Ink), x - 2, y - 2, 5, 9);
            g.FillRectangle(Fx.B(P.Bronze), x - 1, y - 2, 3, 1);
            g.FillRectangle(Fx.B(P.Fire2), x - 1, y - 1, 3, 6);
            g.FillRectangle(Fx.B(P.Fire1), x - 1, y, 3, 4);
            g.FillRectangle(Fx.B(P.Fire0), x, y + 1 + (int)(time * 8) % 2, 1, 2);
            g.FillRectangle(Fx.B(P.Bronze), x - 1, y + 5, 3, 1);
            g.FillRectangle(Fx.B(P.Ink), x, y - 4, 1, 2);
        }

        void DrawPlayer(Graphics g, float x, float y)
        {
            bool step = moving && ((int)(walkT * 8) % 2 == 1);
            int bob = step ? -1 : 0;
            DrawShadow(g, x, y, 7);
            // atkının uçuşan ucu
            int sx = (int)x - facing * 5, sy = (int)y - 11 + bob;
            float fl = (float)Math.Sin(time * 7 + x * 0.1f);
            for (int i = 0; i < 4; i++)
            {
                int yy = sy + (int)Math.Round(Math.Sin(time * 7 + i * 0.9f) * (moving ? 1.2f : 0.6f)) + (i > 2 ? 1 : 0);
                g.FillRectangle(Fx.B(i < 3 ? P.Amber : P.AmberD), sx - facing * (i + 1) - (facing > 0 ? 0 : -1) - 1, yy, 2, 2);
            }
            Spr s = facing > 0 ? (step ? Art.NarWalk : Art.NarStand) : (step ? Art.NarWalkL : Art.NarStandL);
            s.Draw(g, x, y + bob);
            if (hasLantern)
            {
                float lx = x + facing * 9, ly = y - 9 + bob + (moving ? (float)Math.Sin(walkT * 8) * 0.8f : 0);
                DrawLantern(g, lx, ly);
            }
        }

        void DrawSpirit(Graphics g, int id, float x, float y, bool warm, int scale, float shiver)
        {
            Spr[] set = Art.Spirits[id];
            Spr s = set[warm ? 1 : 0];
            float bob = (float)Math.Sin(time * 2.2f + id) * (scale == 1 ? 0.6f : 1.2f);
            float sh = shiver > 0 ? (float)Math.Sin(time * 40) * shiver : 0;
            if (scale == 1) DrawShadow(g, x, y, s.W * 0.4f, 3);
            s.Draw(g, x + sh, y + (id == 1 ? 0 : bob), scale);
        }

        // =====================================================
        //  BÖLGELERİ KURMA
        // =====================================================
        Prop Add(Spr s, float x, float y, float r = 0)
        {
            var p = new Prop { S = s, X = x, Y = y, R = r };
            props.Add(p);
            return p;
        }

        Prop Talk(Prop p, float ir, float promptH, params string[] lines)
        {
            if (!descent) p.Use = () => Say(null, lines); // gece sözleri dönüş yolunda söylenmez
            p.IR = ir; p.PromptH = promptH;
            return p;
        }

        // Dönüş yolunda söylenecek söz; gün nasıl doğduysa ona göre
        Prop Dawn(Prop p, string good, string pale, string burnt)
        {
            if (!descent) return p;
            string s = dawnKind == 0 ? good : (dawnKind == 1 ? pale : burnt);
            p.Use = () => Say(null, s);
            return p;
        }

        static readonly string[][] dawnFriend =
        {
            new[]
            {
                "Karakoncolos|Soru: Senden önce buraya nasıl indim? ...Cevap: kara bir kestirmeden. Yerini söylemem.",
                "Karakoncolos|Gölgem geri geldi! Bak, bak! Ben kolumu kaldırınca o da kaldırıyor. Usul biliyor."
            },
            new[]
            {
                "Turna|Haberi verdim. Ninen pencerede. Elini alnına siper etmiş, dağa bakıyor.",
                "Turna|Buz çözülüyor. Sürüm baharda bu gölden geçer. Bu sefer onları ben karşılarım."
            },
            new[]
            {
                "Börü|İşte geçit. Buradan aşağısı senin yolun, küçük ateş.",
                "Börü|Gün ışığında geçit küçük görünüyor. Gece bana koca bir dünya gibi gelmişti."
            },
            null,
            new[]
            {
                "Çarşamba Karısı|Geldin mi? Hatçe'nin camlarını sildim. Yusuf'un eşiğini süpürdüm. Dönerlerse yarım bulmasınlar.",
                "Çarşamba Karısı|Ninen ocağın başında. Yürü, bekletme. Bekletmek de yarım iştir."
            },
            new[]
            {
                "Şahmeran|Yılanlarım güneşleniyor. Bin tanesi birden. Sazlıktan geçerken ayağına dikkat et.",
                "Şahmeran|Sırrımı tuttun, gün doğdu. İkisi de senin yüzünden."
            },
            new[]
            {
                "Tepegöz|Gün doğdu. Saydım: bir güneş. Eksik yok.",
                "Tepegöz|Taşların gölgesi var artık. Gölgeleri de saydım. Seksen dört etti."
            },
            null
        };
        static readonly string[][] dawnAsh =
        {
            new[] { "Gün ışığında is, karın üstünde kapkara duruyor.", "Soracak sorusu vardı. Soramadı." },
            new[] { "Buz çözülmüş. Su birikintisindeki tüy kıyıya vurmuş." },
            new[] { "Geçitte gün ışığı var. Bekleyen yok." },
            null,
            new[] { "Yanık iğ, erimiş karın suyunda yüzüyor." },
            new[] { "Taç, sazların arasında parlıyor. Gün ışığı onu buldu; sahibini bulamadı." },
            new[] { "Kararmış kaya, gün ışığında da kara.", "Kırk iki taş. Gölgeleri uzamış." },
            null
        };

        void BuildArea(int a)
        {
            area = a;
            theme = themes[a];
            areaW = theme.Width;
            bd = GetBackdrop(a);
            props.Clear();
            spiritProp = null;
            areaSpirits.Clear();
            gunLit = false;
            showPlayer = true;

            switch (a)
            {
                case 0: BuildVillage(); break;
                case 1: BuildForest(); break;
                case 2: BuildLake(); break;
                case 3: BuildPass(); break;
                case 4: BuildSummit(); break;
            }
        }

        void AddCairn(float x, float y, params string[] firstLines)
        {
            string key = "cairn" + area;
            Prop c = Add(Art.Cairn, x, y, 12);
            c.Draw = (g, sx, sy) =>
            {
                Art.Cairn.Draw(g, sx, sy);
                if (seen.Contains(key) && !(descent && dawnKind == 2)) DrawFire(g, sx, sy - 5, 0.7f);
            };
            c.LightR = seen.Contains(key) ? 34 : 0;
            c.LightDY = -8;
            c.IR = 28; c.PromptH = 24;
            c.Use = () =>
            {
                if (descent)
                {
                    Say(null, dawnKind == 0 ? "Yol ocağı hâlâ yanıyor. Gün ışığında alevi görünmüyor; sıcağı duruyor."
                        : (dawnKind == 1 ? "Yol ocağı yanıyor. Başında ısınan yok." : "Yol ocağı sönmüş. Bu sıcakta ateşe gerek kalmamış."));
                    return;
                }
                bool first = seen.Add(key);
                c.LightR = 34;
                kor = korMax;
                Sfx("save", 0.8f);
                Save();
                if (first)
                {
                    var l = new List<string>(firstLines);
                    l.Add("Kor'un tazelendi. Yolculuk kaydedildi.");
                    Say(null, l.ToArray());
                }
                else Say(null, "Ateşin başında biraz soluklandın. Kor'un tazelendi. Yolculuk kaydedildi.");
            };
        }

        void AddSpirit(int id, float x, float y, string[] ashLines, string[] friendLines)
        {
            spX[id] = x; areaSpirits.Add(id);
            if (descent)
            {
                if (dawnAsh[id] != null) ashLines = dawnAsh[id];
                if (dawnFriend[id] != null) friendLines = dawnFriend[id];
            }
            if (fate[id] == 2)
            {
                Prop ash = Add(Art.Ash, x, y);
                ash.Flat = true;
                Talk(ash, 22, 16, ashLines);
                return;
            }
            bool friend = fate[id] == 1;
            Prop p = Add(null, x, friend ? BandTop + 4 : y, friend ? 12 : 14);
            p.Draw = (g, sx, sy) => DrawSpirit(g, id, sx, sy, fate[id] == 1, 1, fate[id] == 1 ? 0 : 0.5f);
            p.PromptH = id == 0 || id == 4 ? 54 : (id == 1 || id == 7 ? 60 : (id == 6 ? 64 : 50));
            p.IR = 30;
            if (friend)
            {
                int n = 0;
                p.Use = () => { Say(null, friendLines[n % friendLines.Length]); n++; };
            }
            spiritProp = p;
        }

        void Decor(Spr s, float x, float y) { Add(s, x, y); }

        void BuildVillage()
        {
            Decor(Art.PineL, 38, 130); Decor(Art.PineM, 82, 126); Decor(Art.PineM, 262, 124); Decor(Art.PineS, 505, 127);
            Decor(Art.PineM, 884, 128); Decor(Art.PineL, 968, 127); Decor(Art.PineS, 660, 126);

            Talk(Add(Art.House1, 150, 131), 34, 66,
                "Demirci Yusuf'un evi. Kapıda paslı bir kilit.",
                "Örsün üstünde kar birikmiş. Dokuz kış önce şehre göçtüler.");
            Decor(Art.Fence, 226, 133); Decor(Art.Fence, 262, 133);

            Prop home = Talk(Add(Art.HouseLit, 330, 131), 34, 66,
                "Sizin ev. Pencerede Nine'nin yaktığı gaz lambası.",
                "İçeriden tarhana kokusu geliyor. Dönünce, diyorsun. Dönünce.");
            home.LightR = 30; home.LightDY = -16;

            Talk(Add(Art.School, 590, 131), 40, 62,
                "Köy okulu. Kapısı açık kalmış; içeri kar dolmuş.",
                "Tahtada yarım kalmış bir toplama işlemi var. Son öğrencisi sendin.");

            Dawn(Talk(Add(Art.House2, 806, 131), 34, 66,
                "Hatçe Teyze'nin evi. Pencerede kurumuş bir fesleğen saksısı.",
                "\"Bahara döneriz\" demişti. Üç bahar oldu."),
                "Hatçe Teyze'nin penceresi. Fesleğen saksısına gün vurmuş. Kuru dalın ucunda yeşil bir şey var. Belki.",
                "Hatçe Teyze'nin penceresi. Fesleğen saksısına gün vurmuş.",
                "Fesleğen saksısı çatlamış. Güneş bu sabah sert.");

            Dawn(Talk(Add(Art.Sign, 866, 141, 5), 24, 34,
                "Tabela: SÖNMEZ KÖYÜ. Altında nüfus yazıyor.",
                "214 çizilmiş. 96 çizilmiş. 31 çizilmiş. En altta, tebeşirle: 2."),
                "Tabela: SÖNMEZ KÖYÜ. Nüfus: 2. Dibinde bir tebeşir duruyor. Seneye lazım olacak.",
                "Tabela: SÖNMEZ KÖYÜ. Nüfus: 2. Gün ışığında da 2.",
                "Tabela: SÖNMEZ KÖYÜ. Nüfus: 2. Dağda sayılacak kimse kalmadı.");

            // köy ocağı
            Prop hearth = Add(Art.Hearth, 420, 178, 21);
            hearth.Draw = (g, sx, sy) => { Art.Hearth.Draw(g, sx, sy); DrawFire(g, sx, sy - 5, 0.75f); };
            hearth.LightR = 58; hearth.LightDY = -10;
            hearth.IR = 34; hearth.PromptH = 34;
            hearth.Use = () =>
            {
                kor = korMax;
                Sfx("save", 0.8f);
                Save();
                Say(null, "Sönmez'in ocağı. Kırk kuşaktır yanıyor; alevi bir çocuk avucu kadar kalmış.", "Kor'un tazelendi. Yolculuk kaydedildi.");
            };

            // Nine
            Prop nine = Add(Art.Nine, 454, 172, 7);
            nine.Draw = (g, sx, sy) =>
            {
                DrawShadow(g, sx, sy, 7);
                Art.Nine.Draw(g, sx, sy + (float)Math.Sin(time * 1.6f) * 0.5f);
                Fx.Line(g, P.C(0x6B4A36), (int)sx + 8, (int)sy - 9, (int)sx + 9, (int)sy - 1); // baston
            };
            nine.IR = 26; nine.PromptH = 34;
            int nineN = 0;
            string[] nineLines =
            {
                "Nine|Yol doğuya gider: önce kayın ormanı, sonra göl, sonra geçit. En son zirve.",
                "Nine|Üşüyene ateş tutulur Nar. Ama ateşi tutanın da eli üşür. Bunu unutma.",
                "Nine|Ben buradayım, ocağın başında. Sen dönene kadar sönmeyecek. Söz."
            };
            nine.Use = () => { Say(null, nineLines[nineN % nineLines.Length]); nineN++; };

            // Nardugan (dilek) ağacı
            int[][] rib = { new[] { -12, -22 }, new[] { 8, -30 }, new[] { -6, -40 }, new[] { 11, -16 }, new[] { -14, -10 }, new[] { 3, -50 }, new[] { 9, -8 } };
            Prop tree = Add(Art.WishTree, 706, 150, 7);
            tree.Draw = (g, sx, sy) => { Art.WishTree.Draw(g, sx, sy); DrawRibbons(g, sx, sy, rib, wish >= 0); };
            tree.IR = 26; tree.PromptH = 70;
            tree.Use = () =>
            {
                if (descent)
                {
                    Say(null, wish < 0 ? "Nardugan ağacı. Çaputlar gün ışığında kıpırdıyor. Aralarında seninki yok."
                        : (dawnKind == 2 ? "Çaputun dalda kavrulmuş. Rengi artık ötekilerle aynı." : "Çaputun hâlâ dalda. Gün ışığında daha da kırmızı."));
                    return;
                }
                if (wish >= 0) { Say(null, "Senin çaputun ötekilerin arasında sallanıyor. En kırmızısı o."); return; }
                Say(() => Ask("Bir dilek tutup bağlar mısın?",
                        new[] { "Nine'nin dizleri iyileşsin.", "Gidenler geri dönsün.", "Gün doğsun, o yeter." },
                        i => { wish = i; Sfx("warm", 0.7f); Say(null, "Çaputu en alçak dala bağladın. Rüzgâr onu hemen sahiplendi."); }),
                    "Nardugan ağacı. Dallarında solmuş çaputlar var; her biri eski bir dilek.",
                    "Cebinde kırmızı bir bez parçası buldun.");
            };

            AddSpirit(4, 962, 176,
                new[] { "Karda yanık bir iğ ve bir tutam kül.", "Köyün son evinin kapısı rüzgârda çarpıyor. Kapatan yok." },
                new[]
                {
                    "Çarşamba Karısı|Bugün çarşamba. Ben öyle karar verdim. Yarın da çarşamba.",
                    "Çarşamba Karısı|Hatçe'nin fesleğenine su verdim. Kurumuş ama olsun; iş iştir.",
                    "Çarşamba Karısı|Hâlâ burada mısın? Yarım iş! Yürü!"
                });

            Decor(Art.Bush, 120, 213); Decor(Art.Mound, 560, 215); Decor(Art.Bush, 862, 214); Decor(Art.Rock, 300, 216); Decor(Art.Mound, 740, 212);
        }

        void BuildForest()
        {
            for (int i = 0; i < 24; i++)
            {
                float x = 20 + i * 55 + Fx.Hash(i, 201) * 30, y = 118 + Fx.Hash(i, 202) * 13;
                float h = Fx.Hash(i, 203);
                Decor(h < 0.4f ? Art.Birch : (h < 0.7f ? Art.BirchB : (h < 0.85f ? Art.PineM : Art.PineL)), x, y);
            }
            Add(Art.Birch, 330, 152, 4); Add(Art.BirchB, 770, 150, 4); Add(Art.Birch, 890, 196, 4); Add(Art.BirchB, 248, 198, 4);
            Add(Art.Bush, 520, 196, 5); Add(Art.Rock, 1150, 150, 7);

            AddCairn(150, 162,
                "Bir yol ocağı. Taşların arasında eski kül var.",
                "Feneri yaklaştırdın. Kül koru tanıdı; yeniden tutuştu.",
                "Ateşe bakarken Nine'nin sesini duydun:",
                "Nine|Ateş paylaşınca azalmaz yavrum. Azalır sanırsın.");

            Dawn(Talk(Add(Art.Sled, 450, 142, 9), 24, 22,
                "Kırık bir kızak. Üstüne çakıyla NAR + NİNE kazınmış.",
                "Bunu sen kazımıştın. Nine o gün yokuştan senden hızlı kaymıştı."),
                "Kırık kızak. NAR + NİNE. Kar gitmeden bir kez daha kayılır mı? Nine'ye sormalı.",
                "Kırık kızak. NAR + NİNE. Yazı gün ışığında daha derin görünüyor.",
                "Kırık kızak. Altında kayacak kar kalmıyor.");

            Talk(Add(Art.Birch, 580, 140, 4), 20, 84,
                "Kayının gövdesinde derin çizikler var. Pençe mi?",
                "Hayır. Biri sıkıntıdan ağaç kaşımış.");

            Dawn(Talk(Add(Art.Sign, 700, 141, 5), 24, 34,
                "Tabela: AKDAĞ - 3 KONAK.",
                "Altına kömürle, eğri büğrü bir yazı eklenmiş: YOLCUYA SORU SORMAK SERBESTTİR!"),
                "Tabela: AKDAĞ - 3 KONAK. Altına taze kömürle bir satır daha eklenmiş: CEVAP VERMEK DE SERBESTTİR!",
                "Tabela: AKDAĞ - 3 KONAK. Altındaki kömür yazı yarı yarıya silinmiş.",
                "Tabela kavrulmuş. Yazısı okunmuyor.");

            AddSpirit(0, 1010, 172,
                new[] { "Karda bir avuç is. Orman çok sessiz." },
                new[]
                {
                    "Karakoncolos|Soru: Dağa çıkan yolcu aç mıdır? ...Dur, cevap verme. Yenisini hazırlıyorum, karıştırma.",
                    "Karakoncolos|Tüylerim kurudu. Kırk yıldır ilk kez. Kaşınıyor ama güzel kaşınıyor.",
                    "Karakoncolos|Hadi git. Gün doğmazsa gölgem olmaz. Gölgesiz Karakoncolos mu olur?"
                });

            Decor(Art.Bush, 200, 214); Decor(Art.Bush, 640, 215); Decor(Art.Mound, 400, 214); Decor(Art.Bush, 1130, 213); Decor(Art.Mound, 900, 216);
        }

        void BuildLake()
        {
            for (int i = 0; i < 18; i++)
                Decor(Fx.Hash(i, 301) < 0.75f ? Art.Reeds : Art.Bush, 30 + i * 72 + Fx.Hash(i, 302) * 40, 129 + Fx.Hash(i, 303) * 4);
            Decor(Art.PineS, 60, 124); Decor(Art.PineS, 1240, 125); Decor(Art.RockB, 240, 130); Decor(Art.RockB, 640, 129);
            Add(Art.Rock, 320, 196, 7); Add(Art.RockB, 880, 148, 13); Add(Art.Reeds, 556, 150, 3); Add(Art.Reeds, 626, 152, 3); Add(Art.Reeds, 610, 204, 3);

            AddCairn(150, 164,
                "Göl kıyısında bir yol ocağı. Rüzgâr külünü çoktan almış.",
                "Yine de feneri yaklaştırınca tutuştu.",
                "Nine|Buz, suyun korkmuş hâlidir. Korkana bağırılmaz.");

            Dawn(Talk(Add(Art.FishHole, 440, 150, 13), 28, 30,
                "Buza açılmış bir balık deliği. Yanında tabure, olta, çaydanlık.",
                "Çaydanlık hâlâ ılık... Yok. Sana öyle geldi."),
                "Balık deliği genişlemiş. Çaydanlık bu sefer gerçekten ılık: güneş vurmuş.",
                "Balık deliği genişlemiş. Tabure buzun üstünde tek başına.",
                "Delik artık delik değil; koca bir su. Tabure dibe gitmiş.");

            AddSpirit(5, 590, 178,
                new[] { "Sazların arasında kararmış bir taç.", "Buzun altında hiçbir kıpırtı yok." },
                new[]
                {
                    "Şahmeran|Yılanlarım kıpırdıyor. Uykularında adını sayıklıyorlar. Ben söylemedim; kendileri duymuş.",
                    "Şahmeran|İlerideki turnaya iyi davran. Kuşları sevmem ama o başka. O da kalakalmış.",
                    "Şahmeran|Git, Nar. Sözün sende, sırrın bende."
                });

            Dawn(Talk(Add(Art.Boat, 730, 148, 17), 30, 26,
                "Buzun içinde kalmış bir kayık. Burnunda adı yazıyor: BAHAR.",
                "Bahar, buzun içinde bekliyor."),
                "Kayığın çevresinde buz çatlamış. BAHAR, suyun üstünde hafifçe sallanıyor.",
                "Kayığın çevresinde buz inceliyor. BAHAR'ın çözülmesine az kalmış.",
                "Göl buharlaşıyor. BAHAR, çamurun üstüne oturmuş.");

            AddSpirit(1, 1020, 174,
                new[] { "Buzun üstünde, eriyip yeniden donmuş bir su birikintisi. İçinde tek bir tüy.", "Gökte hiç kanat sesi yok." },
                new[]
                {
                    "Turna|Ayağımı kıpırdatabiliyorum. Bak. Bak! ...Tamam, bakma. Utandım.",
                    "Turna|Sen yürü. Ben yukarıdan gelirim. Gökten her yol kısa görünür.",
                    "Turna|O türküyü yine söyle bir gün. Sesin çatlasa da olur."
                });

            Decor(Art.Reeds, 100, 214); Decor(Art.Reeds, 380, 215); Decor(Art.Reeds, 396, 213); Decor(Art.Reeds, 820, 215); Decor(Art.Rock, 1100, 216); Decor(Art.Reeds, 1210, 214);
        }

        void BuildPass()
        {
            for (int i = 0; i < 20; i++)
            {
                float h = Fx.Hash(i, 401);
                Decor(h < 0.55f ? Art.RockB : (h < 0.8f ? Art.Rock : Art.PineS), 20 + i * 66 + Fx.Hash(i, 402) * 36, 120 + Fx.Hash(i, 403) * 12);
            }
            Add(Art.RockB, 320, 152, 13); Add(Art.Rock, 440, 200, 7); Add(Art.RockB, 810, 148, 13); Add(Art.Rock, 960, 200, 7);

            AddCairn(150, 164,
                "Geçidin ağzında bir yol ocağı. Taşlarında pençe izleri var.",
                "Nine|Kurt, bakışını kaçırana saldırır. Kaçırmayana da hemen dost olmaz. Sabret.");

            int[][] rib = { new[] { 2, -40 }, new[] { 2, -34 }, new[] { 2, -27 }, new[] { 2, -20 }, new[] { 2, -30 } };
            Prop pole = Add(Art.Pole, 410, 142, 3);
            pole.Draw = (g, sx, sy) => { Art.Pole.Draw(g, sx, sy); DrawRibbons(g, sx, sy, rib, false); g.FillRectangle(Fx.B(P.C(0x8A5FA8)), (int)sx + 2, (int)sy - 14, 4, 2); };
            Dawn(Talk(pole, 22, 52,
                "Geçit direği. Yolcular buraya bez bağlar, dağdan izin isterdi.",
                "Bezlerin hepsi solmuş. En yenisi mor: Nine'nin yazmasından."),
                "Geçit direği. Bezler gün ışığında renklerini hatırladı. Mor olan en önde.",
                "Geçit direği. Bezler kıpırdıyor. Sayıları artmamış.",
                "Geçit direği. Bezler kurumuş, kavrulmuş.");

            AddSpirit(6, 520, 176,
                new[] { "Yolun ortasında kararmış, koca bir kaya.", "Çevresinde kırk iki taş. Sayan yok." },
                new[]
                {
                    "Tepegöz|Kırk iki. Eksik yok. Sen söyledin, ben inandım.",
                    "Tepegöz|Küçük güneşi düşürme. Düşürürsen ben tutarım; elim büyük.",
                    "Tepegöz|Kurda selam söyle. Gelsin, birlikte sayarız."
                });
            // Tepegöz'ün "sürüsü"
            for (int i = 0; i < 9; i++)
                Decor(Art.Stone, 548 + i * 16 + Fx.Hash(i, 411) * 8, 150 + Fx.Hash(i, 412) * 40);

            Decor(Art.Fence, 610, 140); Decor(Art.Fence, 682, 142);
            Dawn(Talk(Add(Art.Fence, 646, 141, 6), 30, 26,
                "Yıkık bir ağıl. Eskiden burada bin koyun yaylardı.",
                "Çobanlar da tok yatardı, kurtlar da."),
                "Yıkık ağıl. Çitin dibinde kar erimiş; altından ot çıkmış. Bir tutam.",
                "Yıkık ağıl. Kar eriyor. Altında ne var, bahar gösterecek.",
                "Yıkık ağıl. Karın altından çıkan ot, çıkar çıkmaz sararmış.");

            Prop paws = Add(null, 900, 178);
            paws.Flat = true;
            paws.Draw = (g, sx, sy) =>
            {
                for (int i = 0; i < 5; i++)
                {
                    int x = (int)sx - 26 + i * 12, y = (int)sy + (i % 2) * 5;
                    g.FillRectangle(Fx.B(110, P.Night2), x, y, 4, 3);
                    g.FillRectangle(Fx.B(110, P.Night2), x + 1, y - 2, 1, 1);
                    g.FillRectangle(Fx.B(110, P.Night2), x + 3, y - 2, 1, 1);
                    g.FillRectangle(Fx.B(110, P.Night2), x + 5, y - 1, 1, 1);
                }
            };
            Talk(paws, 22, 14,
                "Karda taze izler. Büyük. Tek bir hayvanın.",
                "İzler geçide doğru gidiyor. Geri dönen yok.");

            AddSpirit(2, 1050, 174,
                new[] { "Rüzgârın süpüremediği bir tutam boz kıl.", "Geçit artık hiç kimsenin." },
                new[]
                {
                    "Börü|Yürü, küçük ateş. Arkanda ayak sesi duyarsan korkma. Benim.",
                    "Börü|Ekmeğin tuzu hâlâ dilimde. Köy yaşıyor demek.",
                    "Börü|Yukarıdaki ihtiyar serttir. Ama sert olan, kırılan olur. Sen yumuşak dur."
                });

            Decor(Art.Rock, 220, 215); Decor(Art.Mound, 700, 215); Decor(Art.Rock, 1180, 216); Decor(Art.Bush, 440, 214);
        }

        void BuildSummit()
        {
            for (int i = 0; i < 9; i++) Decor(Fx.Hash(i, 501) < 0.6f ? Art.RockB : Art.Rock, 30 + i * 104 + Fx.Hash(i, 502) * 50, 124 + Fx.Hash(i, 503) * 6);

            AddCairn(140, 166,
                "Son yol ocağı. Buradan sonrası gökyüzü.",
                "Nine|Ayaz Ata'dan korkma. O da sadece bekleyenlerden.");

            string[] charLines =
            {
                "Yol boyunca sıra sıra kararmış taş. Her biri, bir yılın ateşi.",
                "Saymaya başladın. Altmışa gelince durdun. Onlar Nine'nin."
            };
            for (int i = 0; i < 12; i++)
                Talk(Add(Art.Char, 232 + i * 27, 139 + (i % 2) * 3, 4), 16, 16, charLines);

            Talk(Add(Art.Balbal, 300, 131), 24, 42,
                "Bir taş baba. Elinde kadeh, yüzü doğuya dönük.",
                "Bin yıldır gün doğumunu bekliyor. Üç gündür, ilk kez boşuna.");
            Decor(Art.Balbal, 590, 130);

            // Yel Ana: yalnızca öteki altı ruh dostsa yolu keser
            if (fate[7] != 0 || AllSix)
                AddSpirit(7, 500, 176,
                    new[] { "Karın üstünde çözülmüş bir düğüm.", "Hava kıpırtısız. Kar dümdüz yağıyor." },
                    new[]
                    {
                        "Yel Ana|Bak, esiyorum. Feneri görüyor musun? Titremiyor bile. ...Biraz titriyor. Çalışıyorum.",
                        "Yel Ana|Yukarıdaki ihtiyar benden hoşlanmaz; sakalını dağıtırım. Bugün dağıtmayacağım.",
                        "Yel Ana|Çık hadi. Arkandan eserim; yokuş hafifler."
                    });

            Prop gun = Add(Art.GunOcagi, 806, 152, 30);
            gun.Draw = (g, sx, sy) =>
            {
                Art.GunOcagi.Draw(g, sx, sy);
                if (gunLit)
                {
                    DrawFire(g, sx, sy - 14, 2.0f + 0.5f * dawn);
                    DrawFire(g, sx - 9, sy - 14, 1.3f);
                    DrawFire(g, sx + 9, sy - 14, 1.3f);
                }
            };
            spX[3] = 716; areaSpirits.Add(3);
            if (fate[3] == 0)
            {
                Prop ay = Add(null, 716, 178, 12);
                ay.Draw = (g, sx, sy) => DrawSpirit(g, 3, sx, sy, false, 1, 0);
                spiritProp = ay;
            }
            Decor(Art.Rock, 420, 216); Decor(Art.Mound, 640, 215); Decor(Art.Rock, 60, 215);
        }

        // =====================================================
        //  BÖLGEYE GİRİŞ / GEZİNME
        // =====================================================
        void EnterArea(int a, bool fromLoad)
        {
            mode = Mode.World;
            dawn = 0;
            BuildArea(a);
            px = a == 0 ? 384 : (fromLoad ? 176 : 28);
            py = a == 0 ? 184 : 178;
            facing = 1; moving = false; paused = false;
            camX = Fx.Clamp(px - W * 0.45f, 0, areaW - W);
            prints.Clear();
            bannerT = 3.4f;
            dActive = false;
            script.Clear(); scriptIdx = 0;
            Music(theme.Track);
            if (a > 0 && !fromLoad) Save();

            if (a == 0 && !hasLantern)
            {
                bannerT = 0;
                Run(
                    SWait(0.9f),
                    SSay(
                        "Nine|Nar... gel yavrum. Ateşin yanına gel.",
                        "Nine|Bak şu ocağa. Kırk kuşaktır sönmedi. Şimdi bir avuç kor kaldı.",
                        "Nine|Altmış kıştır her Nardugan gecesi o koru dağa ben taşıdım. Bu kış dizlerim beni kapıya kadar götürmüyor.",
                        "Nine|Köyde senden başka kimse de kalmadı. Hepsi gitti; ışığı düğmeyle yanan evlere.",
                        "Nine|Al şu feneri. İçinde ocağın son koru var."),
                    SDo(() => { hasLantern = true; Sfx("save", 0.9f); Flash(P.Fire1, 0.5f); }),
                    SSay(
                        "KOR FENERİ'ni aldın. Avucunda küçük bir kalp gibi atıyor.",
                        "Nine|Onu Akdağ'ın tepesindeki Gün Ocağı'na götür. Ayaz Ata orada bekler. Ocak yanarsa gün doğar.",
                        "Nine|Yolda üşüyen çok şey göreceksin. Kor hem ısıtır, hem yakar. Hangisini yapacağını sen bilirsin.",
                        "Nine|Ha, bir de: Karakoncolos yolunu keserse, ne sorarsa sorsun, cevabında \"kara\" geçsin. Usul böyledir.",
                        "Nine|Heybene üç bazlama koydum. Haydi. Gün seni bekliyor."),
                    SDo(() => { bannerT = 3.4f; Save(); }));
            }
        }

        // Gün doğduktan sonra: aynı yoldan, dağdan köye
        void EnterDescent(int a)
        {
            mode = Mode.World;
            descent = true;
            BuildArea(a);
            dawn = 1; gunLit = true; hasLantern = false;
            px = areaW - 28; py = 178; facing = -1; moving = false; paused = false;
            camX = areaW - W;
            prints.Clear();
            bannerT = 3.4f;
            dActive = false;
            script.Clear(); scriptIdx = 0;
            if (a == 3)
                Run(SWait(1.0f), SSay(dawnKind == 0 ? "Dağdan iniyorsun. Yol aynı yol; ama onu ilk kez görüyorsun."
                    : (dawnKind == 1 ? "Dağdan iniyorsun. Gün ışığı, gecenin sakladığı her şeyi gösteriyor."
                    : "Dağdan iniyorsun. Kar, ayağının altında suya dönüyor.")));
        }

        void PushOut(Prop c)
        {
            float dx = px - c.X, dy = (py - c.Y) * 1.7f;
            float d = (float)Math.Sqrt(dx * dx + dy * dy);
            if (d < c.R && d > 0.01f)
            {
                px = c.X + dx / d * c.R;
                py = c.Y + dy / d * c.R / 1.7f;
            }
        }

        Prop NearestUsable()
        {
            Prop best = null;
            float bd2 = 1e9f;
            foreach (Prop p in props)
            {
                if (p.Use == null) continue;
                float dx = px - p.X, dy = (py - p.Y) * 1.4f;
                float d = (float)Math.Sqrt(dx * dx + dy * dy);
                if (d < p.IR && d < bd2) { bd2 = d; best = p; }
            }
            return best;
        }

        // Yolu kesen, henüz karşılaşılmamış ilk ruh (yoksa -1)
        int Gate()
        {
            foreach (int id in areaSpirits) if (fate[id] == 0) return id;
            return -1;
        }

        void UpdateWorld(float dt)
        {
            bannerT = Math.Max(0, bannerT - dt);
            float camT = Fx.Clamp(px - W * 0.45f, 0, areaW - W);
            camX += (camT - camX) * Math.Min(1f, dt * 6f);

            if (paused) { UpdatePause(); return; }
            if (dActive) { moving = false; UpdateDialogue(dt); return; }
            if (ScriptActive) { moving = false; UpdateScript(dt); return; }
            if (fadeDir > 0) return;

            if (Tap(Btn.Menu)) { paused = true; pauseIdx = 0; Sfx("move", 0.6f); return; }

            float dx = 0, dy = 0;
            if (Hold(Btn.Left)) dx -= 1;
            if (Hold(Btn.Right)) dx += 1;
            if (Hold(Btn.Up)) dy -= 1;
            if (Hold(Btn.Down)) dy += 1;
            if (dx != 0 && dy != 0) { dx *= 0.7071f; dy *= 0.7071f; }
            moving = dx != 0 || dy != 0;
            if (dx < 0) facing = -1; else if (dx > 0) facing = 1;

            if (moving)
            {
                walkT += dt;
                stepT -= dt;
                if (stepT <= 0) { stepT = 0.27f; Sfx("step", 0.22f, 0.9f + Rf() * 0.25f); }
                float sp = Hold(Btn.B) ? 112f : 76f;
                px += dx * sp * dt;
                py += dy * sp * 0.72f * dt;
            }

            int sid = descent ? -1 : Gate();
            bool blocked = sid >= 0;
            float gateX = blocked ? spX[sid] : 0;
            float minX = descent && area > 0 ? -30 : 14;
            float maxX = blocked ? gateX - 30 : (descent ? areaW - 14 : areaW + 30);
            px = Fx.Clamp(px, minX, maxX);
            py = Fx.Clamp(py, BandTop, BandBot);
            foreach (Prop p in props) if (p.R > 0) PushOut(p);
            px = Fx.Clamp(px, minX, maxX);
            py = Fx.Clamp(py, BandTop, BandBot);

            if (moving && (prints.Count == 0 || Fx.Dist(prints[prints.Count - 1].X, prints[prints.Count - 1].Y, px, py) > 8))
            {
                prints.Add(new PointF(px + (prints.Count % 2 == 0 ? -2 : 2), py + 1));
                if (prints.Count > 70) prints.RemoveAt(0);
            }

            if (area == 0 && px > 620) seen.Add("yurudu");

            // ruhla karşılaşma
            if (blocked && px > gateX - 66)
            {
                moving = false;
                BeginEncounter(sid);
                return;
            }

            // dönüş yolu: sola doğru, köy ocağına kadar
            if (descent)
            {
                if (area > 0 && px < -4)
                {
                    int prev = area - 1;
                    FadeTo(() => EnterDescent(prev));
                    return;
                }
                if (area == 0 && px < 500)
                {
                    moving = false;
                    FadeTo(EnterVillageEnding, 1.2f);
                    return;
                }
            }
            // bölgeden çıkış
            else if (px > areaW + 4 && area < 4)
            {
                int next = area + 1;
                FadeTo(() => EnterArea(next, false));
                return;
            }

            if (Ok)
            {
                Prop p = NearestUsable();
                if (p != null) { Sfx("ok", 0.5f); p.Use(); }
            }
        }

        // =====================================================
        //  SAHNE ÇİZİMİ
        // =====================================================
        void DrawScene(Graphics g)
        {
            float cam = (float)Math.Round(camX);
            DrawBackdrop(g, cam);

            foreach (PointF p in prints)
                g.FillRectangle(Fx.B(90, P.Night3), (int)(p.X - cam) - 1, (int)p.Y, 3, 2);

            // derinliğe göre sıralı çizim
            drawList.Clear();
            foreach (Prop p in props)
            {
                if (p.X - cam < -70 || p.X - cam > W + 70) continue;
                if (p.Flat) { DrawProp(g, p, cam); continue; }
                drawList.Add(p);
            }
            if (showPlayer) { playerProp.X = px; playerProp.Y = py; drawList.Add(playerProp); }
            drawList.Sort((a, b) => a.Y.CompareTo(b.Y));
            foreach (Prop p in drawList)
            {
                if (p == playerProp) DrawPlayer(g, px - cam, py);
                else DrawProp(g, p, cam);
            }

            // ışıklar
            lights.Clear();
            foreach (Prop p in props)
                if (p.LightR > 0)
                {
                    float lx = p.X - cam, ly = p.Y + p.LightDY;
                    if (lx < -90 || lx > W + 90) continue;
                    float flick = 1f + 0.035f * (float)Math.Sin(time * 9 + p.X);
                    lights.Add(new Light { X = lx, Y = ly, R = p.LightR * 0.85f * flick });
                }
            if (gunLit && area == 4) lights.Add(new Light { X = 806 - cam, Y = 130, R = 120 + 3 * (float)Math.Sin(time * 8) });
            if (showPlayer && hasLantern)
            {
                float k = kor / (float)Math.Max(1, korMax);
                float r = 22 + 22 * k + 0.8f * (float)Math.Sin(time * 11);
                lights.Add(new Light { X = px - cam + facing * 9, Y = py - 7, R = r });
            }
            DrawLighting(g, 1f - dawn);
            if (dawn > 0) g.FillRectangle(Fx.B((int)(dawn * 34), DawnCol(dawnKind, 3)), 0, Horizon - 8, W, H - Horizon + 8);
            DrawSnow(g);
        }

        void DrawProp(Graphics g, Prop p, float cam)
        {
            float sx = p.X - cam, sy = p.Y;
            if (p.Draw != null) p.Draw(g, sx, sy);
            else if (p.S != null) p.S.Draw(g, sx, sy);
        }

        void DrawHud(Graphics g)
        {
            if (!hasLantern) return;
            int bw = korMax * 2;
            g.FillRectangle(Fx.B(170, P.Night0), 4, 4, bw + 46, 15);
            DrawEmber(g, 12, 12, 1f);
            g.FillRectangle(Fx.B(P.Ink), 20, 7, bw + 2, 9);
            g.FillRectangle(Fx.B(P.Night2), 21, 8, bw, 7);
            int fill = kor * 2;
            g.FillRectangle(Fx.B(P.Fire2), 21, 8, fill, 7);
            g.FillRectangle(Fx.B(P.Fire1), 21, 8, fill, 3);
            g.FillRectangle(Fx.B(P.Fire0), 21, 9, fill, 1);
            Font5.DrawS(g, kor.ToString(), 26 + bw, 8, P.Cream, P.Ink);
            if (bazlama > 0)
            {
                int bx = 10;
                g.FillRectangle(Fx.B(170, P.Night0), 4, 21, 34, 12);
                g.FillEllipse(Fx.B(P.C(0xB5803A)), bx - 4, 23, 10, 7);
                g.FillEllipse(Fx.B(P.C(0xE6C08A)), bx - 3, 23, 8, 5);
                Font5.DrawS(g, "×" + bazlama, bx + 9, 23, P.Cream, P.Ink);
            }
        }

        void DrawBanner(Graphics g)
        {
            if (bannerT <= 0) return;
            float t = bannerT > 3.0f ? (3.4f - bannerT) / 0.4f : (bannerT < 0.5f ? bannerT / 0.5f : 1f);
            int y = (int)(-30 + 52 * Fx.Smooth(t));
            int w = Font5.Width(theme.Name) + 40;
            int x = W / 2 - w / 2;
            g.FillRectangle(Fx.B(230, P.Panel), x, y, w, 26);
            DrawKilim(g, x, y - 3, w);
            DrawKilim(g, x, y + 22, w);
            Font5.Center(g, theme.Name, W / 2, y + 9, P.Cream);
        }

        void DrawWorld(Graphics g)
        {
            DrawScene(g);
            DrawHud(g);
            DrawBanner(g);

            if (!dActive && !ScriptActive && !paused && fadeDir == 0)
            {
                Prop p = NearestUsable();
                if (p != null) DrawKey(g, p.X - (float)Math.Round(camX), p.Y - p.PromptH - 12 + (float)Math.Sin(time * 5) * 1.5f, "Z");
                // çıkış oku
                if (descent)
                {
                    if (area > 0 && px < 150 && (int)(time * 2.5f) % 2 == 0) Font5.DrawS(g, "◄", 7, 168, P.Amber, P.Ink);
                }
                else if (area < 4 && px > areaW - 150 && Gate() < 0 && (int)(time * 2.5f) % 2 == 0)
                    Font5.DrawS(g, "►", W - 12, 168, P.Amber, P.Ink);
            }
            // yolculuğun başında kontrol ipucu
            if (area == 0 && hasLantern && !dActive && !ScriptActive && !paused && px < 620 && !seen.Contains("yurudu"))
            {
                string h = "Ok tuşları / WASD: yürü     X basılı: koş     Z: incele, konuş     Esc: mola";
                int hw = Font5.Width(h) + 16;
                g.FillRectangle(Fx.B(200, P.Panel), W / 2 - hw / 2, H - 20, hw, 15);
                Fx.Frame(g, P.Bronze, W / 2 - hw / 2, H - 20, hw, 15);
                Font5.Draw(g, h, W / 2 - hw / 2 + 8, H - 16, P.Cream);
            }
            DrawDialogueBox(g);
            if (paused) DrawPause(g);
        }
    }
}
