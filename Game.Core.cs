using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;

namespace WinterDeveloper.SonKis
{
    [Flags]
    enum Btn { None = 0, Left = 1, Right = 2, Up = 4, Down = 8, A = 16, B = 32, Menu = 64 }

    enum Mode { Title, Intro, World, Battle, GameOver, Ending }

    sealed partial class Game
    {
        public const int W = 384, H = 216;
        const string StudioName = "Winterdeveloper";

        public readonly Bitmap Frame = new Bitmap(W, H, PixelFormat.Format32bppPArgb);
        public bool WantQuit, WantFullscreen, Fullscreen;

        readonly Audio snd;
        readonly Random rnd = new Random();
        Btn held, pressed;
        float time;
        Mode mode = Mode.Title;

        // Geçişler ve ekran efektleri
        float fadeA, fadeSpeed = 2.4f, flashA, shake;
        int fadeDir;
        Action fadeMid;
        Color flashC = Color.White;

        // Süs bitmap'leri
        Bitmap cornerBmp, kilimBmp, logoBmp, logoShadowBmp, moonBmp;

        // ---------- Kalıcı oyun durumu ----------
        int area, kor = 20, korMax = 20, bazlama = 3, wish = -1;
        readonly int[] fate = new int[8]; // 0: karşılaşılmadı, 1: dost, 2: yandı (3 = Ayaz Ata, 7 = Yel Ana)
        // yolculuk sırasıyla yol ruhları ve bulundukları bölgeler
        static readonly int[] roadSpirits = { 4, 0, 5, 1, 6, 2, 7 };
        static readonly int[] spiritArea = { 1, 2, 3, 4, 0, 2, 3, 4 };

        // Dost olan ruhun verdiği hatıra: fate == 1 ise heybededir
        static readonly string[] keepName = { "Kara Tüy", "Turna Teli", "Kurt Dişi", null, "Kırmızı Yumak", "Yılan Pulu", "Çoban Düğmesi", "Yel Düğümü" };
        static readonly string[] keepHint = { "vurulunca uzun korur", "ilk darbeyi savar", "darbeler 1 az yakar", null, "ısıtmak 1 Kor ucuz", "her evre sonu +1 Kor", "kıvılcım +2 Kor verir", "kaçınırken çeviksin" };
        static readonly string[] keepEffect =
        {
            "Bir darbe aldıktan sonra seni daha uzun korur.",
            "Her karşılaşmada ilk darbeyi senin yerine o alır.",
            "Aldığın her darbe 1 Kor daha az yakar.",
            null,
            "Isıtırken 1 Kor daha az harcarsın.",
            "Her kaçınma evresinin sonunda 1 Kor geri verir.",
            "Altın kıvılcımlar 1 yerine 2 Kor verir.",
            "Kaçınırken daha çevik olursun."
        };
        bool Keep(int id) { return fate[id] == 1; }
        bool hasLantern;
        readonly HashSet<string> seen = new HashSet<string>();

        public Game(Audio audio)
        {
            snd = audio;
            Art.Init();
            MakeOrnaments();
            InitSnow();
            EnterTitle();
        }

        public void Shutdown() { if (snd != null) snd.Shutdown(); }
        public void ToggleMute() { if (snd != null) snd.Muted = !snd.Muted; }

        // =====================================================
        //  GİRDİ / SES KISAYOLLARI
        // =====================================================
        bool Tap(Btn b) { return (pressed & b) != 0; }
        bool Hold(Btn b) { return (held & b) != 0; }
        bool Ok { get { return Tap(Btn.A); } }
        bool Back { get { return Tap(Btn.B) || Tap(Btn.Menu); } }
        float Rf() { return (float)rnd.NextDouble(); }
        float Rf(float a, float b) { return a + (b - a) * (float)rnd.NextDouble(); }

        void Sfx(string name, float vol = 1f, float rate = 1f) { if (snd != null) snd.Play(name, vol, rate); }
        void Music(string name) { if (snd != null) snd.Music(name); }

        int Dost { get { int n = 0; foreach (int i in roadSpirits) if (fate[i] == 1) n++; return n; } }
        int Yanan { get { int n = 0; foreach (int i in roadSpirits) if (fate[i] == 2) n++; return n; } }
        // Yel Ana yalnızca öteki altı ruhun hepsi dostsa yola çıkar
        bool AllSix { get { foreach (int i in roadSpirits) if (i != 7 && fate[i] != 1) return false; return true; } }

        void FadeTo(Action mid, float speed = 2.4f)
        {
            if (fadeDir > 0) return;
            fadeMid = mid; fadeDir = 1; fadeSpeed = speed;
        }

