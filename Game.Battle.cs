using System;
using System.Collections.Generic;
using System.Drawing;

namespace WinterDeveloper.SonKis
{
    enum BPhase { Text, Menu, Sub, Aim, Burst, Dodge, Finale }

    sealed class Shot
    {
        public float X, Y, VX, VY, AY, R = 3, T, BX, Amp, Freq, Ph;
        public int Kind, Bounce;
        public bool Heal;
    }

    sealed class Zone
    {
        public RectangleF R;
        public float Delay, Active, T;
        public int Kind;
        public bool Struck;
    }

    struct Part { public float X, Y, VX, VY, Life, Max; public Color C; public int S; }

    sealed partial class Game
    {
        // Mermi türleri
        const int KQ = 0, KCone = 1, KFeather = 2, KShard = 3, KOrb = 4, KFlake = 5, KIcicle = 6, KSpark = 7, KYarn = 8, KNeedle = 9, KSnakeH = 10, KSnakeB = 11, KVenom = 12, KRock = 13, KGust = 14;
        const int PatLongNight = 99;

        static readonly RectangleF TextBox = new RectangleF(42, 112, 300, 60);
        static readonly RectangleF Arena = new RectangleF(130, 104, 124, 74);
        static readonly string[] foeTitle = { "KARAKONCOLOS", "TELLİ TURNA", "BÖRÜ", "AYAZ ATA", "ÇARŞAMBA KARISI", "ŞAHMERAN", "TEPEGÖZ", "YEL ANA" };
        static readonly string[] foeName = { "Karakoncolos", "Turna", "Börü", "Ayaz Ata", "Çarşamba Karısı", "Şahmeran", "Tepegöz", "Yel Ana" };
        static readonly int[] foeFoot = { 104, 108, 102, 134, 108, 102, 116, 110 };
        static readonly int[] foeHpMax = { 38, 46, 56, 110, 30, 50, 66, 60 };
        static readonly string[] menuNames = { "YAK", "KONUŞ", "ISIT", "HEYBE" };

        int foe, foeHp, turn, menuIdx, subIdx, subKind, riddleN, pat, patN, aimPass, startKor, startBaz;
        float warm, trust, anger, foeFlash, dmgT, warmPulse, foeGone, menuShown;
        float aimX, aimDir, burstT, sx, sy, inv, atkT, atkDur, windX, sparkT, dark;
        string dmgText = "", menuText = "";
        string[] subItems = new string[0];
        readonly int[] talkCount = new int[3];
        readonly float[] st = new float[4];
        readonly bool[] rescued = new bool[8];
        readonly List<int> bagIds = new List<int>(); // heybe satırları: -1 bazlama, ötekiler hatıra (ruh numarası)
        BPhase phase;
        RectangleF box = TextBox, boxTarget = TextBox;
        Action burstThen;
        bool longNight, finaleAsked, burnRoute, hurtFlash, teliUsed;
        readonly List<Shot> shots = new List<Shot>();
        readonly List<Zone> zones = new List<Zone>();
        readonly List<Part> parts = new List<Part>();
        Bitmap icoTalk, icoWarm, icoBag;

        // =====================================================
        //  KARŞILAŞMA ÖNCESİ (dünyada)
        // =====================================================
        void BeginEncounter(int id)
        {
            string[] lines;
            switch (id)
            {
                case 0:
                    lines = new[]
                    {
                        "???|Dur! Kimdir o? Kimsin, nesin, necisin?",
                        "Bir kayının arkasından kapkara, tüylü bir şey çıktı. Tüylerinin ucunda buz sarkıtları şıngırdıyor.",
                        "Karakoncolos|Üç gündür... yok, üç YILDIR bu yoldan kimse geçmiyor!",
                        "Karakoncolos|Benim işim yolcuya soru sormak. Yolcu kalmadı! Dilim paslandı, tüylerim buz tuttu!",
                        "Karakoncolos|Sen. Evet sen, fenerli. Cevap vermeden şuradan şuraya geçemezsin!"
                    };
                    break;
                case 1:
                    lines = new[]
                    {
                        "Gölün ortasında, tek ayağı buza saplanmış bir turna duruyor.",
                        "Turna|Yaklaşma! Kanadımı çarparım, gözünü oyarım!",
                        "Turna|Sürüm güneye uçtu. \"Gün doğunca yolu bulur, yetişirsin\" dediler.",
                        "Turna|Gün doğmadı. Su dondu. Ben de... ben de onunla dondum.",
                        "Turna|O ışığı bana doğru tutma! Seni tanımıyorum!"
                    };
                    break;
                case 2:
                    lines = new[]
                    {
                        "Geçidin en dar yerinde boz bir kurt oturuyor. Kıpırdamıyor.",
                        "Börü|İnsan kokusu. Yıllar oldu.",
                        "Börü|Bir sürüm vardı. Çobanlar gidince koyun gitti. Koyun gidince geyik indi. Geyik inince sürü dağıldı.",
                        "Börü|Bir ben kaldım. Bir de bu geçit.",
                        "Börü|Dağa çıkan önce benden geçer. Ateşinle mi geçeceksin, yüreğinle mi? Göster."
                    };
                    break;
                case 4:
                    lines = new[]
                    {
                        "Köyün son evinin önünde biri duruyor. Saçı dolaşmış bir yumak gibi; parmakları iğ kadar uzun.",
                        "???|Kim o? Çarşamba mı bugün? Çarşamba mı?!",
                        "Çarşamba Karısı|İşini yarım bırakanın evine çarşamba günü ben girerim. Yumağını dolar, ununu saçar, beşiğini sallarım!",
                        "Çarşamba Karısı|Girecek ev kalmadı! Dokuz kıştır kapılar kilitli. Dağıtacak bir yumak bile yok!",
                        "Çarşamba Karısı|Sen! Ocağı yarım bırakıp yola çıkıyorsun, değil mi? Yarım iş! Yarım iş!"
                    };
                    break;
                case 5:
                    lines = new[]
                    {
                        "Sazların arasında, buzun üstüne kıvrılmış kocaman bir yılan. Belinden yukarısı bir kadın; başında taç.",
                        "Şahmeran|Yaklaşma, insan yavrusu. Kıpırdayamıyorum diye dişim yok sanma.",
                        "Şahmeran|Yılan kışı uyuyarak geçirir. Ama bu kış bitmiyor. Uyudum, uyandım; hâlâ gece.",
                        "Şahmeran|Kanım soğudu. Yılanlarım taş kesildi. Bir ben uyanığım; o da zar zor.",
                        "Şahmeran|Bir kez bir insana güvendim. Yerimi söyledi. O fenerle bana ne yapacağını ben bilirim."
                    };
                    break;
                case 6:
                    lines = new[]
                    {
                        "Yolun ortasında bir kaya. Kaya nefes alıyor.",
                        "Kaya doğruldu. Alnının ortasında tek bir göz açıldı; kirpikleri buz tutmuş.",
                        "Tepegöz|Kırk bir... kırk iki... Dur. Sen koyun değilsin.",
                        "Tepegöz|Çobanlar gitti. Sürüyü bana bıraktılar. \"Sen büyüksün, sen bakarsın\" dediler.",
                        "Tepegöz|Bakıyorum. Her gece sayıyorum. Her gece bir eksik çıkıyor.",
                        "Tepegöz|Sen mi çalıyorsun koyunlarımı, ufaklık? O ışıkla mı kandırıyorsun onları?"
                    };
                    break;
                case 7:
                    lines = new[]
                    {
                        "Rüzgâr durdu. Birden. Kar taneleri havada asılı kaldı.",
                        "Sonra hepsi bir yere toplandı; kardan ve uğultudan bir kadın oldu.",
                        "???|Altı.",
                        "Yel Ana|Yol boyunca altı üşümüş saydım. Altısını da ısıtmışsın. Hepsine üfledim; hiçbiri soğumadı.",
                        "Yel Ana|Ben Yel Ana'yım. Neye dokunsam söner. Muma, ocağa, lafa. Elimde değil; ben dokununca eser.",
                        "Yel Ana|O yüzden beni kimse içeri almaz. Kapıyı bana karşı kapatırlar. Bacayı bana karşı örerler.",
                        "Yel Ana|O fenerin camı da bana karşı. Aç bakalım kapağını. Sönmeyen ateş neymiş, göreyim."
                    };
                    break;
                default:
                    var l = new List<string>
                    {
                        "Zirvede taştan, dev bir ocak. İçi kapkara; buz tutmuş.",
                        "Önünde, sakalı kırağıdan bir ihtiyar duruyor.",
                        "Ayaz Ata|Geldin.",
                        "Ayaz Ata|Her Nardugan biri gelirdi. Altmış kıştır hep aynı kadın. Ayak sesini ezberlemiştim.",
                        "Ayaz Ata|Bu yıl o ses gelmedi. Bekledim. Gece uzadı. Yine bekledim.",
                        "Ayaz Ata|Sandım ki bu, son kış. İnsanın dağı unuttuğu kış.",
                        "Ayaz Ata|Geceyi ben tutmuyorum, çocuk. Gece, bırakacak bir el bulamıyor."
                    };
                    if (Dost == 0 && Yanan > 0)
                    {
                        l.Add("Ayaz Ata|...Dur. Fenerin gürül gürül yanıyor. Ama is kokuyor. Kül kokuyor.");
                        l.Add("Ayaz Ata|Orman sustu. Göl sustu. Geçit sustu. Yukarı çıkarken hepsini yakmışsın.");
                        l.Add("Ayaz Ata|O ateş ocağıma girmez. Ben buradayken girmez.");
                    }
                    else
                    {
                        l.Add("Ayaz Ata|Göster bana taşıdığını. Gün Ocağı her ateşi kabul etmez.");
                        l.Add("Ayaz Ata|Ben de etmem.");
                    }
                    lines = l.ToArray();
                    break;
            }
            Say(() => { Sfx("encounter", 0.9f); Flash(P.Snow0, 0.8f); FadeTo(() => StartBattle(id), 3.2f); }, lines);
        }

        // =====================================================
        //  SAVAŞ AKIŞI
        // =====================================================
        void StartBattle(int id)
        {
            mode = Mode.Battle;
            foe = id;
            foeHp = foeHpMax[id];
            warm = 0; trust = 1; anger = 0; turn = 0; menuIdx = 0; riddleN = 0;
            for (int i = 0; i < 3; i++) talkCount[i] = 0;
            for (int i = 0; i < rescued.Length; i++) rescued[i] = false;
            foeFlash = 0; dmgT = 0; warmPulse = 0; foeGone = 0; inv = 0; dark = 0;
            longNight = false; finaleAsked = false; teliUsed = false;
            burnRoute = id == 3 && Dost == 0 && Yanan > 0;
            shots.Clear(); zones.Clear(); parts.Clear();
            box = TextBox; boxTarget = TextBox;
            startKor = kor; startBaz = bazlama;
            script.Clear(); scriptIdx = 0;
            Music(id == 3 ? "ayaz" : "savas");
            if (icoTalk == null) MakeIcons();

            string intro = id == 4 ? "Çarşamba Karısı iğini çevirdi!"
                : id == 5 ? "Şahmeran kuyruğunu ağır ağır çözdü."
                : id == 6 ? "Tepegöz ayağa kalktı. Uzun sürdü."
                : id == 7 ? "Yel Ana çevrende dönmeye başladı. Fenerin alevi yattı."
                : id == 0 ? "Karakoncolos yolunu kesti!"
                : (id == 1 ? "Telli Turna kanatlarını gerdi!"
                : (id == 2 ? "Börü ayağa kalkmadı. Gerek duymadı."
                : (burnRoute ? "Ayaz Ata, Gün Ocağı ile senin aranda duruyor." : "Ayaz Ata seni sınıyor.")));
            if (id != 3 && seen.Add("ogretici"))
                BSay(EnterMenu, intro,
                    "YAK, ruhu alevinle yakar. KONUŞ ve ISIT ise onu ısıtır.",
                    "Ama ISIT, kendi korundan verir. Kor'un biterse fenerin söner.");
            else BSay(EnterMenu, intro);
        }

        void RetryBattle()
        {
            kor = Math.Max(startKor, Math.Min(korMax, 12));
            bazlama = startBaz;
            StartBattle(foe);
        }

        void MakeIcons()
        {
            Func<char, int> key = c => c == 'w' ? P.I(0xF6ECD6) : (c == 'k' ? P.I(0x0B0F24) : (c == 'y' ? P.I(0xFFD166) : (c == 'b' ? P.I(0xB5803A) : (c == 'B' ? P.I(0x7A5630) : 0))));
            var a = new Pix(9, 9);
            a.Rows(0, 0, new[] { ".wwwwwww.", "wwwwwwwww", "wwkwkwkww", "wwwwwwwww", ".wwwwwww.", "..www....", ".ww......", ".........", "........." }, key);
            icoTalk = a.Bmp();
            var b = new Pix(9, 9);
            b.Rows(0, 0, new[] { "....y....", ".y.....y.", "...yyy...", "..yyyyy..", "y.yyyyy.y", "..yyyyy..", "...yyy...", ".y.....y.", "....y...." }, key);
            icoWarm = b.Bmp();
            var c2 = new Pix(9, 9);
            c2.Rows(0, 0, new[] { "..BBBBB..", ".B.....B.", ".bbbbbbb.", "bbbbbbbbb", "bbbyyybbb", "bbbbybbbb", "bbbbbbbbb", ".bbbbbbb.", "........." }, key);
            icoBag = c2.Bmp();
        }

        void BSay(Action then, params string[] lines)
        {
            phase = BPhase.Text;
            boxTarget = TextBox;
            Say(then, lines);
        }

