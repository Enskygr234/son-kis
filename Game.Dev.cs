using System;
using System.Collections.Generic;

namespace WinterDeveloper.SonKis
{
    // Geliştirme kancaları: ekran görüntüsü ve otomatik oynanış testi için
    sealed partial class Game
    {
        public bool DevInvincible;
        public int DevFrame;
        readonly HashSet<Prop> devUsed = new HashSet<Prop>();
        float devLastX;
        int devStuck, devDir = 1, devLastDir = 4;

        public int DevHits;
        public string DevHitInfo = "", DevNote = "";
        public static string DevSavePath; // testte kaydı başka yere yaz
        public bool DevInDodge { get { return mode == Mode.Battle && phase == BPhase.Dodge; } }
        public bool DevInDescent { get { return mode == Mode.World && descent; } }
        public void DevHeal() { kor = korMax; }
        public string DevDodgeDump()
        {
            string s = string.Format("s=({0:F1},{1:F1}) atk={2:F2} box=[{3:F0}..{4:F0}]", sx, sy, atkT, box.Left, box.Right);
            foreach (Zone z in zones) s += string.Format(" z[{0:F0}..{1:F0} T={2:F2}]", z.R.Left, z.R.Right, z.T);
            return s;
        }
        public void DevSetWarm(float w) { warm = w; }

        // Makul bir oyuncuyu taklit eden kaçınma botu: yakın gelecekteki tehlikeye bakar
        public Btn DevDodgeAI(bool random)
        {
            DevFrame++;
            if (random)
            {
                int d = (DevFrame / 14 + (int)(Fx.Hash(DevFrame / 14, 5) * 8)) % 9;
                return DirBtn(d);
            }
            float best = float.MaxValue;
            int bestDir = 0;
            for (int dir = 0; dir < 9; dir++)
            {
                float dx = dir % 3 - 1, dy = dir / 3 - 1;
                if (dx != 0 && dy != 0) { dx *= 0.7071f; dy *= 0.7071f; }
                float score = 0;
                for (int step = 0; step <= 6; step++)
                {
                    float t = step == 0 ? 0.02f : step * 0.09f;
                    float fx = Fx.Clamp(sx + (dx * 80 + windX) * t, box.Left + 6, box.Right - 6);
                    float fy = Fx.Clamp(sy + dy * 80 * t, box.Top + 7, box.Bottom - 6);
                    float wgt = 1f / Math.Max(1, step);
                    foreach (Shot s in shots)
                    {
                        if (s.Heal) continue;
                        float tt = s.T + t;
                        float vy = s.VY + s.AY * t;
                        float px2 = s.Amp > 0 ? s.BX + s.VX * t + (float)Math.Sin(tt * s.Freq + s.Ph) * s.Amp : s.X + s.VX * t;
                        float py2 = s.Y + (s.VY + vy) * 0.5f * t;
                        float dd = Fx.Dist(px2, py2, fx, fy), lim = s.R + 2.4f;
                        if (dd < lim + 2) score += 100 * wgt;
                        else if (dd < lim + 9) score += (lim + 9 - dd) * wgt;
                    }
                    foreach (Zone z in zones)
                    {
                        if (!z.R.Contains(fx, fy)) continue;
                        float zt = z.T + t;
                        if (zt >= z.Delay - 0.05f && zt < z.Delay + z.Active + 0.05f) score += 100 * wgt;
                        else if (zt < z.Delay) score += 6 * wgt;
                    }
                    score += (Math.Abs(fx - (box.Left + box.Width / 2)) / box.Width + Math.Abs(fy - (box.Top + box.Height / 2)) / box.Height) * 0.6f * wgt;
                }
                if (dir == devLastDir) score -= 0.4f; // kararsızlığı önle
                if (score < best - 0.001f) { best = score; bestDir = dir; }
            }
            devLastDir = bestDir;
            return DirBtn(bestDir);
        }

