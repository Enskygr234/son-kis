using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace WinterDeveloper.SonKis
{
    // Geliştirme araçları: sprite sayfası, ekran görüntüsü, otomatik oynanış testi
    static partial class Dev
    {
        static Graphics Prep(Bitmap bm)
        {
            var g = Graphics.FromImage(bm);
            g.SmoothingMode = SmoothingMode.None;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            return g;
        }

        // Oyunun çeşitli anlarından ekran görüntüsü alır
        public static void Shots(string dir, string only)
        {
            Art.Init();
            Directory.CreateDirectory(dir);
            var game = new Game(new Audio(false)) { NoSave = true, DevInvincible = true };
            Action<string, int, Btn> snap = (name, frames, b) =>
            {
                for (int i = 0; i < frames; i++) game.Update(1 / 60f, i == frames - 1 ? Btn.None : b);
                game.Render();
                using (var big = new Bitmap(Game.W * 3, Game.H * 3, PixelFormat.Format32bppPArgb))
                {
                    using (var g = Prep(big)) g.DrawImage(game.Frame, new Rectangle(0, 0, big.Width, big.Height));
                    big.Save(Path.Combine(dir, name + ".png"), ImageFormat.Png);
                }
            };
            var list = new List<Tuple<string, string, int, Btn>>
            {
                Tuple.Create("01_title", "title", 50, Btn.None),
                Tuple.Create("02_intro", "intro", 30, Btn.None),
                Tuple.Create("03_koy", "area0", 40, Btn.None),
                Tuple.Create("04_koy_agac", "area0:690", 40, Btn.None),
                Tuple.Create("05_orman", "area1:300", 50, Btn.Right),
                Tuple.Create("06_orman_ruh", "area1:930", 30, Btn.None),
                Tuple.Create("07_gol", "area2:420", 50, Btn.Right),
                Tuple.Create("08_gol_ruh", "area2:940", 30, Btn.None),
                Tuple.Create("09_gecit", "area3:380", 50, Btn.Right),
                Tuple.Create("10_gecit_ruh", "area3:970", 30, Btn.None),
                Tuple.Create("11_zirve", "area4:300", 50, Btn.Right),
                Tuple.Create("12_zirve_ayaz", "area4:640", 30, Btn.None),
                Tuple.Create("13_savas0", "battle0", 40, Btn.None),
                Tuple.Create("14_savas0_menu", "battle0m", 40, Btn.None),
                Tuple.Create("15_isit", "sub1", 20, Btn.None),
                Tuple.Create("16_yak", "aim2", 6, Btn.None),
                Tuple.Create("17_dodge00", "dodge00", 150, Btn.Left),
                Tuple.Create("18_dodge01", "dodge01", 150, Btn.None),
                Tuple.Create("19_dodge02", "dodge02", 110, Btn.None),
                Tuple.Create("20_dodge10", "dodge10", 150, Btn.None),
                Tuple.Create("21_dodge11", "dodge11", 96, Btn.None),
                Tuple.Create("22_dodge12", "dodge12", 150, Btn.None),
                Tuple.Create("23_dodge20", "dodge20", 126, Btn.None),
                Tuple.Create("24_dodge21", "dodge21", 150, Btn.None),
                Tuple.Create("25_dodge22", "dodge22", 200, Btn.None),
                Tuple.Create("26_dodge30", "dodge30", 150, Btn.None),
                Tuple.Create("27_dodge31", "dodge31", 150, Btn.None),
                Tuple.Create("28_dodge32", "dodge32", 150, Btn.None),
                Tuple.Create("29_ayaz", "battle3", 40, Btn.None),
                Tuple.Create("50_carsamba", "battle4", 40, Btn.None),
                Tuple.Create("50_carsamba_m", "battle4m", 40, Btn.None),
                Tuple.Create("51_sahmeran", "battle5", 40, Btn.None),
                Tuple.Create("51_sahmeran_m", "battle5m", 40, Btn.None),
                Tuple.Create("52_tepegoz", "battle6", 40, Btn.None),
                Tuple.Create("52_tepegoz_m", "battle6m", 40, Btn.None),
                Tuple.Create("53_dodge40", "dodge40", 170, Btn.None),
                Tuple.Create("53_dodge41", "dodge41", 108, Btn.None),
                Tuple.Create("53_dodge42", "dodge42", 170, Btn.None),
                Tuple.Create("54_dodge50", "dodge50", 170, Btn.None),
                Tuple.Create("54_dodge51", "dodge51", 112, Btn.None),
                Tuple.Create("54_dodge52", "dodge52", 150, Btn.None),
                Tuple.Create("55_dodge60", "dodge60", 170, Btn.None),
                Tuple.Create("55_dodge61", "dodge61", 112, Btn.None),
                Tuple.Create("55_dodge62", "dodge62", 112, Btn.None),
                Tuple.Create("56_koy_ruh", "area0:880", 30, Btn.None),
                Tuple.Create("57_gol_ruh2", "area2:510", 30, Btn.None),
                Tuple.Create("58_gecit_ruh2", "area3:440", 30, Btn.None),
                Tuple.Create("59_gecit_dost", "area3:640", 30, Btn.None),
                Tuple.Create("60_yel_yol", "area4:420", 30, Btn.None),
                Tuple.Create("60_yel_dost", "area4:560", 30, Btn.None),
                Tuple.Create("61_yel", "battle7", 40, Btn.None),
                Tuple.Create("61_yel_m", "battle7m", 40, Btn.None),
                Tuple.Create("62_dodge70", "dodge70", 170, Btn.None),
                Tuple.Create("62_dodge71", "dodge71", 150, Btn.None),
                Tuple.Create("62_dodge72", "dodge72", 96, Btn.None),
                Tuple.Create("63_heybe", "bag", 20, Btn.None),
                Tuple.Create("70_inis0_gecit", "descent0:3", 80, Btn.Left),
                Tuple.Create("70_inis0_boru", "descent0:3:1090", 30, Btn.None),
                Tuple.Create("71_inis0_gol", "descent0:2:780", 30, Btn.None),
                Tuple.Create("72_inis0_orman", "descent0:1:740", 30, Btn.None),
                Tuple.Create("73_inis0_koy", "descent0:0:900", 30, Btn.None),
                Tuple.Create("74_inis1_gol", "descent1:2:1060", 30, Btn.None),
                Tuple.Create("75_inis2_gecit", "descent2:3:600", 30, Btn.None),
                Tuple.Create("75_inis2_koy", "descent2:0:760", 30, Btn.None),
                Tuple.Create("30_finale", "finale", 500, Btn.None),
                Tuple.Create("31_gameover", "gameover", 60, Btn.None),
                Tuple.Create("40_dlg", "dlg", 10, Btn.None),
                Tuple.Create("41_ask", "ask", 10, Btn.None),
                Tuple.Create("42_pause", "pause", 10, Btn.None),
                Tuple.Create("43_riddle", "riddle", 10, Btn.None),
                Tuple.Create("44_peace", "peace", 30, Btn.None),
                Tuple.Create("45_burned", "burned", 40, Btn.None),
                Tuple.Create("32_son0", "ending0", 60, Btn.None),
                Tuple.Create("35_son1", "ending1", 60, Btn.None),
                Tuple.Create("36_son2", "ending2", 60, Btn.None)
            };
            foreach (var it in list)
            {
                if (only != null && !it.Item1.Contains(only)) continue;
                game.DevGoto(it.Item2);
                snap(it.Item1, it.Item3, it.Item4);
                if (it.Item2.StartsWith("ending"))
                {
                    // sonlardaki konuşmalar: her seçim anında ve hemen ardından birer görüntü
                    for (int c = 0, f = 0; c < 3 && f < 20000 && !game.DevAtCard; f++)
                    {
                        if (!game.DevAtChoice)
                        {
                            // dönüş yürüyüşünde sola yürü
                            Btn walk = f % 4 == 0 ? Btn.A : Btn.None;
                            if (game.DevInDescent) walk |= Btn.Left;
                            game.Update(1 / 60f, walk);
                            continue;
                        }
                        snap(it.Item1 + "_soru" + c, 2, Btn.None);
                        for (int k = 0; k < c; k++) { game.Update(1 / 60f, Btn.Down); game.Update(1 / 60f, Btn.None); }
                        game.Update(1 / 60f, Btn.A);
                        snap(it.Item1 + "_cevap" + c, 50, Btn.None);
                        c++;
                    }
                    game.DevGoto(it.Item2);
                    game.DevEndingSkip(0);
                    snap(it.Item1 + "_safak", 60, Btn.None);
                    game.DevEndingSkip(2);
                    snap(it.Item1 + "_kart", 30, Btn.None);
                }
                if (it.Item2 == "finale")
                    for (int k = 0; k < 3; k++) snap(it.Item1 + "_" + k, 260, Btn.A);
            }
        }

        // Kare başına çizim süresini ölçer
        public static void Bench(string logPath)
        {
            Art.Init();
            var log = new System.Text.StringBuilder();
            var game = new Game(new Audio(false)) { NoSave = true };
            foreach (string what in new[] { "title", "area0", "area1:600", "area2:600", "area3:600", "area4:600", "battle0m", "dodge00", "dodge31", "ending0" })
            {
                game.DevGoto(what);
                for (int i = 0; i < 30; i++) { game.Update(1 / 60f, Btn.None); game.Render(); }
                var sw = System.Diagnostics.Stopwatch.StartNew();
                const int n = 200;
                for (int i = 0; i < n; i++) { game.Update(1 / 60f, Btn.None); game.Render(); }
                log.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0,-12} {1:F2} ms/kare", what, sw.Elapsed.TotalMilliseconds / n));
            }
            File.WriteAllText(logPath, log.ToString());
        }

        // Her kaçınma deseninde, iyi ve dikkatsiz bir oyuncunun kaç darbe aldığını ölçer
        public static void DodgeTest(string logPath)
        {
            Art.Init();
            var log = new System.Text.StringBuilder();
            log.AppendLine("desen   iyi-oyuncu  dikkatsiz   (evre basina ortalama darbe; sicaklik 0 / 60)");
            var game = new Game(new Audio(false)) { NoSave = true };
            for (int foe = 0; foe < 8; foe++)
                for (int turn = 0; turn < 3; turn++)
                {
                    var res = new float[4];
                    for (int mode = 0; mode < 4; mode++)
                    {
                        const int runs = 14;
                        int hits = 0;
                        for (int r = 0; r < runs; r++)
                        {
                            game.DevGoto("dodge" + foe + turn);
                            game.DevSetWarm(mode >= 2 ? 60 : 0);
                            game.DevHits = 0;
                            int guard = 0;
                            while (game.DevInDodge && guard++ < 2000)
                            {
                                game.DevHeal();
                                Btn dbgB = game.DevDodgeAI(mode % 2 == 1);
                                game.Update(1 / 60f, dbgB);
                            }
                            hits += game.DevHits;
                            if (mode == 0 && r < 2 && game.DevHitInfo.Length > 0) log.AppendLine("   " + game.DevHitInfo);
                            game.DevHitInfo = "";
                        }
                        res[mode] = hits / (float)runs;
                    }
                    log.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "ruh{0} tur{1}:  iyi {2:F2} / {4:F2}   dikkatsiz {3:F2} / {5:F2}", foe, turn, res[0], res[1], res[2], res[3]));
                }
            File.WriteAllText(logPath, log.ToString());
        }

        // Oyunu baştan sona bot ile oynatır; kilitlenme ve çökme arar
        public static int SelfTest(string logPath)
        {
            Art.Init();
            var log = new System.Text.StringBuilder();
            int fails = 0;
            for (int policy = 0; policy < 3; policy++)
            {
                var game = new Game(new Audio(false)) { NoSave = true, DevInvincible = true };
                log.AppendLine("=== POLICY " + policy + " ===");
                string lastKey = "", lastState = "";
                int same = 0, frame = 0;
                bool reachedCard = false, ok = false;
                try
                {
                    for (frame = 0; frame < 60 * 60 * 25; frame++)
                    {
                        game.Update(1 / 60f, game.DevBot(policy));
                        if (frame % 15 == 0) game.Render();
                        string s = game.DevState();
                        string key = s.Split(' ')[0] + s.Substring(s.IndexOf("fate=", StringComparison.Ordinal));
                        if (key != lastKey) { log.AppendLine(string.Format("{0,7} {1}", frame, s)); lastKey = key; }
                        if (s == lastState) same++; else { same = 0; lastState = s; }
                        if (same > 60 * 45) { log.AppendLine("SOFTLOCK at " + frame + ": " + s); break; }
                        if (game.DevAtCard) reachedCard = true;
                        if (reachedCard && game.DevAtTitle) { ok = true; break; }
                    }
                }
                catch (Exception ex)
                {
                    log.AppendLine("EXCEPTION at frame " + frame + ": " + ex);
                }
                log.AppendLine(ok ? "OK ending=" + game.DevEndKind + " frames=" + frame : "FAIL state=" + game.DevState());
                if (!ok) fails++;
            }
            // kayıt / yükleme gidiş-dönüş testi (geçici dosyaya)
            try
            {
                Game.DevSavePath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(logPath)), "test_kayit.txt");
                var g2 = new Game(new Audio(false));
                g2.DevGoto("saveload");
                log.AppendLine("SAVELOAD " + g2.DevNote);
                if (!g2.DevNote.Contains("load=True area=3 kor=17/26 baz=1 wish=2 fate=1200") || !g2.DevNote.Contains("title0=Devam Et")) { fails++; log.AppendLine("SAVELOAD FAIL"); }
                // "Devam Et" ile kaldığı yerden başlıyor mu?
                for (int i = 0; i < 200; i++) g2.Update(1 / 60f, i % 8 == 4 ? Btn.A : Btn.None);
                log.AppendLine("CONTINUE " + g2.DevState());
                if (!g2.DevState().StartsWith("World area=3")) { fails++; log.AppendLine("CONTINUE FAIL"); }
                File.Delete(Game.DevSavePath);
            }
            catch (Exception ex) { fails++; log.AppendLine("SAVELOAD EXCEPTION " + ex); }
            finally { Game.DevSavePath = null; }

            File.WriteAllText(logPath, log.ToString());
            return fails;
        }

        public static void Sheet(string path)
        {
            Art.Init();
            using (var bm = new Bitmap(1500, 1000, PixelFormat.Format32bppPArgb))
            using (var g = Prep(bm))
            {
                g.Clear(P.Snow2);
                var all = new List<Spr>
                {
                    Art.NarStand, Art.NarWalk, Art.NarStandL, Art.Nine, Art.Koncol[0], Art.Koncol[1], Art.Turna[0], Art.Turna[1],
                    Art.Boru[0], Art.Boru[1], Art.Ayaz[0], Art.Ayaz[1], Art.Carsamba[0], Art.Carsamba[1], Art.Sahmeran[0], Art.Sahmeran[1], Art.Tepegoz[0], Art.Tepegoz[1], Art.Yel[0], Art.Yel[1], Art.Stone,
                    Art.PineS, Art.PineM, Art.PineL, Art.Birch, Art.BirchB, Art.Bush, Art.Rock, Art.RockB, Art.Mound, Art.Fence, Art.Sign,
                    Art.Sled, Art.Boat, Art.Reeds, Art.Cairn, Art.Balbal, Art.Pole, Art.House0, Art.House1, Art.House2, Art.HouseLit,
                    Art.School, Art.GunOcagi, Art.Char, Art.Ash, Art.FishHole, Art.Hearth, Art.Ember
                };
                int x = 8, y = 8, rowH = 0, sc = 4;
                foreach (var s in all)
                {
                    if (x + s.W * sc > 1490) { x = 8; y += rowH + 8; rowH = 0; }
                    g.DrawImage(s.Bmp, new Rectangle(x, y, s.W * sc, s.H * sc));
                    x += s.W * sc + 8;
                    rowH = Math.Max(rowH, s.H * sc);
                }
                g.Clear(P.Snow2);
                g.DrawImage(Art.NarStand.Bmp, new Rectangle(10, 10, Art.NarStand.W * 10, Art.NarStand.H * 10));
                g.DrawImage(Art.NarWalk.Bmp, new Rectangle(180, 10, Art.NarWalk.W * 10, Art.NarWalk.H * 10));
                g.DrawImage(Art.Nine.Bmp, new Rectangle(350, 10, Art.Nine.W * 10, Art.Nine.H * 10));
                g.DrawImage(Art.Ember.Bmp, new Rectangle(540, 10, Art.Ember.W * 10, Art.Ember.H * 10));
                g.DrawImage(Art.Hearth.Bmp, new Rectangle(660, 10, Art.Hearth.W * 6, Art.Hearth.H * 6));
                g.DrawImage(Art.GunOcagi.Bmp, new Rectangle(950, 10, Art.GunOcagi.W * 5, Art.GunOcagi.H * 5));
                g.DrawImage(Art.FishHole.Bmp, new Rectangle(660, 160, Art.FishHole.W * 6, Art.FishHole.H * 6));
                g.DrawImage(Art.Char.Bmp, new Rectangle(540, 160, Art.Char.W * 6, Art.Char.H * 6));
                g.DrawImage(Art.Ash.Bmp, new Rectangle(540, 240, Art.Ash.W * 6, Art.Ash.H * 6));
                g.DrawImage(Art.Yel[0].Bmp, new Rectangle(900, 150, Art.Yel[0].W * 5, Art.Yel[0].H * 5));
                g.DrawImage(Art.Yel[1].Bmp, new Rectangle(1160, 150, Art.Yel[1].W * 5, Art.Yel[1].H * 5));
                y = 440;
                g.FillRectangle(Fx.B(P.Panel), 0, y, 1500, 1000 - y);
                string[] sample =
                {
                    "ABCÇDEFGĞHIİJKLMNOÖPRSŞTUÜVYZ QWX 0123456789",
                    "abcçdefgğhıijklmnoöprsştuüvyz qwx .,!?:;'\"-—…()[]/+=*×%<>~►",
                    "Nine: Gel yavrum, ateşin yanına gel. Hâlâ üşüyor musun?",
                    "Karakoncolos sorarsa cevabında 'kara' olsun. AKDAĞ, GÜN OCAĞI, IŞIK, ÇIĞ, ŞAFAK."
                };
                for (int i = 0; i < sample.Length; i++)
                    Font5.Draw(g, sample[i], 10, y + 12 + i * 30, i < 2 ? P.Cream : P.Amber, 2);
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                bm.Save(path, ImageFormat.Png);
            }
        }
    }
}
