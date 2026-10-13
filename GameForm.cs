using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace WinterDeveloper.SonKis
{
    static class Native
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct MSG { public IntPtr hwnd; public uint message; public IntPtr wParam, lParam; public uint time; public int x, y; }

        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPINFOHEADER
        {
            public int biSize, biWidth, biHeight;
            public short biPlanes, biBitCount;
            public int biCompression, biSizeImage, biXPelsPerMeter, biYPelsPerMeter, biClrUsed, biClrImportant;
        }

        [DllImport("user32.dll")] public static extern bool PeekMessage(out MSG msg, IntPtr hWnd, uint min, uint max, uint remove);
        [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int vKey);
        [DllImport("gdi32.dll")] public static extern int SetStretchBltMode(IntPtr hdc, int mode);
        [DllImport("gdi32.dll")]
        public static extern int StretchDIBits(IntPtr hdc, int xDest, int yDest, int wDest, int hDest, int xSrc, int ySrc, int wSrc, int hSrc,
            IntPtr bits, ref BITMAPINFOHEADER bmi, uint usage, uint rop);
        [DllImport("winmm.dll")] public static extern uint timeBeginPeriod(uint ms);
        [DllImport("winmm.dll")] public static extern uint timeEndPeriod(uint ms);
        [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr handle);
    }

    sealed class GameForm : Form
    {
        readonly Game game;
        readonly Stopwatch sw = Stopwatch.StartNew();
        double last;
        bool fullscreen, prevF11, prevM, prevAltEnter, closing;
        Rectangle windowedBounds;

        public GameForm()
        {
            Text = "SON KIŞ";
            BackColor = Color.Black;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.Opaque, true);

            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            int scale = Math.Max(2, Math.Min((wa.Width - 40) / Game.W, (wa.Height - 80) / Game.H));
            if (scale > 5) scale = 5;
            ClientSize = new Size(Game.W * scale, Game.H * scale);

            Art.Init();
            game = new Game(new Audio(true));
            try
            {
                using (var bm = new Bitmap(32, 32, PixelFormat.Format32bppArgb))
                {
                    using (var g = Graphics.FromImage(bm))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                        g.Clear(Color.FromArgb(255, 11, 14, 42));
                        g.DrawImage(Art.Ember.Bmp, new Rectangle(7, 5, 18, 22));
                    }
                    IntPtr h = bm.GetHicon();
                    Icon = (Icon)Icon.FromHandle(h).Clone();
                    Native.DestroyIcon(h);
                }
            }
            catch { }

            Native.timeBeginPeriod(1);
            Application.Idle += OnIdle;
        }

        void OnIdle(object sender, EventArgs e)
        {
            Native.MSG msg;
            while (!closing && !Native.PeekMessage(out msg, IntPtr.Zero, 0, 0, 0)) Tick();
        }

        static bool Key(int vk) { return (Native.GetAsyncKeyState(vk) & 0x8000) != 0; }

        Btn ReadKeys()
        {
            if (Form.ActiveForm != this) return Btn.None;
            bool alt = Key(0x12);
            bool f11 = Key(0x7A), m = Key(0x4D), altEnter = alt && Key(0x0D);
            if ((f11 && !prevF11) || (altEnter && !prevAltEnter)) ToggleFullscreen();
            if (m && !prevM) game.ToggleMute();
            prevF11 = f11; prevM = m; prevAltEnter = altEnter;
            if (alt) return Btn.None;

            Btn b = Btn.None;
            if (Key(0x25) || Key(0x41)) b |= Btn.Left;
            if (Key(0x27) || Key(0x44)) b |= Btn.Right;
            if (Key(0x26) || Key(0x57)) b |= Btn.Up;
            if (Key(0x28) || Key(0x53)) b |= Btn.Down;
            if (Key(0x5A) || Key(0x0D) || Key(0x20)) b |= Btn.A;
            if (Key(0x58) || Key(0x10)) b |= Btn.B;
            if (Key(0x1B)) b |= Btn.Menu;
            return b;
        }

        void Tick()
        {
            double now = sw.Elapsed.TotalSeconds;
            double dt = now - last;
            if (dt < 1.0 / 60.0 - 0.0004) { Thread.Sleep(1); return; }
            last = now;
            if (dt > 0.05) dt = 0.05;

            game.Update((float)dt, ReadKeys());
            if (game.WantFullscreen) { game.WantFullscreen = false; ToggleFullscreen(); }
            if (game.WantQuit) { closing = true; Close(); return; }
            game.Render();
            Present();
        }

        void ToggleFullscreen()
        {
            fullscreen = !fullscreen;
            if (fullscreen)
            {
                windowedBounds = Bounds;
                FormBorderStyle = FormBorderStyle.None;
                Bounds = Screen.FromControl(this).Bounds;
            }
            else
            {
                FormBorderStyle = FormBorderStyle.FixedSingle;
                Bounds = windowedBounds;
            }
            game.Fullscreen = fullscreen;
        }

        void Present()
        {
            if (IsDisposed || !IsHandleCreated || WindowState == FormWindowState.Minimized) return;
            Size cs = ClientSize;
            if (cs.Width < 8 || cs.Height < 8) return;
            int scale = Math.Max(1, Math.Min(cs.Width / Game.W, cs.Height / Game.H));
            int dw = Game.W * scale, dh = Game.H * scale;
            int dx = (cs.Width - dw) / 2, dy = (cs.Height - dh) / 2;

            using (Graphics g = CreateGraphics())
            {
                if (dx > 0 || dy > 0)
                {
                    g.FillRectangle(Brushes.Black, 0, 0, cs.Width, dy);
                    g.FillRectangle(Brushes.Black, 0, dy + dh, cs.Width, cs.Height - dy - dh);
                    g.FillRectangle(Brushes.Black, 0, dy, dx, dh);
                    g.FillRectangle(Brushes.Black, dx + dw, dy, cs.Width - dx - dw, dh);
                }
                BitmapData bd = game.Frame.LockBits(new Rectangle(0, 0, Game.W, Game.H), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
                IntPtr hdc = g.GetHdc();
                try
                {
                    var bi = new Native.BITMAPINFOHEADER
                    {
                        biSize = Marshal.SizeOf(typeof(Native.BITMAPINFOHEADER)),
                        biWidth = Game.W, biHeight = -Game.H, biPlanes = 1, biBitCount = 32
                    };
                    Native.SetStretchBltMode(hdc, 3);
                    Native.StretchDIBits(hdc, dx, dy, dw, dh, 0, 0, Game.W, Game.H, bd.Scan0, ref bi, 0, 0x00CC0020);
                }
                finally
                {
                    g.ReleaseHdc(hdc);
                    game.Frame.UnlockBits(bd);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e) { Present(); }
        protected override void OnPaintBackground(PaintEventArgs e) { }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            closing = true;
            Application.Idle -= OnIdle;
            Native.timeEndPeriod(1);
            game.Shutdown();
            base.OnFormClosed(e);
        }
    }
}