        static Btn DirBtn(int dir)
        {
            Btn b = Btn.None;
            int dx = dir % 3 - 1, dy = dir / 3 - 1;
            if (dx < 0) b |= Btn.Left; else if (dx > 0) b |= Btn.Right;
            if (dy < 0) b |= Btn.Up; else if (dy > 0) b |= Btn.Down;
            return b;
        }

        public string DevState()
        {
            return mode + (mode == Mode.Battle ? "/" + phase + " foe=" + foe + " warm=" + (int)warm + " hp=" + foeHp + " turn=" + turn : "")
                + (mode == Mode.World ? " area=" + area + " px=" + (int)px + (descent ? " inis" : "") : "")
                + (mode == Mode.Ending ? " end=" + endKind + " phase=" + endPhase : "")
                + " kor=" + kor + "/" + korMax + " fate=" + string.Join("", fate) + (dActive ? " dlg" : "") + (ScriptActive ? " script" : "");
        }

        public bool DevAtTitle { get { return mode == Mode.Title; } }
        public bool DevAtCard { get { return mode == Mode.Ending && endPhase == 2; } }
        public int DevEndKind { get { return endKind; } }
        public bool DevAtChoice { get { return dActive && cOpts != null && dq.Count == 0 && dShown >= dTotal; } }

