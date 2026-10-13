using System;
using System.Drawing;

namespace WinterDeveloper.SonKis
{
    // Tutunma noktası (ayak ortası) olan sprite
    sealed class Spr
    {
        public Bitmap Bmp;
        public int AX, AY, W, H;

        public Spr(Pix p, int ax, int ay) { Bmp = p.Bmp(); W = p.W; H = p.H; AX = ax; AY = ay; }

        public void Draw(Graphics g, float x, float y, int scale = 1)
        {
            g.DrawImage(Bmp, new Rectangle((int)Math.Round(x) - AX * scale, (int)Math.Round(y) - AY * scale, W * scale, H * scale));
        }
    }

    // Bütün görseller kodla üretilir; dışarıdan dosya yoktur
    static class Art
    {
        static readonly int Ink = P.I(0x0B0F24);

        public static Spr NarStand, NarWalk, NarStandL, NarWalkL, Nine;
        public static Spr[] Koncol, Turna, Boru, Ayaz, Carsamba, Sahmeran, Tepegoz, Yel; // [0] üşümüş, [1] ısınmış
        public static Spr[][] Spirits; // ruh numarasına göre
        public static Spr Stone;
        public static Spr PineS, PineM, PineL, Birch, BirchB, Bush, Rock, RockB, Mound, Fence, Sign, Sled, Boat, Reeds, Cairn, Balbal, Pole;
        public static Spr House0, House1, House2, School, HouseLit, GunOcagi, Char, Ash, FishHole, Hearth, WishTree, Ember;

        static int K(char c)
        {
            switch (c)
            {
                case 'k': return Ink;
                case 'R': return P.I(0xD8433B);
                case 'r': return P.I(0x9B2733);
                case 'L': return P.I(0xF2705A);
                case 's': return P.I(0xF2C9A0);
                case 'S': return P.I(0xD39A78);
                case 'e': return P.I(0x1A1630);
                case 'w': return P.I(0xF4F8FF);
                case 'W': return P.I(0xCBDCF5);
                case 'y': return P.I(0xF2C46D);
                case 'Y': return P.I(0xC98F3C);
                case 'd': return P.I(0x2B3566);
                case 'D': return P.I(0x1C2347);
                case 'b': return P.I(0x7A5238);
                case 'B': return P.I(0x4A3226);
                case 'p': return P.I(0x8A5FA8);
                case 'P': return P.I(0x5E3F7A);
                case 'g': return P.I(0x8C93A8);
                case 'G': return P.I(0x5E6580);
                case 'n': return P.I(0x2F5D50);
                case 'N': return P.I(0x1F4038);
                case 'h': return P.I(0xDDE3EE);
                case 'o': return P.I(0xFF9A3C);
                case 'c': return P.I(0xE9A58F);
                default: return 0;
            }
        }

        static Pix FromRows(string[] rows)
        {
            int w = 0;
            foreach (var r in rows) w = Math.Max(w, r.Length);
            var p = new Pix(w + 2, rows.Length + 2);
            p.Rows(1, 1, rows, K);
            p.Outline(Ink);
            return p;
        }

        public static void Init()
        {
            if (NarStand != null) return;

            // ---------- NAR (oyuncu) ----------
            string[] nar =
            {
                "....rRRRR....",
                "...rRRRRRRR..",
                "..rRRRRRRRRR.",
                "..rRRRrBBBBBB",
                "..rRRrBssssss",
                "..rRRrssesses",
                "..rRRrsssscss",
                "..rRRRrssssss",
                "...rRRRrrsss.",
                "..yyyyyyyyyy.",
                "..YrRRRRRRRR.",
                "...rRRRLRRRR.",
                "...rRRRLRRRyy",
                "...rRRRLRRRyy",
                "..rrRRRRRRRR.",
                "..rrrRRRRRRr.",
                "....dd...dd..",
                "....dd...dd..",
                "...Bbb..Bbb.."
            };
            string[] narW = (string[])nar.Clone();
            narW[16] = "...ddd..ddd..";
            narW[17] = "..dd.....dd..";
            narW[18] = ".Bbb.....Bbb.";
            Pix a = FromRows(nar), b = FromRows(narW);
            NarStand = new Spr(a, 7, 20);
            NarWalk = new Spr(b, 7, 20);
            NarStandL = new Spr(a.Flipped(), 7, 20);
            NarWalkL = new Spr(b.Flipped(), 7, 20);

            // ---------- NİNE ----------
            Pix nine = FromRows(new[]
            {
                "....pppppp....",
                "...pppppppp...",
                "..ppPppppPpp..",
                "..pphhhhhhpp..",
                "..phsssssshp..",
                "..pseesseesp..",
                "..pscsssscsp..",
                "..ppssSSsspp..",
                "...ppsssspp...",
                "..gggppppggg..",
                ".gggggggggggg.",
                ".gGgggggggGgg.",
                ".gGggggggggGg.",
                ".ggGggssgGggg.",
                "..ggnnnnnngg..",
                "..nnnnnnnnnn..",
                "..nNnnnnnnNn..",
                "..nNnnnnnnNn..",
                "..nnnnnnnnnn..",
                "..nnnnnnnnnn.."
            });
            Nine = new Spr(nine, 8, 21);

            Koncol = new[] { MakeKoncol(false), MakeKoncol(true) };
            Turna = new[] { MakeTurna(false), MakeTurna(true) };
            Boru = new[] { MakeBoru(false), MakeBoru(true) };
            Ayaz = new[] { MakeAyaz(false), MakeAyaz(true) };
            Carsamba = new[] { MakeCarsamba(false), MakeCarsamba(true) };
            Sahmeran = new[] { MakeSahmeran(false), MakeSahmeran(true) };
            Tepegoz = new[] { MakeTepegoz(false), MakeTepegoz(true) };
            Yel = new[] { MakeYel(false), MakeYel(true) };
            Spirits = new[] { Koncol, Turna, Boru, Ayaz, Carsamba, Sahmeran, Tepegoz, Yel };
            Stone = MakeRock(12, 9, 7);

            PineS = MakePine(22, 34, 3);
            PineM = MakePine(30, 48, 4);
            PineL = MakePine(40, 66, 5);
            Birch = MakeBirch(1);
            BirchB = MakeBirch(2);
            Bush = MakeBush();
            Rock = MakeRock(18, 11, 3);
            RockB = MakeRock(30, 18, 7);
            Mound = MakeMound();
            Fence = MakeFence();
            Sign = MakeSign();
            Sled = MakeSled();
            Boat = MakeBoat();
            Reeds = MakeReeds();
            Cairn = MakeCairn();
            Balbal = MakeBalbal();
            Pole = MakePole();
            House0 = MakeHouse(0, false);
            House1 = MakeHouse(1, false);
            House2 = MakeHouse(2, false);
            HouseLit = MakeHouse(0, true);
            School = MakeSchool();
            GunOcagi = MakeGunOcagi();
            Char = MakeChar();
            Ash = MakeAsh();
            FishHole = MakeFishHole();
            Hearth = MakeHearth();
            WishTree = MakePine(40, 66, 5);
            Ember = MakeEmber();
        }

        static Spr Finish(Pix p, int ax, int ay) { p.Outline(Ink); return new Spr(p, ax, ay); }

        // Kürk dokusu: dolu piksellere açık/koyu benekler serper
        static void Fur(Pix p, int light, int dark, int target, float density, int seed)
        {
            for (int y = 0; y < p.H; y++)
                for (int x = 0; x < p.W; x++)
                {
                    if (p.D[y * p.W + x] != target) continue;
                    float h = Fx.Hash(x + seed * 31, y * 3 + seed);
                    if (h < density) p.D[y * p.W + x] = light;
                    else if (h > 1f - density) p.D[y * p.W + x] = dark;
                }
        }

