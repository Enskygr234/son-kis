using System;
using System.IO;
using System.Windows.Forms;

namespace WinterDeveloper.SonKis
{
    static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            // Geliştirme komutları (oyuncu için görünmez)
            if (args.Length >= 2)
            {
                switch (args[0])
                {
                    case "--sheet": Dev.Sheet(args[1]); return 0;
                    case "--shots": Dev.Shots(args[1], args.Length > 2 ? args[2] : null); return 0;
                    case "--selftest": return Dev.SelfTest(args[1]);
                    case "--dodgetest": Dev.DodgeTest(args[1]); return 0;
                    case "--bench": Dev.Bench(args[1]); return 0;
                    case "--audio": File.WriteAllText(Path.Combine(args[1], "audio.txt"), Audio.Dump(args[1])); return 0;
                }
            }

            try
            {
                Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new GameForm());
            }
            catch (Exception ex)
            {
                try
                {
                    string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Winterdeveloper", "SonKis");
                    Directory.CreateDirectory(dir);
                    File.WriteAllText(Path.Combine(dir, "hata.txt"), ex.ToString());
                }
                catch { }
                MessageBox.Show("Beklenmeyen bir hata oluştu:\n\n" + ex.Message, "SON KIŞ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
            return 0;
        }
    }
}