        public void DevGoto(string what)
        {
            fadeA = 0; fadeDir = 0; flashA = 0;
            ResetState();
            hasLantern = true;
            if (what == "title") { EnterTitle(); titleT = 2; return; }
            if (what == "intro") { EnterIntro(); introIdx = 2; introT = 9; return; }
            if (what == "gameover") { foe = 0; EnterGameOver(); overT = 3; return; }
            if (what == "dlg" || what == "ask" || what == "pause")
            {
                EnterArea(0, false);
                script.Clear(); scriptIdx = 0; hasLantern = true; bannerT = 0;
                if (what == "dlg") Say(null, "Nine|Altmış kıştır her Nardugan gecesi o koru dağa ben taşıdım. Bu kış dizlerim beni kapıya kadar götürmüyor.");
                else if (what == "ask")
                {
                    px = 700; py = 160; camX = 520;
                    Ask("Bir dilek tutup bağlar mısın?", new[] { "Nine'nin dizleri iyileşsin.", "Gidenler geri dönsün.", "Gün doğsun, o yeter." }, i => { });
                }
                else { paused = true; pauseIdx = 1; }
                dShown = 999;
                return;
            }
            if (what == "riddle" || what == "peace" || what == "burned")
            {
                int id = what == "riddle" ? 0 : (what == "peace" ? 1 : 2);
                for (int i = 0; i < id; i++) fate[i] = 1;
                BuildArea(id + 1);
                camX = Fx.Clamp(spX[id] - 200, 0, areaW - W);
                StartBattle(id);
                if (what == "riddle") { EnterMenu(); DoTalk(0); }
                else if (what == "peace") { warm = 100; DoPeace(); }
                else { foeHp = 0; DoBurned(); }
                dShown = 999;
                return;
            }
            if (what == "saveload")
            {
                area = 3; kor = 17; korMax = 26; bazlama = 1; wish = 2; fate[0] = 1; fate[1] = 2; seen.Add("cairn1"); seen.Add("yurudu");
                Save();
                ResetState();
                bool ok = Load();
                DevNote = "load=" + ok + " area=" + area + " kor=" + kor + "/" + korMax + " baz=" + bazlama + " wish=" + wish + " fate=" + fate[0] + fate[1] + fate[2] + fate[3]
                    + " lantern=" + hasLantern + " seen=" + string.Join("+", seen) + " hasSave=" + HasSave();
                EnterTitle();
                DevNote += " title0=" + titleItems[0];
                return;
            }
            if (what.StartsWith("descent"))
            {
                // "descentK:A[:x]"  K: son türü, A: bölge
                int k = what[7] - '0', a = what[9] - '0';
                foreach (int i in roadSpirits) fate[i] = k == 0 ? 1 : (k == 2 ? 2 : (i == 1 || i == 5 ? 2 : 1));
                fate[3] = k == 2 ? 2 : 1; wish = 1;
                dawnKind = k;
                EnterDescent(a);
                script.Clear(); scriptIdx = 0;
                if (what.Length > 10)
                {
                    px = int.Parse(what.Substring(11));
                    camX = Fx.Clamp(px - W * 0.45f, 0, areaW - W);
                    bannerT = 0;
                }
                return;
            }
            if (what == "bag")
            {
                foreach (int i in roadSpirits) fate[i] = 1;
                BuildArea(4);
                camX = Fx.Clamp(spX[3] - 200, 0, areaW - W);
                StartBattle(3);
                dShown = 999;
                EnterMenu(); menuShown = 999; menuIdx = 3; MenuSelect(); subIdx = 3; box = boxTarget;
                return;
            }
            if (what.StartsWith("area"))
            {
                int a = what[4] - '0';
                foreach (int i in roadSpirits) if (spiritArea[i] < a) fate[i] = 1;
                EnterArea(a, false);
                script.Clear(); scriptIdx = 0;
                hasLantern = true;
                if (what.Length > 5)
                {
                    px = int.Parse(what.Substring(6));
                    // geride kalan ruhlar dost sayılır
                    foreach (int i in areaSpirits.ToArray()) if (i != 3 && spX[i] < px + 20) fate[i] = 1;
                    BuildArea(a);
                }
                camX = Fx.Clamp(px - W * 0.45f, 0, areaW - W);
                bannerT = what.Length > 5 ? 0 : 2.5f;
                return;
            }
            if (what.StartsWith("battle") || what.StartsWith("dodge") || what.StartsWith("sub") || what.StartsWith("aim"))
            {
                bool dodge = what.StartsWith("dodge");
                string num = what.Substring(what.StartsWith("battle") ? 6 : (dodge ? 5 : 3));
                int id = num[0] - '0';
                if (what.EndsWith("burn")) { foreach (int i in roadSpirits) fate[i] = 2; korMax = 44; kor = 44; }
                else foreach (int i in roadSpirits) { if (i == id) break; fate[i] = 1; }
                BuildArea(spiritArea[id]);
                camX = Fx.Clamp(spX[id] - 200, 0, areaW - W);
                StartBattle(id);
                dShown = 999;
                if (dodge)
                {
                    turn = num.Length > 1 && char.IsDigit(num[1]) ? num[1] - '0' : 0;
                    warm = 45;
                    StartDodge();
                    box = boxTarget;
                }
                else if (what.StartsWith("sub")) { EnterMenu(); menuShown = 999; menuIdx = 2; MenuSelect(); box = boxTarget; }
                else if (what.StartsWith("aim")) { EnterMenu(); menuIdx = 0; MenuSelect(); aimX = 0.42f; }
                else if (num.Length > 1 && num[1] == 'm') { EnterMenu(); menuShown = 999; warm = 70; }
                return;
            }
            if (what == "finale")
            {
                foreach (int i in roadSpirits) fate[i] = 1;
                BuildArea(4);
                camX = areaW - W;
                StartBattle(3);
                StartRescue();
                return;
            }
            if (what.StartsWith("ending"))
            {
                int k = what[6] - '0';
                foreach (int i in roadSpirits) fate[i] = k == 0 ? 1 : (k == 2 ? 2 : (i == 1 || i == 5 ? 2 : 1));
                wish = 1;
                StartEnding(k);
                return;
            }
        }

        public void DevEndingSkip(int phase)
        {
            if (phase == 0) { gunLit = true; hasLantern = false; dawn = 1; dawnRise = true; script.Clear(); scriptIdx = 0; dActive = false; }
            else if (phase == 2) { endPhase = 2; cardT = 9; script.Clear(); scriptIdx = 0; dActive = false; }
        }