        string Flavor()
        {
            bool low = foeHp < foeHpMax[foe] * 0.4f;
            switch (foe)
            {
                case 0:
                    if (low) return "Karakoncolos is kokuyor. Sana korkuyla bakıyor.";
                    if (warm < 25) return "Karakoncolos tir tir titriyor. Titredikçe tüylerindeki buzlar şıngırdıyor.";
                    if (warm < 60) return "Karakoncolos yeni bir soru düşünüyor. Dudakları kıpırdıyor.";
                    return "Karakoncolos'un tüyleri kabardı. Isındıkça büyüyor gibi.";
                case 1:
                    if (low) return "Turna'nın tüyleri kavrulmuş. Kaçamıyor.";
                    if (warm < 25) return "Turna kanatlarını gerdi. Buza saplı ayağı titriyor.";
                    if (warm < 60) return "Turna gözünü fenerden ayırmıyor.";
                    return "Buz çatırdıyor. Turna nefesini tuttu.";
                case 2:
                    if (low) return "Börü yaralı. Yine de oturuşunu bozmuyor.";
                    if (warm < 25) return "Börü kıpırdamıyor. Nefesi buhar olup yükseliyor.";
                    if (warm < 60) return "Börü'nün kulakları sana döndü.";
                    return "Börü'nün kuyruğu karı süpürüyor. Farkında değil.";
                case 4:
                    if (low) return "Çarşamba Karısı'nın saçı tütüyor. İğini düşürdü.";
                    if (warm < 25) return "Çarşamba Karısı'nın parmakları titriyor. İpi bir türlü tutamıyor.";
                    if (warm < 60) return "Çarşamba Karısı günleri sayıyor. Hep çarşambaya çıkıyor.";
                    return "Çarşamba Karısı saçını kulağının arkasına attı.";
                case 5:
                    if (low) return "Şahmeran'ın pulları kavruldu. Tacı yana kaydı.";
                    if (warm < 25) return "Şahmeran çok yavaş nefes alıyor. Pulları kırağı tutmuş.";
                    if (warm < 60) return "Şahmeran'ın kuyruğunun ucu kıpırdadı.";
                    return "Şahmeran'ın pulları yeniden parlıyor. Dili havayı yokladı.";
                case 6:
                    if (low) return "Tepegöz dizinin üstüne çöktü. Hâlâ sayıyor.";
                    if (warm < 25) return "Tepegöz gözünü kırpıştırıyor. Kirpiklerindeki buz görmesine engel.";
                    if (warm < 60) return "Tepegöz sana bakıyor. Koyun olmadığından hâlâ emin değil.";
                    return "Tepegöz yere oturdu. Yer biraz sarsıldı.";
                case 7:
                    if (low) return "Yel Ana'nın etekleri tutuştu. Rüzgâr yanar mı? Yanıyormuş.";
                    if (warm < 25) return "Yel Ana bir an bile yerinde durmuyor. Durursa yok olacak sanıyor.";
                    if (warm < 60) return "Yel Ana fenerin çevresinde dönüyor. Yaklaşıyor, kaçıyor, yine yaklaşıyor.";
                    return "Yel Ana yavaşladı. Esmiyor; soluk alıyor.";
                default:
                    if (burnRoute) return low ? "Ayaz Ata'nın kaftanı tütüyor. Asasına yaslandı." : "Ayaz Ata burnunu kırıştırdı. Kül kokusu onu rahatsız ediyor.";
                    if (warm < 30) return "Ayaz Ata bekliyor. Çevresinde kar bile yağmaya çekiniyor.";
                    if (warm < 70) return "Ayaz Ata'nın sakalından bir damla su düştü.";
                    return "Ayaz Ata gözlerini fenerinden ayırmıyor.";
            }
        }

        void EnterMenu()
        {
            // Ayaz Ata (iyilik yolu): son sınav
            if (foe == 3 && !burnRoute && !finaleAsked && (turn >= 3 || warm >= 90))
            {
                finaleAsked = true;
                BSay(StartLongNight,
                    "Ayaz Ata asasını yere vurdu. Zirvedeki bütün ışıklar titredi.",
                    "Ayaz Ata|Sıcaksın. Bunu gördüm. Şimdi asıl soruyu soracağım.",
                    "Ayaz Ata|Üç günlük gecenin tamamını taşıyabilir misin?");
                return;
            }
            phase = BPhase.Menu;
            boxTarget = TextBox;
            dActive = false;
            menuText = Flavor();
            menuShown = 0;
        }

        void AddWarm(float v)
        {
            warm = Math.Min(foe == 3 ? 90 : 100, warm + v);
            warmPulse = 1f;
        }

        // Eylemden sonra: barış mı, saldırı mı?
        void AfterKind(params string[] lines)
        {
            if (warm >= 100) BSay(DoPeace, lines);
            else BSay(StartDodge, lines);
        }

        void MenuSelect()
        {
            Sfx("ok", 0.6f);
            switch (menuIdx)
            {
                case 0: // YAK
                    phase = BPhase.Aim;
                    aimX = 0; aimDir = 1; aimPass = 0;
                    break;

                case 1: // KONUŞ
                    if (burnRoute) { BSay(StartDodge, "Ayaz Ata dinlemiyor. Gözü fenerindeki iste."); break; }
                    subKind = 1; subIdx = 0; phase = BPhase.Sub;
                    subItems = foe == 0 ? new[] { "Soru iste", "Tüylerini öv", "Sen sor" }
                        : (foe == 1 ? new[] { "Türkü söyle", "Güneyi anlat", "Buza bak" }
                        : (foe == 2 ? new[] { "Gözünü kaçırma", "Ekmeğini böl", "Birlikte ulu" }
                        : foe == 4 ? new[] { "Günü sor", "Saçını çöz", "İpi tut" }
                        : foe == 5 ? new[] { "Söz ver", "Tacını sor", "Masal anlat" }
                        : foe == 6 ? new[] { "Koyunları say", "Gözünü sil", "Islık çal" }
                        : foe == 7 ? new[] { "Yön sor", "Siper ol", "Körüğü anlat" }
                        : new[] { "Nine'yi anlat", "Yolu anlat", "Sus ve bekle" }));
                    break;

                case 2: // ISIT
                    if (burnRoute) { BSay(StartDodge, "Fenerin kapağını açtın. Ayaz Ata başını çevirdi.", "Ayaz Ata|Külle ısınılmaz."); break; }
                    subKind = 2; subIdx = 0; phase = BPhase.Sub;
                    {
                        int off = Keep(4) ? 1 : 0; // Kırmızı Yumak
                        subItems = new[] { "Bir tutam|" + (2 - off) + " Kor verir", "Bir avuç|" + (4 - off) + " Kor verir", "Kucak dolusu|" + (7 - off) + " Kor verir" };
                    }
                    break;

                case 3: // HEYBE
                    {
                        bagIds.Clear();
                        var items = new List<string>();
                        if (bazlama > 0) { bagIds.Add(-1); items.Add("Bazlama ×" + bazlama + "|+9 Kor"); }
                        foreach (int i in roadSpirits)
                            if (Keep(i)) { bagIds.Add(i); items.Add(keepName[i] + "|" + keepHint[i]); }
                        if (items.Count > 0) { subKind = 3; subIdx = 0; phase = BPhase.Sub; subItems = items.ToArray(); }
                        else BSay(EnterMenu, "Heyben boş. Dibinde birkaç kırıntı var.");
                    }
                    break;
            }
        }

        void SubSelect()
        {
            Sfx("ok", 0.6f);
            if (subKind == 1) DoTalk(subIdx);
            else if (subKind == 2)
            {
                int give = subIdx == 0 ? 2 : (subIdx == 1 ? 4 : 7);
                int cost = give - (Keep(4) ? 1 : 0);
                if (kor <= cost)
                {
                    BSay(EnterMenu, "Bu kadarını verirsen fenerin söner. Daha azını dene.");
                    return;
                }
                kor -= cost;
                bool capped = foe == 3 && warm >= 90;
                AddWarm(give * 6f * trust);
                Sfx("warm", 0.8f);
                for (int i = 0; i < 14; i++)
                    parts.Add(new Part { X = 192 + Rf(-10, 10), Y = 150, VX = Rf(-14, 14), VY = Rf(-60, -30), Life = 0.9f, Max = 0.9f, C = P.Fire1, S = 2 });
                string[] l;
                switch (foe)
                {
                    case 0:
                        l = turn % 2 == 0
                            ? new[] { "Fenerin kapağını açıp koru ona uzattın. Tüylerindeki buz çıtırdayarak eridi.", "Karakoncolos|Oh... bu... sıcak mı bu? Unutmuşum." }
                            : new[] { "Koru biraz daha yaklaştırdın. Fenerin hafifledi.", "Karakoncolos|Biraz daha. Şuraya; sırtıma doğru." };
                        break;
                    case 1:
                        l = turn % 2 == 0
                            ? new[] { "Feneri ayağındaki buza dayadın. Buz inleyerek çatladı.", "Turna|...Yakmıyor. Neden yakmıyor?" }
                            : new[] { "Buzun kenarından su sızmaya başladı. Fenerin hafifledi.", "Turna|Su oluyor. Su!" };
                        break;
                    case 2:
                        l = turn % 2 == 0
                            ? new[] { "Feneri karın üstüne, ikinizin arasına bıraktın. Börü ağır ağır yaklaşıp yanına çöktü.", "Börü|Ateşini bana harcıyorsun. Dağa ne götüreceksin?" }
                            : new[] { "Koru ona doğru ittin. Bıyıklarındaki kırağı çözüldü.", "Börü|...Sağ ol, yavru." };
                        break;
                    case 4:
                        l = turn % 2 == 0
                            ? new[] { "Feneri ona uzattın. Uzun parmaklarını cama dayadı.", "Çarşamba Karısı|Ocak sıcağı... Ben bunu hep pencereden seyrederdim. İçeriden değil." }
                            : new[] { "Koru biraz daha yaklaştırdın. Fenerin hafifledi.", "Çarşamba Karısı|Parmaklarım çözülüyor. Böyle düğüm atılmaz ki... İyi. Atmayayım." };
                        break;
                    case 5:
                        l = turn % 2 == 0
                            ? new[] { "Feneri kıvrımlarının arasına bıraktın. Pulları ısındıkça renk değiştirdi.", "Şahmeran|Kanım... kanım yürüyor. Bu karıncalanmayı unutmuşum." }
                            : new[] { "Koru biraz daha yaklaştırdın. Fenerin hafifledi.", "Şahmeran|Yakmıyorsun. İnsan eli ateş tutuyor ve yakmıyor." };
                        break;
                    case 6:
                        l = turn % 2 == 0
                            ? new[] { "Feneri avucuna koydun. Avucu bir sini kadar.", "Tepegöz|Küçük güneş. Avucumda küçük güneş var." }
                            : new[] { "Koru biraz daha uzattın. Fenerin hafifledi.", "Tepegöz|Sürüye de ver. Üşüyorlar. Hep üşüyorlar." };
                        break;
                    case 7:
                        l = turn % 2 == 0
                            ? new[] { "Fenerin kapağını ona doğru açtın. Alev yattı, titredi... sönmedi.", "Yel Ana|Sönmedi. Dokundum, sönmedi!" }
                            : new[] { "Koru biraz daha uzattın. Fenerin hafifledi.", "Yel Ana|Yavaş eseyim. Yavaş. Böyle mi? ...Böyle." };
                        break;
                    default:
                        l = capped
                            ? new[] { "Koru uzattın. Ayaz Ata'nın çevresindeki soğuk onu yuttu.", "Ayaz Ata|Tek bir kor, bütün bir kışı ısıtamaz. Bunu sen de biliyorsun." }
                            : (turn % 2 == 0
                                ? new[] { "Feneri iki elinle ona doğru kaldırdın.", "Ayaz Ata|Kendi ateşinden veriyorsun. Azaldığını görmüyor musun?" }
                                : new[] { "Koru bir kez daha uzattın. Sakalından su damladı.", "Ayaz Ata|Altmış kış, o kadın da böyle yapardı." });
                        break;
                }
                AfterKind(l);
            }
            else if (bagIds[subIdx] >= 0)
            {
                // hatıraya bakmak sıra harcamaz
                int id = bagIds[subIdx];
                BSay(EnterMenu, keepName[id] + ". " + foeName[id] + " vermişti.", keepEffect[id]);
            }
            else
            {
                bazlama--;
                kor = Math.Min(korMax, kor + 9);
                Sfx("warm", 0.7f);
                BSay(StartDodge, "Bazlamayı fenerin üstünde ısıtıp yedin. Nine'nin elinin tadı.", "Kor +9.");
            }
        }

