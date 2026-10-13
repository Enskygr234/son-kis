using System;
using System.Collections.Generic;
using System.Drawing;

namespace WinterDeveloper.SonKis
{
    sealed partial class Game
    {
        static readonly string[] endName = { "NARDUGAN", "SOLGUN ŞAFAK", "KAVURAN GÜN" };

        int endKind, endPhase;
        float endT, cardT;
        bool dawnRise;
        string[] cardLines = new string[0];
        List<Func<float, bool>> endVillage; // köydeki son sahne; dönüş yürüyüşünden sonra oynar

        void AddStill(int id, float x, float y)
        {
            Prop p = Add(null, x, y);
            p.Draw = (g, sx, sy) => DrawSpirit(g, id, sx, sy, true, 1, 0);
        }

        void StartEnding(int kind)
        {
            mode = Mode.Ending;
            endKind = kind; endPhase = 0; endT = 0;
            dawnKind = kind; dawn = 0; dawnRise = false;
            fate[3] = kind == 2 ? 2 : 1;
            shots.Clear(); zones.Clear(); parts.Clear();
            dActive = false;
            script.Clear(); scriptIdx = 0;

            BuildArea(4);
            px = 752; py = 146; facing = 1; moving = false; hasLantern = true; // konuşma kutusunun üstünde kalsınlar
            camX = areaW - W;
            prints.Clear();
            if (kind != 2) AddStill(3, 718, 142);
            else Add(Art.Ash, 714, 138).Flat = true;
            if (fate[0] == 1) AddStill(0, 682, 146);
            if (fate[6] == 1) AddStill(6, 592, 140);
            if (fate[4] == 1) AddStill(4, 556, 146);
            if (fate[5] == 1) AddStill(5, 636, 147);
            if (fate[2] == 1) AddStill(2, 868, 146);
            if (fate[1] == 1) AddStill(1, 896, 140);
            if (fate[7] == 1) AddStill(7, 792, 80); // ocağın üstünde süzülür
            Music(null);

            // --- zirve: ocağı yakmadan önce ---
            var pre = new List<Func<float, bool>>();
            if (kind == 0)
            {
                var l = new List<string>();
                l.Add("Gün Ocağı'nın başındasınız. Sen, Ayaz Ata, bir de yol boyunca ısıttıkların.");
                l.Add("Nar|Ayaz Ata... Ya tutmazsa? Ocağın ağzı buz tutmuş.");
                l.Add("Ayaz Ata|Tutar. O buzu ben koydum, ben kaldırırım.");
                l.Add("Börü|Çekinme, yavru. Sürü arkanda.");
                if (fate[4] == 1) l.Add("Çarşamba Karısı|Hadi kız, buraya kadar geldin! Yarım bırakırsan saçını ben dolarım!");
                l.Add("Karakoncolos|Soru: Fener neden ağır? ...Cevap: içinde hepimizden bir parça var da ondan.");
                l.Add("Nar|Bu sefer cevabı kara bir şey değil.");
                l.Add("Karakoncolos|...Değil. Tuhaf. Hoşuma gitti.");
                if (fate[6] == 1) l.Add("Tepegöz|Küçük güneş, büyük ocak. Saydım: bir. Eksik yok.");
                if (fate[5] == 1) l.Add("Şahmeran|Yak, Nar. Bu sır değil. Bunu herkes görsün.");
                if (fate[7] == 1) l.Add("Yel Ana|Ben körüklerim. Az. Söz, az.");
                l.Add("Turna|Hadi Nar. Gökyüzü üç gündür seni bekliyor.");
                l.Add("Feneri iki elinle kaldırdın. Kor, Gün Ocağı'nın buz tutmuş ağzına düştü.");
                pre.Add(SSay(l.ToArray()));
            }
            else if (kind == 1)
            {
                var l = new List<string>();
                l.Add("Gün Ocağı'nın başındasın. Yanında Ayaz Ata ve yolda ısıttıkların var. Hepsi değil.");
                l.Add("Ayaz Ata|Say bakalım, çocuk. Dağa kaç kişi çıktınız?");
                l.Add("Nar|...Eksiğiz.");
                if (fate[0] == 1) l.Add("Karakoncolos|Soru: Buraya kaç kişi gelecektik? ...Cevabını bilmek istemiyorum.");
                if (fate[1] == 1) l.Add("Turna|Gökten baktım. Aşağıda kül vardı, Nar. Rüzgâr hâlâ dağıtamadı.");
                if (fate[2] == 1) l.Add("Börü|Sürü eksik. Kurt bunu saymadan bilir.");
                if (fate[4] == 1) l.Add("Çarşamba Karısı|Yarım kalmış. Bu yol yarım kalmış, kız. Ben anlarım.");
                if (fate[5] == 1) l.Add("Şahmeran|İnsana güvendim. Yarısı doğru çıktı.");
                if (fate[6] == 1) l.Add("Tepegöz|Saydım. Eksik var. Bu sefer gerçekten eksik var.");
                if (fate[7] == 1) l.Add("Yel Ana|Yol boyunca estim. Soğumuş yerler var, Nar. Ben soğutmadım.");
                l.Add("Nar|Korktum. Başka yolu yok sandım.");
                l.Add("Ayaz Ata|Vardı. Bazısında buldun, bazısında aramadın.");
                l.Add("Ayaz Ata|Yak ocağı. Gün, eksik de olsa doğmalı.");
                l.Add("Feneri kaldırdın. Kor, Gün Ocağı'nın ağzına düştü.");
                pre.Add(SSay(l.ToArray()));
            }
            else
            {
                pre.Add(SSay(
                    "Önünde duran kimse kalmadı. Yalnızca kül, bir de külün içinden gelen bir ses.",
                    "Ayaz Ata|...Hâlâ buradayım, çocuk. Biraz. Kar kolay yanmaz.",
                    "Nar|Yolumu kestin. Hepiniz kestiniz.",
                    "Ayaz Ata|Kestik. Üşüyorduk. Senden istediğimiz bir avuç sıcaktı."));
                pre.Add(SAsk("Ayaz Ata|Bir şey sorayım. Fener ağırlaştı mı?",
                    new[] { "Hayır. Hiç bu kadar hafif olmamıştı.", "...Evet.", "Sus artık." },
                    i => i == 0 ? new[] { "Ayaz Ata|Öyledir. İçinde senden başka kimse kalmayınca hafifler." }
                        : (i == 1 ? new[] { "Ayaz Ata|Taşıdığın artık kor değil. Bunu bil, yeter." }
                        : new[] { "Ayaz Ata|Susarım. Zaten az kaldı.", "Ayaz Ata|Sessizliğe alış, çocuk. Bundan sonra çok duyacaksın." })));
                pre.Add(SSay(
                    "Ayaz Ata|Yak ocağı. Gün doğsun. Ama bil: onu ısıtsın diye değil, kavursun diye uyandırıyorsun.",
                    "Ses kesildi. Feneri, Gün Ocağı'nın ağzına fırlattın."));
            }

            string[] during = kind == 0
                ? new[] { "Gün Ocağı tutuştu.", "Önce dağın ucu ağardı. Sonra göl. Sonra orman.", "Üç gündür bekleyen sabah, Akdağ'ın ardından doğdu." }
                : (kind == 1
                    ? new[] { "Gün Ocağı tutuştu. Alev, yorgun bir nefes gibi yükseldi.", "Güneş doğdu. Solgun; ama oradaydı." }
                    : new[] { "Alev göğe bir mızrak gibi saplandı.", "Güneş doğdu. Kıpkızıl. Öfkeli." });

            // --- zirve: gün doğduktan sonra ---
            var after = new List<Func<float, bool>>();
            if (kind == 0)
            {
                after.Add(SSay(
                    "Nar|Doğdu... Nine görüyor mudur?",
                    "Ayaz Ata|Görür. Altmış kıştır o ışığı köyde ilk o görür.",
                    "Nar|Peki sen? Gün doğunca sana ne olacak? Eriyecek misin?",
                    "Ayaz Ata|Kış bitmez, Nar. Kış gider, yine gelir.",
                    "Ayaz Ata|Biten, yalnızlıktı."));
                after.Add(SAsk("Ayaz Ata|Seneye ateşi yine sen mi getireceksin?",
                    new[] { "Getiririm. Ama yalnız çıkmam.", "Sen in köye. Ocağımız büyük.", "Seneye ben de büyümüş olurum." },
                    i => i == 0 ? new[] { "Ayaz Ata|Yalnız çıkamazsın zaten. Bu yol artık seni tanıyor." }
                        : (i == 1 ? new[] { "Ayaz Ata|Köye... Altmış kıştır beni kimse çağırmadı.", "Ayaz Ata|İnerim. Eşikte dururum; içerisi bana fazla gelir. Ama inerim. Hepimiz ineriz." }
                        : new[] { "Ayaz Ata|Büyü. Ama feneri tutan o küçük eli unutma. O el yetmişti." })));
                var l = new List<string>();
                l.Add("Karakoncolos|Soru: Köyde bazlama var mı? ...Bu sefer cevap bekliyorum!");
                l.Add("Nar|Var. Nine her sabah yapar.");
                l.Add("Karakoncolos|Cevap verdi. Biri bana CEVAP VERDİ! Hem de doğrusunu!");
                if (fate[6] == 1) l.Add("Tepegöz|Kırk üç. Seni de saydım, küçük güneş. Sürüdensin artık.");
                if (fate[5] == 1) l.Add("Şahmeran|Yılanlarım uyanıyor. Gün ışığını pulumda duyuyorum.");
                if (fate[4] == 1) l.Add("Çarşamba Karısı|Gün doğdu! Demek bugün... perşembe. Olsun. Perşembe de iyidir.");
                if (fate[7] == 1) l.Add("Yel Ana|Bundan sonra bacalardan gireceğim. Söndürmeye değil; tüttürmeye.");
                l.Add("Turna|Ben önden uçup haber vereyim. Ninen pencereye çıksın.");
                l.Add("Börü|Ben de seni geçide kadar indiririm. Sırtıma bin demiyorum. ...Ama binebilirsin.");
                l.Add("Nar|Binerim!");
                after.Add(SSay(l.ToArray()));
            }
            else if (kind == 1)
            {
                var l = new List<string>();
                if (fate[0] == 2) l.Add("Kayın ormanında artık kimse soru sormuyor.");
                if (fate[1] == 2) l.Add("Turna Gölü'nün buzu çözüldü. Üstünden kimse havalanmadı.");
                if (fate[2] == 2) l.Add("Kurt Geçidi'nde dağ, cevap vereceği bir uluma bekledi. Gelmedi.");
                if (fate[4] == 2) l.Add("Sönmez'in boş evlerinde artık hiçbir şey yerinden oynamıyor.");
                if (fate[5] == 2) l.Add("Gölün altında bin yılan, uyandıranı olmadan uyuyor.");
                if (fate[6] == 2) l.Add("Geçitte kırk iki taş, sayılmayı bekliyor.");
                if (fate[7] == 2) l.Add("Zirve yolunda rüzgâr esmiyor. Kar, düştüğü yerde kalıyor.");
                l.Add("Nar|Gün doğdu. Neden sevinemiyorum?");
                l.Add("Ayaz Ata|Çünkü bedelini biliyorsun. Bilmek iyidir.");
                after.Add(SSay(l.ToArray()));
                after.Add(SAsk("Ayaz Ata|Söyle. Yaktıklarına ne derdin?",
                    new[] { "Özür dilerim.", "Korkmuştum.", "Bir daha kimseyi yakmayacağım." },
                    i => i == 0 ? new[] { "Ayaz Ata|Kül özür duymaz. Ama sen duydun. O da bir şeydir." }
                        : (i == 1 ? new[] { "Ayaz Ata|Onlar da korkmuştu. Üşüyen her şey korkar." }
                        : new[] { "Ayaz Ata|Bu sözü bana verme. Seneye yola ver." })));
                after.Add(SSay(
                    "Nar|Seneye yine gelebilir miyim?",
                    "Ayaz Ata|Gel. Bu sefer kimseyi yakmadan gel.",
                    "Ayaz Ata|Git şimdi, çocuk. Ninen bekler."));
            }
            else
            {
                after.Add(SSay(
                    "O gün kar erimeye başladı. Üç günde dereler taştı.",
                    "Nar|...Kimse yok mu?",
                    "Dağ cevap vermedi. Akdağ'da ne bir kanat kalmıştı, ne bir pati, ne bir soru."));
            }

            // --- köy ---
            var village = new List<Func<float, bool>>();
            if (kind == 0)
            {
                village.Add(SSay(
                    "Sönmez'de Umay Nine, ocağın başında, gün ışığını yüzünde duydu.",
                    "Nar|Nine! Nine, yaktım! Gün Ocağı yandı!",
                    "Nine|Gördüm yavrum. Işık önce ocağa düştü, sonra yüzüme. Aferin sana.",
                    "Nar|Tek başıma yapmadım ama. Yolda dostlar buldum."));
                village.Add(SAsk("Nine|Hele anlat. En çok hangisi aklında kaldı?",
                    new[] { "Hep soru soran, kara tüylü biri.", "Gölde kanadı donmuş bir turna.", "Geçitte tek başına bir kurt." },
                    i => i == 0 ? new[] { "Nine|Karakoncolos! Hâlâ mı o bilmeceler?", "Nine|Cevabı hep kara bir şeydir onun. Bildiğimi söyleme; bilmediğimi sanınca seviniyor." }
                        : (i == 1 ? new[] { "Nine|Telli Turna... Ben gençken o göl hiç donmazdı.", "Nine|Kanadı çözüldüyse bahar yakındır." }
                        : new[] { "Nine|Börü. O geçidi altmış kış onunla paylaştım; bir kez olsun yolumu kesmedi.", "Nine|Seninkini kesti demek. Sınamış seni. Geçmişsin." })));
                var l = new List<string>();
                l.Add("Nar|Nine... Seneye hepsi köye inecek. Ayaz Ata da.");
                l.Add("Nine|Ayaz mı? Hah! O zaman bazlamayı çok yapmak gerek. O ihtiyar soğuk yer ama çok yer.");
                if (wish == 0) l.Add("O bahar Nine'nin dizleri onu bahçeye kadar taşıdı. Yetti de arttı.");
                else if (wish == 1) l.Add("O bahar Demirci Yusuf'un bacası tüttü. Sonra Hatçe Teyze'ninki.");
                else if (wish == 2) l.Add("Dileğin neydi? \"Gün doğsun, o yeter.\" Yetti.");
                l.Add("Sönmez'in ocağı o yıl da sönmedi.");
                village.Add(SSay(l.ToArray()));
            }
            else if (kind == 1)
            {
                village.Add(SSay(
                    "Nine seni ocağın başında karşıladı. Yüzüne uzun uzun baktı.",
                    "Nine|Gün doğdu. Aferin yavrum. ...Ama yüzün gülmüyor.",
                    "Nar|Nine, yolda... hepsini ısıtamadım.",
                    "Nine|Biliyorum. Fenerinin isinden belli. Kor ısıtınca is bırakmaz; yakınca bırakır."));
                village.Add(SAsk("Nine|Anlatmak ister misin?",
                    new[] { "Anlatırım. Hepsini.", "Şimdi değil, Nine." },
                    i => i == 0 ? new[] { "Nine|Anlat. Ben dinlerim, ocak da dinler. Ateşin kulağı vardır.", "Sabaha kadar anlattın. Nine bir kez bile sözünü kesmedi." }
                        : new[] { "Nine|Olsun. Gerisini baharda konuşuruz.", "Nine|Söz bekler. Önce ellerini ısıt." }));
                village.Add(SSay("Sönmez'in ocağı o yıl da sönmedi. Ama Akdağ biraz daha sessiz."));
            }
            else
            {
                village.Add(SSay(
                    "Köye döndüğünde Nine fenerine baktı. Sonra sana.",
                    "Nar|Yaktım, Nine. Gün Ocağı yandı. Gün doğdu.",
                    "Nine|Doğdu. Gördüm. Kızıl doğdu.",
                    "Nine|Yolda kimse yok muydu? Soru soran biri, bir turna, bir kurt?"));
                village.Add(SAsk("Nine|Onlara ne oldu, Nar?",
                    new[] { "Yolumu kestiler.", "Yaktım.", "..." },
                    i => i == 0 ? new[] { "Nine|Benim de kestiler. Altmış kış. Her seferinde bir avuç sıcak verdim, yol açıldı." }
                        : (i == 1 ? new[] { "Nine|...Hepsini mi?", "Nar|Hepsini." }
                        : new[] { "Nine|Susman da bir cevap. En ağırı." })));
                village.Add(SSay(
                    "Nine|Ben sana \"ısıt\" demiştim.",
                    "Nine|Ateş iki iş bilir, yavrum. Ben sana birini öğrettim. Ötekini kimden öğrendin?",
                    "Nar|Kimseden. Kendi kendime.",
                    "Sönmez'in ocağı o yıl da sönmedi. Çünkü artık hiçbir şey sönmüyordu."));
            }

            cardLines = kind == 0 ? new[] { "Bunu son kış sanmışlardı.", "İlk bahardı." }
                : (kind == 1 ? new[] { "Kış gitti.", "Eksik gitti." } : new[] { "Bu, son kıştı." });

            var steps = new List<Func<float, bool>>();
            steps.Add(SWait(1.4f));
            steps.AddRange(pre);
            steps.Add(SDo(() =>
            {
                gunLit = true; hasLantern = false; dawnRise = true;
                Flash(kind == 2 ? P.Fire3 : P.Fire0, 1f);
                shake = 0.5f;
                Sfx("burn", 1f); Sfx("dawn", 0.9f);
                Music(kind == 0 ? "safak" : (kind == 1 ? "ocak" : "gecit"));
            }));
            steps.Add(SWait(1.6f));
            steps.Add(SSay(during));
            steps.Add(dt => dawn >= 1f);
            steps.Add(SWait(0.6f));
            steps.AddRange(after);
            endVillage = village;
            steps.Add(SDo(() => FadeTo(() => EnterDescent(3), 1.2f)));
            steps.Add(SWait(999f));
            Run(steps.ToArray());
        }