        // Basit bot: 0 = herkesi ısıt, 1 = herkesi yak, 2 = karışık
        public Btn DevBot(int policy)
        {
            DevFrame++;
            bool tapFrame = DevFrame % 4 == 0;
            Btn tapA = tapFrame ? Btn.A : Btn.None;

            switch (mode)
            {
                case Mode.Title:
                    if (titleItems[titleIdx] == "Yeni Yolculuk") return tapA;
                    return tapFrame ? Btn.Down : Btn.None;

                case Mode.Intro:
                case Mode.GameOver:
                case Mode.Ending:
                    return tapA;

                case Mode.World:
                    if (dActive || ScriptActive) return tapA;
                    Prop near = NearestUsable();
                    if (near != null && devUsed.Add(near)) return Btn.A;
                    if (DevFrame % 30 == 0)
                    {
                        if (Math.Abs(px - devLastX) < 2) devStuck++; else devStuck = 0;
                        devLastX = px;
                        if (devStuck > 0) devDir = -devDir;
                    }
                    // ara sıra etkileşim noktalarına uğra
                    Btn b = descent ? Btn.Left : Btn.Right;
                    if (devStuck > 0) b |= devDir > 0 ? Btn.Down : Btn.Up;
                    else if (py > BandTop + 8 && (DevFrame / 240) % 2 == 0) b |= Btn.Up;
                    else if (py < BandBot - 8 && (DevFrame / 240) % 2 == 1) b |= Btn.Down;
                    return b;

                case Mode.Battle:
                    return BotBattle(policy, tapFrame, tapA);
            }
            return Btn.None;
        }

        Btn BotBattle(int policy, bool tapFrame, Btn tapA)
        {
            bool burn = policy == 1 || (policy == 2 && foe == 1) || burnRoute;
            switch (phase)
            {
                case BPhase.Text:
                case BPhase.Finale:
                    if (dActive && cOpts != null && dShown >= dTotal)
                    {
                        int want = 0;
                        for (int i = 0; i < cOpts.Length; i++)
                            if (cOpts[i].ToLowerInvariant().Contains("kara")) want = i;
                        if (cIdx != want) return tapFrame ? Btn.Down : Btn.None;
                    }
                    return tapA;

                case BPhase.Menu:
                    {
                        int want;
                        if (burn) want = 0;
                        else if (kor < 8 && bazlama > 0) want = 3;
                        else if (talkCount[0] < (foe == 0 ? 3 : 1) || talkCount[1] < 1 || talkCount[2] < 1) want = 1;
                        else want = kor > 2 ? 2 : 1;
                        if (menuIdx != want) return tapFrame ? Btn.Right : Btn.None;
                        return tapA;
                    }

                case BPhase.Sub:
                    {
                        int want = 0;
                        if (subKind == 1) want = talkCount[0] < (foe == 0 ? 3 : 1) ? 0 : (talkCount[1] < 1 ? 1 : (talkCount[2] < 1 ? 2 : 2));
                        else if (subKind == 2) want = kor > 12 ? 2 : (kor > 6 ? 1 : 0);
                        if (subKind == 2 && kor <= 2) return tapFrame ? Btn.B : Btn.None;
                        if (subIdx != want) return tapFrame ? Btn.Down : Btn.None;
                        return tapA;
                    }

                case BPhase.Aim:
                    return Math.Abs(aimX - 0.5f) < 0.04f ? Btn.A : Btn.None;

                case BPhase.Dodge:
                    if (DevInvincible) inv = 0.5f;
                    int d = (DevFrame / 20) % 4;
                    return d == 0 ? Btn.Left : (d == 1 ? Btn.Up : (d == 2 ? Btn.Right : Btn.Down));
            }
            return Btn.None;
        }
    }
}
