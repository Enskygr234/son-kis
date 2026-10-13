using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace WinterDeveloper.SonKis
{
    // Ses üretimi: bütün efektler ve müzikler kodla sentezlenir
    static class Synth
    {
        public const int SR = 22050;

        static void Add(float[] o, bool wrap, int i, float v)
        {
            if (i < 0) return;
            if (i >= o.Length) { if (!wrap) return; i %= o.Length; }
            o[i] += v;
        }

        public static float Freq(string note, int transpose)
        {
            int[] semi = { 9, 11, 0, 2, 4, 5, 7 }; // a b c d e f g
            int s = semi[note[0] - 'a'];
            int k = 1;
            if (k < note.Length && note[k] == '#') { s++; k++; }
            else if (k < note.Length - 1 && note[k] == 'b') { s--; k++; }
            int oct = note[k] - '0';
            int midi = 12 * (oct + 1) + s + transpose;
            return 440f * (float)Math.Pow(2, (midi - 69) / 12.0);
        }

        // wave: 0 sinüs, 1 üçgen, 2 kare
        public static void Tone(float[] o, bool wrap, float start, float dur, float f0, float f1, int wave, float vol, float atk, float rel)
        {
            int n = (int)(dur * SR), s0 = (int)(start * SR);
            double ph = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float f = f0 + (f1 - f0) * t;
                ph += f / SR;
                double p = ph - Math.Floor(ph);
                float v = wave == 0 ? (float)Math.Sin(p * Math.PI * 2) : (wave == 1 ? (float)(4 * Math.Abs(p - 0.5) - 1) : (p < 0.5 ? 0.6f : -0.6f));
                float ti = i / (float)SR, env = 1;
                if (ti < atk) env = ti / atk;
                float left = dur - ti;
                if (left < rel) env *= left / rel;
                Add(o, wrap, s0 + i, v * vol * env);
            }
        }

        // Sönümlenen sinüs (çan / zil)
        public static void Bell(float[] o, bool wrap, float start, float freq, float dur, float vol)
        {
            int n = (int)(dur * SR), s0 = (int)(start * SR);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float env = (float)Math.Exp(-t * 5.5 / dur) * Math.Min(1f, t / 0.004f);
                float v = (float)(Math.Sin(t * freq * Math.PI * 2) + 0.35 * Math.Sin(t * freq * 2.01 * Math.PI * 2) + 0.12 * Math.Sin(t * freq * 3.02 * Math.PI * 2));
                Add(o, wrap, s0 + i, v * vol * env * 0.7f);
            }
        }

        // Süzülmüş gürültü; lp: 0..1 (1 = parlak)
        public static void Noise(float[] o, bool wrap, float start, float dur, float vol, float lp0, float lp1, float atk, float rel, int seed)
        {
            var r = new Random(seed);
            int n = (int)(dur * SR), s0 = (int)(start * SR);
            float y = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n, lp = lp0 + (lp1 - lp0) * t;
                y += lp * ((float)(r.NextDouble() * 2 - 1) - y);
                float ti = i / (float)SR, env = 1;
                if (ti < atk) env = ti / atk;
                float left = dur - ti;
                if (left < rel) env *= left / rel;
                Add(o, wrap, s0 + i, y * vol * env);
            }
        }

        // Telli çalgı (Karplus-Strong): saz / bağlama tınısı
        public static void Pluck(float[] o, bool wrap, float start, float freq, float t60, float vol, float bright, int seed)
        {
            var r = new Random(seed);
            float D = SR / freq - 0.5f;
            int M = (int)D + 4, L = (int)D + 1;
            var ring = new float[M];
            int n = (int)(Math.Min(t60 * 1.15f, 4f) * SR), s0 = (int)(start * SR);
            float g = (float)Math.Pow(0.001, 1.0 / (freq * t60));
            float prev = 0, ex = 0;
            for (int i = 0; i < n; i++)
            {
                float rp = i - D;
                int i0 = (int)Math.Floor(rp);
                float fr = rp - i0;
                float a = i0 >= 0 ? ring[i0 % M] : 0, b = i0 + 1 >= 0 ? ring[(i0 + 1) % M] : 0;
                float cur = a + (b - a) * fr;
                float y = 0.5f * (cur + prev) * g;
                prev = cur;
                float inp = 0;
                if (i < L) { ex += bright * ((float)(r.NextDouble() * 2 - 1) - ex); inp = ex; }
                float v = inp + y;
                ring[i % M] = v;
                float fade = i > n - 400 ? (n - i) / 400f : 1f;
                Add(o, wrap, s0 + i, v * vol * fade);
            }
        }

        // Çift telli saz: hafif akortsuz ikinci tel
        public static void Saz(float[] o, bool wrap, float start, float freq, float t60, float vol, int seed)
        {
            Pluck(o, wrap, start, freq, t60, vol, 0.55f, seed);
            Pluck(o, wrap, start + 0.011f, freq * 1.004f, t60 * 0.9f, vol * 0.6f, 0.45f, seed + 7);
        }

        // Ney: nefesli, titreşimli üflemeli
        public static void Ney(float[] o, bool wrap, float start, float freq, float dur, float vol, int seed)
        {
            var r = new Random(seed);
            dur += 0.12f;
            int n = (int)(dur * SR), s0 = (int)(start * SR);
            double ph = 0;
            float nz = 0, nz2 = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float vib = t > 0.22f ? (float)Math.Sin((t - 0.22f) * 5.2 * Math.PI * 2) * 0.006f * Math.Min(1f, (t - 0.22f) * 3f) : 0;
                float scoop = t < 0.07f ? -0.02f * (1 - t / 0.07f) : 0;
                ph += freq * (1 + vib + scoop) / SR;
                double p = ph * Math.PI * 2;
                float v = (float)(Math.Sin(p) + 0.28 * Math.Sin(p * 2) + 0.10 * Math.Sin(p * 3));
                float w = (float)(r.NextDouble() * 2 - 1);
                nz += 0.35f * (w - nz); nz2 += 0.08f * (nz - nz2);
                float breath = (nz - nz2) * 0.55f;
                float env = Math.Min(1f, t / 0.09f);
                float left = dur - t;
                if (left < 0.18f) env *= left / 0.18f;
                Add(o, wrap, s0 + i, (v * 0.62f + breath) * vol * env);
            }
        }

        public static void Pad(float[] o, bool wrap, float start, float freq, float dur, float vol)
        {
            dur += 0.5f;
            int n = (int)(dur * SR), s0 = (int)(start * SR);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                double a = t * freq * 1.003, b = t * freq * 0.997, c = t * freq * 2.0;
                float v = (float)((4 * Math.Abs(a - Math.Floor(a) - 0.5) - 1) + (4 * Math.Abs(b - Math.Floor(b) - 0.5) - 1) + 0.5 * Math.Sin(c * Math.PI * 2));
                float env = Math.Min(1f, t / 0.45f);
                float left = dur - t;
                if (left < 0.6f) env *= left / 0.6f;
                Add(o, wrap, s0 + i, v * 0.4f * vol * env);
            }
        }

        public static void Dum(float[] o, bool wrap, float start, float vol)
        {
            int n = (int)(0.34f * SR), s0 = (int)(start * SR);
            double ph = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float f = 48 + 70 * (float)Math.Exp(-t * 22);
                ph += f / SR;
                float env = (float)Math.Exp(-t * 11) * Math.Min(1f, t / 0.003f);
                Add(o, wrap, s0 + i, (float)Math.Sin(ph * Math.PI * 2) * vol * env);
            }
        }

        public static void Tek(float[] o, bool wrap, float start, float vol, int seed)
        {
            var r = new Random(seed);
            int n = (int)(0.09f * SR), s0 = (int)(start * SR);
            float lp = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float w = (float)(r.NextDouble() * 2 - 1);
                lp += 0.5f * (w - lp);
                float env = (float)Math.Exp(-t * 55);
                float v = (w - lp) * 0.7f + (float)Math.Sin(t * 820 * Math.PI * 2) * 0.5f * (float)Math.Exp(-t * 90);
                Add(o, wrap, s0 + i, v * vol * env);
            }
        }

        // Döngüye sığan rüzgâr uğultusu
        public static void Wind(float[] o, float vol, int seed)
        {
            var r = new Random(seed);
            float len = o.Length / (float)SR;
            float y1 = 0, y2 = 0;
            for (int i = 0; i < o.Length; i++)
            {
                float t = i / (float)SR;
                double c1 = Math.PI * 2 * t / len;
                float gust = 0.55f + 0.25f * (float)Math.Sin(c1 * 3 + 1) + 0.2f * (float)Math.Sin(c1 * 7 + 2.3);
                float cut = 0.012f + 0.03f * gust;
                float w = (float)(r.NextDouble() * 2 - 1);
                y1 += cut * (w - y1); y2 += cut * (y1 - y2);
                o[i] += y2 * 5.5f * vol * (0.35f + 0.65f * gust);
            }
        }

        public static void Echo(float[] o, float delay, float fb, float mix)
        {
            int D = (int)(delay * SR), n = o.Length;
            if (D >= n) return;
            var wet = new float[n];
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < n; i++)
                    wet[i] = o[i] + fb * wet[(i - D + n) % n];
            var res = new float[n];
            for (int i = 0; i < n; i++) res[i] = o[i] + mix * wet[(i - D + n) % n];
            Array.Copy(res, o, n);
        }

        public static void Normalize(float[] o, float peak)
        {
            float m = 0.0001f;
            foreach (float v in o) m = Math.Max(m, Math.Abs(v));
            float k = peak / m;
            for (int i = 0; i < o.Length; i++) o[i] *= k;
        }

        public struct Ch
        {
            public string Ins; public float Vol; public string Pat;
            public Ch(string ins, float vol, string pat) { Ins = ins; Vol = vol; Pat = pat; }
        }

        // Adım ızgarasına yazılmış notaları müziğe çevirir.
        // "a4" nota, "." uzatma, "-" sus, "x" vurmalı vuruş, "|" ölçü çizgisi (yok sayılır)
        public static float[] Render(float step, int transpose, float echo, float wind, params Ch[] chans)
        {
            int steps = 0;
            var toks = new List<string[]>();
            foreach (var c in chans)
            {
                var list = new List<string>();
                foreach (string t in c.Pat.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                    if (t != "|") list.Add(t);
                toks.Add(list.ToArray());
                steps = Math.Max(steps, list.Count);
            }
            var o = new float[(int)(steps * step * SR)];
            for (int ci = 0; ci < chans.Length; ci++)
            {
                Ch c = chans[ci];
                string[] tk = toks[ci];
                for (int s = 0; s < steps; s++)
                {
                    string t = tk[s % tk.Length];
                    if (t == "." || t == "-") continue;
                    int len = 1;
                    while (s + len < steps && tk[(s + len) % tk.Length] == ".") len++;
                    float start = s * step, dur = len * step;
                    int seed = ci * 1000 + s;
                    if (c.Ins == "dum") { Dum(o, true, start, c.Vol); continue; }
                    if (c.Ins == "tek") { Tek(o, true, start, c.Vol, seed); continue; }
                    float f = Freq(t, transpose);
                    switch (c.Ins)
                    {
                        case "saz": Saz(o, true, start, f, Math.Max(1.4f, dur * 1.6f), c.Vol, seed); break;
                        case "bas": Pluck(o, true, start, f, 1.3f, c.Vol, 0.3f, seed); break;
                        case "ney": Ney(o, true, start, f, dur * 0.96f, c.Vol, seed); break;
                        case "pad": Pad(o, true, start, f, dur, c.Vol); break;
                        case "bell": Bell(o, true, start, f, 1.6f, c.Vol); break;
                    }
                }
            }
            if (echo > 0) Echo(o, step * 3, 0.38f, echo);
            // ses yüksekliğini parçalar arasında eşitle (RMS), tepeleri yumuşakça sınırla
            double sum = 0;
            foreach (float v in o) sum += v * v;
            float k = 0.135f / (float)Math.Max(1e-6, Math.Sqrt(sum / o.Length));
            for (int i = 0; i < o.Length; i++)
            {
                float v = o[i] * k;
                float a = Math.Abs(v);
                if (a > 0.6f) v = Math.Sign(v) * (0.6f + (1f - (float)Math.Exp(-(a - 0.6f) * 2.5f)) * 0.3f);
                o[i] = v;
            }
            if (wind > 0) Wind(o, wind, 99);
            return o;
        }
    }

    static class Tunes
    {
        public static readonly string[] Names = { "ocak", "yol", "savas", "gol", "gecit", "ayaz", "safak" };

        static string Rep(string s, int n)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < n; i++) sb.Append(s).Append(' ');
            return sb.ToString();
        }

        public static float[] Make(string name)
        {
            switch (name)
            {
                case "ocak": return Ocak();
                case "yol": return Yol(0, 0.26f, 0.05f);
                case "gol": return Yol(3, 0.30f, 0.035f);
                case "gecit": return Yol(-2, 0.24f, 0.10f);
                case "savas": return Savas(0, 0.17f, false);
                case "ayaz": return Savas(-5, 0.205f, true);
                case "safak": return Safak();
            }
            return new float[Synth.SR];
        }

        // Köy ve başlık: ağır bir ninni (La minör, 6/8)
        static float[] Ocak()
        {
            string Am = "a2 e3 a3 c4 a3 e3 ", F = "f2 c3 f3 a3 f3 c3 ", G = "g2 d3 g3 b3 g3 d3 ", E = "e2 b2 e3 g#3 e3 b2 ",
                   C = "c3 g3 c4 e4 c4 g3 ", Dm = "d3 a3 d4 f4 d4 a3 ";
            string saz = Am + Am + F + G + Am + Am + F + E + C + G + Am + Am + Dm + Am + E + Am;
            string ney =
                "a4 . . c5 . . | e5 . . . d5 c5 | a4 . . c5 . . | b4 . . . . . | " +
                "a4 . . c5 . . | e5 . . g5 . e5 | f5 . . e5 . d5 | e5 . . . . . | " +
                "g5 . . e5 . . | d5 . . b4 . . | c5 . . a4 . . | a4 . . . . - | " +
                "d5 . . f5 . . | e5 . . c5 . . | b4 . . . g#4 . | a4 . . . . - ";
            string pad = "";
            foreach (string r in new[] { "a2", "a2", "f2", "g2", "a2", "a2", "f2", "e2", "c3", "g2", "a2", "a2", "d3", "a2", "e2", "a2" })
                pad += r + " . . . . . ";
            return Synth.Render(0.30f, 0, 0.30f, 0.045f,
                new Synth.Ch("saz", 0.42f, saz), new Synth.Ch("ney", 0.30f, ney), new Synth.Ch("pad", 0.10f, pad));
        }

        // Yol: yürüyüş temposunda (Mi minör)
        static float[] Yol(int tr, float step, float wind)
        {
            string Em = "e3 b3 e4 b3 g4 b3 e4 b3 ", C = "c3 g3 c4 g3 e4 g3 c4 g3 ", D = "d3 a3 d4 a3 f#4 a3 d4 a3 ",
                   B = "b2 f#3 b3 f#3 d#4 f#3 b3 f#3 ", G = "g2 d3 g3 d3 b3 d3 g3 d3 ";
            string saz = Em + Em + C + D + Em + Em + C + B + G + D + Em + Em + C + D + Em + Em;
            string ney =
                "b4 . . . e5 . . . | d5 . . . b4 . . . | c5 . . . e5 . . . | d5 . . . . . a4 . | " +
                "b4 . . . e5 . . . | g5 . . . e5 . . . | e5 . . . c5 . . . | b4 . . . . . . . | " +
                "d5 . . . g5 . . . | f#5 . . . d5 . . . | e5 . . . . . b4 . | e5 . . . . . - . | " +
                "c5 . . . e5 . g5 . | f#5 . . . d5 . . . | e5 . . . . . . . | - . . . . . . . ";
            string pad = "";
            foreach (string r in new[] { "e2", "e2", "c3", "d3", "e2", "e2", "c3", "b2", "g2", "d3", "e2", "e2", "c3", "d3", "e2", "e2" })
                pad += r + " . . . . . . . ";
            return Synth.Render(step, tr, 0.28f, wind,
                new Synth.Ch("saz", 0.36f, saz), new Synth.Ch("ney", 0.30f, ney), new Synth.Ch("pad", 0.10f, pad),
                new Synth.Ch("dum", 0.30f, "x - - - - - - -"), new Synth.Ch("tek", 0.07f, "- - - - x - - -"));
        }

        // Karşılaşma: 9/8 aksak, Hicaz
        static float[] Savas(int tr, float step, bool heavy)
        {
            string lead =
                "a4 . bb4 . c#5 . d5 c#5 bb4 | a4 . . . e5 . d5 c#5 . | d5 . e5 . f5 . e5 d5 c#5 | d5 . c#5 . bb4 . a4 . . | " +
                "e5 . f5 . g5 . f5 e5 d5 | e5 . . . a5 . g5 f5 . | e5 . d5 . c#5 . d5 c#5 bb4 | a4 . . . . . - . . ";
            string neyB =
                "a4 . . . c#5 . . . . | a4 . . . e5 . . . . | d5 . . . f5 . . . . | d5 . . . bb4 . a4 . . | " +
                "e5 . . . g5 . . . . | e5 . . . a5 . . . . | e5 . . . c#5 . . . . | a4 . . . . . . . . ";
            string ney = Rep("- . . . . . . . .", 8) + neyB;
            string A = "a2 . e3 . a2 . e3 . e3 ", Dm = "d3 . a3 . d3 . a3 . a3 ";
            string bas = A + A + Dm + A + Dm + A + A + A;
            var ch = new List<Synth.Ch>
            {
                new Synth.Ch("saz", 0.40f, lead), new Synth.Ch("ney", 0.24f, ney), new Synth.Ch("bas", 0.50f, bas),
                new Synth.Ch("dum", heavy ? 0.62f : 0.5f, "x - - - x - - - -"), new Synth.Ch("tek", 0.16f, "- - x - - - x x -")
            };
            if (heavy) ch.Add(new Synth.Ch("pad", 0.16f, "a2 . . . . . . . . . . . . . . . . ."));
            return Synth.Render(step, tr, 0.16f, heavy ? 0.06f : 0f, ch.ToArray());
        }

        // Şafak: aydınlık, Do majör
        static float[] Safak()
        {
            string C = "c3 g3 c4 e4 c4 g3 ", G = "g2 d3 g3 b3 g3 d3 ", Am = "a2 e3 a3 c4 a3 e3 ", F = "f2 c3 f3 a3 f3 c3 ", FG = "f2 c3 f3 g2 d3 g3 ";
            string half = C + G + Am + F + C + G + FG + C;
            string neyA =
                "e5 . . g5 . . | d5 . . b4 . d5 | c5 . . e5 . . | a4 . . c5 . . | " +
                "e5 . . g5 . . | g5 . . b5 . a5 | a5 . f5 g5 . d5 | ";
            string ney = neyA + "e5 . d5 c5 . . | " + neyA + "c5 . . . . . ";
            string bell = Rep("- - - - - -", 8) +
                "c6 - - e6 - - | b5 - - d6 - - | c6 - - e6 - - | a5 - - c6 - - | c6 - - e6 - - | d6 - - g6 - - | c6 - - d6 - - | e6 - - c6 - - ";
            string pad = "";
            foreach (string r in new[] { "c3", "g2", "a2", "f2", "c3", "g2", "f2", "c3" }) pad += r + " . . . . . ";
            return Synth.Render(0.28f, 0, 0.30f, 0f,
                new Synth.Ch("saz", 0.40f, half + half), new Synth.Ch("ney", 0.30f, ney), new Synth.Ch("bell", 0.14f, bell), new Synth.Ch("pad", 0.12f, pad));
        }

        public static Dictionary<string, float[]> MakeSfx()
        {
            var d = new Dictionary<string, float[]>();
            float[] b;

            b = new float[(int)(0.045f * Synth.SR)]; Synth.Tone(b, false, 0, 0.04f, 500, 470, 1, 0.5f, 0.003f, 0.02f); d["blip"] = b;
            b = new float[(int)(0.05f * Synth.SR)]; Synth.Tone(b, false, 0, 0.045f, 700, 760, 1, 0.4f, 0.003f, 0.02f); d["move"] = b;
            b = new float[(int)(0.2f * Synth.SR)]; Synth.Tone(b, false, 0, 0.07f, 660, 660, 1, 0.42f, 0.003f, 0.03f); Synth.Tone(b, false, 0.07f, 0.12f, 990, 990, 1, 0.42f, 0.003f, 0.08f); d["ok"] = b;
            b = new float[(int)(0.16f * Synth.SR)]; Synth.Tone(b, false, 0, 0.15f, 440, 300, 1, 0.4f, 0.003f, 0.08f); d["back"] = b;
            b = new float[(int)(0.3f * Synth.SR)]; Synth.Noise(b, false, 0, 0.2f, 0.7f, 0.5f, 0.08f, 0.002f, 0.15f, 1); Synth.Tone(b, false, 0, 0.26f, 220, 70, 2, 0.4f, 0.002f, 0.15f); d["hurt"] = b;
            b = new float[(int)(0.7f * Synth.SR)]; Synth.Noise(b, false, 0, 0.6f, 0.8f, 0.03f, 0.5f, 0.12f, 0.35f, 2); Synth.Dum(b, false, 0.14f, 0.7f); d["burn"] = b;
            b = new float[(int)(0.9f * Synth.SR)]; Synth.Bell(b, false, 0, 659.3f, 0.8f, 0.3f); Synth.Bell(b, false, 0.06f, 880, 0.8f, 0.25f); Synth.Bell(b, false, 0.12f, 1318.5f, 0.7f, 0.2f); d["warm"] = b;
            b = new float[(int)(0.3f * Synth.SR)]; Synth.Bell(b, false, 0, 1568, 0.25f, 0.3f); Synth.Bell(b, false, 0.05f, 2093, 0.22f, 0.25f); d["spark"] = b;
            b = new float[(int)(0.07f * Synth.SR)]; Synth.Noise(b, false, 0, 0.06f, 0.5f, 0.25f, 0.05f, 0.004f, 0.04f, 3); d["step"] = b;
            b = new float[(int)(1.3f * Synth.SR)];
            { float[] fr = { 440, 554.4f, 659.3f, 880 }; for (int i = 0; i < 4; i++) Synth.Bell(b, false, i * 0.11f, fr[i], 0.9f, 0.28f); }
            d["save"] = b;
            b = new float[(int)(1.0f * Synth.SR)]; Synth.Dum(b, false, 0, 0.8f); Synth.Saz(b, false, 0.02f, 220, 0.8f, 0.4f, 5); Synth.Saz(b, false, 0.16f, 233.1f, 0.8f, 0.4f, 6); Synth.Saz(b, false, 0.30f, 277.2f, 0.9f, 0.45f, 7); d["encounter"] = b;
            b = new float[(int)(1.2f * Synth.SR)]; Synth.Noise(b, false, 0, 1.1f, 0.5f, 0.4f, 0.02f, 0.01f, 0.8f, 4); Synth.Tone(b, false, 0, 1.0f, 420, 60, 1, 0.35f, 0.01f, 0.6f); d["dissolve"] = b;
            b = new float[(int)(1.8f * Synth.SR)];
            { float[] fr = { 523.3f, 659.3f, 784, 1046.5f, 1318.5f }; for (int i = 0; i < 5; i++) Synth.Bell(b, false, i * 0.14f, fr[i], 1.1f, 0.26f); }
            d["peace"] = b;
            b = new float[(int)(0.07f * Synth.SR)]; Synth.Tone(b, false, 0, 0.06f, 1250, 1250, 1, 0.3f, 0.003f, 0.03f); d["warn"] = b;
            b = new float[(int)(0.14f * Synth.SR)]; Synth.Noise(b, false, 0, 0.12f, 0.6f, 0.7f, 0.15f, 0.002f, 0.1f, 8); d["strike"] = b;
            b = new float[(int)(0.5f * Synth.SR)]; Synth.Bell(b, false, 0, 1318.5f, 0.4f, 0.3f); Synth.Bell(b, false, 0, 1975.5f, 0.4f, 0.22f); Synth.Noise(b, false, 0, 0.1f, 0.4f, 0.8f, 0.3f, 0.002f, 0.08f, 9); d["crit"] = b;
            b = new float[(int)(3.6f * Synth.SR)];
            Synth.Pad(b, false, 0, 130.8f, 2.8f, 0.5f); Synth.Pad(b, false, 0.3f, 196, 2.5f, 0.4f); Synth.Pad(b, false, 0.6f, 329.6f, 2.2f, 0.35f);
            Synth.Bell(b, false, 1.0f, 1046.5f, 1.8f, 0.2f);
            d["dawn"] = b;
            b = new float[(int)(1.6f * Synth.SR)]; Synth.Tone(b, false, 0, 1.5f, 196, 98, 0, 0.5f, 0.02f, 1.0f); Synth.Noise(b, false, 0, 1.2f, 0.3f, 0.1f, 0.01f, 0.01f, 1.0f, 11); d["out"] = b;

            foreach (var kv in d)
            {
                float m = 0;
                foreach (float v in kv.Value) m = Math.Max(m, Math.Abs(v));
                if (m > 0.75f) { float k = 0.75f / m; for (int i = 0; i < kv.Value.Length; i++) kv.Value[i] *= k; }
            }
            return d;
        }
    }

    sealed class Audio
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct WAVEFORMATEX { public short wFormatTag, nChannels; public int nSamplesPerSec, nAvgBytesPerSec; public short nBlockAlign, wBitsPerSample, cbSize; }

        [StructLayout(LayoutKind.Sequential)]
        struct WAVEHDR { public IntPtr lpData; public int dwBufferLength, dwBytesRecorded; public IntPtr dwUser; public int dwFlags, dwLoops; public IntPtr lpNext, reserved; }

        [DllImport("winmm.dll")] static extern int waveOutOpen(out IntPtr h, int dev, ref WAVEFORMATEX fmt, IntPtr cb, IntPtr inst, int flags);
        [DllImport("winmm.dll")] static extern int waveOutPrepareHeader(IntPtr h, IntPtr hdr, int size);
        [DllImport("winmm.dll")] static extern int waveOutWrite(IntPtr h, IntPtr hdr, int size);
        [DllImport("winmm.dll")] static extern int waveOutUnprepareHeader(IntPtr h, IntPtr hdr, int size);
        [DllImport("winmm.dll")] static extern int waveOutReset(IntPtr h);
        [DllImport("winmm.dll")] static extern int waveOutClose(IntPtr h);

        sealed class Voice { public float[] D; public double Pos, Rate; public float Vol; }

        readonly object sync = new object();
        readonly Dictionary<string, float[]> sfx;
        readonly Dictionary<string, float[]> tracks = new Dictionary<string, float[]>();
        readonly List<Voice> voices = new List<Voice>();
        float[] cur, prev;
        double curPos, prevPos;
        float curGain, prevGain, fadeStep = 1f / (0.8f * Synth.SR);
        string wantMusic, curName;
        volatile bool run;
        Thread mixThread, renderThread;

        public bool Muted;
        public bool HasDevice { get; private set; }

        public Audio(bool device)
        {
            sfx = Tunes.MakeSfx();
            if (!device) return;
            run = true;
            renderThread = new Thread(RenderAll) { IsBackground = true, Priority = ThreadPriority.BelowNormal };
            renderThread.Start();
            mixThread = new Thread(MixLoop) { IsBackground = true, Priority = ThreadPriority.AboveNormal };
            mixThread.Start();
        }

        void RenderAll()
        {
            foreach (string n in Tunes.Names)
            {
                if (!run) return;
                try
                {
                    float[] t = Tunes.Make(n);
                    lock (sync) tracks[n] = t;
                }
                catch { }
            }
        }

        public void Play(string name, float vol = 1f, float rate = 1f)
        {
            float[] d;
            if (!run || !sfx.TryGetValue(name, out d)) return;
            lock (sync)
            {
                if (voices.Count > 24) voices.RemoveAt(0);
                voices.Add(new Voice { D = d, Rate = rate, Vol = vol });
            }
        }

        public void Music(string name)
        {
            lock (sync) wantMusic = name;
        }

        void Fill(float[] o)
        {
            Array.Clear(o, 0, o.Length);
            lock (sync)
            {
                if (wantMusic != curName)
                {
                    float[] t = null;
                    if (wantMusic == null || tracks.TryGetValue(wantMusic, out t))
                    {
                        prev = cur; prevPos = curPos; prevGain = curGain;
                        cur = t; curPos = 0; curGain = 0; curName = wantMusic;
                    }
                }
                int n = o.Length;
                if (cur != null)
                    for (int i = 0; i < n; i++)
                    {
                        if (curGain < 1) curGain = Math.Min(1, curGain + fadeStep);
                        o[i] += cur[(int)curPos] * curGain * 0.55f;
                        curPos += 1; if (curPos >= cur.Length) curPos = 0;
                    }
                if (prev != null)
                {
                    for (int i = 0; i < n && prevGain > 0; i++)
                    {
                        prevGain -= fadeStep;
                        if (prevGain <= 0) break;
                        o[i] += prev[(int)prevPos] * prevGain * 0.55f;
                        prevPos += 1; if (prevPos >= prev.Length) prevPos = 0;
                    }
                    if (prevGain <= 0) prev = null;
                }
                for (int v = voices.Count - 1; v >= 0; v--)
                {
                    Voice vc = voices[v];
                    for (int i = 0; i < n; i++)
                    {
                        int ix = (int)vc.Pos;
                        if (ix >= vc.D.Length - 1) { voices.RemoveAt(v); break; }
                        float fr = (float)(vc.Pos - ix);
                        o[i] += (vc.D[ix] + (vc.D[ix + 1] - vc.D[ix]) * fr) * vc.Vol * 0.6f;
                        vc.Pos += vc.Rate;
                    }
                }
            }
            if (Muted) Array.Clear(o, 0, o.Length);
        }

        void MixLoop()
        {
            const int NB = 4, N = 512;
            IntPtr h = IntPtr.Zero;
            var hdr = new IntPtr[NB];
            var data = new IntPtr[NB];
            int hdrSize = Marshal.SizeOf(typeof(WAVEHDR));
            int flagsOff = (int)Marshal.OffsetOf(typeof(WAVEHDR), "dwFlags");
            try
            {
                var fmt = new WAVEFORMATEX
                {
                    wFormatTag = 1, nChannels = 1, nSamplesPerSec = Synth.SR, nAvgBytesPerSec = Synth.SR * 2, nBlockAlign = 2, wBitsPerSample = 16, cbSize = 0
                };
                if (waveOutOpen(out h, -1, ref fmt, IntPtr.Zero, IntPtr.Zero, 0) != 0) { run = false; return; }
                HasDevice = true;
                for (int i = 0; i < NB; i++)
                {
                    data[i] = Marshal.AllocHGlobal(N * 2);
                    hdr[i] = Marshal.AllocHGlobal(hdrSize);
                    var wh = new WAVEHDR { lpData = data[i], dwBufferLength = N * 2 };
                    Marshal.StructureToPtr(wh, hdr[i], false);
                    waveOutPrepareHeader(h, hdr[i], hdrSize);
                }
                var mix = new float[N];
                var pcm = new short[N];
                var queued = new bool[NB];
                while (run)
                {
                    bool did = false;
                    for (int i = 0; i < NB; i++)
                    {
                        int flags = Marshal.ReadInt32(hdr[i], flagsOff);
                        if (queued[i] && (flags & 1) == 0) continue;
                        Fill(mix);
                        for (int k = 0; k < N; k++)
                        {
                            float v = mix[k];
                            v = v > 1.5f ? 1f : (v < -1.5f ? -1f : v - v * v * v * 0.148f); // yumuşak kırpma
                            pcm[k] = (short)(v * 30000);
                        }
                        Marshal.Copy(pcm, 0, data[i], N);
                        waveOutWrite(h, hdr[i], hdrSize);
                        queued[i] = true;
                        did = true;
                    }
                    if (!did) Thread.Sleep(4);
                }
            }
            catch { }
            finally
            {
                try
                {
                    if (h != IntPtr.Zero)
                    {
                        waveOutReset(h);
                        for (int i = 0; i < NB; i++)
                            if (hdr[i] != IntPtr.Zero) { waveOutUnprepareHeader(h, hdr[i], hdrSize); Marshal.FreeHGlobal(hdr[i]); Marshal.FreeHGlobal(data[i]); }
                        waveOutClose(h);
                    }
                }
                catch { }
            }
        }

        public void Shutdown()
        {
            run = false;
            try { if (mixThread != null) mixThread.Join(600); } catch { }
        }

        // Geliştirme: müzikleri WAV olarak yazar ve ölçer
        public static string Dump(string dir)
        {
            Directory.CreateDirectory(dir);
            var sb = new System.Text.StringBuilder();
            foreach (string n in Tunes.Names)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                float[] t = Tunes.Make(n);
                sw.Stop();
                WriteWav(Path.Combine(dir, n + ".wav"), t);
                sb.AppendLine(Stats(n, t) + " render=" + sw.ElapsedMilliseconds + "ms");
            }
            foreach (var kv in Tunes.MakeSfx()) sb.AppendLine(Stats("sfx:" + kv.Key, kv.Value));

            // akort denetimi: çalgıların ürettiği frekans, istenen notaya ne kadar yakın?
            foreach (float f in new[] { 110f, 220f, 329.63f, 440f, 659.26f, 880f })
            {
                var a = new float[Synth.SR]; Synth.Pluck(a, false, 0, f, 2f, 0.5f, 0.55f, 1);
                var b = new float[Synth.SR]; Synth.Ney(b, false, 0, f, 0.8f, 0.5f, 1);
                float fa = Pitch(a, f), fb = Pitch(b, f);
                sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, "akort {0,7:F2} Hz  saz {1,7:F2} ({2,5:F1} cent)  ney {3,7:F2} ({4,5:F1} cent)",
                    f, fa, 1200 * Math.Log(fa / f, 2), fb, 1200 * Math.Log(fb / f, 2)));
            }
            return sb.ToString();
        }

        // Öz ilinti ile temel frekans kestirimi
        static float Pitch(float[] d, float expect)
        {
            int start = Synth.SR / 5, n = Synth.SR / 2;
            double best = double.MinValue, bestLag = Synth.SR / expect;
            double lo = Synth.SR / (expect * 1.06), hi = Synth.SR / (expect * 0.94);
            for (double lag = lo; lag <= hi; lag += 0.02)
            {
                double sum = 0;
                int l0 = (int)lag;
                double fr = lag - l0;
                for (int i = start; i < start + n; i++)
                {
                    double v = d[i + l0] + (d[i + l0 + 1] - d[i + l0]) * fr;
                    sum += d[i] * v;
                }
                if (sum > best) { best = sum; bestLag = lag; }
            }
            return (float)(Synth.SR / bestLag);
        }

        static string Stats(string name, float[] d)
        {
            double peak = 0, sum = 0;
            bool bad = false;
            foreach (float v in d)
            {
                if (float.IsNaN(v) || float.IsInfinity(v)) bad = true;
                peak = Math.Max(peak, Math.Abs(v));
                sum += v * v;
            }
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0,-14} {1,6:F2}s peak={2:F3} rms={3:F3}{4}",
                name, d.Length / (float)Synth.SR, peak, Math.Sqrt(sum / Math.Max(1, d.Length)), bad ? " NaN!" : "");
        }

        static void WriteWav(string path, float[] d)
        {
            using (var bw = new BinaryWriter(File.Create(path)))
            {
                bw.Write(new[] { 'R', 'I', 'F', 'F' }); bw.Write(36 + d.Length * 2); bw.Write(new[] { 'W', 'A', 'V', 'E' });
                bw.Write(new[] { 'f', 'm', 't', ' ' }); bw.Write(16); bw.Write((short)1); bw.Write((short)1);
                bw.Write(Synth.SR); bw.Write(Synth.SR * 2); bw.Write((short)2); bw.Write((short)16);
                bw.Write(new[] { 'd', 'a', 't', 'a' }); bw.Write(d.Length * 2);
                foreach (float v in d) bw.Write((short)(Math.Max(-1f, Math.Min(1f, v)) * 30000));
            }
        }
    }
}