        // ---------- KARAKONCOLOS ----------
        static Spr MakeKoncol(bool warm)
        {
            var p = new Pix(46, 48);
            int fur = P.I(0x241E36), furL = P.I(0x3D3358), furD = P.I(0x16122A), hand = P.I(0x352C4E);
            int horn = P.I(0xE9DEC2), hornD = P.I(0xB9A98A), eye = P.I(0xFFD84A), eyeD = P.I(0xE0A62E);
            int mouth = P.I(0x5A1830), tooth = P.I(0xF6ECD6), frost = P.I(0xA9C7EE), frostL = P.I(0xE4F0FF);
            float cx = 23;

            // bacak ve ayaklar
            p.Rect(15, 38, 6, 6, furD); p.Rect(25, 38, 6, 6, furD);
            p.Ellipse(17.5f, 44.5f, 5.2f, 2.3f, hand); p.Ellipse(28.5f, 44.5f, 5.2f, 2.3f, hand);
            // kollar
            p.Ellipse(7.5f, 28, 3.3f, 10, fur); p.Ellipse(38.5f, 28, 3.3f, 10, fur);
            p.Ellipse(7.5f, 38.5f, 3.7f, 3f, hand); p.Ellipse(38.5f, 38.5f, 3.7f, 3f, hand);
            for (int i = -1; i <= 1; i++)
            {
                p.Rect(7 + i * 3, 41, 1, 2, tooth);
                p.Rect(38 + i * 3, 41, 1, 2, tooth);
            }
            // kulaklar
            p.Poly(furL, 11, 14, 3, 11, 5, 17, 12, 19);
            p.Poly(furL, 35, 14, 43, 11, 41, 17, 34, 19);
            // boynuzlar
            p.Poly(horn, 16, 11, 12, 2, 14, 2, 20, 9);
            p.Poly(horn, 30, 11, 34, 2, 32, 2, 26, 9);
            p.Set(13, 3, hornD); p.Set(14, 5, hornD); p.Set(15, 7, hornD);
            p.Set(33, 3, hornD); p.Set(32, 5, hornD); p.Set(31, 7, hornD);
            // gövde + dikenli kürk
            p.Ellipse(cx, 25, 14, 15, fur);
            for (int i = 0; i < 22; i++)
            {
                double an = i * Math.PI * 2 / 22 + 0.1;
                float bx = cx + (float)Math.Cos(an) * 13f, by = 25 + (float)Math.Sin(an) * 14f;
                float tx = cx + (float)Math.Cos(an + 0.12) * 17.5f, ty = 25 + (float)Math.Sin(an + 0.12) * 18.5f;
                float nx = (float)-Math.Sin(an) * 2.4f, ny = (float)Math.Cos(an) * 2.4f;
                if (ty > 40) continue;
                p.Poly(fur, bx - nx, by - ny, tx, ty, bx + nx, by + ny);
            }
            // kürk dokusu
            p.OnlyOpaque = true;
            for (int y = 8; y < 30; y++)
                for (int x = 9; x < 30; x++)
                    if (p.Get(x, y) == fur && Fx.Dist(x, y, 17, 17) < 9) p.Set(x, y, furL);
            p.OnlyOpaque = false;
            Fur(p, P.I(0x54487A), furD, furL, 0.14f, 3);
            Fur(p, furL, furD, fur, 0.12f, 5);

            // gözler
            p.Ellipse(17, 21, 4.4f, 4.8f, eye); p.Ellipse(29, 21, 4.4f, 4.8f, eye);
            p.Rect(14, 24, 6, 1, eyeD); p.Rect(26, 24, 6, 1, eyeD);
            if (warm)
            {
                p.Rect(16, 19, 3, 4, Ink); p.Rect(28, 19, 3, 4, Ink);
                p.Set(16, 19, P.I(0xFFFFFF)); p.Set(28, 19, P.I(0xFFFFFF));
                // gülümseme
                p.Poly(mouth, 15, 30, 31, 30, 28, 35, 18, 35);
                p.Rect(17, 30, 2, 2, tooth); p.Rect(27, 30, 2, 2, tooth);
                p.Rect(20, 33, 6, 1, P.I(0xC2506A));
            }
            else
            {
                p.Rect(17, 21, 2, 3, Ink); p.Rect(28, 21, 2, 3, Ink);
                // çatık kaşlar
                p.Line(12, 15, 20, 18, furD); p.Line(12, 16, 20, 19, furD);
                p.Line(34, 15, 26, 18, furD); p.Line(34, 16, 26, 19, furD);
                // asık, sivri dişli ağız
                p.Poly(mouth, 15, 34, 18, 30, 28, 30, 31, 34);
                for (int i = 0; i < 4; i++) { p.Set(18 + i * 3, 30, tooth); p.Set(18 + i * 3, 31, tooth); p.Set(19 + i * 3, 30, tooth); }
                // kırağı ve buz sarkıtları
                for (int y = 0; y < 30; y++)
                    for (int x = 0; x < p.W; x++)
                    {
                        int c = p.Get(x, y);
                        if ((c == fur || c == furL || c == furD) && Fx.Hash(x * 7, y * 13) < 0.10f) p.Set(x, y, frost);
                    }
                p.Rect(4, 17, 1, 3, frost); p.Rect(41, 17, 1, 3, frost); p.Set(4, 20, frostL); p.Set(41, 20, frostL);
                p.Rect(6, 41, 1, 3, frost); p.Rect(39, 41, 1, 3, frost);
                p.Rect(13, 2, 2, 1, frostL); p.Rect(32, 2, 2, 1, frostL);
            }
            return Finish(p, 23, 47);
        }

        // ---------- TELLİ TURNA ----------
        static Spr MakeTurna(bool warm)
        {
            var p = new Pix(44, 54);
            int grey = P.I(0xA8B4C8), greyD = P.I(0x7D8AA3), greyL = P.I(0xD3DCEA), black = P.I(0x1C1E2E), blackL = P.I(0x34384E);
            int white = P.I(0xF4F8FF), beak = P.I(0xE6C36A), eye = P.I(0xE0483A), leg = P.I(0x2A2C3C);
            int ice = P.I(0x8FD3E8), iceL = P.I(0xD8F4FC), iceD = P.I(0x4F8FB8);

            // açılmış kanat (ısınınca)
            if (warm)
            {
                p.Poly(greyD, 22, 26, 31, 8, 36, 5, 41, 9, 38, 14, 40, 17, 35, 20, 36, 24, 30, 28);
                p.Poly(black, 31, 8, 36, 5, 41, 9, 38, 11);
                p.Line(30, 12, 35, 22, grey); p.Line(33, 10, 37, 18, grey);
            }
            // bacaklar
            p.Rect(20, 35, 1, 13, leg); p.Rect(26, 35, 1, 13, leg);
            p.Set(21, 41, leg); p.Set(27, 41, leg);
            p.Rect(17, 48, 6, 1, leg); p.Rect(24, 48, 6, 1, leg);
            // kuyruk tüyleri
            p.Poly(black, 30, 24, 40, 29, 41, 36, 37, 39, 33, 33, 28, 31);
            p.Line(36, 31, 38, 37, blackL); p.Line(33, 29, 35, 34, blackL);
            // gövde
            p.Ellipse(22.5f, 28.5f, 11, 7.5f, grey);
            p.OnlyOpaque = true;
            p.Ellipse(25, 31, 9, 5, greyD);
            p.Ellipse(19, 24.5f, 8, 3, greyL);
            p.Line(18, 29, 30, 31, P.I(0x66728C)); p.Line(20, 32, 30, 34, P.I(0x66728C));
            p.OnlyOpaque = false;
            // göğüs tüyleri (siyah, sarkık)
            p.Poly(black, 10, 21, 16, 21, 15, 30, 13, 34, 11, 30);
            // boyun
            float[] nx = { 13.5f, 12.5f, 11.5f, 11.5f, 12, 12.5f, 12.5f }, ny = { 22, 19.5f, 17, 14.5f, 12, 10, 8 };
            for (int i = 0; i < nx.Length; i++) p.Ellipse(nx[i], ny[i], 2.5f, 2.4f, black);
            // baş
            p.Ellipse(12, 7.5f, 3.6f, 3.2f, black);
            p.Rect(10, 4, 5, 1, grey); p.Rect(11, 3, 3, 1, greyL);
            // gaga
            p.Poly(beak, 9, 7, 2, 9, 9, 10);
            // göz ve "tel" (beyaz süs tüyü)
            if (warm) { p.Rect(10, 8, 2, 1, eye); }
            else p.Set(11, 7, eye);
            p.Line(13, 8, 17, 11, white); p.Line(14, 8, 19, 13, white); p.Set(20, 15, white); p.Set(18, 13, white);

            if (!warm)
            {
                // ayağı saran buz
                p.Poly(ice, 12, 51, 14, 45, 19, 42, 25, 43, 29, 46, 31, 51);
                p.Poly(iceL, 15, 45, 19, 43, 21, 44, 17, 47);
                p.Line(22, 44, 24, 50, iceD); p.Line(27, 47, 28, 50, iceD); p.Line(14, 49, 17, 50, iceD);
                p.Rect(12, 51, 20, 1, iceD);
                // kırağı
                for (int y = 20; y < 36; y++)
                    for (int x = 10; x < 40; x++)
                        if ((p.Get(x, y) == grey || p.Get(x, y) == greyD) && Fx.Hash(x * 5, y * 11) < 0.08f) p.Set(x, y, white);
            }
            return Finish(p, 21, 52);
        }