        void Flash(Color c, float a = 1f) { flashC = c; flashA = a; }

        // =====================================================
        //  ANA DÖNGÜ
        // =====================================================
        public void Update(float dt, Btn b)
        {
            pressed = b & ~held;
            held = b;
            time += dt;
            flashA = Math.Max(0, flashA - dt * 1.7f);
            shake = Math.Max(0, shake - dt);

            if (fadeDir != 0)
            {
                fadeA += fadeDir * fadeSpeed * dt;
                if (fadeDir > 0 && fadeA >= 1)
                {
                    fadeA = 1; fadeDir = -1;
                    Action m = fadeMid; fadeMid = null;
                    if (m != null) m();
                }
                else if (fadeDir < 0 && fadeA <= 0) { fadeA = 0; fadeDir = 0; }
                if (fadeDir > 0) { pressed = Btn.None; held = Btn.None; }
            }

            UpdateSnow(dt);
            switch (mode)
            {
                case Mode.Title: UpdateTitle(dt); break;
                case Mode.Intro: UpdateIntro(dt); break;
                case Mode.World: UpdateWorld(dt); break;
                case Mode.Battle: UpdateBattle(dt); break;
                case Mode.GameOver: UpdateGameOver(dt); break;
                case Mode.Ending: UpdateEnding(dt); break;
            }
        }

        public void Render()
        {
            using (Graphics g = Graphics.FromImage(Frame))
            {
                g.SmoothingMode = SmoothingMode.None;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.CompositingQuality = CompositingQuality.HighSpeed;
                g.Clear(Color.Black);

                GraphicsState st = g.Save();
                if (shake > 0) g.TranslateTransform(rnd.Next(-2, 3), rnd.Next(-2, 3));
                switch (mode)
                {
                    case Mode.Title: DrawTitle(g); break;
                    case Mode.Intro: DrawIntro(g); break;
                    case Mode.World: DrawWorld(g); break;
                    case Mode.Battle: DrawBattle(g); break;
                    case Mode.GameOver: DrawGameOver(g); break;
                    case Mode.Ending: DrawEnding(g); break;
                }
                g.Restore(st);

                if (flashA > 0) g.FillRectangle(Fx.B((int)(Math.Min(1f, flashA) * 255) / 16 * 16, flashC), 0, 0, W, H);
                if (fadeA > 0) g.FillRectangle(Fx.B(Math.Min(255, (int)(fadeA * 8.99f) * 32), Color.Black), 0, 0, W, H);
            }
        }