        void DoTalk(int i)
        {
            int n = talkCount[i]++;
            switch (foe)
            {
                case 0:
                    if (i == 0)
                    {
                        if (riddleN >= 3) { talkCount[i]--; AfterKind("Karakoncolos|Sorularım bitti! Kırk yıldır biriktirmiştim; üç lokmada yedin."); return; }
                        string[] q = { "Karakoncolos: Nereden gelirsin?", "Karakoncolos: Nereye gidersin?", "Karakoncolos: Fenerinde ne taşırsın?" };
                        string[][] opts =
                        {
                            new[] { "Sönmez köyünden.", "Karlı yoldan.", "Karanlığın içinden." },
                            new[] { "Karakışın bittiği yere.", "Akdağ'ın tepesine.", "Karşı dağa." },
                            new[] { "Bir avuç kor.", "Kara geceye bir kıvılcım.", "Nine'nin ateşini." }
                        };
                        int[] right = { 2, 0, 1 };
                        int qi = riddleN;
                        phase = BPhase.Text;
                        boxTarget = TextBox;
                        Ask(q[qi], opts[qi], pick =>
                        {
                            if (pick == right[qi])
                            {
                                riddleN++;
                                AddWarm(18);
                                Sfx("warm", 0.7f);
                                AfterKind(qi == 0 ? "Karakoncolos|KARA! Dedi! Usulünce! Ohh... Kırk yıllık soru; hâlâ tadı yerinde."
                                    : (qi == 1 ? "Karakoncolos|Yine KARA! Sen bu işi biliyorsun, fenerli!"
                                    : "Karakoncolos|Üçte üç! Ninen seni iyi yetiştirmiş. Selam söyle o inatçı kadına."));
                            }
                            else
                            {
                                anger = Math.Min(1f, anger + 0.3f);
                                Sfx("hurt", 0.4f, 1.4f);
                                BSay(StartDodge, "Karakoncolos|YANLIŞ! İçinde \"kara\" yok! Usul diye bir şey var, usul!", "Karakoncolos öfkeyle tepindi.");
                            }
                        });
                    }
                    else if (i == 1)
                    {
                        if (n == 0) { AddWarm(10); AfterKind("Tüylerinin ne kadar... kara olduğunu söyledin.", "Karakoncolos|Zemheride taranır bunlar. Kolay mı sandın!", "Karakoncolos biraz kabardı."); }
                        else { AddWarm(3); AfterKind("Karakoncolos|Yeter, yeter. Şımartma."); }
                    }
                    else
                    {
                        if (n == 0)
                        {
                            AddWarm(14); trust = 1.4f;
                            AfterKind("Bu sefer sen sordun: \"Sen hiç üşür müsün?\"", "Karakoncolos sustu. Tüylerindeki buzlar bile şıngırdamayı bıraktı.", "Karakoncolos|Bana... bana kimse soru sormamıştı.");
                        }
                        else { AddWarm(4); AfterKind("Bir soru daha sordun: \"En sevdiğin renk?\"", "Karakoncolos|Kara.", "Hiç düşünmedi."); }
                    }
                    break;

                case 1:
                    if (i == 0)
                    {
                        if (n == 0) { AddWarm(20); AfterKind("Bildiğin tek turna türküsünü söyledin. Sesin soğuktan çatladı.", "Turna başını yana eğdi.", "Turna|O bizim türkümüz. İnsanlar onu hâlâ söylüyor mu?"); }
                        else { AddWarm(6); AfterKind("Türküyü baştan aldın. Turna alçak sesle eşlik etti."); }
                    }
                    else if (i == 1)
                    {
                        if (n == 0) { AddWarm(16); AfterKind("Ona güneyi anlattın: ılık sazlıkları, portakal kokusunu.", "Hiç görmedin; ama Nine anlatmıştı.", "Turna|...Portakal. Evet. Öyle kokar."); }
                        else { AddWarm(4); AfterKind("Güneyi bir daha anlattın. Bu sefer portakalları biraz abarttın."); }
                    }
                    else
                    {
                        if (n == 0) { AddWarm(6); trust = 1.5f; AfterKind("Ayağının çevresindeki buza baktın. Bir karış kalınlığında.", "Kırılmaz. Ama erir. Koru nereye tutacağını artık biliyorsun.", "(Isıtmak artık daha çok işe yarayacak.)"); }
                        else AfterKind("Buz hâlâ orada. Bakmakla erimiyor.");
                    }
                    break;

                case 2:
                    if (i == 0)
                    {
                        if (n == 0) { AddWarm(16); trust = 1.3f; AfterKind("Kurdun sarı gözlerine baktın. O da sana baktı.", "Uzun sürdü. Önce o kırptı.", "Börü|Hm. Kaçırmadın."); }
                        else { AddWarm(5); AfterKind("Yine bakıştınız. Bu sefer ikiniz de kırpmadınız."); }
                    }
                    else if (i == 1)
                    {
                        if (n == 0 && bazlama > 0)
                        {
                            bazlama--; AddWarm(26);
                            AfterKind("Bazlamayı ikiye böldün. Yarısını karın üstüne, ikinizin ortasına koydun.", "Börü kokladı. Yedi.", "Börü|Tuz. Buğday. Ocak isi. Köy hâlâ yaşıyor demek.");
                        }
                        else if (n == 0)
                        {
                            AddWarm(8);
                            AfterKind("Heyben boş. Avucunu açıp gösterdin.", "Börü boş avucunu kokladı. Yine de kuyruğu kıpırdadı.");
                        }
                        else { AddWarm(3); AfterKind("Börü bıyıklarını yalıyor. Hâlâ ekmeğin tadında."); }
                    }
                    else
                    {
                        if (n == 0) { AddWarm(20); AfterKind("Başını kaldırıp uludun. Sesin inceydi.", "Börü önce güldü. Kurtlar güler mi? Sonra o da uludu.", "Dağ ikinize birden cevap verdi."); }
                        else { AddWarm(6); AfterKind("Bir kez daha uludunuz. Bu sefer sesin o kadar da ince değildi."); }
                    }
                    break;

                case 4:
                    if (i == 0)
                    {
                        if (n == 0) { AddWarm(22); AfterKind("\"Bugün günlerden ne?\" diye sordun. Üç gündür gün doğmuyor; sen de bilmiyorsun.", "Çarşamba Karısı|...Sen de mi bilmiyorsun?", "Çarşamba Karısı|Ben delirdim sanmıştım. Meğer gün delirmiş."); }
                        else { AddWarm(6); AfterKind("Birlikte parmak hesabı yaptınız. İkinizde de çarşamba çıktı.", "Çarşamba Karısı pek memnun oldu."); }
                    }
                    else if (i == 1)
                    {
                        if (n == 0) { AddWarm(14); trust = 1.4f; AfterKind("Saçındaki düğümlerden birini parmaklarınla çözdün. Bağırmadı.", "Çarşamba Karısı|Elleme! ...Dur. Şurayı da.", "(Isıtmak artık daha çok işe yarayacak.)"); }
                        else { AddWarm(5); AfterKind("Bir düğüm daha çözdün. Altından eski bir toka çıktı."); }
                    }
                    else
                    {
                        if (n == 0) { AddWarm(24); AfterKind("İpin ucunu sen tuttun, o sardı. Yumak bitti. Yarım kalmadı.", "Çarşamba Karısı|Bitti. Bir iş bitti.", "Çarşamba Karısı|Dokuz kıştır ilk."); }
                        else { AddWarm(6); AfterKind("Bir yumak daha sardınız. Bu sefer ucunu kaçırmadın."); }
                    }
                    break;

                case 5:
                    if (i == 0)
                    {
                        if (n == 0) { AddWarm(14); trust = 1.4f; AfterKind("\"Yerini kimseye söylemem\" dedin.", "Şahmeran|Öteki de öyle demişti.", "\"Ben öteki değilim. Ben Nar'ım.\" Şahmeran sana uzun uzun baktı."); }
                        else { AddWarm(4); AfterKind("Sözünü yineledin. Şahmeran başını eğdi.", "Şahmeran|Söz bir kez verilir. İkincisi şüphe uyandırır."); }
                    }
                    else if (i == 1)
                    {
                        if (n == 0) { AddWarm(16); AfterKind("Tacındaki taşı sordun. Soğuktan matlaşmış.", "Şahmeran|Yılanlarımın gözüdür o. Uyurken onunla bakarlar.", "Şahmeran|Sen yaklaşınca kırpıştı. Merak ediyorlar seni."); }
                        else { AddWarm(4); AfterKind("Taşa bir daha baktın. Sana doğru göz kırptı sanki."); }
                    }
                    else
                    {
                        if (n == 0) { AddWarm(22); AfterKind("Nine'den dinlediğin Şahmeran masalını anlattın. Masalda iyi olan oydu; ihanet eden insan.", "Şahmeran|...Köyde beni böyle mi anlatıyorlar?", "Şahmeran|Demek biri doğrusunu hatırlıyor."); }
                        else { AddWarm(6); AfterKind("Masalın sonunu bu sefer değiştirdin: kimse kimseyi ele vermedi.", "Şahmeran|Bu sonu daha çok sevdim."); }
                    }
                    break;

                case 6:
                    if (i == 0)
                    {
                        if (n == 0) { AddWarm(18); AfterKind("Onunla birlikte saydın. Kırk iki. Koyun değil; taş.", "Söylemedin. \"Tamam\" dedin, \"eksik yok.\"", "Tepegöz|Eksik yok mu? ...Eksik yok. İlk kez."); }
                        else { AddWarm(5); AfterKind("Bir daha saydınız. Yine kırk iki.", "Tepegöz derin bir nefes aldı; karlar savruldu."); }
                    }
                    else if (i == 1)
                    {
                        if (n == 0) { AddWarm(8); trust = 1.5f; AfterKind("Parmak uçlarında yükselip kirpiklerindeki buzu sildin. Göz kocaman açıldı.", "Tepegöz|Aa. Sen küçükmüşsün. Ben seni uzakta sanıyordum.", "(Isıtmak artık daha çok işe yarayacak.)"); }
                        else { AddWarm(3); AfterKind("Gözünün kenarında kalan son buzu da aldın.", "Tepegöz gözünü kırptı. Rüzgâr yaptı."); }
                    }
                    else
                    {
                        if (n == 0) { AddWarm(22); AfterKind("Kavalın yok. Islıkla bir çoban havası çaldın.", "Tepegöz'ün gözü doldu. Tek göz, koca bir damla.", "Tepegöz|Sarı Çoban böyle çalardı. Babamdı o. Kimse inanmaz."); }
                        else { AddWarm(6); AfterKind("Aynı havayı bir daha çaldın. Tepegöz mırıldanarak eşlik etti; dağ titredi."); }
                    }
                    break;

                case 7:
                    if (i == 0)
                    {
                        if (n == 0) { AddWarm(18); AfterKind("\"Nereden esiyorsun?\" diye sordun.", "Yel Ana|Her yerden. Hiçbir yerden.", "Yel Ana|Kimse sormamıştı. Ben de hiç dönüp bakmamıştım."); }
                        else { AddWarm(5); AfterKind("\"Peki nereye?\" diye sordun.", "Yel Ana|Bilmem. Bu gece buraya."); }
                    }
                    else if (i == 1)
                    {
                        if (n == 0) { AddWarm(8); trust = 1.5f; AfterKind("Sırtını ona döndün; feneri gövdenle siper ettin. Rüzgâr omuzlarından ikiye ayrıldı.", "Yel Ana|Kaçmıyorsun. Kapı da kapatmıyorsun. Önümde duruyorsun yalnızca.", "(Isıtmak artık daha çok işe yarayacak.)"); }
                        else { AddWarm(3); AfterKind("Yine siper oldun. Atkın rüzgârda dümdüz uzadı.", "Yel Ana atkının ucuyla oynadı. Söndürmeden oynanacak bir şey bulmuştu."); }
                    }
                    else
                    {
                        if (n == 0) { AddWarm(22); AfterKind("Ona Demirci Yusuf'un körüğünü anlattın: az esince kor nasıl kızarır, nasıl büyür.", "Yel Ana|Yalan. Ben esince söner.", "\"Çok esince söner\" dedin. \"Az esince yanar.\""); }
                        else { AddWarm(6); AfterKind("Körüğü bir daha anlattın. Yel Ana nefesini tutup dinledi.", "Rüzgâr için kolay iş değil."); }
                    }
                    break;

                default:
                    if (i == 0)
                    {
                        if (n == 0) { AddWarm(18); AfterKind("Ona Nine'yi anlattın: dizlerini, ocağı, \"gün seni bekliyor\" deyişini.", "Ayaz Ata'nın sakalından bir buz parçası düştü.", "Ayaz Ata|Umay... Demek yaşıyor."); }
                        else { AddWarm(4); AfterKind("Nine'nin tarhanasını anlattın.", "Ayaz Ata|...Bana da hiç getirmedi."); }
                    }
                    else if (i == 1)
                    {
                        if (n == 0)
                        {
                            var l = new List<string> { "Yolda karşılaştıklarını anlattın." };
                            if (fate[0] == 1) l.Add("Soru soran bir Karakoncolos'u.");
                            if (fate[1] == 1) l.Add("Ayağı buza saplanmış bir turnayı.");
                            if (fate[2] == 1) l.Add("Geçidi bekleyen yaşlı bir kurdu.");
                            if (Yanan > 0) l.Add("Bazılarını anlatırken sesin kısıldı. Ayaz Ata fark etti.");
                            if (fate[4] == 1) l.Add("Günlerini şaşırmış bir Çarşamba Karısı'nı.");
                            if (fate[5] == 1) l.Add("Sazlıkta üşüyen Şahmeran'ı. Yerini söylemedin.");
                            if (fate[6] == 1) l.Add("Taşlarını koyun diye sayan Tepegöz'ü.");
                            if (fate[7] == 1) l.Add("Hiçbir şeye söndürmeden dokunamayan Yel Ana'yı.");
                            l.Add(Yanan == 0 ? "Ayaz Ata|Hepsine ateşinden verdin. Sonra da buraya, bana mı geldin?" : "Ayaz Ata|Hm.");
                            AddWarm(4 * Dost);
                            AfterKind(l.ToArray());
                        }
                        else { AddWarm(3); AfterKind("Yolu bir daha anlattın. Ayaz Ata sözünü kesmedi."); }
                    }
                    else { AddWarm(6); AfterKind("Hiçbir şey demedin. O da demedi.", "Kar, ikinizin arasına yağdı."); }
                    break;
            }
        }