        // ---------- BÖRÜ (yaşlı kurt) ----------
        static Spr MakeBoru(bool warm)
        {
            var p = new Pix(46, 43);
            int fur = P.I(0x66728E), furD = P.I(0x47516B), furL = P.I(0x8B98B5), cream = P.I(0xD5DCEA), creamD = P.I(0xAEB8CE);
            int eye = P.I(0xFFD84A), ear = P.I(0x8A5F72), scar = P.I(0xE6A0A8), tongue = P.I(0xE56B7E);

            // kuyruk
            p.Ellipse(37, 36, 6, 3.4f, furD); p.Ellipse(41, 35, 2.4f, 2f, cream);
            // kalçalar
            p.Ellipse(12, 32, 7, 7.5f, furD); p.Ellipse(33, 32, 7, 7.5f, furD);
            // gövde
            p.Ellipse(22.5f, 28, 10, 12, fur);
            p.OnlyOpaque = true;
            p.Poly(cream, 17, 20, 28, 20, 27, 30, 22.5f, 37, 18, 30);
            p.Poly(creamD, 22.5f, 24, 26, 29, 22.5f, 36, 19, 29);
            p.OnlyOpaque = false;
            // ön bacaklar ve patiler
            p.Rect(16, 30, 4, 9, furL); p.Rect(25, 30, 4, 9, furL);
            p.Rect(19, 31, 1, 8, fur); p.Rect(28, 31, 1, 8, fur);
            p.Ellipse(18, 40, 3.4f, 2f, cream); p.Ellipse(27, 40, 3.4f, 2f, cream);
            // kulaklar
            p.Poly(fur, 13, 9, 12, 0, 20, 5);
            p.Poly(fur, 32, 9, 33, 0, 25, 5);
            p.Poly(ear, 14.5f, 7, 13.5f, 3, 17.5f, 5.5f);
            p.Poly(ear, 30.5f, 7, 31.5f, 3, 27.5f, 5.5f);
            // yanak tüyleri
            p.Poly(furL, 13, 11, 8, 17, 12, 17, 15, 19, 17, 14);
            p.Poly(furL, 32, 11, 37, 17, 33, 17, 30, 19, 28, 14);
            // baş
            p.Ellipse(22.5f, 12, 9, 7.5f, fur);
            p.OnlyOpaque = true;
            p.Poly(furD, 20, 4, 25, 4, 24, 11, 21, 11);
            p.Ellipse(16, 13, 3.5f, 3f, furL); p.Ellipse(29, 13, 3.5f, 3f, furL);
            p.OnlyOpaque = false;
            // burun bölgesi
            p.Ellipse(22.5f, 16, 4.4f, 3.6f, cream);
            p.Rect(21, 13, 3, 2, Ink); p.Set(22, 15, Ink);
            p.Set(21, 16, Ink); p.Set(20, 17, Ink); p.Set(23, 16, Ink); p.Set(24, 17, Ink);
            Fur(p, furL, furD, fur, 0.10f, 9);

            if (warm)
            {
                // kısılmış, yumuşamış gözler
                p.Rect(16, 11, 3, 1, Ink); p.Set(15, 10, Ink); p.Set(19, 10, Ink);
                p.Rect(26, 11, 3, 1, Ink); p.Set(25, 10, Ink); p.Set(29, 10, Ink);
                p.Rect(22, 18, 2, 2, tongue);
            }
            else
            {
                p.Rect(16, 10, 3, 2, eye); p.Set(17, 10, Ink); p.Set(17, 11, Ink);
                p.Rect(26, 10, 3, 2, eye); p.Set(27, 10, Ink); p.Set(27, 11, Ink);
                p.Line(14, 8, 19, 9, Ink); p.Line(30, 8, 26, 9, Ink);
                // kırağı
                for (int y = 0; y < 30; y++)
                    for (int x = 0; x < p.W; x++)
                    {
                        int c = p.Get(x, y);
                        if ((c == fur || c == furD) && Fx.Hash(x * 3, y * 17) < 0.07f) p.Set(x, y, P.I(0xC8DAF2));
                    }
            }
            // eski yara izi
            p.Set(28, 6, scar); p.Set(28, 7, scar); p.Set(29, 8, scar); p.Set(29, 13, scar); p.Set(30, 14, scar);
            return Finish(p, 22, 42);
        }

        // ---------- AYAZ ATA ----------
        static Spr MakeAyaz(bool warm)
        {
            var p = new Pix(50, 64);
            int robe = P.I(0x3E5FA8), robeD = P.I(0x2A4380), robeL = P.I(0x6C8FD6), white = P.I(0xF4F8FF), whiteD = P.I(0xCBDCF5), whiteDD = P.I(0x9DB3DD);
            int skin = P.I(0xD8C8CC), skinD = P.I(0xB39AA6), ice = P.I(0x8FD3E8), iceL = P.I(0xE4F8FF), staff = P.I(0x9FB6D6), staffD = P.I(0x6B82A8);
            int amber = P.I(0xF2C46D), red = P.I(0xD8433B);

            // asa
            p.Rect(40, 8, 2, 54, staff); p.Rect(41, 8, 1, 54, staffD);
            p.Poly(ice, 41, 0, 46, 6, 41, 12, 36, 6);
            p.Poly(iceL, 41, 1, 44, 6, 41, 8, 39, 6);
            p.Set(41, 5, white); p.Set(40, 6, white);
            // kaftan
            p.Poly(robe, 16, 24, 34, 24, 39, 60, 10, 60);
            p.OnlyOpaque = true;
            p.Poly(robeD, 16, 24, 20, 24, 15, 60, 10, 60);
            p.Poly(robeD, 31, 24, 34, 24, 39, 60, 35, 60);
            p.Rect(23, 26, 4, 34, robeL);
            p.Rect(24, 26, 2, 34, P.I(0x8FB0EE));
            // etek kilim şeridi
            for (int x = 9; x < 41; x++)
            {
                int m = (x + 100) % 4;
                p.Set(x, 54, amber);
                p.Set(x, 55, m == 0 ? red : amber); p.Set(x, 56, m == 0 || m == 1 || m == 3 ? red : amber); p.Set(x, 57, m == 0 ? red : amber);
                p.Set(x, 58, amber);
            }
            p.OnlyOpaque = false;
            p.Rect(9, 59, 32, 3, white); p.Rect(9, 61, 32, 1, whiteD);
            // kuşak
            p.Rect(17, 40, 16, 2, amber); p.Rect(17, 42, 16, 1, P.I(0xB5803A));
            // kollar
            p.Poly(robeD, 16, 25, 9, 40, 14, 43, 20, 31);
            p.Poly(robeD, 34, 25, 42, 37, 38, 42, 31, 31);
            p.Rect(9, 40, 6, 2, white); p.Rect(37, 38, 6, 2, white);
            p.Ellipse(12, 43.5f, 2.6f, 2.2f, skin); p.Ellipse(40.5f, 41.5f, 2.6f, 2.2f, skin);
            // baş
            p.Ellipse(25, 17, 6.5f, 6f, skin);
            // börk (kalpak)
            p.Poly(robe, 18, 13, 32, 13, 28, 2, 22, 2);
            p.OnlyOpaque = true; p.Poly(robeD, 27, 2, 32, 13, 28, 13, 25, 2); p.OnlyOpaque = false;
            p.Rect(17, 11, 16, 4, white); p.Rect(17, 14, 16, 1, whiteD); p.Set(17, 11, 0); p.Set(32, 11, 0);
            p.Ellipse(25, 2, 2.3f, 1.8f, white);
            p.Set(25, 7, amber); p.Set(24, 8, amber); p.Set(26, 8, amber); p.Set(25, 9, amber);
            // sakal ve bıyık
            p.Poly(white, 18, 19, 32, 19, 32, 30, 28, 38, 25, 42, 22, 38, 18, 30);
            p.OnlyOpaque = true;
            p.Line(21, 24, 22, 36, whiteD); p.Line(25, 25, 25, 40, whiteD); p.Line(29, 24, 28, 36, whiteD);
            p.Line(19, 22, 19, 30, whiteDD); p.Line(31, 22, 31, 30, whiteDD);
            p.OnlyOpaque = false;
            p.Rect(20, 19, 10, 2, white); p.Rect(19, 20, 2, 2, white); p.Rect(29, 20, 2, 2, white);
            p.Set(25, 18, skinD); p.Set(24, 18, skinD);
            // kaşlar ve gözler
            if (warm)
            {
                p.Rect(20, 15, 4, 1, white); p.Rect(26, 15, 4, 1, white);
                p.Rect(21, 17, 2, 1, Ink); p.Set(20, 16, Ink); p.Set(23, 16, Ink);
                p.Rect(27, 17, 2, 1, Ink); p.Set(26, 16, Ink); p.Set(29, 16, Ink);
                p.Set(20, 18, P.I(0xE9A58F)); p.Set(29, 18, P.I(0xE9A58F));
            }
            else
            {
                p.Line(19, 15, 23, 16, white); p.Line(30, 15, 26, 16, white);
                p.Rect(21, 17, 2, 1, Ink); p.Rect(27, 17, 2, 1, Ink);
            }
            // kaftanda kırağı pırıltıları
            for (int y = 26; y < 54; y++)
                for (int x = 10; x < 40; x++)
                {
                    int c = p.Get(x, y);
                    if ((c == robe || c == robeD) && Fx.Hash(x * 9, y * 5) < 0.045f) p.Set(x, y, robeL);
                }
            return Finish(p, 25, 63);
        }

        // Dolu piksellere kırağı serper
        static void Frost(Pix p, int[] targets, float density, int yMax)
        {
            int frost = P.I(0xA9C7EE);
            for (int y = 0; y < Math.Min(yMax, p.H); y++)
                for (int x = 0; x < p.W; x++)
                {
                    int c = p.Get(x, y);
                    if (Array.IndexOf(targets, c) >= 0 && Fx.Hash(x * 7 + 3, y * 13 + 5) < density) p.Set(x, y, frost);
                }
        }