        // =====================================================
        //  SÜSLEMELER: panel, kilim şeridi, logo
        // =====================================================
        void MakeOrnaments()
        {
            int ink = P.I(0x0B0F24), am = P.I(0xF2C46D), rd = P.I(0xD8433B), cr = P.I(0xF6ECD6), bg = P.I(0x6E1C2C), br = P.I(0x7A5630);

            var c = new Pix(7, 7);
            c.Rows(0, 0, new[] { "..kkk..", ".kkakk.", "kkarakk", "karcrak", "kkarakk", ".kkakk.", "..kkk.." },
                ch => ch == 'k' ? ink : (ch == 'a' ? am : (ch == 'r' ? rd : (ch == 'c' ? cr : 0))));
            cornerBmp = c.Bmp();

            var k = new Pix(8, 7);
            k.Rows(0, 0, new[] { "bbbbbbbb", "...a....", "..aca...", ".acrca.c", "..aca...", "...a....", "bbbbbbbb" },
                ch => ch == 'b' ? br : (ch == 'a' ? am : (ch == 'r' ? rd : (ch == 'c' ? cr : bg))));
            kilimBmp = k.Bmp();

            // Hilal
            var m = new Pix(22, 22);
            for (int y = 0; y < 22; y++)
                for (int x = 0; x < 22; x++)
                {
                    float d1 = Fx.Dist(x + 0.5f, y + 0.5f, 11, 11), d2 = Fx.Dist(x + 0.5f, y + 0.5f, 15.5f, 8.5f);
                    if (d1 <= 9.5f && d2 > 8.6f) m.Set(x, y, d1 > 8.3f ? P.I(0xCBDCF5) : P.I(0xF4F8FF));
                }
            moonBmp = m.Bmp();

            // Logo: "SON KIŞ" — O harfi bir kor gibi yanar
            const int sc = 5;
            string text = "SON KIŞ";
            int tw = 0;
            foreach (char ch in text) { int gw; Font5.Bits(ch, out gw); tw += (gw + 1) * sc; }
            tw -= sc;
            var lp = new Pix(tw + 4, 9 * sc + 12);
            int[] iceCols = { 0xF4FAFF, 0xC4E4FA, 0x8CC4F0, 0x5C9CE0, 0x4474C8 };
            int[] fireCols = { 0xFFF6D0, 0xFFE08A, 0xFFB84A, 0xFF8A34, 0xE8552D };
            int ox = 2;
            foreach (char ch in text)
            {
                int gw;
                bool[] bits = Font5.Bits(ch, out gw);
                bool fire = ch == 'O';
                for (int gy = 3; gy < 12; gy++)
                    for (int gx = 0; gx < gw; gx++)
                    {
                        if (!bits[gy * gw + gx]) continue;
                        bool above = gy > 0 && bits[(gy - 1) * gw + gx];
                        bool below = gy < 11 && bits[(gy + 1) * gw + gx];
                        for (int py = 0; py < sc; py++)
                            for (int px = 0; px < sc; px++)
                            {
                                int yy = (gy - 3) * sc + py + 4;
                                float t = ((gy - 3) * sc + py) / (7f * sc);
                                int[] cols = fire ? fireCols : iceCols;
                                int ci = Math.Min(cols.Length - 1, (int)(t * cols.Length + (Fx.Hash(ox + gx * sc + px, yy) - 0.5f) * 0.9f));
                                if (ci < 0) ci = 0;
                                int col = P.I(cols[ci]);
                                if (!above && py < 2 && !fire) col = P.I(0xFFFFFF);
                                lp.Set(ox + gx * sc + px, yy, col);
                            }
                        // kar birikintisi ve buz sarkıtı
                        if (!above && !fire)
                            for (int px = 0; px < sc; px++)
                                if (Fx.Hash(ox + gx * sc + px, gy * 7) < 0.6f) lp.Set(ox + gx * sc + px, (gy - 3) * sc + 3, P.I(0xFFFFFF));
                        if (!below && !fire)
                            for (int px = 0; px < sc; px++)
                            {
                                float hh = Fx.Hash(ox + gx * sc + px + 31, gy * 13);
                                if (hh < 0.3f)
                                    for (int q = 0; q < 1 + (int)(hh * 12); q++) lp.Set(ox + gx * sc + px, (gy - 2) * sc + 4 + q, P.I(0xA9DDF8));
                            }
                    }
                ox += (gw + 1) * sc;
            }
            lp.Outline(ink);
            logoBmp = lp.Bmp();
            var sh = lp.Clone();
            for (int i = 0; i < sh.D.Length; i++) if (((uint)sh.D[i] >> 24) != 0) sh.D[i] = P.I(0x070A1C);
            logoShadowBmp = sh.Bmp();
        }

        void DrawKilim(Graphics g, int x, int y, int w)
        {
            for (int i = 0; i < w; i += 8)
            {
                int cw = Math.Min(8, w - i);
                g.DrawImage(kilimBmp, new Rectangle(x + i, y, cw, 7), 0, 0, cw, 7, GraphicsUnit.Pixel);
            }
        }

        void DrawPanel(Graphics g, int x, int y, int w, int h, int alpha = 246)
        {
            g.FillRectangle(Fx.B(alpha, P.Panel), x, y, w, h);
            Fx.Frame(g, P.Ink, x, y, w, h);
            Fx.Frame(g, P.Amber, x + 1, y + 1, w - 2, h - 2);
            Fx.Frame(g, P.Bronze, x + 2, y + 2, w - 4, h - 4);
            g.DrawImage(cornerBmp, x - 2, y - 2, 7, 7);
            g.DrawImage(cornerBmp, x + w - 5, y - 2, 7, 7);
            g.DrawImage(cornerBmp, x - 2, y + h - 5, 7, 7);
            g.DrawImage(cornerBmp, x + w - 5, y + h - 5, 7, 7);
        }

        // Klavye tuşu simgesi
        void DrawKey(Graphics g, float cx, float cy, string label)
        {
            int w = Font5.Width(label) + 6, x = (int)cx - w / 2, y = (int)cy;
            g.FillRectangle(Fx.B(P.Ink), x - 1, y - 1, w + 2, 13);
            g.FillRectangle(Fx.B(P.Cream), x, y, w, 10);
            g.FillRectangle(Fx.B(P.AmberD), x, y + 10, w, 1);
            Font5.Draw(g, label, x + 3, y + 2, P.Ink);
        }

        void DrawMenuList(Graphics g, string[] items, int idx, int cx, int y, int lineH = 14)
        {
            for (int i = 0; i < items.Length; i++)
            {
                bool sel = i == idx;
                int w = Font5.Width(items[i]);
                Font5.DrawS(g, items[i], cx - w / 2, y + i * lineH, sel ? P.Fire1 : P.Dim, P.Ink);
                if (sel)
                {
                    float bob = (float)Math.Sin(time * 6) * 1.5f;
                    DrawEmber(g, cx - w / 2 - 10 - bob, y + i * lineH + 3, 1f);
                    DrawEmber(g, cx + w / 2 + 9 + bob, y + i * lineH + 3, 1f);
                }
            }
        }