        void ResolveAim(bool pressed)
        {
            float acc = pressed ? 1f - Math.Abs(aimX - 0.5f) * 2f : 0f;
            bool crit = acc > 0.9f;
            // yaktıkça büyüyen alev daha çok yakar
            float power = korMax / 20f;
            int dmg = (int)Math.Round((crit ? 15 : (pressed ? 5 + 9 * acc * acc : 4)) * power);
            bool immune = foe == 3 && !burnRoute;
            if (immune) dmg = 0;

            Sfx("burn", 0.9f);
            if (crit && !immune) Sfx("crit", 0.7f);
            foeHp = Math.Max(0, foeHp - dmg);
            foeFlash = immune ? 0 : 0.5f;
            shake = immune ? 0 : 0.25f;
            dmgText = immune ? "0" : (crit ? "TAM İSABET  -" + dmg : "-" + dmg);
            dmgT = 1.1f;
            if (!immune) { warm = Math.Max(0, warm - 35); anger = Math.Min(1f, anger + 0.3f); }
            int fy = foeFoot[foe];
            for (int i = 0; i < 26; i++)
                parts.Add(new Part
                {
                    X = 192 + Rf(-22, 22), Y = fy - Rf(10, 70), VX = Rf(-40, 40), VY = Rf(-70, -10), Life = Rf(0.4f, 0.9f), Max = 0.9f,
                    C = i % 3 == 0 ? P.Fire0 : (i % 3 == 1 ? P.Fire1 : P.Fire3), S = 2 + i % 2
                });

            phase = BPhase.Burst;
            burstT = 0.75f;
            burstThen = () =>
            {
                if (immune)
                {
                    BSay(StartDodge, "Alevin, onun ayazına değmeden titredi ve küçüldü.", "Ayaz Ata|Yakmak kolaydır, çocuk. Onu sınamıyorum.");
                    return;
                }
                if (foeHp <= 0) { DoBurned(); return; }
                string who = foeName[foe] + "|";
                string[][] react =
                {
                    new[] { "Ay! Yaktı! Soru soranı yakarlar mı hiç?!", "Tüylerim! Kırk yılda uzadı onlar!" },
                    new[] { "Uzak dur! Uzak dur dedim!", "Kanadım... Kanadım yanıyor!" },
                    new[] { "Demek ateşinle.", "Vur. Yaşlı kurt kolay ölmez." },
                    new[] { "Yak bakalım. Kış, ateşten eskidir.", "Sakalım tutuştu. Yıllardır ilk kez bir şey hissediyorum." },
                    new[] { "Yakma! Saçım! Dokuz kışta dolaştı o!", "Ateş evde durur, yolda değil! Terbiyesiz!" },
                    new[] { "Bildim! İnsan işte!", "Yine mi... Yine mi güvendim?" },
                    new[] { "Acıdı! Küçük şey acıttı!", "Gözüme tutma! Bir tane o!" },
                    new[] { "Yanıyorum! Yel yanar mı hiç?!", "Üflüyorum, üflüyorum... Kendimi söndüremiyorum!" }
                };
                BSay(StartDodge, (crit ? "Alev tam yerine oturdu. " : "") + foeName[foe] + " " + dmg + " yanık aldı.", who + react[foe][turn % 2]);
            };
        }

        // ---------- Barış ----------
        void DoPeace()
        {
            Sfx("peace", 0.9f);
            Music(null);
            shots.Clear(); zones.Clear();
            string[] l;
            switch (foe)
            {
                case 0:
                    l = new[]
                    {
                        "Karakoncolos'un tüylerindeki son buz da eridi. Biraz ıslak köpek gibi koktu.",
                        "Karakoncolos|Oh. Ohhh. Parmak uçlarım. Parmak uçlarım vardı benim!",
                        "Karakoncolos|Geç bakalım, fenerli. Usul bilen yolcuya yol verilir.",
                        "Karakoncolos|...Dönüşte yine uğra. Yeni sorularım olacak. Kolay sorular. Söz.",
                        "Karakoncolos sana bir tutam KARA TÜY verdi. Hâlâ ılık."
                    };
                    break;
                case 1:
                    l = new[]
                    {
                        "Buz bir çatırtıyla ikiye ayrıldı. Turna ayağını çekti, sendeledi, kanatlarını açtı.",
                        "Turna|Ayağım... Ayağım benim!",
                        "Turna|Güneye artık yetişemem. Ama gün doğarsa yolu bulurum.",
                        "Turna|Sen o ateşi dağa götür. Ben de gökten bakarım; yolunu şaşırma diye.",
                        "Turna sana bir TURNA TELİ verdi. Parmağına kendiliğinden dolandı."
                    };
                    break;
                case 4:
                    l = new[]
                    {
                        "Çarşamba Karısı'nın parmakları çözüldü. İğ, elinde ilk kez düzgün döndü.",
                        "Çarşamba Karısı|Isındım. Evsiz, ocaksız, sokak ortasında ısındım. Olur şey değil.",
                        "Çarşamba Karısı|Git. Evlere ben bakarım. Dağıtmam; söz. Belki bir iki yastık.",
                        "Çarşamba Karısı|Ninenin ocağına da göz kulak olurum. Yarım işi severim ama yarım kalmış çocuğu sevmem. Dön.",
                        "Çarşamba Karısı sana bir KIRMIZI YUMAK verdi. Ucu bileğine dolandı."
                    };
                    break;
                case 5:
                    l = new[]
                    {
                        "Şahmeran doğruldu. Kıvrımları çözüldü; pulları gölün buzuna yeşil yeşil yansıdı.",
                        "Şahmeran|Kanım ısındı. Aklım da onunla ısındı galiba; sana kızamıyorum.",
                        "Şahmeran|Yerimi söylemeyeceksin. Biliyorum. Sormadan biliyorum.",
                        "Şahmeran|Yılanlarımı uyandırayım. Yolunu ısırmazlar; ısıtırlar.",
                        "Şahmeran sana bir YILAN PULU verdi. Avucunda ılık bir para gibi durdu."
                    };
                    break;
                case 6:
                    l = new[]
                    {
                        "Tepegöz'ün kirpiklerindeki buz eridi. Göz, feneri ilk kez net gördü.",
                        "Tepegöz|Sıcak. Ağılda koyunların arasında yatmak gibi.",
                        "Tepegöz|...Onlar taş, değil mi. Biliyordum. Sayacak bir şey lazımdı.",
                        "Tepegöz|Geç. Yukarıda bir kurt var. Söyle ona, kızgın değilim. Koyunları o yemedi. Kış yedi.",
                        "Tepegöz sana bir ÇOBAN DÜĞMESİ verdi. Senin avucunda tabak kadar."
                    };
                    break;
                case 7:
                    l = new[]
                    {
                        "Rüzgâr dindi. Havada asılı kalan kar taneleri ağır ağır yere indi.",
                        "Yel Ana|Sıcak. İçimden geçiyor ve sönmüyor. Ben de onu söndürmüyorum.",
                        "Yel Ana|Demek az esmek diye bir şey varmış. Bunca yıl hep sonuna kadar estim.",
                        "Yel Ana|Çık yukarı. İhtiyara söyle: bu gece ocağına üflemeyeceğim. Körükleyeceğim.",
                        "Yel Ana sana bir YEL DÜĞÜMÜ verdi. İpin içinde küçük bir esinti kıpırdıyor."
                    };
                    break;
                default:
                    l = new[]
                    {
                        "Börü burnunu fenerin camına dayadı. Bıyıklarındaki kırağı çözüldü.",
                        "Börü|Sıcak. Ana kucağı gibi.",
                        "Börü|Geç, küçük ateş. Geçit senin.",
                        "Börü|Ve bil ki bundan sonra bu dağda tek başına yürümüyorsun.",
                        "Börü sana bir KURT DİŞİ verdi. \"Kendi düştü\" dedi; inanmadın ama sormadın."
                    };
                    break;
            }
            var all = new List<string>(l);
            all.Add("(" + keepEffect[foe] + ")");
            BSay(() => { fate[foe] = 1; Save(); FadeTo(ReturnToWorld); }, all.ToArray());
        }

        // ---------- Yakıp yok etme ----------
        void DoBurned()
        {
            Sfx("dissolve", 0.9f);
            Music(null);
            shots.Clear(); zones.Clear();
            foeGone = 0.01f;
            int fy = foeFoot[foe];
            for (int i = 0; i < 60; i++)
                parts.Add(new Part
                {
                    X = 192 + Rf(-26, 26), Y = fy - Rf(4, 84), VX = Rf(-22, 22), VY = Rf(-46, -8), Life = Rf(0.8f, 2.2f), Max = 2.2f,
                    C = i % 4 == 0 ? P.Fire2 : (i % 4 == 1 ? P.C(0x56566C) : P.C(0x2A2A3C)), S = 1 + i % 3
                });

            if (foe == 3)
            {
                BSay(() => FadeTo(() => StartEnding(2), 1.2f),
                    "Ayaz Ata dizlerinin üstüne çöktü. Sakalı buhar olup uçuyordu.",
                    "Ayaz Ata|Yakmak kolaydır, çocuk. Asıl zor olan... ısıtmaktı.",
                    "Ayaz Ata|Gün doğacak. Ama onu karşılayacak kimse bırakmadın.",
                    "Ayaz Ata bir buhar bulutu oldu. Rüzgâr onu da aldı.");
                return;
            }
            string[] l;
            switch (foe)
            {
                case 0:
                    l = new[] { "Alev, kara tüyleri bir anda sardı.", "Karakoncolos|Ben... sadece... bir soru...", "Geriye bir avuç is kaldı. Fenerin alevi büyüdü.", "Kor'un en fazlası 4 arttı. Orman çok sessiz." };
                    break;
                case 1:
                    l = new[] { "Tüyler tutuştu. Turna bir kez haykırdı; ses gölün öbür yakasından geri döndü.", "Buzun üstünde, eriyip yeniden donmuş bir su birikintisi kaldı.", "Fenerin alevi büyüdü. Kor'un en fazlası 4 arttı.", "Gökte hiç kanat sesi yok." };
                    break;
                case 4:
                    l = new[] { "Dolaşık saçlar bir anda tutuştu.", "Çarşamba Karısı|Yarım... kaldı...", "Karda yanık bir iğ kaldı. Fenerin alevi büyüdü.", "Kor'un en fazlası 4 arttı. Köyün nüfusu yine ikiye indi." };
                    break;
                case 5:
                    l = new[] { "Alev, pulların arasından içeri sızdı.", "Şahmeran|İnsan... hep aynı...", "Sazların arasında boş bir taç kaldı. Fenerin alevi büyüdü.", "Kor'un en fazlası 4 arttı. Gölün altında bin yılan, uyandıranı olmadan uyuyor." };
                    break;
                case 6:
                    l = new[] { "Alev koca gövdeyi ağır ağır sardı. Tepegöz kaçmadı; nereye kaçacağını bilemedi.", "Tepegöz|Kırk bir... kırk iki...", "Yolun ortasında kararmış bir kaya kaldı. Fenerin alevi büyüdü.", "Kor'un en fazlası 4 arttı. Taşları artık sayan yok." };
                    break;
                case 7:
                    l = new[] { "Alev rüzgârı tuttu; etekten saça yürüdü.", "Yel Ana|Demek... en çok esen... en çabuk yanıyor...", "Karın üstünde çözülmüş bir düğüm kaldı. Fenerin alevi büyüdü.", "Kor'un en fazlası 4 arttı. Zirve yolunda artık hiçbir şey esmiyor." };
                    break;
                default:
                    l = new[] { "Börü kaçmadı. Alevin içinde bile oturuşunu bozmadı.", "Börü|Demek... ateşinle.", "Geçitte bir avuç boz kül kaldı. Rüzgâr onu da aldı.", "Fenerin alevi büyüdü. Kor'un en fazlası 4 arttı." };
                    break;
            }
            BSay(() => { fate[foe] = 2; korMax += 4; kor = Math.Min(korMax, kor + 4); Save(); FadeTo(ReturnToWorld); }, l);
        }

        void ReturnToWorld()
        {
            mode = Mode.World;
            BuildArea(area);
            px = spX[foe] - 46; py = 182; facing = 1; moving = false;
            dActive = false;
            script.Clear(); scriptIdx = 0;
            shots.Clear(); zones.Clear(); parts.Clear();
            Music(theme.Track);
        }

        // =====================================================
        //  KAÇINMA EVRESİ
        // =====================================================
        void StartDodge()
        {
            phase = BPhase.Dodge;
            dActive = false;
            boxTarget = Arena;
            atkT = 0;
            atkDur = foe == 3 ? 7.5f : 6.5f;
            pat = (foe < 4 ? foe * 3 : 12 + (foe - 4) * 3) + turn % 3;
            for (int i = 0; i < 4; i++) st[i] = 0.5f;
            st[1] = 1.0f;
            patN = 0;
            windX = 0;
            sparkT = 1.2f;
            shots.Clear(); zones.Clear();
            sx = Arena.X + Arena.Width / 2; sy = Arena.Y + Arena.Height / 2 + 10;
        }

        void StartLongNight()
        {
            StartDodge();
            longNight = true;
            pat = PatLongNight;
            atkDur = 10f;
        }

        void Hurt(int dmg)
        {
            if (inv > 0) return;
            if (Keep(1) && !teliUsed)
            {
                // Turna Teli: karşılaşmanın ilk darbesini savar
                teliUsed = true;
                inv = 1.1f;
                Sfx("spark", 0.9f);
                for (int k = 0; k < 14; k++)
                    parts.Add(new Part { X = sx, Y = sy, VX = Rf(-46, 46), VY = Rf(-50, 20), Life = 0.6f, Max = 0.6f, C = k % 2 == 0 ? P.Snow0 : P.Ice, S = 1 + k % 2 });
                return;
            }
            DevHits++;
            if (Keep(2)) dmg = Math.Max(1, dmg - 1); // Kurt Dişi
            kor -= dmg;
            inv = Keep(0) ? 1.8f : 1.1f; // Kara Tüy
            shake = 0.25f;
            hurtFlash = true;
            Sfx("hurt", 0.8f);
            if (kor <= 0)
            {
                kor = 0;
                if (longNight) StartRescue();
                else EnterGameOver();
            }
        }