        // ---------- ÇARŞAMBA KARISI ----------
        static Spr MakeCarsamba(bool warm)
        {
            var p = new Pix(42, 52);
            int shawl = P.I(0x5A3358), shawlD = P.I(0x3C2240), shawlL = P.I(0x7E4C78), dress = P.I(0x3A3554), dressD = P.I(0x29263E);
            int skin = P.I(0xC9CDB8), skinD = P.I(0x979D8C), hair = P.I(0x8E93A8), hairL = P.I(0xC4C8D8), hairD = P.I(0x5E6278);
            int yarn = P.I(0xD8433B), yarnD = P.I(0x9B2733), wood = P.I(0x8A5A3A), white = P.I(0xF4F8FF), amber = P.I(0xF2C46D);

            // saç (arkada)
            p.Ellipse(20, 11, 10, 8, hair);
            p.Poly(hair, 11, 10, 6, 33, 12, 34, 15, 14);
            p.Poly(hair, 29, 10, 34, 33, 28, 34, 25, 14);
            if (!warm)
            {
                // darmadağın teller
                for (int i = 0; i < 13; i++)
                {
                    double an = Math.PI + i * Math.PI / 12;
                    float len = 11 + Fx.Hash(i, 71) * 5;
                    p.Line(20, 11, (int)(20 + Math.Cos(an) * len), (int)(11 + Math.Sin(an) * len * 0.9f), i % 2 == 0 ? hair : hairD);
                }
                p.Line(8, 20, 3, 27, hair); p.Line(32, 20, 37, 26, hair); p.Line(7, 30, 4, 37, hairD); p.Line(33, 30, 36, 38, hairD);
            }
            // etek
            p.Poly(dress, 13, 24, 27, 24, 33, 50, 7, 50);
            p.OnlyOpaque = true;
            p.Poly(dressD, 13, 24, 16, 24, 12, 50, 7, 50);
            p.Poly(dressD, 25, 24, 27, 24, 33, 50, 29, 50);
            for (int x = 7; x < 34; x++) { p.Set(x, 47, amber); p.Set(x, 48, (x % 4 == 0) ? yarn : amber); p.Set(x, 49, yarnD); }
            p.OnlyOpaque = false;
            // şal
            p.Poly(shawl, 10, 21, 30, 21, 32, 33, 20, 38, 8, 33);
            p.OnlyOpaque = true;
            p.Poly(shawlD, 10, 21, 14, 21, 11, 34, 8, 33);
            p.Line(20, 24, 20, 37, shawlL); p.Line(16, 23, 14, 34, shawlL); p.Line(24, 23, 26, 34, shawlL);
            p.OnlyOpaque = false;
            for (int x = 9; x < 32; x += 3) p.Rect(x, 34 + (x % 2), 1, 3, shawlL); // saçak
            // sol kol (aşağıda, uzun parmaklar)
            p.Poly(shawlD, 10, 23, 5, 35, 9, 37, 14, 27);
            p.Ellipse(7, 38, 2.4f, 2f, skin);
            p.Rect(4, 39, 1, 5, skin); p.Rect(6, 40, 1, 6, skin); p.Rect(8, 40, 1, 5, skin); p.Rect(10, 39, 1, 3, skin);
            // sağ kol (iğ tutuyor)
            p.Poly(shawlD, 30, 23, 36, 30, 33, 34, 27, 27);
            p.Rect(36, 20, 1, 20, wood);
            p.Ellipse(36.5f, 38, 3.2f, 1.6f, wood);
            p.Ellipse(36.5f, 27, 2.6f, 4.2f, yarn);
            p.OnlyOpaque = true; p.Line(35, 24, 38, 26, yarnD); p.Line(35, 27, 38, 29, yarnD); p.Line(35, 30, 38, 31, yarnD); p.OnlyOpaque = false;
            p.Ellipse(36, 33, 2.4f, 2f, skin);
            p.Rect(34, 31, 1, 4, skin); p.Rect(38, 31, 1, 4, skin);
            // ip: iğden öbür ele
            p.Line(35, 36, 24, 43, yarn); p.Line(24, 43, 9, 41, yarn);
            // baş
            p.Ellipse(20, 14, 5.6f, 6.4f, skin);
            p.OnlyOpaque = true; p.Rect(15, 18, 11, 3, skinD); p.Ellipse(20, 14, 4.6f, 5.2f, skin); p.OnlyOpaque = false;
            // perçem
            p.Poly(hair, 14, 8, 26, 8, 25, 11, 21, 10, 19, 12, 15, 11);
            p.Set(17, 9, hairL); p.Set(22, 9, hairL); p.Set(12, 14, hairL); p.Set(28, 16, hairL); p.Set(10, 24, hairL); p.Set(30, 26, hairL);
            // burun
            p.Rect(20, 14, 1, 4, skinD); p.Set(21, 17, skinD);
            if (warm)
            {
                // taranmış saç, kırmızı yün kurdele
                p.Rect(16, 3, 9, 2, yarn); p.Rect(14, 2, 3, 4, yarn); p.Rect(24, 2, 3, 4, yarn); p.Set(20, 5, yarnD);
                p.Line(13, 12, 9, 32, hairL); p.Line(27, 12, 31, 32, hairL);
                p.Set(16, 14, Ink); p.Set(17, 13, Ink); p.Set(18, 14, Ink);
                p.Set(22, 14, Ink); p.Set(23, 13, Ink); p.Set(24, 14, Ink);
                p.Set(17, 19, yarnD); p.Rect(18, 20, 5, 1, yarnD); p.Set(23, 19, yarnD);
                p.Set(15, 16, P.I(0xE9A58F)); p.Set(25, 16, P.I(0xE9A58F));
            }
            else
            {
                p.Rect(16, 13, 3, 3, white); p.Rect(22, 13, 3, 3, white);
                p.Rect(17, 14, 1, 2, Ink); p.Rect(23, 14, 1, 2, Ink);
                p.Line(15, 11, 19, 12, hairD); p.Line(25, 11, 21, 12, hairD);
                p.Rect(17, 20, 6, 1, P.I(0x5A1830)); p.Set(16, 21, P.I(0x5A1830)); p.Set(23, 21, P.I(0x5A1830));
                Frost(p, new[] { hair, hairD, shawl, shawlD, dress }, 0.05f, 46);
                p.Rect(5, 34, 1, 3, P.I(0xE4F0FF)); p.Rect(34, 34, 1, 3, P.I(0xE4F0FF)); p.Rect(3, 28, 1, 2, P.I(0xE4F0FF)); p.Rect(37, 27, 1, 2, P.I(0xE4F0FF));
            }
            return Finish(p, 20, 51);
        }

        // ---------- ŞAHMERAN ----------
        static Spr MakeSahmeran(bool warm)
        {
            var p = new Pix(56, 48);
            int sc = warm ? P.I(0x2F8F72) : P.I(0x2C6F68), scD = warm ? P.I(0x1E6050) : P.I(0x1D4C50), scL = warm ? P.I(0x6FD0A0) : P.I(0x5AA596);
            int belly = P.I(0xDCCB82), bellyD = P.I(0xA8964E), skin = P.I(0xDDBB9C), skinD = P.I(0xB08E72), hair = P.I(0x1B1630), hairL = P.I(0x3A3158);
            int gold = P.I(0xF2C46D), goldD = P.I(0xB5803A), gem = P.I(0xD8433B), vest = P.I(0x8A2F4E), vestD = P.I(0x5E1F38), white = P.I(0xF4F8FF);

            // kuyruk ucu
            p.Poly(sc, 44, 38, 50, 29, 53, 24, 54, 26, 52, 32, 48, 40);
            p.Set(54, 23, scL); p.Set(53, 24, scL);
            // alt halka
            p.Ellipse(27, 40, 23, 6.6f, scD);
            p.Ellipse(27, 39, 22, 5.4f, sc);
            // üst halka
            p.Ellipse(27, 32, 16, 6.2f, scD);
            p.Ellipse(27, 31, 15, 5f, sc);
            // gövde (yukarı doğru yılan)
            p.Poly(sc, 21, 31, 33, 31, 32, 20, 22, 20);
            p.OnlyOpaque = true;
            p.Poly(scD, 21, 31, 23, 31, 23, 20, 22, 20);
            p.Rect(25, 20, 5, 12, belly);
            for (int y = 21; y < 32; y += 2) p.Rect(25, y, 5, 1, bellyD);
            // pul deseni
            for (int y = 18; y < 47; y++)
                for (int x = 0; x < p.W; x++)
                    if (p.Get(x, y) == sc && (x + y) % 4 == 0 && (x - y + 60) % 4 == 0) p.Set(x, y, scL);
            // halkaların karın çizgisi
            for (int x = 8; x < 47; x += 3) { if (p.Get(x, 43) != 0) p.Set(x, 43, belly); if (p.Get(x, 44) != 0) p.Set(x, 44, bellyD); }
            for (int x = 15; x < 40; x += 3) if (p.Get(x, 35) == sc || p.Get(x, 35) == scL) p.Set(x, 35, belly);
            p.OnlyOpaque = false;
            // saç (arkada)
            p.Ellipse(27, 8, 6.4f, 5.6f, hair);
            p.Poly(hair, 21, 8, 18, 23, 22, 23, 24, 10);
            p.Poly(hair, 33, 8, 36, 23, 32, 23, 30, 10);
            // yelek
            p.Poly(vest, 22, 21, 32, 21, 33, 14, 21, 14);
            p.OnlyOpaque = true;
            p.Rect(26, 14, 2, 7, vestD); p.Rect(22, 20, 10, 1, gold); p.Set(27, 16, gold); p.Set(27, 18, gold);
            p.OnlyOpaque = false;
            // kollar
            p.Poly(skin, 21, 15, 15, 25, 17, 27, 23, 19);
            p.Poly(skin, 33, 15, 39, 25, 37, 27, 31, 19);
            p.Ellipse(15.5f, 27, 2.2f, 2f, skin); p.Ellipse(38.5f, 27, 2.2f, 2f, skin);
            p.Rect(15, 23, 3, 1, gold); p.Rect(36, 23, 3, 1, gold);
            // boyun ve baş
            p.Rect(26, 12, 3, 3, skinD);
            p.Ellipse(27, 9, 4.6f, 5f, skin);
            p.Poly(hair, 22, 5, 32, 5, 32, 7, 29, 6, 27, 7, 25, 6, 22, 7);
            p.Set(20, 14, hairL); p.Set(34, 16, hairL); p.Set(19, 19, hairL);
            // taç
            p.Rect(22, 3, 11, 2, gold); p.Rect(22, 4, 11, 1, goldD);
            p.Set(22, 2, gold); p.Rect(27, 0, 1, 3, gold); p.Set(32, 2, gold); p.Set(24, 2, gold); p.Set(30, 2, gold);
            p.Set(27, 3, gem);
            if (warm)
            {
                p.Rect(24, 8, 2, 2, Ink); p.Rect(29, 8, 2, 2, Ink); p.Set(24, 8, white); p.Set(29, 8, white);
                p.Set(25, 11, vestD); p.Rect(26, 12, 3, 1, vestD); p.Set(29, 11, vestD);
                p.Set(23, 10, P.I(0xE9A58F)); p.Set(31, 10, P.I(0xE9A58F));
                p.Set(27, 3, P.I(0xFF8A70));
            }
            else
            {
                // yarı kapalı gözler
                p.Rect(24, 8, 2, 1, skinD); p.Rect(29, 8, 2, 1, skinD);
                p.Rect(24, 9, 2, 1, Ink); p.Rect(29, 9, 2, 1, Ink);
                p.Rect(26, 12, 3, 1, vestD);
                Frost(p, new[] { sc, scD, scL, hair }, 0.06f, 48);
                p.Rect(13, 28, 1, 2, P.I(0xE4F0FF)); p.Rect(40, 28, 1, 2, P.I(0xE4F0FF));
            }
            return Finish(p, 27, 47);
        }