        // Küçük canlı alev (imleç ve savaştaki "kor")
        void DrawEmber(Graphics g, float x, float y, float size)
        {
            int ix = (int)Math.Round(x), iy = (int)Math.Round(y);
            int f = (int)(time * 10) % 3;
            Art.Ember.Draw(g, ix, iy);
            // titreyen uç
            g.FillRectangle(Fx.B(P.Fire2), ix + (f == 0 ? -1 : (f == 1 ? 1 : 0)), iy - 7, 1, 1);
            if (size > 1.2f) g.FillRectangle(Fx.B(P.Fire1), ix, iy - 8 - f % 2, 1, 1);
        }

        // =====================================================
        //  DİYALOG
        // =====================================================
        sealed class DPage { public string Who; public List<string> Lines; }

        readonly List<DPage> dq = new List<DPage>();
        DPage dCur;
        int dTotal, dBlip;
        float dShown;
        bool dActive;
        Action dDone;
        string[] cOpts;
        int cIdx;
        Action<int> cPick;

        int WrapW { get { return mode == Mode.Battle ? 278 : 336; } }

        void Say(Action done, params string[] lines)
        {
            dq.Clear();
            foreach (string raw in lines)
            {
                string who = null, text = raw;
                int bar = raw.IndexOf('|');
                if (bar >= 0) { who = raw.Substring(0, bar); text = raw.Substring(bar + 1); }
                List<string> wrapped = Font5.Wrap(text, WrapW);
                for (int i = 0; i < wrapped.Count; i += 3)
                    dq.Add(new DPage { Who = who, Lines = wrapped.GetRange(i, Math.Min(3, wrapped.Count - i)) });
            }
            dDone = done; cOpts = null; cPick = null;
            dActive = true;
            NextPage();
        }

        void Ask(string prompt, string[] opts, Action<int> pick)
        {
            Say(null, prompt);
            cOpts = opts; cIdx = 0; cPick = pick;
        }

        void NextPage()
        {
            dCur = dq[0];
            dq.RemoveAt(0);
            dTotal = 0;
            foreach (string l in dCur.Lines) dTotal += l.Length;
            dShown = 0; dBlip = 0;
        }

        static readonly Dictionary<string, Color> whoColor = new Dictionary<string, Color>
        {
            { "Nine", P.C(0xE6B8F2) }, { "Karakoncolos", P.C(0xC9A6FF) }, { "Turna", P.C(0xBFE6F5) }, { "Börü", P.C(0xC2CEE6) },
            { "Ayaz Ata", P.C(0x9CC8FF) }, { "???", P.C(0x93A5CF) }, { "Nar", P.C(0xFFB36B) },
            { "Çarşamba Karısı", P.C(0xE89AA8) }, { "Şahmeran", P.C(0x8FE0B4) }, { "Tepegöz", P.C(0xD8C9A0) }, { "Yel Ana", P.C(0xD6E6FA) }
        };
        static readonly Dictionary<string, float> whoPitch = new Dictionary<string, float>
        {
            { "Nine", 0.84f }, { "Karakoncolos", 0.56f }, { "Turna", 1.55f }, { "Börü", 0.46f }, { "Ayaz Ata", 0.38f }, { "???", 0.6f }, { "Nar", 1.22f },
            { "Çarşamba Karısı", 1.02f }, { "Şahmeran", 0.72f }, { "Tepegöz", 0.3f }, { "Yel Ana", 1.36f }
        };

        void UpdateDialogue(float dt)
        {
            if (!dActive) return;
            bool done = dShown >= dTotal;
            if (!done)
            {
                dShown += dt * 46f;
                int n = (int)dShown;
                if (n > dBlip)
                {
                    dBlip = n;
                    if (n % 2 == 0)
                    {
                        float pitch = 1.15f;
                        if (dCur.Who != null) whoPitch.TryGetValue(dCur.Who, out pitch);
                        if (pitch <= 0) pitch = 1f;
                        Sfx("blip", dCur.Who == null ? 0.22f : 0.42f, pitch * (0.97f + Rf() * 0.06f));
                    }
                }
                if (Ok || Tap(Btn.B)) dShown = dTotal;
                return;
            }

            if (cOpts != null && dq.Count == 0)
            {
                if (Tap(Btn.Up)) { cIdx = (cIdx + cOpts.Length - 1) % cOpts.Length; Sfx("move", 0.6f); }
                if (Tap(Btn.Down)) { cIdx = (cIdx + 1) % cOpts.Length; Sfx("move", 0.6f); }
                if (Ok)
                {
                    Sfx("ok", 0.6f);
                    Action<int> pick = cPick;
                    int idx = cIdx;
                    cOpts = null; cPick = null; dActive = false;
                    if (pick != null) pick(idx);
                }
                return;
            }

            if (Ok)
            {
                if (dq.Count > 0) NextPage();
                else
                {
                    dActive = false;
                    Action d = dDone; dDone = null;
                    if (d != null) d();
                }
            }
        }