        Shot AddShot(int kind, float x, float y, float vx, float vy, float r = 3)
        {
            var s = new Shot { Kind = kind, X = x, Y = y, BX = x, VX = vx, VY = vy, R = r };
            shots.Add(s);
            return s;
        }

        void AddZone(int kind, RectangleF r, float delay, float active)
        {
            zones.Add(new Zone { Kind = kind, R = r, Delay = delay, Active = active });
            Sfx("warn", 0.5f);
        }

        void SpawnPattern(float dt)
        {
            float m = (1f - warm / 260f) * (1f + anger * 0.35f);
            float L = Arena.Left, T = Arena.Top, R = Arena.Right, B = Arena.Bottom;
            for (int i = 0; i < 4; i++) st[i] -= dt;

            switch (pat)
            {
                case 0: // Karakoncolos: soru yağmuru
                    if (st[0] <= 0)
                    {
                        st[0] = 0.27f / m;
                        Shot s = AddShot(KQ, Rf(L + 8, R - 8), T - 6, 0, Rf(44, 58) * m, 3.4f);
                        s.Amp = 11; s.Freq = Rf(2.5f, 4.5f); s.Ph = Rf(0, 6.28f);
                    }
                    break;

                case 1: // Karakoncolos: seken kozalaklar
                    if (st[0] <= 0)
                    {
                        st[0] = 0.5f / m;
                        bool left = rnd.Next(2) == 0;
                        Shot s = AddShot(KCone, left ? L + 6 : R - 6, T - 4, (left ? 1 : -1) * Rf(26, 62) * m, -10, 3.6f);
                        s.AY = 175; s.Bounce = 2;
                    }
                    break;

                case 2: // Karakoncolos: pençe
                    if (st[0] <= 0)
                    {
                        st[0] = 1.15f;
                        float bh = Arena.Height / 4f;
                        int band = (int)Fx.Clamp((sy - T) / bh, 0, 3);
                        AddZone(0, new RectangleF(L, T + band * bh, Arena.Width, bh), 0.62f, 0.22f);
                        if (anger > 0.2f || atkT > 3f)
                        {
                            int other = (band + 1 + rnd.Next(3)) % 4;
                            AddZone(0, new RectangleF(L, T + other * bh, Arena.Width, bh), 0.62f, 0.22f);
                        }
                    }
                    if (st[1] <= 0)
                    {
                        st[1] = 0.8f;
                        Shot s = AddShot(KQ, Rf(L + 8, R - 8), T - 6, 0, 36 * m, 3.4f);
                        s.Amp = 8; s.Freq = 3; s.Ph = Rf(0, 6.28f);
                    }
                    break;

                case 3: // Turna: süzülen tüyler
                    if (st[0] <= 0)
                    {
                        st[0] = 0.25f / m;
                        Shot s = AddShot(KFeather, Rf(L + 4, R - 4), T - 6, 0, Rf(30, 40) * m, 3.2f);
                        s.Amp = 22; s.Freq = Rf(1.8f, 2.6f); s.Ph = Rf(0, 6.28f);
                    }
                    break;

                case 4: // Turna: buzdan dikenler
                    if (st[0] <= 0)
                    {
                        st[0] = 0.95f;
                        float cw = Arena.Width / 7f;
                        int col = (int)Fx.Clamp((sx - L) / cw, 0, 6);
                        AddZone(1, new RectangleF(L + col * cw, T, cw, Arena.Height), 0.6f, 0.25f);
                        int n = atkT > 3 ? 2 : 1;
                        for (int k = 0; k < n; k++)
                        {
                            int o = (col + 2 + rnd.Next(4)) % 7;
                            AddZone(1, new RectangleF(L + o * cw, T, cw, Arena.Height), 0.6f, 0.25f);
                        }
                    }
                    break;

                case 5: // Turna: kanat rüzgârı
                    if (st[1] <= 0) { st[1] = 1.7f; windX = (windX >= 0 && atkT > 0.2f ? -1 : 1) * 40f; }
                    if (st[0] <= 0 && windX != 0)
                    {
                        st[0] = 0.3f / m;
                        bool fromLeft = windX > 0;
                        AddShot(KShard, fromLeft ? L - 6 : R + 6, Rf(T + 6, B - 6), (fromLeft ? 1 : -1) * 82 * m, 0, 3f);
                    }
                    break;

                case 6: // Börü: çene
                    if (st[0] <= 0)
                    {
                        st[0] = 1.4f;
                        // boşluk, yetişilebilecek bir uzaklıkta açılır
                        float gx = Fx.Clamp(sx + Rf(-46, 46), L + 24, R - 24);
                        AddZone(2, new RectangleF(L, T, gx - 17 - L, Arena.Height), 0.85f, 0.2f);
                        AddZone(2, new RectangleF(gx + 17, T, R - gx - 17, Arena.Height), 0.85f, 0.2f);
                    }
                    break;

                case 7: // Börü: pençe izi
                    if (st[0] <= 0)
                    {
                        st[0] = 0.9f / m;
                        float side = rnd.Next(2) == 0 ? -1 : 1;
                        float ox = sx + side * 78, oy = T - 22;
                        float dx = sx - ox, dy = sy - oy, d = (float)Math.Sqrt(dx * dx + dy * dy);
                        dx /= d; dy /= d;
                        for (int k = -1; k <= 1; k++)
                            AddShot(KShard, ox - dy * k * 11, oy + dx * k * 11, dx * 104 * m, dy * 104 * m, 3f);
                    }
                    break;

                case 8: // Börü: uluma halkaları
                    if (st[0] <= 0)
                    {
                        st[0] = 1.3f / m;
                        int gap = 3 + rnd.Next(9);
                        for (int k = 0; k < 17; k++)
                        {
                            if (k >= gap && k < gap + 3) continue;
                            double an = (16 + k * 9.25) * Math.PI / 180;
                            AddShot(KOrb, 192, T - 12, (float)Math.Cos(an) * 46 * m, (float)Math.Sin(an) * 46 * m, 3f);
                        }
                    }
                    break;

                case 9: // Ayaz Ata: buz sarkıtları
                    if (st[0] <= 0)
                    {
                        st[0] = 0.21f / m;
                        float x = rnd.Next(2) == 0 ? sx + Rf(-16, 16) : Rf(L + 4, R - 4);
                        Shot s = AddShot(KIcicle, Fx.Clamp(x, L + 4, R - 4), T - 8, 0, 88 * m, 2.6f);
                        s.AY = 40;
                    }
                    break;

                case 10: // Ayaz Ata: kar tanesi sarmalı
                    if (st[0] <= 0)
                    {
                        st[0] = 0.09f / m;
                        bool arm = ((int)(atkT * 11)) % 2 == 0;
                        double an = Math.PI / 2 + (arm ? Math.Sin(atkT * 1.6) : -Math.Sin(atkT * 1.6 + 1.2)) * 1.15;
                        AddShot(KFlake, 192, T - 6, (float)Math.Cos(an) * 52 * m, (float)Math.Sin(an) * 52 * m, 3f);
                    }
                    break;

                case 11: // Ayaz Ata: tipi
                    windX = 36f * (float)Math.Sin(atkT * 1.3f);
                    if (st[0] <= 0)
                    {
                        st[0] = 0.12f / m;
                        AddShot(KFlake, Rf(L - 24, R + 24), T - 6, windX * 1.2f, 62 * m, 3f);
                    }
                    if (st[1] <= 0)
                    {
                        st[1] = 1.3f;
                        float cw = Arena.Width / 7f;
                        int col = (int)Fx.Clamp((sx - L) / cw, 0, 6);
                        AddZone(1, new RectangleF(L + col * cw, T, cw, Arena.Height), 0.65f, 0.25f);
                    }
                    break;

                case 12: // Çarşamba Karısı: yuvarlanan yumaklar
                    if (st[0] <= 0)
                    {
                        st[0] = 0.62f / m;
                        Shot s = AddShot(KYarn, Rf(L + 8, R - 8), T - 6, Rf(-14, 14), Rf(26, 34) * m, 4f);
                        s.AY = 60; s.Bounce = 1;
                    }
                    break;

                case 13: // Çarşamba Karısı: gerilen ipler
                    if (st[0] <= 0)
                    {
                        st[0] = 1.25f;
                        float bh = Arena.Height / 5f;
                        int band = (int)Fx.Clamp((sy - T) / bh, 0, 4);
                        AddZone(3, new RectangleF(L, T + band * bh, Arena.Width, bh), 0.75f, 0.2f);
                        if (atkT > 3.2f)
                        {
                            int other = (band + 2 + rnd.Next(2)) % 5;
                            AddZone(3, new RectangleF(L, T + other * bh, Arena.Width, bh), 0.75f, 0.2f);
                        }
                    }
                    break;

                case 14: // Çarşamba Karısı: dönen iğ
                    if (st[0] <= 0)
                    {
                        st[0] = 0.4f / m;
                        double an = Math.PI / 2 + Math.Sin(atkT * 1.3) * 1.05;
                        AddShot(KNeedle, 192, T - 6, (float)Math.Cos(an) * 50 * m, (float)Math.Sin(an) * 50 * m, 2.6f);
                        AddShot(KNeedle, 192, T - 6, (float)Math.Cos(an + 0.9) * 50 * m, (float)Math.Sin(an + 0.9) * 50 * m, 2.6f);
                    }
                    break;

                case 15: // Şahmeran: süzülen yılanlar
                    if (st[0] <= 0)
                    {
                        st[0] = 1.05f / m;
                        float bx = rnd.Next(2) == 0 ? Fx.Clamp(sx + Rf(-20, 20), L + 16, R - 16) : Rf(L + 16, R - 16);
                        float fr = Rf(2.6f, 3.4f), ph = Rf(0, 6.28f), vy = 40 * m;
                        for (int k = 0; k < 6; k++)
                        {
                            Shot s = AddShot(k == 0 ? KSnakeH : KSnakeB, bx, T - 6 - k * 6, 0, vy, k == 0 ? 3.2f : 2.6f);
                            s.Amp = 13; s.Freq = fr; s.Ph = ph - k * 0.45f;
                        }
                    }
                    break;

                case 16: // Şahmeran: kuyruk süpürmesi
                    if (st[0] <= 0)
                    {
                        st[0] = 1.5f;
                        bool leftSide = sx < L + Arena.Width / 2;
                        float w = Arena.Width * 0.45f;
                        AddZone(4, new RectangleF(leftSide ? L : R - w, T, w, Arena.Height), 1.1f, 0.22f);
                    }
                    if (st[1] <= 0)
                    {
                        st[1] = 0.75f / m;
                        Shot s = AddShot(KVenom, Rf(L + 6, R - 6), T - 6, 0, 34 * m, 2.8f);
                        s.AY = 30;
                    }
                    break;

                case 17: // Şahmeran: zehir yelpazesi
                    if (st[0] <= 0)
                    {
                        st[0] = 1.2f / m;
                        bool fromLeft = ((int)(atkT / 1.2f)) % 2 == 0;
                        float ox = fromLeft ? L + 6 : R - 6;
                        int gap = 1 + rnd.Next(5);
                        for (int k = 0; k < 8; k++)
                        {
                            if (k == gap || k == gap + 1) continue;
                            double an = (fromLeft ? 8 + k * 11.5 : 172 - k * 11.5) * Math.PI / 180;
                            AddShot(KVenom, ox, T - 8, (float)Math.Cos(an) * 48 * m, (float)Math.Sin(an) * 48 * m, 2.8f);
                        }
                    }
                    break;

                case 18: // Tepegöz: kaya yağmuru
                    if (st[0] <= 0)
                    {
                        st[0] = 0.5f / m;
                        float x = rnd.Next(3) == 0 ? sx + Rf(-10, 10) : Rf(L + 8, R - 8);
                        Shot s = AddShot(KRock, Fx.Clamp(x, L + 8, R - 8), T - 10, Rf(-10, 10), 30 * m, 5.2f);
                        s.AY = 110; s.Bounce = 1;
                    }
                    break;

                case 19: // Tepegöz: yumruk
                    if (st[0] <= 0)
                    {
                        st[0] = 1.3f;
                        float w = 46;
                        float x0 = Fx.Clamp(sx - w / 2, L, R - w);
                        AddZone(5, new RectangleF(x0, T, w, Arena.Height), 0.8f, 0.22f);
                    }
                    if (st[1] <= 0)
                    {
                        // sarsıntıyla dökülen taşlar
                        st[1] = 0.95f / m;
                        Shot s = AddShot(KRock, Rf(L + 8, R - 8), T - 10, 0, 26 * m, 4f);
                        s.AY = 60;
                    }
                    break;

                case 20: // Tepegöz: tek gözün bakışı
                    if (st[0] <= 0)
                    {
                        st[0] = 0.62f;
                        float bh = Arena.Height / 4f;
                        int step = (int)(atkT / 0.62f);
                        int[] seq = { 0, 1, 2, 3, 2, 1 };
                        int band = seq[step % 6];
                        AddZone(6, new RectangleF(L, T + band * bh, Arena.Width, bh), 0.6f, 0.3f);
                    }
                    if (st[1] <= 0)
                    {
                        st[1] = 0.85f / m;
                        Shot s = AddShot(KRock, Rf(L + 8, R - 8), T - 10, 0, 26 * m, 4.2f);
                        s.AY = 70;
                    }
                    break;

                case 21: // Yel Ana: poyraz (yön değiştiren rüzgârda savrulan kar)
                    if (st[1] <= 0) { st[1] = 2.0f; windX = (windX > 0 ? -1 : 1) * 48f; Sfx("warn", 0.4f); }
                    if (st[0] <= 0)
                    {
                        st[0] = 0.14f / m;
                        AddShot(KFlake, Rf(L - 30, R + 30), T - 6, windX * 1.1f, 54 * m, 3f);
                    }
                    break;

                case 22: // Yel Ana: sağanak (yandan geçen dizi; bir aralığı açık)
                    if (st[0] <= 0)
                    {
                        st[0] = 1.05f / m;
                        bool fromLeft = patN++ % 2 == 0;
                        float rh = Arena.Height / 6f;
                        int gap = rnd.Next(5);
                        for (int k = 0; k < 6; k++)
                        {
                            if (k == gap || k == gap + 1) continue;
                            float ox = 8 + (k % 2) * 10;
                            AddShot(KGust, fromLeft ? L - ox : R + ox, T + rh * (k + 0.5f), (fromLeft ? 1 : -1) * 66 * m, 0, 3.2f);
                        }
                    }
                    break;

                case 23: // Yel Ana: üfleme (rüzgâr seni üfleyeceği yana iter)
                    if (st[0] <= 0)
                    {
                        st[0] = 1.7f;
                        bool leftSide = patN++ % 2 == 0;
                        float w = Arena.Width * 0.42f;
                        windX = (leftSide ? -1 : 1) * 26f;
                        AddZone(7, new RectangleF(leftSide ? L : R - w, T, w, Arena.Height), 1.05f, 0.25f);
                    }
                    if (st[1] <= 0)
                    {
                        st[1] = 0.4f / m;
                        AddShot(KFlake, Rf(L + 6, R - 6), T - 6, windX * 0.8f, 40 * m, 3f);
                    }
                    break;

                case PatLongNight: // Ayaz Ata: Uzun Gece
                    {
                        float k = Math.Min(1f, atkT / 8.5f);
                        boxTarget = new RectangleF(Arena.X + 22 * k, Arena.Y + 10 * k, Arena.Width - 44 * k, Arena.Height - 20 * k);
                        dark = k * 0.8f;
                        if (st[0] <= 0)
                        {
                            st[0] = 0.13f - 0.05f * k;
                            double an = Math.PI / 2 + Math.Sin(atkT * 1.9) * 1.2;
                            AddShot(KFlake, 192, T - 6, (float)Math.Cos(an) * 54, (float)Math.Sin(an) * 54, 3f);
                        }
                        if (st[1] <= 0)
                        {
                            st[1] = 0.5f - 0.2f * k;
                            Shot s = AddShot(KIcicle, Fx.Clamp(sx + Rf(-20, 20), L + 4, R - 4), T - 8, 0, 84, 2.6f);
                            s.AY = 40;
                        }
                    }
                    break;
            }

            // dost kıvılcımları: ısındıkça karşıdaki de ısıtır
            if (!longNight && warm >= 20)
            {
                sparkT -= dt;
                if (sparkT <= 0)
                {
                    sparkT = Fx.Lerp(3.0f, 1.1f, warm / 100f) * (1f - 0.06f * Dost);
                    Shot s = AddShot(KSpark, Rf(L + 10, R - 10), T - 4, 0, 26, 5f);
                    s.Heal = true; s.Amp = 6; s.Freq = 3; s.Ph = Rf(0, 6.28f);
                }
            }
        }