        // ---------- YEL ANA ----------
        static Spr MakeYel(bool warm)
        {
            var p = new Pix(50, 56);
            int robe = warm ? P.I(0x8FA6CC) : P.I(0x7488B6), robeD = warm ? P.I(0x62749E) : P.I(0x4C5C8C), robeL = P.I(0xC6D6F0);
            int skin = P.I(0xD9DFEA), skinD = P.I(0xA6B0C6), hair = P.I(0xE6EEFA), hairD = P.I(0xA9BADC), white = P.I(0xF4F8FF);
            int amber = P.I(0xF2C46D), fire = P.I(0xFF9A3C);

            // saç (arkada): üşürken yana savrulur, ısınınca omuzlara iner
            p.Ellipse(22, 10, 7, 6.2f, hair);
            if (warm)
            {
                p.Poly(hair, 15, 9, 12, 29, 17, 30, 18, 12);
                p.Poly(hair, 29, 9, 32, 29, 27, 30, 26, 12);
            }
            else
            {
                p.Poly(hair, 17, 5, 30, 3, 44, 5, 48, 8, 37, 9, 47, 13, 35, 14, 28, 14);
                p.Poly(hairD, 28, 12, 40, 16, 46, 20, 36, 20, 27, 16);
                p.Poly(hair, 15, 9, 13, 21, 17, 21, 18, 12);
            }
            // etek: aşağı doğru incelen hortum
            float[] cx = { 22, 21, 20, 20, 21, 23, 24, 24, 23, 22 };
            for (int i = 0; i < 10; i++)
                p.Ellipse(cx[i], 27 + i * 2.7f, 11.5f - i * 1.05f, 2.6f, i % 2 == 0 ? robe : robeD);
            p.OnlyOpaque = true;
            for (int i = 0; i < 9; i += 2)
                p.Line((int)(cx[i] - 8 + i), (int)(27 + i * 2.7f), (int)(cx[i] + 3 - i / 2), (int)(26 + i * 2.7f), robeL);
            p.OnlyOpaque = false;
            // gövde
            p.Poly(robe, 15, 18, 29, 18, 33, 29, 11, 29);
            p.OnlyOpaque = true;
            p.Poly(robeD, 15, 18, 18, 18, 14, 29, 11, 29);
            p.Line(22, 20, 22, 28, robeL);
            p.OnlyOpaque = false;
            // yenler: iki yana açılmış, uçları savruk
            p.Poly(robe, 16, 19, 6, 23, 2, 31, 8, 30, 15, 25);
            p.Poly(robe, 28, 19, 38, 23, 42, 31, 36, 30, 29, 25);
            p.OnlyOpaque = true;
            p.Line(4, 30, 9, 26, robeD); p.Line(40, 30, 35, 26, robeD);
            p.OnlyOpaque = false;
            p.Ellipse(5, 32, 2.2f, 1.8f, skin); p.Ellipse(39.5f, 32, 2.2f, 1.8f, skin);
            // boyun ve baş
            p.Rect(21, 15, 3, 3, skinD);
            p.Ellipse(22, 11, 5, 5.6f, skin);
            p.Poly(hair, 17, 6, 28, 6, 27, 8, 24, 7, 21, 9, 17, 9);
            if (warm)
            {
                // sakin gözler, göğsünde küçük bir kor
                p.Set(19, 12, Ink); p.Set(20, 11, Ink); p.Set(21, 12, Ink);
                p.Set(23, 12, Ink); p.Set(24, 11, Ink); p.Set(25, 12, Ink);
                p.Set(21, 14, skinD); p.Rect(22, 15, 2, 1, skinD); p.Set(24, 14, skinD);
                p.Set(18, 13, P.I(0xE9A58F)); p.Set(26, 13, P.I(0xE9A58F));
                p.Rect(21, 22, 3, 3, fire); p.Rect(22, 21, 1, 3, amber); p.Set(22, 23, white);
                p.OnlyOpaque = true;
                for (int y = 19; y < 28; y++)
                    for (int x = 17; x < 28; x++)
                    {
                        float d = Fx.Dist(x, y, 22, 23);
                        if (d > 1.6f && d < 4.5f) p.Blend(x, y, amber, 0.34f - d * 0.06f);
                    }
                p.OnlyOpaque = false;
                p.Rect(13, 26, 1, 3, hairD); p.Rect(31, 26, 1, 3, hairD);
            }
            else
            {
                // oyuk gözler, üfleyen ağız
                p.Rect(19, 11, 2, 2, Ink); p.Rect(24, 11, 2, 2, Ink);
                p.Set(19, 11, skinD); p.Set(25, 11, skinD);
                p.Rect(22, 14, 1, 2, P.I(0x5A6690));
                p.Set(18, 13, P.I(0xB9CBEA)); p.Set(26, 13, P.I(0xB9CBEA));
                Frost(p, new[] { robe, robeD }, 0.07f, 54);
                p.Set(46, 16, white); p.Set(44, 23, white); p.Set(3, 20, white); p.Set(40, 38, white); p.Set(8, 40, white);
            }
            return Finish(p, 22, 55);
        }

        // ---------- TEPEGÖZ ----------
        static Spr MakeTepegoz(bool warm)
        {
            var p = new Pix(58, 58);
            int skin = P.I(0x7C86A0), skinD = P.I(0x596280), skinL = P.I(0x9FAAC4), vest = P.I(0xDCD5C0), vestD = P.I(0xA89F8A), pants = P.I(0x4A3A30), pantsD = P.I(0x33271F);
            int white = P.I(0xF4F0E0), iris = P.I(0xE0A62E), irisD = P.I(0xB5803A), wood = P.I(0x8A5A3A), woodD = P.I(0x5E3C26), tooth = P.I(0xF6ECD6), mouth = P.I(0x5A1830), snow = P.I(0xF4F8FF);

            // çoban değneği
            p.Rect(52, 10, 2, 46, wood); p.Rect(53, 10, 1, 46, woodD);
            p.Rect(47, 7, 7, 2, wood); p.Rect(47, 7, 2, 6, wood); p.Set(48, 12, woodD);
            // bacaklar
            p.Rect(17, 44, 9, 10, pants); p.Rect(31, 44, 9, 10, pants);
            p.Rect(17, 44, 2, 10, pantsD); p.Rect(31, 44, 2, 10, pantsD);
            p.Ellipse(20.5f, 55, 6.6f, 2.6f, skinD); p.Ellipse(36.5f, 55, 6.6f, 2.6f, skinD);
            // kollar
            p.Ellipse(8, 34, 5.4f, 12.5f, skin); p.Ellipse(49, 34, 5.4f, 12.5f, skin);
            p.Ellipse(8, 46.5f, 5.8f, 4.6f, skinD); p.Ellipse(50, 46.5f, 5.8f, 4.6f, skinD);
            p.Rect(5, 46, 1, 3, skin); p.Rect(8, 46, 1, 3, skin); p.Rect(11, 46, 1, 3, skin);
            p.Rect(47, 46, 1, 3, skin); p.Rect(50, 46, 1, 3, skin);
            // gövde
            p.Ellipse(28.5f, 33, 16.5f, 15, skin);
            // post yelek
            p.OnlyOpaque = true;
            p.Poly(vest, 12, 22, 24, 22, 22, 47, 12, 47);
            p.Poly(vest, 33, 22, 45, 22, 45, 47, 35, 47);
            p.Rect(12, 44, 10, 3, vestD); p.Rect(35, 44, 10, 3, vestD);
            p.OnlyOpaque = false;
            Fur(p, snow, vestD, vest, 0.08f, 9);
            p.Ellipse(28.5f, 40, 4.6f, 3.4f, skinL); // göbek
            p.Set(28, 41, skinD);
            // baş
            p.Ellipse(28.5f, 14, 12, 11, skin);
            p.Ellipse(16, 15, 2.6f, 3.6f, skinD); p.Ellipse(41, 15, 2.6f, 3.6f, skinD);
            p.OnlyOpaque = true;
            p.Ellipse(28.5f, 22, 9, 4, skinD);
            p.Ellipse(28.5f, 20, 9, 4.6f, skin);
            p.OnlyOpaque = false;
            Fur(p, skinL, skinD, skin, 0.07f, 4);
            p.Rect(26, 2, 5, 2, P.I(0x3A3040)); p.Set(28, 1, P.I(0x3A3040)); // perçem
            // tek göz
            p.Ellipse(28.5f, 12, 6.6f, 5.4f, white);
            if (warm)
            {
                p.Ellipse(28.5f, 12, 3.6f, 3.8f, iris);
                p.Ellipse(28.5f, 12.5f, 2f, 2.4f, Ink);
                p.Set(27, 10, white); p.Set(28, 10, white);
                p.Line(21, 4, 36, 4, skinD);
                p.Poly(mouth, 22, 20, 35, 20, 32, 24, 25, 24);
                p.Rect(25, 20, 2, 2, tooth); p.Rect(30, 20, 2, 2, tooth);
                p.Set(19, 18, P.I(0xC9A0A8)); p.Set(38, 18, P.I(0xC9A0A8));
            }
            else
            {
                p.Ellipse(28.5f, 13, 3.2f, 3f, irisD);
                p.Rect(28, 12, 2, 3, Ink);
                // ağır göz kapağı ve buzlu kirpikler
                p.Poly(skinD, 21, 6, 36, 6, 36, 11, 21, 10);
                for (int x = 22; x < 36; x += 2) p.Rect(x, 11, 1, 2, P.I(0xE4F0FF));
                p.Line(20, 3, 37, 6, skinD); p.Line(20, 4, 37, 7, skinD);
                p.Rect(23, 22, 12, 1, Ink); p.Set(22, 23, Ink); p.Set(35, 23, Ink);
                p.Rect(24, 20, 2, 2, tooth); p.Rect(32, 20, 2, 2, tooth);
                Frost(p, new[] { skin, skinD, pants }, 0.03f, 56);
                // omuzlarda ve başta kar
                p.Rect(11, 21, 11, 2, snow); p.Rect(36, 21, 10, 2, snow); p.Rect(22, 3, 4, 1, snow); p.Rect(31, 3, 5, 1, snow);
                p.Rect(4, 22, 7, 2, snow); p.Rect(46, 22, 6, 2, snow);
            }
            return Finish(p, 28, 57);
        }