        // Kutunun içine metni çizer (x, y: iç sol üst köşe)
        void DrawDialogueText(Graphics g, int x, int y, int innerW, int innerH)
        {
            if (dCur == null) return;
            int row = 0;
            bool tab = cOpts != null && dCur.Who != null; // seçimde ad, kutunun üst kenarına etiket olur
            if (tab)
            {
                Color wc;
                if (!whoColor.TryGetValue(dCur.Who, out wc)) wc = P.Amber;
                int nw = Font5.Width(dCur.Who);
                g.FillRectangle(Fx.B(P.Ink), x - 4, y - 17, nw + 8, 13);
                Fx.Frame(g, wc, x - 4, y - 17, nw + 8, 13);
                Font5.Draw(g, dCur.Who, x, y - 14, wc);
            }
            else if (dCur.Who != null)
            {
                Color wc;
                if (!whoColor.TryGetValue(dCur.Who, out wc)) wc = P.Amber;
                Font5.Draw(g, dCur.Who, x, y, wc);
                int nw = Font5.Width(dCur.Who);
                g.FillRectangle(Fx.B(120, wc), x, y + 9, nw, 1);
                row = 1;
            }
            int left = (int)dShown;
            Color tc = dCur.Who == null ? P.Narr : P.Cream;
            foreach (string l in dCur.Lines)
            {
                if (left <= 0) break;
                string vis = left >= l.Length ? l : l.Substring(0, left);
                Font5.Draw(g, vis, x, y + row * 12 + (dCur.Who != null && !tab ? 2 : 0), tc);
                left -= l.Length;
                row++;
            }
            bool done = dShown >= dTotal;
            if (done && cOpts != null && dq.Count == 0)
            {
                for (int i = 0; i < cOpts.Length; i++)
                {
                    bool sel = i == cIdx;
                    int oy = y + (row + i) * 12 + 1;
                    Font5.Draw(g, cOpts[i], x + 16, oy, sel ? P.Fire1 : P.Dim);
                    if (sel) DrawEmber(g, x + 6, oy + 3, 1f);
                }
            }
            else if (done && (int)(time * 3) % 2 == 0)
            {
                int ax = x + innerW - 7, ay = y + innerH - 6;
                g.FillRectangle(Fx.B(P.Amber), ax, ay, 5, 1);
                g.FillRectangle(Fx.B(P.Amber), ax + 1, ay + 1, 3, 1);
                g.FillRectangle(Fx.B(P.Amber), ax + 2, ay + 2, 1, 1);
            }
        }

        void DrawDialogueBox(Graphics g)
        {
            if (!dActive) return;
            if (cOpts != null)
            {
                // seçenekli soru: satır sayısına göre yükselen kutu
                int rows = (dCur != null ? dCur.Lines.Count : 1) + cOpts.Length;
                int ph = Math.Max(54, rows * 12 + 18);
                DrawPanel(g, 12, 208 - ph, 360, ph);
                DrawDialogueText(g, 24, 218 - ph, 336, ph - 16);
                return;
            }
            DrawPanel(g, 12, 150, 360, 58);
            DrawDialogueText(g, 24, 160, 336, 42);
        }

        // =====================================================
        //  SIRALI SAHNE BETİĞİ
        // =====================================================
        readonly List<Func<float, bool>> script = new List<Func<float, bool>>();
        int scriptIdx, scriptVer;
        bool ScriptActive { get { return scriptIdx < script.Count; } }

        void Run(params Func<float, bool>[] steps)
        {
            script.Clear();
            script.AddRange(steps);
            scriptIdx = 0;
            scriptVer++;
        }

        void UpdateScript(float dt)
        {
            int guard = 0;
            while (ScriptActive && guard++ < 50)
            {
                int ver = scriptVer;
                bool done = script[scriptIdx](dt);
                if (ver != scriptVer) return; // betik değiştirildi
                if (!done) return;
                scriptIdx++;
            }
        }