        void UpdateDodge(float dt)
        {
            atkT += dt;
            float dx = 0, dy = 0;
            if (Hold(Btn.Left)) dx -= 1;
            if (Hold(Btn.Right)) dx += 1;
            if (Hold(Btn.Up)) dy -= 1;
            if (Hold(Btn.Down)) dy += 1;
            if (dx != 0 && dy != 0) { dx *= 0.7071f; dy *= 0.7071f; }
            float speed = Hold(Btn.B) ? 38f : (Keep(7) ? 94f : 80f); // Yel Düğümü
            sx += (dx * speed + windX) * dt;
            sy += dy * speed * dt;
            sx = Fx.Clamp(sx, box.Left + 6, box.Right - 6);
            sy = Fx.Clamp(sy, box.Top + 7, box.Bottom - 6);

            if (atkT < atkDur - 0.6f) SpawnPattern(dt);

            float floor = Arena.Bottom - 5;
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                Shot s = shots[i];
                s.T += dt;
                s.VY += s.AY * dt;
                if (s.Amp > 0) { s.BX += s.VX * dt; s.X = s.BX + (float)Math.Sin(s.T * s.Freq + s.Ph) * s.Amp; }
                else s.X += s.VX * dt;
                s.Y += s.VY * dt;
                if (s.Bounce > 0 && s.Y > floor && s.VY > 0) { s.Y = floor; s.VY = -s.VY * 0.74f; s.Bounce--; }

                if (s.X < Arena.Left - 40 || s.X > Arena.Right + 40 || s.Y > Arena.Bottom + 12 || s.Y < Arena.Top - 60 || s.T > 12)
                {
                    shots.RemoveAt(i);
                    continue;
                }
                // kutunun dışındakiler zarar vermez
                if (s.X < box.Left || s.X > box.Right || s.Y < box.Top || s.Y > box.Bottom) continue;
                float ddx = s.X - sx, ddy = s.Y - sy, rr = s.R + 2.4f;
                if (ddx * ddx + ddy * ddy < rr * rr)
                {
                    if (s.Heal)
                    {
                        shots.RemoveAt(i);
                        kor = Math.Min(korMax, kor + (Keep(6) ? 2 : 1)); // Çoban Düğmesi
                        Sfx("spark", 0.7f);
                        for (int k = 0; k < 6; k++)
                            parts.Add(new Part { X = sx, Y = sy, VX = Rf(-30, 30), VY = Rf(-40, 10), Life = 0.5f, Max = 0.5f, C = P.Fire1, S = 1 });
                    }
                    else { Hurt(foe == 3 ? 3 : 2); if (mode != Mode.Battle || phase != BPhase.Dodge) return; }
                }
            }

            for (int i = zones.Count - 1; i >= 0; i--)
            {
                Zone z = zones[i];
                z.T += dt;
                if (z.T >= z.Delay && !z.Struck) { z.Struck = true; Sfx("strike", 0.6f); }
                if (z.T >= z.Delay && z.T < z.Delay + z.Active && z.R.Contains(sx, sy))
                {
                    if (inv <= 0) DevHitInfo += string.Format("zone k{0} T={1:F2} R=[{2:F0}..{3:F0}] s=({4:F0},{5:F0}) atk={6:F2} zones={7}; ", z.Kind, z.T, z.R.Left, z.R.Right, sx, sy, atkT, zones.Count);
                    Hurt(foe == 3 ? 3 : 2);
                    if (mode != Mode.Battle || phase != BPhase.Dodge) return;
                }
                if (z.T > z.Delay + z.Active + 0.12f) zones.RemoveAt(i);
            }

            if (atkT >= atkDur)
            {
                shots.Clear(); zones.Clear();
                windX = 0;
                if (longNight) { StartRescue(); return; }
                if (Keep(5) && kor < korMax) kor++; // Yılan Pulu
                turn++;
                EnterMenu();
            }
        }

        // =====================================================
        //  SON: KOR SÖNER, DOSTLAR GELİR
        // =====================================================
        void StartRescue()
        {
            phase = BPhase.Finale;
            longNight = false;
            shots.Clear(); zones.Clear();
            boxTarget = TextBox;
            windX = 0;
            dActive = false;
            int dost = Dost;

            var steps = new List<Func<float, bool>>
            {
                SDo(() => { kor = 0; dark = 1f; Music(null); Sfx("out", 0.9f); Flash(P.Ice, 0.9f); }),
                SWait(1.0f),
                SSay("Fenerin içindeki kor, son bir kez parladı.", "Ve söndü."),
                SWait(1.1f),
                SSay("Karanlık.", "Ayaz Ata|...Demek buraya kadarmış. Altmış kıştan sonra, son kış buymuş."),
                SWait(0.8f),
                SSay("Karanlıkta bir şey kıpırdadı.")
            };
            string[][] lines =
            {
                new[] { "Karakoncolos|Soru! Ateşi sönen yolcuya ne verilir?", "Karakoncolos|Cevap: KARA tüyden bir kıvılcım! ...Kendi soruma kendim cevap verdim. İlk kez oluyor." },
                new[] { "Turna|Gökten gördüm. \"Fener söndü\" dediler. \"Sönmedi\" dedim.", "Turna|Al. Kanadımın altında sakladım; senin verdiğin sıcaktan." },
                new[] { "Börü|Tek başına yürümüyorsun, demiştim.", "Börü|Kurt sözünü tutar. Al şu sıcağı. Zaten senindi." },
                null,
                new[] { "Çarşamba Karısı|Fener mi sönmüş? Yarım iş! Ben yarım işe dayanamam!", "Çarşamba Karısı|Al şu yumağı. İçine bir kıvılcım sarmıştım; lazım olur diye." },
                new[] { "Şahmeran|Bir insana bir kez daha güvendim. Bu sefer yanılmamışım.", "Şahmeran|Al. Pulumun altında sakladım. Soğuk kan, sıcağı iyi saklar." },
                new[] { "Tepegöz|Küçük güneş söndü mü? Ağlama. Ağlama, bak.", "Tepegöz|Avucumda biraz kalmıştı. Sönmesin diye hiç sıkmadım." },
                new[] { "Yel Ana|Sönmüş mü? Ben üflemedim! Yemin ederim üflemedim!", "Yel Ana|Dur. Külün altında bir kıvılcım var. Az eseceğim. Az. ...Bak. Kızardı." }
            };
            foreach (int i in roadSpirits)
            {
                if (fate[i] != 1) continue;
                int id = i;
                steps.Add(SDo(() => { rescued[id] = true; Sfx("spark", 0.9f); Flash(P.Fire1, 0.35f); }));
                steps.Add(SSay(lines[id]));
                steps.Add(SDo(() =>
                {
                    kor = Math.Min(korMax, kor + korMax / Math.Max(1, dost) + 1);
                    Sfx("warm", 0.9f);
                    dark = Math.Max(0.15f, dark - 0.85f / Math.Max(1, dost));
                    for (int k = 0; k < 20; k++)
                        parts.Add(new Part { X = 192 + Rf(-60, 60), Y = 150 + Rf(-10, 10), VX = Rf(-20, 20), VY = Rf(-50, -10), Life = 1f, Max = 1f, C = P.Fire1, S = 2 });
                }));
                steps.Add(SWait(0.5f));
            }
            if (Yanan == 0)
                steps.Add(SSay(
                    "Kıvılcımlar fenerde buluştu. Tek kor oldu; Sönmez'in ocağından bile parlak.",
                    "Ayaz Ata|...Bu ateşi tek başına taşımadın.",
                    "Ayaz Ata|Altmış kış aynı kadın geldi, aynı ateşle. Sıcaktı. Ama hiç bu kadar kalabalık olmamıştı.",
                    "Ayaz Ata|Son kış sanmıştım. Yanılmışım.",
                    "Ayaz Ata|Yak ocağı, Nar. Gün senindir."));
            else
                steps.Add(SSay(
                    "Kor yeniden tutuştu. Küçük, ama diri.",
                    "Ayaz Ata|Yetmez sandım. Yeter. Ucu ucuna.",
                    "Ayaz Ata|Yolda sönen sesleri duydum, çocuk. Onları sen söndürdün. Bunu da taşıyacaksın.",
                    "Ayaz Ata|Yak ocağı."));
            int kind = Yanan == 0 ? 0 : 1;
            steps.Add(SDo(() => { fate[3] = 1; warm = 100; FadeTo(() => StartEnding(kind), 1.2f); }));
            steps.Add(SWait(99f));
            Run(steps.ToArray());
        }

        // =====================================================
        //  GÜNCELLEME
        // =====================================================
        void UpdateBattle(float dt)
        {
            foeFlash = Math.Max(0, foeFlash - dt);
            dmgT = Math.Max(0, dmgT - dt);
            inv = Math.Max(0, inv - dt);
            warmPulse = Math.Max(0, warmPulse - dt * 1.5f);
            if (foeGone > 0) foeGone = Math.Min(1f, foeGone + dt * 0.9f);
            float k = Math.Min(1f, dt * 12f);
            box = new RectangleF(box.X + (boxTarget.X - box.X) * k, box.Y + (boxTarget.Y - box.Y) * k,
                box.Width + (boxTarget.Width - box.Width) * k, box.Height + (boxTarget.Height - box.Height) * k);

            for (int i = parts.Count - 1; i >= 0; i--)
            {
                Part p = parts[i];
                p.Life -= dt;
                if (p.Life <= 0) { parts.RemoveAt(i); continue; }
                p.X += p.VX * dt; p.Y += p.VY * dt; p.VY += 30 * dt;
                parts[i] = p;
            }

            switch (phase)
            {
                case BPhase.Text:
                    UpdateDialogue(dt);
                    break;

                case BPhase.Menu:
                    menuShown += dt * 46f;
                    if (Tap(Btn.Left)) { menuIdx = (menuIdx + 3) % 4; Sfx("move", 0.6f); }
                    if (Tap(Btn.Right)) { menuIdx = (menuIdx + 1) % 4; Sfx("move", 0.6f); }
                    if (Ok) MenuSelect();
                    break;

                case BPhase.Sub:
                    if (Tap(Btn.Up)) { subIdx = (subIdx + subItems.Length - 1) % subItems.Length; Sfx("move", 0.6f); }
                    if (Tap(Btn.Down)) { subIdx = (subIdx + 1) % subItems.Length; Sfx("move", 0.6f); }
                    if (Back) { Sfx("back", 0.6f); EnterMenu(); break; }
                    if (Ok) SubSelect();
                    break;

                case BPhase.Aim:
                    aimX += aimDir * dt * 1.35f;
                    if (aimX >= 1) { aimX = 1; aimDir = -1; aimPass++; }
                    else if (aimX <= 0 && aimDir < 0) { aimX = 0; aimDir = 1; aimPass++; }
                    if (Ok) ResolveAim(true);
                    else if (Back) { Sfx("back", 0.6f); EnterMenu(); }
                    else if (aimPass >= 4) ResolveAim(false);
                    break;

                case BPhase.Burst:
                    burstT -= dt;
                    if (burstT <= 0)
                    {
                        Action a = burstThen; burstThen = null;
                        if (a != null) a();
                    }
                    break;

                case BPhase.Dodge:
                    UpdateDodge(dt);
                    break;

                case BPhase.Finale:
                    if (dActive) UpdateDialogue(dt);
                    else UpdateScript(dt);
                    break;
            }
        }