        // Köyde son sahne: dönüş yürüyüşü ocağın başında biter
        void EnterVillageEnding()
        {
            mode = Mode.Ending;
            descent = false;
            endPhase = 1;
            BuildArea(0);
            dawn = 1; gunLit = true;
            px = 398; py = 146; facing = 1; moving = false; hasLantern = false;
            foreach (Prop p in props)
                if (p.S == Art.Hearth) p.Y = 146; else if (p.S == Art.Nine) p.Y = 142;
            camX = 284;
            prints.Clear();
            dActive = false;
            var vs = new List<Func<float, bool>>();
            vs.Add(SWait(1.2f));
            vs.AddRange(endVillage);
            vs.Add(SDo(() => FadeTo(() => { endPhase = 2; cardT = 0; }, 1.2f)));
            vs.Add(SWait(999f));
            Run(vs.ToArray());
        }

        void UpdateEnding(float dt)
        {
            endT += dt;
            if (dawnRise) dawn = Math.Min(1f, dawn + dt / 9f);
            if (endPhase == 2)
            {
                cardT += dt;
                if (cardT > 6f && Ok) { Sfx("ok", 0.7f); FadeTo(EnterTitle, 1.2f); }
                return;
            }
            if (dActive) UpdateDialogue(dt);
            else UpdateScript(dt);
        }