        // ---------- AĞAÇLAR ----------
        static Spr MakePine(int w, int h, int tiers)
        {
            var p = new Pix(w + 2, h + 2);
            int g1 = P.I(0x1F5350), g2 = P.I(0x153C40), g3 = P.I(0x2B6B62), snow = P.I(0xEAF2FF), snowD = P.I(0xB4C8EC), trunk = P.I(0x5A4034), trunkD = P.I(0x3B2A26);
            float cx = (w + 2) / 2f;
            int trunkH = h / 7;
            p.Rect((int)cx - 2, h - trunkH, 4, trunkH + 1, trunk);
            p.Rect((int)cx, h - trunkH, 2, trunkH + 1, trunkD);
            float tierH = (h - trunkH) / (tiers * 0.62f + 0.38f);
            for (int i = tiers - 1; i >= 0; i--)
            {
                float top = 1 + i * tierH * 0.62f;
                float bot = top + tierH;
                float hw = (w / 2f) * (0.42f + 0.58f * (i + 1) / tiers);
                // sarkık dallı üçgen
                p.Poly(g1, cx, top, cx + hw, bot, cx + hw * 0.55f, bot - 2, cx + hw * 0.3f, bot + 1, cx, bot - 2,
                    cx - hw * 0.3f, bot + 1, cx - hw * 0.55f, bot - 2, cx - hw, bot);
                p.OnlyOpaque = true;
                p.Poly(g2, cx + 1, top, cx + hw, bot, cx + hw * 0.3f, bot + 1, cx + 1, bot - 2);
                for (int k = 0; k < (int)(hw * 1.2f); k++)
                {
                    int sx = (int)(cx - hw + Fx.Hash(k + i * 7, 5 + w) * hw), sy = (int)(top + tierH * (0.4f + 0.6f * Fx.Hash(k + i * 3, 9 + h)));
                    p.Set(sx, sy, g3);
                }
                // kar
                p.Poly(snow, cx, top, cx + hw * 0.5f, top + tierH * 0.5f, cx + hw * 0.2f, top + tierH * 0.42f, cx - hw * 0.1f, top + tierH * 0.62f,
                    cx - hw * 0.45f, top + tierH * 0.55f, cx - hw * 0.72f, top + tierH * 0.78f);
                p.Poly(snowD, cx + 1, top + 2, cx + hw * 0.5f, top + tierH * 0.5f, cx + hw * 0.2f, top + tierH * 0.42f);
                p.OnlyOpaque = false;
            }
            return Finish(p, (w + 2) / 2, h + 1);
        }

        static Spr MakeBirch(int variant)
        {
            int w = 34, h = variant == 1 ? 78 : 64;
            var p = new Pix(w, h);
            int bark = P.I(0xE6ECF5), barkD = P.I(0xB3C0D8), mark = P.I(0x23263A), twig = P.I(0x3A3148), snow = P.I(0xF4F8FF);
            int cx = 17;
            // dallar
            int[][] br = variant == 1
                ? new[] { new[] { 0, 34, -12, 14 }, new[] { 0, 28, 13, 10 }, new[] { 0, 20, -9, 3 }, new[] { 0, 14, 10, 1 }, new[] { -12, 14, -15, 6 }, new[] { 13, 10, 15, 2 }, new[] { 0, 44, 10, 30 }, new[] { -6, 24, -3, 12 } }
                : new[] { new[] { 0, 30, 12, 12 }, new[] { 0, 24, -13, 9 }, new[] { 0, 16, 8, 2 }, new[] { 0, 12, -8, 1 }, new[] { 12, 12, 15, 5 }, new[] { -13, 9, -15, 1 }, new[] { 0, 38, -10, 26 } };
            foreach (var bq in br)
            {
                p.Line(cx + bq[0], bq[1], cx + bq[2], bq[3], twig);
                if (Math.Abs(bq[2] - bq[0]) > 8) p.Line(cx + bq[0] + (bq[2] > bq[0] ? 1 : -1), bq[1] - 1, cx + bq[2], bq[3] - 1, snow);
            }
            // gövde
            for (int y = 4; y < h - 1; y++)
            {
                int half = y < 14 ? 1 : (y < h - 20 ? 2 : 3);
                for (int x = -half; x <= half; x++) p.Set(cx + x, y, x >= half - 1 && half > 1 ? barkD : bark);
            }
            for (int k = 0; k < 9; k++)
            {
                int y = 12 + (int)(Fx.Hash(k, variant * 7) * (h - 20));
                int side = Fx.Hash(k, 3 + variant) < 0.5f ? -2 : 0;
                p.OnlyOpaque = true;
                p.Rect(cx + side, y, 3, 1, mark);
                if (k % 3 == 0) p.Rect(cx + side + 1, y + 1, 1, 1, mark);
                p.OnlyOpaque = false;
            }
            return Finish(p, cx, h - 1);
        }

        static Spr MakeBush()
        {
            var p = new Pix(22, 15);
            int twig = P.I(0x4A3B4E), snow = P.I(0xEAF2FF), snowD = P.I(0xB4C8EC);
            int[][] t = { new[] { 10, 13, 3, 3 }, new[] { 10, 13, 8, 1 }, new[] { 11, 13, 14, 2 }, new[] { 11, 13, 18, 5 }, new[] { 10, 13, 5, 7 }, new[] { 8, 6, 6, 3 }, new[] { 14, 6, 17, 2 } };
            foreach (var q in t) p.Line(q[0], q[1], q[2], q[3], twig);
            p.Ellipse(5, 4, 3, 1.6f, snow); p.Ellipse(15, 3, 3.4f, 1.6f, snow); p.Ellipse(10, 2, 2.4f, 1.4f, snow);
            p.Ellipse(11, 13.5f, 8, 1.6f, snowD);
            return Finish(p, 11, 14);
        }

        static Spr MakeRock(int w, int h, int seed)
        {
            var p = new Pix(w + 2, h + 2);
            int r1 = P.I(0x5A6384), r2 = P.I(0x3E4666), r3 = P.I(0x7C87A8), snow = P.I(0xEAF2FF), snowD = P.I(0xB4C8EC);
            p.Poly(r1, 1, h + 1, w * 0.15f, h * 0.45f, w * 0.4f, h * 0.1f, w * 0.7f, h * 0.2f, w * 0.95f, h * 0.6f, w + 1, h + 1);
            p.OnlyOpaque = true;
            p.Poly(r2, w * 0.55f, h * 0.15f, w * 0.95f, h * 0.6f, w + 1, h + 1, w * 0.6f, h + 1);
            p.Poly(r3, w * 0.2f, h * 0.5f, w * 0.4f, h * 0.2f, w * 0.5f, h * 0.5f, w * 0.3f, h * 0.8f);
            p.Poly(snow, 0, h * 0.5f, w * 0.4f, 0, w * 0.75f, h * 0.2f, w * 0.6f, h * 0.38f, w * 0.42f, h * 0.3f, w * 0.25f, h * 0.55f);
            p.Poly(snowD, w * 0.6f, h * 0.2f, w * 0.75f, h * 0.2f, w * 0.6f, h * 0.38f);
            p.OnlyOpaque = false;
            return Finish(p, (w + 2) / 2, h + 1);
        }

        static Spr MakeMound()
        {
            var p = new Pix(40, 10);
            p.Ellipse(20, 10, 19, 8.5f, P.I(0xCBDCF5));
            p.OnlyOpaque = true;
            p.Ellipse(16, 8, 13, 5, P.I(0xEAF2FF));
            p.Ellipse(28, 12, 12, 5, P.I(0x9DB3DD));
            p.OnlyOpaque = false;
            return new Spr(p, 20, 9);
        }

        static Spr MakeFence()
        {
            var p = new Pix(36, 20);
            int wood = P.I(0x6B4A36), woodD = P.I(0x4A3226), snow = P.I(0xEAF2FF);
            p.Rect(1, 9, 34, 2, wood); p.Rect(1, 14, 34, 2, woodD);
            p.Rect(4, 4, 3, 15, wood); p.Rect(6, 4, 1, 15, woodD);
            p.Rect(28, 5, 3, 14, wood); p.Rect(30, 5, 1, 14, woodD);
            p.Rect(3, 3, 5, 2, snow); p.Rect(27, 4, 5, 2, snow); p.Rect(9, 8, 17, 1, snow);
            return Finish(p, 18, 19);
        }