        // =====================================================
        //  ÇİZİM
        // =====================================================
        void DrawShot(Graphics g, Shot s)
        {
            float x = s.X, y = s.Y;
            switch (s.Kind)
            {
                case KQ:
                    g.FillEllipse(Fx.B(P.C(0x5E3F7A)), x - 5, y - 6, 10, 12);
                    g.FillEllipse(Fx.B(P.C(0x2C2048)), x - 4, y - 5, 8, 10);
                    Font5.Draw(g, "?", x - 2, y - 4, P.C(0xF0E4FF));
                    break;
                case KCone:
                    g.FillEllipse(Fx.B(P.C(0x4A3226)), x - 4, y - 5, 8, 10);
                    g.FillEllipse(Fx.B(P.C(0x8A5A3A)), x - 3, y - 4, 6, 8);
                    g.FillRectangle(Fx.B(P.C(0x4A3226)), x - 2, y - 2, 4, 1);
                    g.FillRectangle(Fx.B(P.C(0x4A3226)), x - 2, y + 1, 4, 1);
                    g.FillRectangle(Fx.B(P.C(0xC89060)), x - 1, y - 4, 1, 1);
                    break;
                case KFeather:
                    {
                        float lean = (float)Math.Cos(s.T * s.Freq + s.Ph) * 3;
                        g.FillPolygon(Fx.B(P.C(0x7D8AA3)), new[] { new PointF(x + lean, y - 6), new PointF(x + 3, y), new PointF(x - lean * 0.6f, y + 6), new PointF(x - 3, y) });
                        g.FillPolygon(Fx.B(P.C(0xE6ECF5)), new[] { new PointF(x + lean, y - 5), new PointF(x + 1.5f, y), new PointF(x - lean * 0.6f, y + 5), new PointF(x - 2, y) });
                        g.FillRectangle(Fx.B(P.C(0x7D8AA3)), x, y - 3, 1, 6);
                    }
                    break;
                case KShard:
                    {
                        float d = (float)Math.Sqrt(s.VX * s.VX + s.VY * s.VY) + 0.01f, ux = s.VX / d, uy = s.VY / d;
                        g.FillPolygon(Fx.B(P.IceD), new[] { new PointF(x + ux * 8, y + uy * 8), new PointF(x - uy * 3.5f, y + ux * 3.5f), new PointF(x - ux * 7, y - uy * 7), new PointF(x + uy * 3.5f, y - ux * 3.5f) });
                        g.FillPolygon(Fx.B(P.C(0xE4F8FF)), new[] { new PointF(x + ux * 6, y + uy * 6), new PointF(x - uy * 2f, y + ux * 2f), new PointF(x - ux * 4, y - uy * 4), new PointF(x + uy * 2f, y - ux * 2f) });
                    }
                    break;
                case KOrb:
                    g.FillEllipse(Fx.B(P.IceD), x - 4, y - 4, 8, 8);
                    g.FillEllipse(Fx.B(P.C(0xCFE0F8)), x - 3, y - 3, 6, 6);
                    g.FillRectangle(Fx.B(P.Snow0), x - 2, y - 2, 2, 2);
                    break;
                case KFlake:
                    {
                        Brush c = Fx.B(P.C(0xDCEBFF));
                        g.FillRectangle(c, x - 0.5f, y - 3.5f, 1, 7);
                        g.FillRectangle(c, x - 3.5f, y - 0.5f, 7, 1);
                        g.FillRectangle(c, x - 2.5f, y - 2.5f, 1, 1); g.FillRectangle(c, x + 1.5f, y - 2.5f, 1, 1);
                        g.FillRectangle(c, x - 2.5f, y + 1.5f, 1, 1); g.FillRectangle(c, x + 1.5f, y + 1.5f, 1, 1);
                        g.FillRectangle(Fx.B(P.Snow0), x - 1.5f, y - 1.5f, 3, 3);
                    }
                    break;
                case KIcicle:
                    g.FillPolygon(Fx.B(P.IceD), new[] { new PointF(x - 3, y - 8), new PointF(x + 3, y - 8), new PointF(x, y + 6) });
                    g.FillPolygon(Fx.B(P.C(0xCFF1FA)), new[] { new PointF(x - 2, y - 8), new PointF(x + 1, y - 8), new PointF(x, y + 4) });
                    break;
                case KYarn:
                    g.FillEllipse(Fx.B(P.RedD), x - 5, y - 5, 10, 10);
                    g.FillEllipse(Fx.B(P.Red), x - 4, y - 4, 8, 8);
                    g.FillRectangle(Fx.B(P.RedL), x - 3, y - 2, 6, 1);
                    g.FillRectangle(Fx.B(P.RedD), x - 3, y + 1, 6, 1);
                    g.FillRectangle(Fx.B(P.RedL), x - 1, y - 4, 1, 8);
                    g.FillRectangle(Fx.B(P.Red), x + 3, y + 3, 4, 1);
                    break;
                case KNeedle:
                    {
                        float d = (float)Math.Sqrt(s.VX * s.VX + s.VY * s.VY) + 0.01f, ux = s.VX / d, uy = s.VY / d;
                        g.FillPolygon(Fx.B(P.Bronze), new[] { new PointF(x + ux * 8, y + uy * 8), new PointF(x - uy * 2.5f, y + ux * 2.5f), new PointF(x - ux * 7, y - uy * 7), new PointF(x + uy * 2.5f, y - ux * 2.5f) });
                        g.FillPolygon(Fx.B(P.Cream), new[] { new PointF(x + ux * 7, y + uy * 7), new PointF(x - uy * 1.2f, y + ux * 1.2f), new PointF(x - ux * 5, y - uy * 5), new PointF(x + uy * 1.2f, y - ux * 1.2f) });
                        g.FillEllipse(Fx.B(P.Red), x - ux * 3 - 2.5f, y - uy * 3 - 2.5f, 5, 5);
                    }
                    break;
                case KSnakeH:
                    g.FillEllipse(Fx.B(P.C(0x1D4C50)), x - 5, y - 5, 10, 10);
                    g.FillEllipse(Fx.B(P.C(0x58B894)), x - 4, y - 4, 8, 8);
                    g.FillRectangle(Fx.B(P.Fire1), x - 3, y, 2, 2);
                    g.FillRectangle(Fx.B(P.Fire1), x + 1, y, 2, 2);
                    g.FillRectangle(Fx.B(P.Red), x - 0.5f, y + 4, 1, 3);
                    break;
                case KSnakeB:
                    g.FillEllipse(Fx.B(P.C(0x1D4C50)), x - 4, y - 4, 8, 8);
                    g.FillEllipse(Fx.B(P.C(0x2F8F72)), x - 3, y - 3, 6, 6);
                    g.FillRectangle(Fx.B(P.C(0xDCCB82)), x - 1, y - 1, 2, 2);
                    break;
                case KVenom:
                    g.FillPolygon(Fx.B(P.C(0x3E7A2E)), new[] { new PointF(x, y - 6), new PointF(x + 4, y + 1), new PointF(x, y + 5), new PointF(x - 4, y + 1) });
                    g.FillEllipse(Fx.B(P.C(0x9BE05A)), x - 3, y - 2, 6, 6);
                    g.FillRectangle(Fx.B(P.C(0xE8FFC0)), x - 2, y, 2, 2);
                    break;
                case KRock:
                    {
                        float r = s.R + 1;
                        g.FillEllipse(Fx.B(P.C(0x2E3550)), x - r - 1, y - r - 1, r * 2 + 2, r * 2 + 2);
                        g.FillEllipse(Fx.B(P.C(0x5A6384)), x - r, y - r, r * 2, r * 2);
                        g.FillEllipse(Fx.B(P.C(0x7C87A8)), x - r * 0.7f, y - r * 0.8f, r, r * 0.9f);
                        g.FillRectangle(Fx.B(P.C(0x3E4666)), x + r * 0.1f, y + r * 0.2f, r * 0.6f, 1);
                    }
                    break;
                case KGust:
                    {
                        float dir = s.VX >= 0 ? 1 : -1;
                        g.FillEllipse(Fx.B(P.Snow3), x - 6, y - 4, 12, 8);
                        g.FillEllipse(Fx.B(P.Snow0), x - 4 + dir, y - 3, 8, 6);
                        g.FillRectangle(Fx.B(170, P.Snow1), x - dir * 12 - 3, y - 1, 6, 1);
                        g.FillRectangle(Fx.B(170, P.Snow1), x - dir * 9 - 2, y + 2, 4, 1);
                    }
                    break;
                case KSpark:
                    {
                        float tw = (float)Math.Sin(time * 14 + s.Ph);
                        g.FillEllipse(Fx.B(40, P.Fire1), x - 6, y - 6, 12, 12);
                        g.FillRectangle(Fx.B(P.Fire1), x - 0.5f, y - 3.5f - tw, 1, 7 + tw * 2);
                        g.FillRectangle(Fx.B(P.Fire1), x - 3.5f - tw, y - 0.5f, 7 + tw * 2, 1);
                        g.FillRectangle(Fx.B(P.Fire0), x - 1.5f, y - 1.5f, 3, 3);
                    }
                    break;
            }
        }

        void DrawZone(Graphics g, Zone z)
        {
            RectangleF r = RectangleF.Intersect(z.R, box);
            if (r.Width <= 0 || r.Height <= 0) return;
            if (z.T < z.Delay)
            {
                // uyarı
                float p = z.T / z.Delay;
                bool blink = (int)(z.T * (8 + p * 12)) % 2 == 0;
                g.FillRectangle(Fx.B(blink ? 70 : 34, P.RedL), r);
                Fx.Frame(g, Color.FromArgb(blink ? 220 : 120, P.RedL), (int)r.X, (int)r.Y, (int)r.Width, (int)r.Height);
                if (blink) Font5.Draw(g, "!", r.X + r.Width / 2 - 1, r.Y + r.Height / 2 - 3, P.Cream);
                return;
            }
            float a = z.T - z.Delay;
            if (a > z.Active + 0.12f) return;
            int alpha = a < z.Active ? 255 : 110;
            if (z.Kind == 0)
            {
                g.FillRectangle(Fx.B(alpha / 3, P.Ice), r);
                for (int k = 0; k < 3; k++)
                {
                    float yy = r.Y + r.Height * (0.22f + 0.28f * k);
                    g.FillPolygon(Fx.B(alpha, P.Snow0), new[] { new PointF(r.X, yy - 1), new PointF(r.Right, yy - 3), new PointF(r.Right, yy + 2), new PointF(r.X, yy + 1) });
                }
            }
            else if (z.Kind == 1)
            {
                g.FillRectangle(Fx.B(alpha / 4, P.Ice), r);
                g.FillPolygon(Fx.B(alpha, P.IceD), new[] { new PointF(r.X, r.Bottom), new PointF(r.X + r.Width / 2, r.Y + 2), new PointF(r.Right, r.Bottom) });
                g.FillPolygon(Fx.B(alpha, P.C(0xCFF1FA)), new[] { new PointF(r.X + 2, r.Bottom), new PointF(r.X + r.Width / 2, r.Y + 6), new PointF(r.X + r.Width / 2 + 1, r.Bottom) });
            }
            else if (z.Kind == 3)
            {
                // gergin ip
                g.FillRectangle(Fx.B(alpha / 4, P.Red), r);
                float yy = r.Y + r.Height / 2;
                g.FillRectangle(Fx.B(alpha, P.RedD), r.X, yy - 2, r.Width, 4);
                g.FillRectangle(Fx.B(alpha, P.RedL), r.X, yy - 1, r.Width, 1);
                for (float xx = r.X + 3; xx < r.Right - 2; xx += 9) g.FillRectangle(Fx.B(alpha, P.Cream), xx, yy - 3, 2, 6);
            }
            else if (z.Kind == 4)
            {
                // kuyruk
                g.FillRectangle(Fx.B(alpha / 3, P.Teal), r);
                float yy = r.Y + r.Height * 0.5f;
                g.FillPolygon(Fx.B(alpha, P.C(0x1D4C50)), new[] { new PointF(r.X, yy - 12), new PointF(r.Right, yy - 5), new PointF(r.Right, yy + 5), new PointF(r.X, yy + 12) });
                g.FillPolygon(Fx.B(alpha, P.C(0x58B894)), new[] { new PointF(r.X, yy - 9), new PointF(r.Right, yy - 3), new PointF(r.Right, yy + 1), new PointF(r.X, yy + 3) });
                for (float xx = r.X + 4; xx < r.Right - 3; xx += 8) g.FillRectangle(Fx.B(alpha, P.C(0xDCCB82)), xx, yy + 5, 3, 2);
            }
            else if (z.Kind == 5)
            {
                // yumruk
                g.FillRectangle(Fx.B(alpha / 3, P.Snow3), r);
                float fh = Math.Min(r.Height, 46), fy0 = r.Bottom - fh;
                g.FillRectangle(Fx.B(alpha, P.C(0x596280)), r.X + 3, r.Y, r.Width - 6, r.Height - fh + 4);
                g.FillRectangle(Fx.B(alpha, P.C(0x596280)), r.X, fy0, r.Width, fh);
                g.FillRectangle(Fx.B(alpha, P.C(0x7C86A0)), r.X + 2, fy0 + 2, r.Width - 4, fh - 6);
                for (int k = 1; k < 4; k++) g.FillRectangle(Fx.B(alpha, P.C(0x596280)), r.X + r.Width * k / 4f, fy0 + fh - 16, 1, 12);
                g.FillRectangle(Fx.B(alpha, P.Snow0), r.X - 4, r.Bottom - 2, r.Width + 8, 2);
            }
            else if (z.Kind == 7)
            {
                // üfleme
                g.FillRectangle(Fx.B(alpha / 3, P.Snow1), r);
                for (float yy = r.Y + 4; yy < r.Bottom - 2; yy += 7)
                {
                    float off = (yy * 13) % 9;
                    g.FillRectangle(Fx.B(alpha, P.Snow0), r.X + off, yy, Math.Max(0, r.Width - off - 4), 2);
                    g.FillRectangle(Fx.B(alpha, P.Snow2), r.X + off + 6, yy + 2, Math.Max(0, r.Width - off - 16), 1);
                }
            }
            else if (z.Kind == 6)
            {
                // bakış
                g.FillRectangle(Fx.B(alpha / 3, P.Amber), r);
                g.FillRectangle(Fx.B(alpha, P.AmberD), r.X, r.Y + r.Height * 0.25f, r.Width, r.Height * 0.5f);
                g.FillRectangle(Fx.B(alpha, P.Fire1), r.X, r.Y + r.Height * 0.36f, r.Width, r.Height * 0.28f);
                g.FillRectangle(Fx.B(alpha, P.Fire0), r.X, r.Y + r.Height * 0.46f, r.Width, Math.Max(1, r.Height * 0.08f));
            }
            else
            {
                g.FillRectangle(Fx.B(alpha / 3, P.RedD), r);
                int n = Math.Max(1, (int)(r.Width / 12));
                float tw = r.Width / n;
                for (int k = 0; k < n; k++)
                {
                    float x0 = r.X + k * tw;
                    g.FillPolygon(Fx.B(alpha, P.Cream), new[] { new PointF(x0 + 1, r.Y), new PointF(x0 + tw - 1, r.Y), new PointF(x0 + tw / 2, r.Y + r.Height * 0.55f) });
                    g.FillPolygon(Fx.B(alpha, P.Cream), new[] { new PointF(x0 + 1, r.Bottom), new PointF(x0 + tw - 1, r.Bottom), new PointF(x0 + tw / 2, r.Bottom - r.Height * 0.55f) });
                }
            }
        }