        void DrawEnding(Graphics g)
        {
            if (endPhase < 2)
            {
                DrawScene(g);
                DrawDialogueBox(g);
                return;
            }

            // son kart
            Color bg = endKind == 0 ? P.C(0x1A2450) : (endKind == 1 ? P.C(0x14182C) : P.C(0x1E0A10));
            g.FillRectangle(Fx.B(bg), 0, 0, W, H);
            if (endKind != 2) DrawSnow(g);
            DrawKilim(g, 0, 14, W);
            DrawKilim(g, 0, H - 21, W);

            float t = cardT;
            int y = 44;
            for (int i = 0; i < cardLines.Length; i++)
            {
                float lt = t - 0.6f - i * 1.5f;
                if (lt <= 0) break;
                int n = Math.Min(cardLines[i].Length, (int)(lt * 26));
                Font5.DrawS(g, cardLines[i].Substring(0, n), W / 2 - Font5.Width(cardLines[i]) / 2, y, P.Cream, P.Ink);
                y += 16;
            }
            float t2 = t - 0.6f - cardLines.Length * 1.5f - 0.6f;
            if (t2 > 0)
            {
                Color ac = endKind == 0 ? P.Fire1 : (endKind == 1 ? P.Snow2 : P.Fire3);
                Font5.Center(g, "SON " + (endKind + 1) + " / 3", W / 2, 92, P.Dim);
                Font5.CenterS(g, endName[endKind], W / 2, 106, ac, P.Ink, 2);
            }
            if (t2 > 0.9f)
            {
                DrawKilim(g, W / 2 - 60, 132, 120);
                Font5.CenterS(g, "SON KIŞ", W / 2, 146, P.Snow1, P.Ink, 2);
                Font5.Center(g, "bir Nardugan masalı  -  " + StudioName, W / 2, 170, P.Dim);
                string hint = endKind == 0 ? "Ateşi taşıdığın için teşekkürler."
                    : (endKind == 1 ? "Herkesi ısıtırsan gün başka doğar." : "Isıtmayı denersen gün başka doğar.");
                Font5.Center(g, hint, W / 2, 183, P.Narr);
            }
            if (cardT > 6f && (int)(time * 2.5f) % 2 == 0) DrawKey(g, W - 20, H - 36, "Z");
        }
    }
}