        static Spr MakeSign()
        {
            var p = new Pix(26, 26);
            int wood = P.I(0x7A5238), woodD = P.I(0x4A3226), woodL = P.I(0x9A7048), snow = P.I(0xEAF2FF);
            p.Rect(12, 10, 3, 15, woodD);
            p.Rect(2, 4, 22, 10, wood); p.Rect(2, 4, 22, 1, woodL); p.Rect(2, 13, 22, 1, woodD);
            p.Rect(5, 7, 12, 1, woodD); p.Rect(5, 10, 16, 1, woodD);
            p.Rect(1, 2, 24, 2, snow); p.Rect(3, 1, 9, 1, snow);
            return Finish(p, 13, 25);
        }

        static Spr MakeSled()
        {
            var p = new Pix(30, 14);
            int wood = P.I(0x8A5A3A), woodD = P.I(0x4A3226), snow = P.I(0xEAF2FF);
            p.Rect(4, 5, 20, 2, wood); p.Rect(4, 7, 20, 1, woodD);
            p.Rect(7, 7, 2, 4, woodD); p.Rect(19, 7, 2, 4, woodD);
            p.Line(2, 11, 24, 11, wood); p.Line(24, 11, 27, 8, wood); p.Line(27, 8, 27, 5, wood);
            p.Line(13, 5, 16, 9, woodD); // kırık tahta
            p.Rect(8, 3, 9, 2, snow); p.Rect(18, 4, 4, 1, snow);
            return Finish(p, 15, 13);
        }

        static Spr MakeBoat()
        {
            var p = new Pix(46, 20);
            int wood = P.I(0x7A5238), woodD = P.I(0x4A3226), woodL = P.I(0xA57A52), snow = P.I(0xEAF2FF), ice = P.I(0xA9DDF0);
            p.Poly(wood, 2, 6, 44, 4, 38, 15, 8, 16);
            p.OnlyOpaque = true;
            p.Rect(0, 6, 46, 2, woodL); p.Rect(0, 11, 46, 1, woodD);
            p.Poly(woodD, 30, 4, 44, 4, 38, 15, 32, 15);
            p.OnlyOpaque = false;
            p.Rect(8, 3, 26, 3, snow); p.Rect(12, 2, 14, 1, snow);
            p.Rect(4, 16, 38, 2, ice); p.Rect(8, 18, 30, 1, P.I(0x4F8FB8));
            return Finish(p, 23, 18);
        }

        static Spr MakeReeds()
        {
            var p = new Pix(18, 22);
            int reed = P.I(0x6E6248), reedD = P.I(0x463E36), head = P.I(0x3A2A26), snow = P.I(0xEAF2FF);
            int[][] t = { new[] { 4, 20, 2, 5 }, new[] { 8, 20, 8, 1 }, new[] { 12, 20, 15, 6 }, new[] { 6, 20, 5, 9 }, new[] { 10, 20, 12, 10 } };
            foreach (var q in t) p.Line(q[0], q[1], q[2], q[3], reed);
            p.Rect(2, 3, 1, 4, head); p.Rect(8, 0, 1, 4, head); p.Rect(15, 4, 1, 4, head);
            p.Set(2, 2, snow); p.Set(8, 0, snow); p.Set(15, 3, snow);
            p.Line(5, 14, 5, 20, reedD);
            return Finish(p, 9, 21);
        }

        static Spr MakeCairn()
        {
            var p = new Pix(30, 16);
            int s1 = P.I(0x6B7494), s2 = P.I(0x474F70), s3 = P.I(0x8D97B8), snow = P.I(0xEAF2FF), coal = P.I(0x1A1826);
            p.Ellipse(15, 10, 9, 3.5f, coal);
            float[][] st = { new[] { 5f, 11, 4, 3 }, new[] { 10f, 13, 4, 2.6f }, new[] { 16f, 13.5f, 4, 2.4f }, new[] { 22f, 12.5f, 4, 2.8f }, new[] { 25f, 9.5f, 3.4f, 2.6f }, new[] { 20f, 6.5f, 3.6f, 2.4f }, new[] { 13f, 6, 3.6f, 2.4f }, new[] { 7f, 7.5f, 3.4f, 2.4f } };
            for (int i = 0; i < st.Length; i++)
            {
                var q = st[i];
                p.Ellipse(q[0], q[1], q[2], q[3], i % 3 == 0 ? s2 : s1);
                p.Set((int)q[0] - 1, (int)q[1] - 1, s3); p.Set((int)q[0], (int)q[1] - 1, s3);
            }
            p.Set(5, 8, snow); p.Set(6, 8, snow); p.Set(25, 7, snow); p.Set(24, 7, snow);
            return Finish(p, 15, 13);
        }

        static Spr MakeBalbal()
        {
            var p = new Pix(18, 36);
            int s1 = P.I(0x6B7494), s2 = P.I(0x474F70), s3 = P.I(0x8D97B8), snow = P.I(0xEAF2FF);
            p.Poly(s1, 4, 35, 3, 12, 5, 9, 13, 9, 15, 12, 14, 35);
            p.Ellipse(9, 6.5f, 4.6f, 5f, s1);
            p.OnlyOpaque = true;
            p.Rect(11, 2, 5, 34, s2);
            p.Rect(5, 4, 2, 6, s3); p.Rect(4, 13, 1, 20, s3);
            // yüz ve kadeh
            p.Rect(6, 6, 2, 1, s2); p.Rect(10, 6, 2, 1, P.I(0x30374F));
            p.Rect(8, 7, 1, 2, s2); p.Rect(7, 10, 4, 1, s2);
            p.Line(5, 18, 8, 21, s2); p.Line(13, 18, 10, 21, P.I(0x30374F));
            p.Rect(8, 19, 3, 3, s3); p.Rect(9, 22, 1, 2, s3);
            p.OnlyOpaque = false;
            p.Rect(6, 1, 6, 1, snow); p.Rect(5, 2, 3, 1, snow); p.Rect(3, 11, 3, 1, snow); p.Rect(12, 11, 3, 1, snow);
            return Finish(p, 9, 35);
        }

        static Spr MakePole()
        {
            var p = new Pix(8, 46);
            int wood = P.I(0x6B4A36), woodD = P.I(0x4A3226), snow = P.I(0xEAF2FF);
            p.Rect(3, 2, 2, 43, wood); p.Rect(4, 2, 1, 43, woodD);
            p.Rect(2, 1, 4, 1, snow);
            return Finish(p, 4, 45);
        }

        // ---------- YAPILAR ----------
        static Spr MakeHouse(int variant, bool lit)
        {
            var p = new Pix(74, 62);
            int[] wallC = { 0x6A4A3A, 0x7C8298, 0x7A6652 }, wallDC = { 0x4E352B, 0x5A6078, 0x5A4A3C };
            int wall = P.I(wallC[variant]), wallD = P.I(wallDC[variant]), stone = P.I(0x565C78), stoneL = P.I(0x747B98);
            int roof = P.I(0x3B2A30), snow = P.I(0xEAF2FF), snowD = P.I(0xB4C8EC), door = P.I(0x33232A), doorL = P.I(0x4E3838);
            int win = lit ? P.I(0xFFD166) : P.I(0x121735), winF = P.I(0x2A1E26), amber = P.I(0xF2C46D);

            // baca
            p.Rect(50, 8, 7, 16, stone); p.Rect(55, 8, 2, 16, P.I(0x3E4460)); p.Rect(49, 6, 9, 3, snow);
            // duvar
            p.Rect(9, 28, 56, 26, wall);
            for (int y = 31; y < 54; y += 5) p.Rect(9, y, 56, 1, wallD);
            if (variant == 1)
                for (int k = 0; k < 26; k++) p.Rect(10 + (int)(Fx.Hash(k, 1) * 52), 29 + (int)(Fx.Hash(k, 2) * 23), 3, 1, wallD);
            p.Rect(58, 28, 7, 26, wallD);
            // taş temel
            p.Rect(8, 54, 58, 6, stone);
            for (int k = 0; k < 8; k++) p.Rect(9 + k * 7 + (k % 2), 55 + (k % 2) * 2, 4, 2, stoneL);
            // üçgen çatı alınlığı
            p.Poly(wallD, 9, 28, 37, 9, 65, 28);
            p.Rect(34, 17, 6, 6, winF); p.Rect(35, 18, 4, 4, lit ? P.I(0xFF9A3C) : P.I(0x121735));
            // çatı ve kar
            p.Poly(roof, 3, 30, 37, 6, 71, 30, 67, 30, 37, 11, 7, 30);
            p.Poly(snow, 1, 29, 37, 3, 73, 29, 70, 26, 37, 7, 4, 26);
            p.Poly(snowD, 37, 7, 70, 26, 73, 29, 66, 27, 37, 9);
            for (int k = 0; k < 7; k++)
            {
                int ix = 6 + k * 10 + (int)(Fx.Hash(k, variant + 20) * 5);
                float t = Math.Abs(ix - 37) / 34f;
                int iy = (int)(7 + t * 22);
                p.Rect(ix, iy, 1, 2 + (int)(Fx.Hash(k, variant + 9) * 3), P.I(0xCFF1FA));
            }
            // kapı
            p.Rect(32, 39, 10, 15, door); p.Rect(33, 40, 3, 13, doorL); p.Rect(37, 40, 4, 13, doorL);
            p.Set(40, 47, amber);
            // pencereler
            p.Rect(14, 37, 11, 10, winF); p.Rect(15, 38, 9, 8, win);
            p.Rect(49, 37, 11, 10, winF); p.Rect(50, 38, 9, 8, win);
            p.Rect(19, 38, 1, 8, winF); p.Rect(15, 41, 9, 1, winF);
            p.Rect(54, 38, 1, 8, winF); p.Rect(50, 41, 9, 1, winF);
            p.Rect(13, 47, 13, 1, snow); p.Rect(48, 47, 13, 1, snow);
            if (!lit)
            {
                // terk edilmiş: kapı önünde kar, pencerede tahta
                p.Poly(snow, 28, 54, 34, 49, 42, 50, 46, 54);
                if (variant != 1) { p.Line(15, 45, 23, 38, P.I(0x6B4A36)); p.Line(15, 44, 22, 38, P.I(0x6B4A36)); }
            }
            return Finish(p, 37, 60);
        }