        void DrawBar(Graphics g, int x, int y, int w, float frac, Color c1, Color c2)
        {
            g.FillRectangle(Fx.B(P.Ink), x - 1, y - 1, w + 2, 7);
            g.FillRectangle(Fx.B(P.Night2), x, y, w, 5);
            int f = (int)(w * Fx.Clamp(frac, 0, 1));
            g.FillRectangle(Fx.B(c2), x, y, f, 5);
            g.FillRectangle(Fx.B(c1), x, y, f, 2);
        }

        void DrawBattle(Graphics g)
        {
            // sahne
            DrawBackdrop(g, camX);
            g.FillRectangle(Fx.B(120, P.Night0), 0, 0, W, H);
            int fy = foeFoot[foe];
            g.FillEllipse(Fx.B(60, P.Snow3), 192 - 70, 92, 140, 26);
            float wn = warm / 100f;
            if (wn > 0.05f && foeGone == 0)
                for (int i = 0; i < 4; i++)
                {
                    float r = (36 + 34 * wn) * (1f - i * 0.2f) + warmPulse * 8;
                    g.FillEllipse(Fx.B((int)(10 + 10 * wn), P.Fire2), 192 - r, fy - 44 - r * 0.8f, r * 2, r * 1.6f);
                }

            // ruh
            if (foeGone < 0.35f)
            {
                bool flash = foeFlash > 0 && (int)(time * 30) % 2 == 0;
                if (!flash)
                {
                    float shiver = foeFlash > 0 ? 2f : (1f - wn) * 1.0f;
                    if (foe == 3) shiver = foeFlash > 0 ? 2f : 0;
                    DrawSpirit(g, foe, 192, fy, wn >= 0.6f || (foe == 3 && phase == BPhase.Finale && Dost > 0 && kor > 0), 2, shiver);
                }
            }
            if (dark > 0) g.FillRectangle(Fx.B((int)(dark * 235), Color.Black), 0, 0, W, H);

            // sona gelen dostlar
            if (phase == BPhase.Finale)
            {
                if (rescued[6]) DrawSpirit(g, 6, 34, 114, true, 1, 0);
                if (rescued[4]) DrawSpirit(g, 4, 300, 112, true, 1, 0);
                if (rescued[5]) DrawSpirit(g, 5, 262, 112, true, 1, 0);
                if (rescued[0]) DrawSpirit(g, 0, 76, 112, true, 1, 0);
                if (rescued[1]) DrawSpirit(g, 1, 346, 112, true, 1, 0);
                if (rescued[2]) DrawSpirit(g, 2, 118, 108, true, 1, 0);
                if (rescued[7]) DrawSpirit(g, 7, 232, 100, true, 1, 0);
                // ortadaki fener
                float glow = kor / (float)Math.Max(1, korMax);
                if (glow > 0)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        float r = (14 + 50 * glow) * (1f - i * 0.2f);
                        g.FillEllipse(Fx.B(22, P.Fire2), 192 - r, 86 - r, r * 2, r * 2);
                    }
                    DrawEmber(g, 192, 90, 1.5f);
                }
                else g.FillRectangle(Fx.B(P.Fire4), 191, 90, 3, 2);
            }

            foreach (Part p in parts)
            {
                int a = (int)(255 * Math.Min(1f, p.Life / (p.Max * 0.5f))) / 32 * 32;
                if (a > 0) g.FillRectangle(Fx.B(a, p.C), (int)p.X, (int)p.Y, p.S, p.S);
            }
            DrawSnow(g);

            if (dmgT > 0)
            {
                string t = dmgText;
                float yy = fy - 86 - (1.1f - dmgT) * 14;
                Font5.CenterS(g, t, 192, Math.Max(4, yy), t == "0" ? P.Dim : P.Fire1, P.Ink, t.Length > 4 ? 1 : 2);
            }

            // ruhun adı ve göstergeleri
            if (phase != BPhase.Finale)
            {
                Font5.DrawS(g, foeTitle[foe], 10, 8, P.Cream, P.Ink);
                Font5.DrawS(g, "SICAKLIK", 10, 22, P.Dim, P.Ink);
                DrawBar(g, 62, 23, 70, warm / 100f, P.Fire0, P.Fire2);
                if (foeHp < foeHpMax[foe])
                {
                    Font5.DrawS(g, "CAN", 10, 34, P.Dim, P.Ink);
                    DrawBar(g, 62, 35, 70, foeHp / (float)foeHpMax[foe], P.RedL, P.RedD);
                }
                if (trust > 1.01f) Font5.DrawS(g, "güven kazandın", 10, foeHp < foeHpMax[foe] ? 46 : 34, P.Green, P.Ink);
            }

            // kutu
            int bx = (int)Math.Round(box.X), by = (int)Math.Round(box.Y), bw = (int)Math.Round(box.Width), bh = (int)Math.Round(box.Height);
            DrawPanel(g, bx, by, bw, bh, 255);

            if (phase == BPhase.Text || (phase == BPhase.Finale && dActive))
                DrawDialogueText(g, bx + 11, by + 9, bw - 22, bh - 16);
            else if (phase == BPhase.Menu)
            {
                int left = (int)menuShown, row = 0;
                foreach (string l in Font5.Wrap(menuText, 278))
                {
                    if (left <= 0) break;
                    Font5.Draw(g, left >= l.Length ? l : l.Substring(0, left), bx + 11, by + 9 + row * 12, P.Narr);
                    left -= l.Length; row++;
                }
            }
            else if (phase == BPhase.Sub)
            {
                // en çok üç satır sığar; fazlası kayar
                int top = subItems.Length <= 3 ? 0 : Math.Max(0, Math.Min(subIdx - 1, subItems.Length - 3));
                for (int i = top; i < subItems.Length && i < top + 3; i++)
                {
                    bool sel = i == subIdx;
                    int ry = by + 10 + (i - top) * 14;
                    string[] part = subItems[i].Split('|');
                    Font5.Draw(g, part[0], bx + 30, ry, sel ? P.Fire1 : P.Cream);
                    if (part.Length > 1) Font5.Draw(g, part[1], bx + 130, ry, sel ? P.Amber : P.Dim);
                    if (sel) DrawEmber(g, bx + 18, ry + 3, 1f);
                }
                if (top > 0) g.FillPolygon(Fx.B(P.Amber), new[] { new PointF(bx + bw - 14, by + 13), new PointF(bx + bw - 10, by + 8), new PointF(bx + bw - 6, by + 13) });
                if (top + 3 < subItems.Length) g.FillPolygon(Fx.B(P.Amber), new[] { new PointF(bx + bw - 14, by + 32), new PointF(bx + bw - 10, by + 37), new PointF(bx + bw - 6, by + 32) });
                Font5.Draw(g, "X: geri", bx + bw - 48, by + bh - 14, P.Dim);
            }
            else if (phase == BPhase.Aim || phase == BPhase.Burst)
            {
                int tx = bx + 24, tw = bw - 48, ty = by + 26;
                Font5.Center(g, "Alev tam ortadayken Z'ye bas", bx + bw / 2, by + 8, P.Dim);
                g.FillRectangle(Fx.B(P.Ink), tx - 1, ty - 1, tw + 2, 12);
                g.FillRectangle(Fx.B(P.Night2), tx, ty, tw, 10);
                g.FillRectangle(Fx.B(P.Fire4), tx + tw / 2 - 40, ty, 80, 10);
                g.FillRectangle(Fx.B(P.Fire3), tx + tw / 2 - 20, ty, 40, 10);
                g.FillRectangle(Fx.B(P.Fire1), tx + tw / 2 - 6, ty, 12, 10);
                float mx = tx + aimX * tw;
                g.FillRectangle(Fx.B(P.Snow0), mx - 1, ty - 4, 2, 18);
                DrawEmber(g, mx, ty - 8, 1f);
            }
            else if (phase == BPhase.Dodge)
            {
                g.SetClip(new Rectangle(bx + 3, by + 3, bw - 6, bh - 6));
                if (windX != 0)
                    for (int i = 0; i < 9; i++)
                    {
                        float wy = by + 8 + i * (bh - 16) / 8f;
                        float wxp = ((time * windX * 3 + i * 47) % bw + bw) % bw;
                        g.FillRectangle(Fx.B(70, P.Snow1), bx + wxp, wy, 10, 1);
                    }
                foreach (Zone z in zones) DrawZone(g, z);
                foreach (Shot s in shots) DrawShot(g, s);
                g.ResetClip();
                if (inv <= 0 || (int)(time * 20) % 2 == 0)
                {
                    g.FillEllipse(Fx.B(26, P.Fire1), sx - 9, sy - 9, 18, 18);
                    DrawEmber(g, sx, sy + 1, 1f);
                }
                // kalan süre
                float left = Fx.Clamp(1f - atkT / atkDur, 0, 1);
                g.FillRectangle(Fx.B(P.Bronze), bx + 4, by + bh + 2, (int)((bw - 8) * left), 1);
                // ilk karşılaşmada kısa ipucu
                if (foe == 0 && turn < 2)
                {
                    Font5.DrawS(g, "Ok tuşları: kaçın", 262, 112, P.Cream, P.Ink);
                    Font5.DrawS(g, "X basılı: yavaş", 262, 125, P.Dim, P.Ink);
                    Font5.DrawS(g, "Altın kıvılcım: +Kor", 262, 138, P.Fire1, P.Ink);
                }
            }

            // durum satırı
            Font5.DrawS(g, "NAR", 42, 181, P.Amber, P.Ink);
            DrawEmber(g, 72, 185, 1f);
            int kw = korMax * 3;
            g.FillRectangle(Fx.B(P.Ink), 79, 180, kw + 2, 9);
            g.FillRectangle(Fx.B(P.RedD), 80, 181, kw, 7);
            g.FillRectangle(Fx.B(P.Fire2), 80, 181, kor * 3, 7);
            g.FillRectangle(Fx.B(P.Fire1), 80, 181, kor * 3, 3);
            Font5.DrawS(g, kor + "/" + korMax, 86 + kw, 181, P.Cream, P.Ink);
            if (Dost > 0)
            {
                string d = "dost ×" + Dost;
                Font5.DrawS(g, d, 342 - Font5.Width(d), 181, P.Green, P.Ink);
            }

            // komut düğmeleri
            for (int i = 0; i < 4; i++)
            {
                int x = 22 + i * 86;
                bool sel = phase == BPhase.Menu && i == menuIdx;
                bool dim = phase != BPhase.Menu && !(phase == BPhase.Sub && i == menuIdx);
                g.FillRectangle(Fx.B(P.Ink), x, 194, 82, 18);
                g.FillRectangle(Fx.B(sel ? P.PanelHi : P.Panel), x + 1, 195, 80, 16);
                Fx.Frame(g, sel ? P.Fire1 : (dim ? P.Bronze : P.AmberD), x + 1, 195, 80, 16);
                Color tc = sel ? P.Fire1 : (dim ? P.Dim : P.Cream);
                int lw = Font5.Width(menuNames[i]);
                int ix = x + 41 - (lw + 13) / 2;
                if (i == 0) Art.Ember.Draw(g, ix + 4, 204);
                else g.DrawImage(i == 1 ? icoTalk : (i == 2 ? icoWarm : icoBag), ix, 199, 9, 9);
                Font5.Draw(g, menuNames[i], ix + 13, 200, tc);
                if (sel) g.FillRectangle(Fx.B(P.Fire1), x + 3, 209, 76, 1);
            }

            if (hurtFlash) { g.FillRectangle(Fx.B(60, P.Red), 0, 0, W, H); hurtFlash = false; }
        }
    }
}