        Func<float, bool> SSay(params string[] lines)
        {
            bool started = false;
            return dt =>
            {
                if (!started) { started = true; Say(null, lines); return false; }
                return !dActive;
            };
        }

        // Seçenekli soru; seçime göre karşı tarafın cevabını söyletir
        Func<float, bool> SAsk(string prompt, string[] opts, Func<int, string[]> reply)
        {
            bool started = false;
            return dt =>
            {
                if (started) return !dActive;
                started = true;
                Ask(prompt, opts, i =>
                {
                    string[] r = reply(i);
                    if (r != null && r.Length > 0) Say(null, r);
                });
                return false;
            };
        }

        Func<float, bool> SWait(float sec)
        {
            float e = 0;
            return dt => { e += dt; return e >= sec; };
        }

        Func<float, bool> SDo(Action a)
        {
            return dt => { a(); return true; };
        }

        // =====================================================
        //  KAYIT
        // =====================================================
        static string SavePath
        {
            get
            {
                if (DevSavePath != null) return DevSavePath;
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), StudioName, "SonKis");
                return Path.Combine(dir, "kayit.txt");
            }
        }

        public bool NoSave; // testlerde diske yazma

        bool HasSave() { try { return !NoSave && File.Exists(SavePath); } catch { return false; } }

        void Save()
        {
            if (NoSave) return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
                var lines = new List<string>
                {
                    "area=" + area, "kor=" + kor, "korMax=" + korMax, "bazlama=" + bazlama, "wish=" + wish,
                    "fate=" + string.Join(",", fate),
                    "lantern=" + (hasLantern ? 1 : 0), "seen=" + string.Join(",", seen)
                };
                File.WriteAllLines(SavePath, lines);
            }
            catch { }
        }

        bool Load()
        {
            try
            {
                var d = new Dictionary<string, string>();
                foreach (string l in File.ReadAllLines(SavePath))
                {
                    int eq = l.IndexOf('=');
                    if (eq > 0) d[l.Substring(0, eq)] = l.Substring(eq + 1);
                }
                area = Math.Max(0, Math.Min(4, int.Parse(d["area"], CultureInfo.InvariantCulture)));
                korMax = Math.Max(20, Math.Min(60, int.Parse(d["korMax"], CultureInfo.InvariantCulture)));
                kor = Math.Max(1, Math.Min(korMax, int.Parse(d["kor"], CultureInfo.InvariantCulture)));
                bazlama = Math.Max(0, Math.Min(9, int.Parse(d["bazlama"], CultureInfo.InvariantCulture)));
                wish = Math.Max(-1, Math.Min(2, int.Parse(d["wish"], CultureInfo.InvariantCulture)));
                string[] f = d["fate"].Split(',');
                for (int i = 0; i < fate.Length; i++) fate[i] = i < f.Length ? Math.Max(0, Math.Min(2, int.Parse(f[i], CultureInfo.InvariantCulture))) : 0; // eski kayıtlarda 4 ruh var
                hasLantern = d["lantern"] == "1";
                seen.Clear();
                foreach (string s in d["seen"].Split(',')) if (s.Length > 0) seen.Add(s);
                return true;
            }
            catch { return false; }
        }

        void ResetState()
        {
            area = 0; kor = 20; korMax = 20; bazlama = 3; wish = -1; hasLantern = false; descent = false;
            for (int i = 0; i < fate.Length; i++) fate[i] = 0;
            seen.Clear();
        }

        // =====================================================
        //  BAŞLIK EKRANI
        // =====================================================
        string[] titleItems;
        int titleIdx;
        float titleT;

        void EnterTitle()
        {
            mode = Mode.Title;
            titleT = 0;
            titleIdx = 0;
            titleItems = HasSave()
                ? new[] { "Devam Et", "Yeni Yolculuk", "Tam Ekran", "Çıkış" }
                : new[] { "Yeni Yolculuk", "Tam Ekran", "Çıkış" };
            dActive = false;
            script.Clear(); scriptIdx = 0;
            dawn = 0;
            ResetState();
            hasLantern = true;
            BuildArea(0);
            px = 376; py = 184; facing = 1; moving = false;
            camX = 284;
            Music("ocak");
        }

        void UpdateTitle(float dt)
        {
            titleT += dt;
            if (titleT < 0.6f) return;
            if (Tap(Btn.Up)) { titleIdx = (titleIdx + titleItems.Length - 1) % titleItems.Length; Sfx("move", 0.6f); }
            if (Tap(Btn.Down)) { titleIdx = (titleIdx + 1) % titleItems.Length; Sfx("move", 0.6f); }
            if (!Ok) return;
            Sfx("ok", 0.7f);
            switch (titleItems[titleIdx])
            {
                case "Yeni Yolculuk":
                    FadeTo(() => { ResetState(); EnterIntro(); }, 1.6f);
                    break;
                case "Devam Et":
                    FadeTo(() =>
                    {
                        ResetState();
                        if (Load()) EnterArea(area, true);
                        else EnterIntro();
                    }, 1.6f);
                    break;
                case "Tam Ekran": WantFullscreen = true; break;
                case "Çıkış": WantQuit = true; break;
            }
        }

        void DrawTitle(Graphics g)
        {
            DrawScene(g);

            // üstte hafif karartma: logo okunaklı olsun
            for (int i = 0; i < 3; i++) g.FillRectangle(Fx.B(44, P.Night0), 0, 0, W, 66 - i * 16);

            float bob = (float)Math.Sin(time * 1.3f) * 1.5f;
            int lx = (W - logoBmp.Width) / 2, ly = 20 + (int)Math.Round(bob);
            // "O" harfinin arkasında kor ışıltısı
            float ox = lx + 39, oy = ly + 22;
            for (int i = 0; i < 5; i++)
            {
                float r = 30 - i * 5 + (float)Math.Sin(time * 3 + i) * 1.5f;
                g.FillEllipse(Fx.B(14, P.Fire2), ox - r, oy - r, r * 2, r * 2);
            }
            g.DrawImage(logoShadowBmp, lx + 2, ly + 3, logoBmp.Width, logoBmp.Height);
            g.DrawImage(logoBmp, lx, ly, logoBmp.Width, logoBmp.Height);

            Font5.CenterS(g, "bir Nardugan masalı", W / 2, 80, P.Amber, P.Ink);
            DrawKilim(g, W / 2 - 60, 93, 120);

            if (titleT > 0.6f)
            {
                int ph = titleItems.Length * 14 + 12;
                DrawPanel(g, 300 - 62, 124, 124, ph, 222);
                DrawMenuList(g, titleItems, titleIdx, 300, 133);
            }
            Font5.DrawS(g, StudioName, 6, H - 12, P.Dim, P.Ink);
            string keys = "Ok tuşları: seç   Z: onayla   F11: tam ekran   M: ses";
            Font5.DrawS(g, keys, W - 6 - Font5.Width(keys), H - 12, P.Dim, P.Ink);
        }

        // =====================================================
        //  GİRİŞ MASALI
        // =====================================================
        static readonly string[] introCards =
        {
            "Eskiler bilir:\nyılın en uzun gecesine Nardugan denir.",
            "O gece karanlık ile gün dövüşür.\nGün kazanır. Güneş yeniden doğar.",
            "Ama güneş kendiliğinden doğmaz.\nHer yıl biri, köy ocağından bir kor alır,\nAkdağ'ın tepesindeki Gün Ocağı'na taşır.",
            "Sönmez köyünde o kişi,\naltmış kıştır Umay Nine'ydi.",
            "Bu yıl Nine dağa çıkamadı.",
            "Ve gece bitmedi.\nÜç gündür sabah olmuyor."
        };
        int introIdx;
        float introT;

        void EnterIntro()
        {
            mode = Mode.Intro;
            introIdx = 0; introT = 0;
            BuildArea(0);
            camX = 0;
            Music("ocak");
        }

        void UpdateIntro(float dt)
        {
            introT += dt;
            camX = Math.Min(areaW - W, camX + dt * 5f);
            int total = introCards[introIdx].Length;
            bool done = introT * 30f >= total;
            if (Tap(Btn.Menu) || Tap(Btn.B)) { FadeTo(StartJourney, 1.8f); return; }
            if (Ok)
            {
                if (!done) introT = total / 30f + 0.01f;
                else if (introIdx < introCards.Length - 1) { introIdx++; introT = 0; Sfx("move", 0.5f); }
                else FadeTo(StartJourney, 1.4f);
            }
        }

        void DrawIntro(Graphics g)
        {
            DrawBackdrop(g, camX);
            DrawSnow(g);
            g.FillRectangle(Fx.B(150, P.Night0), 0, 0, W, H);
            DrawKilim(g, 0, 22, W);
            DrawKilim(g, 0, H - 29, W);

            string card = introCards[introIdx];
            string[] lines = card.Split('\n');
            int shown = (int)(introT * 30f);
            int y = H / 2 - lines.Length * 8;
            foreach (string l in lines)
            {
                if (shown <= 0) break;
                string vis = shown >= l.Length ? l : l.Substring(0, shown);
                Font5.DrawS(g, vis, W / 2 - Font5.Width(l) / 2, y, P.Cream, P.Ink);
                shown -= l.Length;
                y += 16;
            }
            if (introT * 30f >= card.Length && (int)(time * 2.5f) % 2 == 0)
                DrawKey(g, W / 2, H - 50, "Z");
            Font5.Draw(g, "X: geç", W - 44, H - 16, P.Dim);
        }

        void StartJourney()
        {
            ResetState();
            EnterArea(0, false);
        }

        // =====================================================
        //  KOR SÖNDÜ (oyun bitti)
        // =====================================================
        int overIdx;
        float overT;

        void EnterGameOver()
        {
            mode = Mode.GameOver;
            overIdx = 0; overT = 0;
            Music(null);
            Sfx("out", 0.9f);
        }

        void UpdateGameOver(float dt)
        {
            overT += dt;
            if (overT < 1.6f) return;
            if (Tap(Btn.Up) || Tap(Btn.Down)) { overIdx ^= 1; Sfx("move", 0.6f); }
            if (Ok)
            {
                Sfx("ok", 0.7f);
                if (overIdx == 0) FadeTo(RetryBattle);
                else FadeTo(EnterTitle);
            }
        }

        void DrawGameOver(Graphics g)
        {
            g.FillRectangle(Fx.B(P.Night0), 0, 0, W, H);
            DrawSnow(g);
            // sönen kor
            float t = Math.Min(1f, overT / 1.4f);
            float r = 26 * (1 - t) + 5;
            for (int i = 0; i < 4; i++)
                g.FillEllipse(Fx.B((int)(18 * (1 - t * 0.7f)), P.Fire3), W / 2 - r + i * 2, 66 - r + i * 2, (r - i * 2) * 2, (r - i * 2) * 2);
            if (t < 1) DrawEmber(g, W / 2, 66, 1f);
            else
            {
                g.FillRectangle(Fx.B(P.Fire4), W / 2 - 2, 66, 4, 2);
                g.FillRectangle(Fx.B(P.Fire3), W / 2 - 1, 66, 2, 1);
                float st = (time * 0.5f) % 1f;
                g.FillRectangle(Fx.B((int)(120 * (1 - st)), P.Dim), W / 2 + (int)(Math.Sin(time * 2) * 2), 62 - (int)(st * 16), 1, 2);
            }
            if (overT > 0.7f) Font5.CenterS(g, "KOR SÖNDÜ", W / 2, 92, P.Ice, P.Ink, 2);
            if (overT > 1.2f)
            {
                Font5.Center(g, "Nine'nin sesi, çok uzaktan:", W / 2, 122, P.Dim);
                Font5.Center(g, "\"Kalk yavrum. Ateş küllenir; sönmez.\"", W / 2, 135, P.C(0xE6B8F2));
            }
            if (overT > 1.6f) DrawMenuList(g, new[] { "Yeniden dene", "Ana menü" }, overIdx, W / 2, 162);
        }

        // =====================================================
        //  DURAKLATMA MENÜSÜ
        // =====================================================
        bool paused;
        int pauseIdx;

        void UpdatePause()
        {
            string[] items = PauseItems();
            if (Tap(Btn.Up)) { pauseIdx = (pauseIdx + items.Length - 1) % items.Length; Sfx("move", 0.6f); }
            if (Tap(Btn.Down)) { pauseIdx = (pauseIdx + 1) % items.Length; Sfx("move", 0.6f); }
            if (Tap(Btn.Menu) || Tap(Btn.B)) { paused = false; Sfx("back", 0.6f); return; }
            if (!Ok) return;
            Sfx("ok", 0.6f);
            switch (pauseIdx)
            {
                case 0: paused = false; break;
                case 1: ToggleMute(); break;
                case 2: WantFullscreen = true; break;
                case 3: paused = false; FadeTo(EnterTitle); break;
            }
        }

        string[] PauseItems()
        {
            return new[] { "Devam", "Ses: " + (snd != null && snd.Muted ? "Kapalı" : "Açık"), "Tam Ekran", "Ana Menüye Dön" };
        }

        void DrawPause(Graphics g)
        {
            g.FillRectangle(Fx.B(140, P.Night0), 0, 0, W, H);
            DrawPanel(g, W / 2 - 80, 44, 160, 128);
            Font5.Center(g, "MOLA", W / 2, 54, P.Amber);
            DrawKilim(g, W / 2 - 60, 66, 120);
            DrawMenuList(g, PauseItems(), pauseIdx, W / 2, 82);
            string st = "Dost: " + Dost + "   Yanan: " + Yanan;
            Font5.Center(g, st, W / 2, 146, P.Dim);
            Font5.Center(g, "Kayıt yol ocaklarında yapılır.", W / 2, 158, P.Dim);
        }
    }
}