        static Spr MakeSchool()
        {
            var p = new Pix(96, 58);
            int wall = P.I(0xB9B3A8), wallD = P.I(0x8C8780), stone = P.I(0x565C78), roof = P.I(0x6A2F33), snow = P.I(0xEAF2FF), snowD = P.I(0xB4C8EC);
            int win = P.I(0x121735), winF = P.I(0x3B3F58), door = P.I(0x2F4A60);
            p.Rect(6, 22, 84, 30, wall);
            p.Rect(80, 22, 10, 30, wallD);
            p.Rect(5, 52, 86, 5, stone);
            p.Poly(roof, 2, 24, 14, 10, 82, 10, 94, 24);
            p.Poly(snow, 1, 23, 13, 8, 83, 8, 95, 23, 90, 20, 80, 12, 16, 12, 6, 20);
            p.Poly(snowD, 80, 12, 83, 8, 95, 23, 90, 20);
            for (int k = 0; k < 4; k++)
            {
                int x = 12 + k * 20 + (k >= 2 ? 6 : 0);
                p.Rect(x, 30, 12, 12, winF); p.Rect(x + 1, 31, 10, 10, win);
                p.Rect(x + 5, 31, 1, 10, winF); p.Rect(x + 1, 35, 10, 1, winF);
                p.Rect(x - 1, 42, 14, 1, snow);
            }
            p.Rect(43, 36, 10, 16, door); p.Rect(44, 37, 8, 1, P.I(0x4A6A84));
            p.Rect(38, 26, 20, 6, P.I(0x2A3350)); p.Rect(39, 27, 18, 4, P.I(0xE6ECF5));
            p.Rect(41, 28, 3, 2, P.I(0x2A3350)); p.Rect(45, 28, 4, 2, P.I(0x2A3350)); p.Rect(50, 28, 2, 2, P.I(0x2A3350)); p.Rect(53, 28, 3, 2, P.I(0x2A3350));
            p.Poly(snow, 38, 52, 44, 47, 54, 48, 58, 52);
            return Finish(p, 48, 56);
        }

        static Spr MakeHearth()
        {
            var p = new Pix(44, 22);
            int s1 = P.I(0x6B7494), s2 = P.I(0x474F70), s3 = P.I(0x8D97B8), coal = P.I(0x1A1826), snow = P.I(0xEAF2FF);
            p.Ellipse(22, 12, 20, 8, s2);
            p.Ellipse(22, 11, 20, 7.5f, s1);
            p.Ellipse(22, 10.5f, 15, 5, coal);
            for (int k = 0; k < 14; k++)
            {
                double an = k * Math.PI * 2 / 14;
                int x = (int)(22 + Math.Cos(an) * 17.5), y = (int)(11 + Math.Sin(an) * 6.2);
                p.Rect(x - 1, y - 1, 3, 2, k % 2 == 0 ? s3 : s2);
            }
            p.Rect(3, 9, 3, 1, snow); p.Rect(38, 9, 3, 1, snow);
            p.Line(15, 12, 29, 8, P.I(0x5A3A2A)); p.Line(15, 8, 29, 12, P.I(0x4A2E22)); p.Line(16, 12, 30, 8, P.I(0x5A3A2A));
            return Finish(p, 22, 16);
        }

        // Zirvedeki büyük taş ocak
        static Spr MakeGunOcagi()
        {
            var p = new Pix(92, 84);
            int s1 = P.I(0x5E6788), s2 = P.I(0x3D4466), s3 = P.I(0x8591B4), dark = P.I(0x0D0F1E), ice = P.I(0x8FD3E8), snow = P.I(0xEAF2FF), gold = P.I(0xC9A45A);
            // basamaklar
            p.Rect(4, 74, 84, 8, s2); p.Rect(4, 74, 84, 2, s1);
            p.Rect(10, 66, 72, 8, s1); p.Rect(10, 66, 72, 2, s3); p.Rect(70, 68, 12, 6, s2);
            // sütunlar
            p.Rect(14, 22, 14, 44, s1); p.Rect(64, 22, 14, 44, s1);
            p.Rect(24, 22, 4, 44, s2); p.Rect(74, 22, 4, 44, s2);
            p.Rect(14, 22, 2, 44, s3); p.Rect(64, 22, 2, 44, s3);
            // kemer
            for (int y = 0; y < 40; y++)
                for (int x = 0; x < 92; x++)
                {
                    float d = Fx.Dist(x + 0.5f, y + 0.5f, 46, 36);
                    if (d < 35 && d > 21 && y < 37) p.Set(x, y, x > 58 ? s2 : s1);
                    if (d < 35 && d > 33 && y < 37) p.Set(x, y, s3);
                }
            // ocak ağzı
            p.Rect(28, 36, 36, 30, dark);
            for (int y = 14; y < 40; y++)
                for (int x = 24; x < 68; x++)
                    if (Fx.Dist(x + 0.5f, y + 0.5f, 46, 36) < 21) p.Set(x, y, dark);
            // güneş kabartması (kilit taşı)
            p.Ellipse(46, 8, 5, 5, s2);
            p.Ellipse(46, 8, 3, 3, gold);
            for (int k = 0; k < 8; k++)
            {
                double an = k * Math.PI / 4;
                p.Set((int)(46 + Math.Cos(an) * 5.4), (int)(8 + Math.Sin(an) * 5.4), gold);
            }
            // taş derzleri
            for (int y = 28; y < 66; y += 8) { p.Rect(14, y, 14, 1, s2); p.Rect(64, y, 14, 1, P.I(0x30374F)); }
            // buz ve kar
            p.Rect(30, 62, 32, 4, ice); p.Rect(32, 60, 10, 2, ice); p.Rect(50, 61, 9, 1, P.I(0xD8F4FC));
            for (int k = 0; k < 9; k++) p.Rect(30 + k * 4, 36, 1, 2 + (int)(Fx.Hash(k, 77) * 6), ice);
            p.Rect(12, 20, 18, 2, snow); p.Rect(62, 20, 18, 2, snow);
            p.Rect(4, 72, 84, 2, snow); p.Rect(10, 64, 20, 2, snow); p.Rect(62, 64, 20, 2, snow);
            for (int x = 20; x < 72; x++)
            {
                float d = (float)Math.Sqrt(Math.Max(0, 35 * 35 - (x - 46) * (x - 46)));
                int y = (int)(36 - d);
                p.Set(x, y, snow); p.Set(x, y + 1, snow);
            }
            return Finish(p, 46, 80);
        }

        static Spr MakeChar()
        {
            var p = new Pix(14, 9);
            p.Ellipse(7, 5.5f, 6, 3, P.I(0x1E1D2B));
            p.Ellipse(6, 4.5f, 4, 2, P.I(0x34344A));
            p.Set(4, 3, P.I(0xEAF2FF)); p.Set(5, 3, P.I(0xEAF2FF));
            return Finish(p, 7, 8);
        }

        static Spr MakeAsh()
        {
            var p = new Pix(24, 9);
            p.Ellipse(12, 5, 11, 3.5f, P.I(0x3A3A4E));
            p.Ellipse(11, 4.5f, 7, 2.2f, P.I(0x56566C));
            p.Ellipse(9, 4, 3, 1.2f, P.I(0x74748A));
            p.Set(16, 6, P.I(0x1E1D2B)); p.Set(6, 6, P.I(0x1E1D2B)); p.Set(13, 3, P.I(0x1E1D2B));
            return new Spr(p, 12, 6);
        }

        static Spr MakeFishHole()
        {
            var p = new Pix(40, 24);
            int water = P.I(0x0D1A3A), ice = P.I(0xD8F4FC), wood = P.I(0x7A5238), woodD = P.I(0x4A3226), tin = P.I(0x9AA4B8);
            p.Ellipse(14, 18, 9, 3.6f, ice);
            p.Ellipse(14, 18, 7, 2.6f, water);
            // tabure
            p.Rect(26, 12, 10, 2, wood); p.Rect(27, 14, 1, 6, woodD); p.Rect(34, 14, 1, 6, woodD);
            // olta
            p.Line(28, 11, 13, 2, woodD); p.Line(13, 2, 13, 16, P.I(0xCBDCF5));
            // çaydanlık
            p.Rect(31, 7, 5, 5, tin); p.Rect(36, 8, 2, 1, tin); p.Rect(32, 6, 3, 1, P.I(0x5E6580));
            return Finish(p, 20, 21);
        }

        // Savaşta oyuncunun "kor"u
        static Spr MakeEmber()
        {
            var p = new Pix(9, 11);
            p.Rows(0, 0, new[]
            {
                "....o....",
                "...oo....",
                "...oyo...",
                "..oyyo.o.",
                ".ooyyyoo.",
                ".oyywyyo.",
                "ooywwwyoo",
                "oyywwwyyo",
                ".oyywyyo.",
                "..ooyoo..",
                "...ooo..."
            }, c => c == 'o' ? P.I(0xFF7A2E) : (c == 'y' ? P.I(0xFFD166) : (c == 'w' ? P.I(0xFFF6D0) : 0)));
            return new Spr(p, 4, 6);
        }
    }
}
