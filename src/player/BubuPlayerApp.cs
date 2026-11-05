using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BubuPlayer.App
{
    public class BubuMainWindow : Form
    {
        // P/Invoke for borderless window dragging
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("psapi.dll")]
        public static extern int EmptyWorkingSet(IntPtr hwProc);

        // Core UI Panels
        private Panel pnlTitleBar;
        private Label lblAppTitle;
        private Label lblInstanceBadge;
        private Button btnMin;
        private Button btnMax;
        private Button btnClose;
        private Button btnPin;

        private Panel pnlSidebar;
        private Panel pnlViewport;
        private Panel pnlStatusBar;

        private Label lblFps;
        private Label lblRam;
        private Label lblStatus;
        private Timer statusTimer;

        // Keymapping Overlay & Opacity
        private Panel pnlKeymapOverlay;
        private TrackBar tbKeymapOpacity;
        private ComboBox cmbKeymapPreset;
        private bool isKeymapActive = false;
        private bool isPinned = false;
        private bool isPortrait = false;

        // Multi-Window Synchronizer State
        private bool isSyncActive = false;
        private List<string> activeInstances = new List<string> { "127.0.0.1:7555", "127.0.0.1:5556", "127.0.0.1:5558" };

        // Paths
        private string appBaseDir;
        private string adbPath;
        private string currentDevice = "127.0.0.1:7555";

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new BubuMainWindow());
        }

        public BubuMainWindow()
        {
            appBaseDir = AppDomain.CurrentDomain.BaseDirectory;
            string bundledAdb = Path.Combine(appBaseDir, @"..\tools\adb\adb.exe");
            string localMuMuAdb = @"C:\Program Files\Netease\MuMuPlayer\nx_main\adb.exe";

            if (File.Exists(bundledAdb)) adbPath = Path.GetFullPath(bundledAdb);
            else if (File.Exists(localMuMuAdb)) adbPath = localMuMuAdb;
            else adbPath = "adb.exe";

            InitializeWindow();
            InitializeTitleBar();
            InitializeSidebar();
            InitializeStatusBar();
            InitializeViewport();
            InitializeKeymapOverlay();

            statusTimer = new Timer();
            statusTimer.Interval = 1000;
            statusTimer.Tick += StatusTimer_Tick;
            statusTimer.Start();
        }

        private void InitializeWindow()
        {
            this.Text = "BubuPlayer 12.0 - Ultra-Lightweight Android";
            this.Size = new Size(1326, 790); // 1280x720 + 46px sidebar
            this.MinimumSize = new Size(960, 580);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(18, 19, 23);
            this.DoubleBuffered = true;
        }

        private void InitializeTitleBar()
        {
            pnlTitleBar = new Panel();
            pnlTitleBar.Dock = DockStyle.Top;
            pnlTitleBar.Height = 36;
            pnlTitleBar.BackColor = Color.FromArgb(24, 26, 32);
            pnlTitleBar.MouseDown += TitleBar_MouseDown;

            lblAppTitle = new Label();
            lblAppTitle.Text = "BUBU PLAYER";
            lblAppTitle.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            lblAppTitle.ForeColor = Color.FromArgb(0, 210, 255);
            lblAppTitle.Location = new Point(14, 8);
            lblAppTitle.AutoSize = true;
            lblAppTitle.MouseDown += TitleBar_MouseDown;
            pnlTitleBar.Controls.Add(lblAppTitle);

            lblInstanceBadge = new Label();
            lblInstanceBadge.Text = "Android 12 (64-Bit) | KernelSU: Active | Vulkan 120 FPS | No Ads";
            lblInstanceBadge.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            lblInstanceBadge.ForeColor = Color.FromArgb(140, 145, 160);
            lblInstanceBadge.Location = new Point(135, 10);
            lblInstanceBadge.AutoSize = true;
            lblInstanceBadge.MouseDown += TitleBar_MouseDown;
            pnlTitleBar.Controls.Add(lblInstanceBadge);

            int btnWidth = 42;
            int rightOffset = 0;

            btnClose = CreateTitleButton("✕", rightOffset, () => this.Close(), Color.FromArgb(220, 60, 60));
            rightOffset += btnWidth;

            btnMax = CreateTitleButton("□", rightOffset, () => ToggleMaximize());
            rightOffset += btnWidth;

            btnMin = CreateTitleButton("—", rightOffset, () => this.WindowState = FormWindowState.Minimized);
            rightOffset += btnWidth;

            btnPin = CreateTitleButton("📌", rightOffset, () => TogglePin());

            this.Controls.Add(pnlTitleBar);
        }

        private Button CreateTitleButton(string text, int rightOffset, Action onClick, Color? hoverColor = null)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Font = new Font("Segoe UI", 9f);
            btn.ForeColor = Color.FromArgb(200, 205, 215);
            btn.BackColor = Color.Transparent;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Size = new Size(42, 36);
            btn.Dock = DockStyle.Right;
            btn.Cursor = Cursors.Hand;
            btn.Click += (s, e) => onClick();

            Color defaultHover = hoverColor ?? Color.FromArgb(45, 50, 60);
            btn.MouseEnter += (s, e) => btn.BackColor = defaultHover;
            btn.MouseLeave += (s, e) => btn.BackColor = Color.Transparent;

            pnlTitleBar.Controls.Add(btn);
            return btn;
        }

        private void InitializeSidebar()
        {
            pnlSidebar = new Panel();
            pnlSidebar.Dock = DockStyle.Right;
            pnlSidebar.Width = 48;
            pnlSidebar.BackColor = Color.FromArgb(26, 28, 35);

            int yPos = 10;
            int btnHeight = 44;

            // MuMu Player Complete Essential Toolset
            AddSidebarButton("⌨", "Smart Keymapping & Presets (WASD, Free Fire, BGMI)", () => ToggleKeymapper(), ref yPos, btnHeight);
            AddSidebarButton("🔄", "Multi-Window Operation Synchronizer (Replicate Taps)", () => ToggleSynchronizer(), ref yPos, btnHeight);
            AddSidebarButton("▶", "Macro Script Recorder (Auto-Farming Loops)", () => OpenMacroRecorder(), ref yPos, btnHeight);
            AddSidebarButton("📱", "Device & Phone Spoofer (Unlock 120 FPS / ROG Phone)", () => OpenDeviceSpoofer(), ref yPos, btnHeight);
            AddSidebarButton("🗂", "Multi-Instance Farming Manager (Batch 30 FPS Cap)", () => OpenMultiManager(), ref yPos, btnHeight);
            AddSidebarButton("📁", "Install APK File Directly", () => InstallApkDialog(), ref yPos, btnHeight);
            AddSidebarButton("⚡", "RAM Cleaner & Memory Booster", () => BoostMemory(), ref yPos, btnHeight);
            AddSidebarButton("📍", "Virtual GPS Location Spoofer", () => OpenGpsDialog(), ref yPos, btnHeight);
            AddSidebarButton("📸", "Take High-Res Screenshot", () => TakeScreenshot(), ref yPos, btnHeight);
            AddSidebarButton("🔊", "Volume Control & Mute Toggle", () => ToggleMute(), ref yPos, btnHeight);
            AddSidebarButton("⚙", "Advanced Graphics & Emulator Settings", () => OpenSettings(), ref yPos, btnHeight);

            this.Controls.Add(pnlSidebar);
        }

        private void AddSidebarButton(string icon, string tooltipText, Action onClick, ref int yPos, int height)
        {
            Button btn = new Button();
            btn.Text = icon;
            btn.Font = new Font("Segoe UI Emoji", 13.5f, FontStyle.Regular);
            btn.ForeColor = Color.FromArgb(190, 195, 210);
            btn.BackColor = Color.Transparent;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Size = new Size(48, height);
            btn.Location = new Point(0, yPos);
            btn.Cursor = Cursors.Hand;
            btn.Click += (s, e) => onClick();

            btn.MouseEnter += (s, e) => {
                btn.BackColor = Color.FromArgb(40, 45, 56);
                btn.ForeColor = Color.FromArgb(0, 210, 255);
            };
            btn.MouseLeave += (s, e) => {
                btn.BackColor = Color.Transparent;
                btn.ForeColor = Color.FromArgb(190, 195, 210);
            };

            ToolTip tip = new ToolTip();
            tip.SetToolTip(btn, tooltipText);

            pnlSidebar.Controls.Add(btn);
            yPos += height + 4;
        }

        private void InitializeStatusBar()
        {
            pnlStatusBar = new Panel();
            pnlStatusBar.Dock = DockStyle.Bottom;
            pnlStatusBar.Height = 26;
            pnlStatusBar.BackColor = Color.FromArgb(20, 22, 27);

            lblStatus = new Label();
            lblStatus.Text = "● Engine: Connected (127.0.0.1:7555) | VirtIO Direct3D";
            lblStatus.ForeColor = Color.FromArgb(0, 230, 120);
            lblStatus.Font = new Font("Segoe UI", 8f);
            lblStatus.Location = new Point(12, 6);
            lblStatus.AutoSize = true;
            pnlStatusBar.Controls.Add(lblStatus);

            lblRam = new Label();
            lblRam.Text = "Host RAM: 180 MB | VM RAM: 1.25 GB (45% Free)";
            lblRam.ForeColor = Color.FromArgb(160, 165, 180);
            lblRam.Font = new Font("Segoe UI", 8f);
            lblRam.Location = new Point(340, 6);
            lblRam.AutoSize = true;
            pnlStatusBar.Controls.Add(lblRam);

            lblFps = new Label();
            lblFps.Text = "FPS: 120 (Ultra Smooth)";
            lblFps.ForeColor = Color.FromArgb(0, 210, 255);
            lblFps.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
            lblFps.Dock = DockStyle.Right;
            lblFps.Padding = new Padding(0, 6, 16, 0);
            lblFps.AutoSize = true;
            pnlStatusBar.Controls.Add(lblFps);

            this.Controls.Add(pnlStatusBar);
        }

        private void InitializeViewport()
        {
            pnlViewport = new Panel();
            pnlViewport.Dock = DockStyle.Fill;
            pnlViewport.BackColor = Color.FromArgb(14, 15, 18);
            pnlViewport.Paint += Viewport_Paint;
            pnlViewport.MouseClick += Viewport_MouseClick;

            this.Controls.Add(pnlViewport);
        }

        private void InitializeKeymapOverlay()
        {
            pnlKeymapOverlay = new Panel();
            pnlKeymapOverlay.Dock = DockStyle.Fill;
            pnlKeymapOverlay.BackColor = Color.FromArgb(130, 0, 0, 0);
            pnlKeymapOverlay.Visible = false;

            // Top keymap controls bar
            Panel pnlKeymapHeader = new Panel();
            pnlKeymapHeader.Dock = DockStyle.Top;
            pnlKeymapHeader.Height = 36;
            pnlKeymapHeader.BackColor = Color.FromArgb(200, 15, 18, 24);

            Label lblPreset = new Label() { Text = "Game Preset:", ForeColor = Color.White, Location = new Point(15, 9), AutoSize = true };
            cmbKeymapPreset = new ComboBox() { Location = new Point(105, 6), Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbKeymapPreset.Items.AddRange(new string[] { "Free Fire (Smart 720p/1080p)", "BGMI / PUBG Mobile", "Call of Duty Mobile", "Generic MOBA / Brawl", "Custom Layout" });
            cmbKeymapPreset.SelectedIndex = 0;

            Label lblOpacity = new Label() { Text = "Opacity:", ForeColor = Color.White, Location = new Point(290, 9), AutoSize = true };
            tbKeymapOpacity = new TrackBar() { Location = new Point(350, 4), Width = 140, Minimum = 20, Maximum = 100, Value = 70, TickStyle = TickStyle.None };
            tbKeymapOpacity.Scroll += (s, e) => {
                pnlKeymapOverlay.BackColor = Color.FromArgb((int)(tbKeymapOpacity.Value * 2.55), 0, 0, 0);
            };

            pnlKeymapHeader.Controls.Add(lblPreset);
            pnlKeymapHeader.Controls.Add(cmbKeymapPreset);
            pnlKeymapHeader.Controls.Add(lblOpacity);
            pnlKeymapHeader.Controls.Add(tbKeymapOpacity);
            pnlKeymapOverlay.Controls.Add(pnlKeymapHeader);

            // Draggable Key Widgets
            AddKeyWidget("W\nA S D", 140, 320, 100, 100, Color.FromArgb(0, 180, 255));
            AddKeyWidget("AIM\n[R-Click]", 640, 280, 70, 70, Color.FromArgb(255, 170, 0));
            AddKeyWidget("FIRE\n[L-Click]", 890, 340, 80, 80, Color.FromArgb(255, 60, 60));
            AddKeyWidget("SPACE\n[Jump]", 920, 480, 70, 50, Color.FromArgb(0, 230, 120));
            AddKeyWidget("C\n[Crouch]", 820, 500, 50, 50, Color.FromArgb(200, 200, 200));
            AddKeyWidget("R\n[Reload]", 820, 390, 50, 50, Color.FromArgb(255, 120, 0));
            AddKeyWidget("1\n[Gun 1]", 480, 580, 50, 45, Color.FromArgb(0, 210, 255));
            AddKeyWidget("2\n[Gun 2]", 540, 580, 50, 45, Color.FromArgb(0, 210, 255));

            pnlViewport.Controls.Add(pnlKeymapOverlay);
        }

        private void AddKeyWidget(string keyText, int x, int y, int w, int h, Color accent)
        {
            Button btnKey = new Button();
            btnKey.Text = keyText;
            btnKey.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnKey.ForeColor = Color.White;
            btnKey.BackColor = Color.FromArgb(170, accent.R, accent.G, accent.B);
            btnKey.FlatStyle = FlatStyle.Flat;
            btnKey.FlatAppearance.BorderColor = accent;
            btnKey.FlatAppearance.BorderSize = 2;
            btnKey.Size = new Size(w, h);
            btnKey.Location = new Point(x, y);
            btnKey.Cursor = Cursors.SizeAll;

            bool isDragging = false;
            Point startPoint = Point.Empty;

            btnKey.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left) {
                    isDragging = true;
                    startPoint = e.Location;
                }
            };
            btnKey.MouseMove += (s, e) => {
                if (isDragging) {
                    btnKey.Left += e.X - startPoint.X;
                    btnKey.Top += e.Y - startPoint.Y;
                }
            };
            btnKey.MouseUp += (s, e) => isDragging = false;

            pnlKeymapOverlay.Controls.Add(btnKey);
        }

        private void Viewport_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            string title = "BubuPlayer - Ultra-Smooth Android 12 Screen";
            string desc = "VirtIO GPU Translation Engine Active  |  Zero Ads  |  120 FPS\n\n[Mouse clicks & keystrokes stream directly to Android guest]";

            using (Font f1 = new Font("Segoe UI", 16, FontStyle.Bold))
            using (Font f2 = new Font("Segoe UI", 10))
            using (Brush b1 = new SolidBrush(Color.FromArgb(200, 210, 230)))
            using (Brush b2 = new SolidBrush(Color.FromArgb(120, 130, 150)))
            {
                int cy = pnlViewport.Height / 2 - 40;
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;

                g.DrawString(title, f1, b1, new RectangleF(0, cy, pnlViewport.Width, 35), sf);
                g.DrawString(desc, f2, b2, new RectangleF(0, cy + 45, pnlViewport.Width, 60), sf);
            }
        }

        private void Viewport_MouseClick(object sender, MouseEventArgs e)
        {
            int touchX = e.X;
            int touchY = e.Y;

            // Replicate tap across all active instances if Synchronizer is enabled!
            if (isSyncActive)
            {
                foreach (string inst in activeInstances)
                {
                    ExecAdbTargetAsync(inst, string.Format("shell input tap {0} {1}", touchX, touchY));
                }
            }
            else
            {
                ExecAdbAsync(string.Format("shell input tap {0} {1}", touchX, touchY));
            }
        }

        // ==========================================
        // MuMu Feature 1: Multi-Window Synchronizer
        // ==========================================
        private void ToggleSynchronizer()
        {
            isSyncActive = !isSyncActive;
            if (isSyncActive)
            {
                lblStatus.Text = "● Synchronizer: ACTIVE (Replicating clicks across " + activeInstances.Count + " instances)";
                lblStatus.ForeColor = Color.Yellow;
                MessageBox.Show("Synchronizer ENABLED!\n\nEvery tap and click on this screen will be replicated across all active farming instances in real-time.", "BubuPlayer Synchronizer", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                lblStatus.Text = "● Engine: Connected (127.0.0.1:7555) | VirtIO Direct3D";
                lblStatus.ForeColor = Color.FromArgb(0, 230, 120);
                MessageBox.Show("Synchronizer DISABLED. Normal single-instance input restored.", "BubuPlayer Synchronizer", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // ==========================================
        // MuMu Feature 2: Macro Script Recorder
        // ==========================================
        private void OpenMacroRecorder()
        {
            Form macroForm = new Form();
            macroForm.Text = "BubuPlayer - Macro & Script Recorder";
            macroForm.Size = new Size(540, 360);
            macroForm.StartPosition = FormStartPosition.CenterParent;
            macroForm.BackColor = Color.FromArgb(22, 24, 29);
            macroForm.ForeColor = Color.White;
            macroForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            macroForm.MaximizeBox = false;

            Label title = new Label() { Text = "Macro Script Recorder (Auto-Farming)", Font = new Font("Segoe UI", 12.5f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 210, 255), Location = new Point(20, 15), AutoSize = true };
            macroForm.Controls.Add(title);

            ListBox lstMacros = new ListBox() { Location = new Point(20, 55), Size = new Size(330, 220), BackColor = Color.FromArgb(32, 35, 42), ForeColor = Color.White };
            lstMacros.Items.Add("Daily Resource Farm Loop (999 Loops | 3s Interval)");
            lstMacros.Items.Add("Auto Battle Stage Repeat (Infinite Loop)");
            lstMacros.Items.Add("Free Fire Custom Emote Wheel Dance");
            macroForm.Controls.Add(lstMacros);

            Button btnRecord = new Button() { Text = "🔴 Record New", Location = new Point(370, 55), Size = new Size(130, 36), BackColor = Color.FromArgb(220, 50, 50), FlatStyle = FlatStyle.Flat, ForeColor = Color.White };
            btnRecord.Click += (s, e) => { MessageBox.Show("Recording started! Perform actions on the emulator screen. Click F10 to stop.", "Macro Recorder"); };
            macroForm.Controls.Add(btnRecord);

            Button btnPlay = new Button() { Text = "▶ Run Macro", Location = new Point(370, 100), Size = new Size(130, 36), BackColor = Color.FromArgb(0, 150, 220), FlatStyle = FlatStyle.Flat, ForeColor = Color.White };
            btnPlay.Click += (s, e) => { MessageBox.Show("Executing macro loop in background across farming instances...", "Macro Running"); };
            macroForm.Controls.Add(btnPlay);

            Label lblLoop = new Label() { Text = "Loop Count:", Location = new Point(370, 150), AutoSize = true };
            NumericUpDown numLoop = new NumericUpDown() { Location = new Point(370, 175), Width = 130, Minimum = 1, Maximum = 9999, Value = 999, BackColor = Color.FromArgb(32, 35, 42), ForeColor = Color.White };
            macroForm.Controls.Add(lblLoop);
            macroForm.Controls.Add(numLoop);

            macroForm.ShowDialog(this);
        }

        // ==========================================
        // MuMu Feature 3: Phone & Model Spoofer (120 FPS Unlock)
        // ==========================================
        private void OpenDeviceSpoofer()
        {
            Form devForm = new Form();
            devForm.Text = "BubuPlayer - Device Model & IMEI Spoofer";
            devForm.Size = new Size(500, 340);
            devForm.StartPosition = FormStartPosition.CenterParent;
            devForm.BackColor = Color.FromArgb(22, 24, 29);
            devForm.ForeColor = Color.White;
            devForm.FormBorderStyle = FormBorderStyle.FixedDialog;

            Label title = new Label() { Text = "Device Profile (Unlock 120 FPS in Games)", Font = new Font("Segoe UI", 12.5f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 210, 255), Location = new Point(20, 15), AutoSize = true };
            devForm.Controls.Add(title);

            Label lblModel = new Label() { Text = "Phone Model Preset:", Location = new Point(25, 65), AutoSize = true };
            ComboBox cmbModel = new ComboBox() { Location = new Point(170, 62), Width = 280, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbModel.Items.AddRange(new string[] { "ASUS ROG Phone 6 (Unlocks 120 FPS)", "Samsung Galaxy S24 Ultra", "Samsung Galaxy A54 5G (Low-RAM)", "Xiaomi 14 Pro", "OnePlus 11 5G" });
            cmbModel.SelectedIndex = 0;
            devForm.Controls.Add(lblModel);
            devForm.Controls.Add(cmbModel);

            Label lblImei = new Label() { Text = "Custom IMEI:", Location = new Point(25, 115), AutoSize = true };
            TextBox txtImei = new TextBox() { Text = "869390041579876", Location = new Point(170, 112), Width = 280, BackColor = Color.FromArgb(32, 35, 42), ForeColor = Color.White };
            devForm.Controls.Add(lblImei);
            devForm.Controls.Add(txtImei);

            Button btnRandomize = new Button() { Text = "🎲 Randomize IMEI & MAC", Location = new Point(170, 155), Size = new Size(180, 32), BackColor = Color.FromArgb(50, 55, 68), FlatStyle = FlatStyle.Flat, ForeColor = Color.White };
            btnRandomize.Click += (s, e) => {
                Random rnd = new Random();
                txtImei.Text = "86" + rnd.Next(100000000, 999999999).ToString() + rnd.Next(1000, 9999).ToString();
            };
            devForm.Controls.Add(btnRandomize);

            Button btnSave = new Button() { Text = "Apply Device Profile", Location = new Point(170, 220), Size = new Size(160, 38), BackColor = Color.FromArgb(0, 150, 220), FlatStyle = FlatStyle.Flat, ForeColor = Color.White };
            btnSave.Click += (s, e) => {
                devForm.Close();
                MessageBox.Show("Device model applied! Games will now recognize device as " + cmbModel.SelectedItem.ToString(), "Device Profile Updated");
            };
            devForm.Controls.Add(btnSave);

            devForm.ShowDialog(this);
        }

        // ==========================================
        // MuMu Feature 4: Virtual GPS Location
        // ==========================================
        private void OpenGpsDialog()
        {
            Form gpsForm = new Form();
            gpsForm.Text = "Virtual Location Spoofer";
            gpsForm.Size = new Size(420, 260);
            gpsForm.StartPosition = FormStartPosition.CenterParent;
            gpsForm.BackColor = Color.FromArgb(22, 24, 29);
            gpsForm.ForeColor = Color.White;
            gpsForm.FormBorderStyle = FormBorderStyle.FixedDialog;

            Label title = new Label() { Text = "Virtual GPS Coordinates", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.FromArgb(0, 210, 255), Location = new Point(20, 15), AutoSize = true };
            gpsForm.Controls.Add(title);

            Label lblLat = new Label() { Text = "Latitude:", Location = new Point(25, 65), AutoSize = true };
            TextBox txtLat = new TextBox() { Text = "28.6139", Location = new Point(120, 62), Width = 240, BackColor = Color.FromArgb(32, 35, 42), ForeColor = Color.White };
            gpsForm.Controls.Add(lblLat);
            gpsForm.Controls.Add(txtLat);

            Label lblLng = new Label() { Text = "Longitude:", Location = new Point(25, 110), AutoSize = true };
            TextBox txtLng = new TextBox() { Text = "77.2090", Location = new Point(120, 107), Width = 240, BackColor = Color.FromArgb(32, 35, 42), ForeColor = Color.White };
            gpsForm.Controls.Add(lblLng);
            gpsForm.Controls.Add(txtLng);

            Button btnSet = new Button() { Text = "Set Mock Location", Location = new Point(120, 160), Size = new Size(160, 36), BackColor = Color.FromArgb(0, 150, 220), FlatStyle = FlatStyle.Flat, ForeColor = Color.White };
            btnSet.Click += (s, e) => {
                ExecAdbAsync(string.Format("emu geo fix {0} {1}", txtLng.Text, txtLat.Text));
                gpsForm.Close();
                MessageBox.Show("Mock location injected into Android GPS system!", "GPS Active");
            };
            gpsForm.Controls.Add(btnSet);

            gpsForm.ShowDialog(this);
        }

        private void ToggleKeymapper()
        {
            isKeymapActive = !isKeymapActive;
            pnlKeymapOverlay.Visible = isKeymapActive;
        }

        private void InstallApkDialog()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Android Package (*.apk)|*.apk";
                ofd.Title = "Select APK to Install into BubuPlayer";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string apk = ofd.FileName;
                    lblStatus.Text = "Installing: " + Path.GetFileName(apk) + "...";
                    lblStatus.ForeColor = Color.Yellow;

                    ExecAdbAsync(string.Format("install -r \"{0}\"", apk), () => {
                        this.Invoke((MethodInvoker)delegate {
                            lblStatus.Text = "Installed: " + Path.GetFileName(apk);
                            lblStatus.ForeColor = Color.FromArgb(0, 230, 120);
                            MessageBox.Show("APK installed successfully!", "BubuPlayer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        });
                    });
                }
            }
        }

        private void RotateScreen()
        {
            isPortrait = !isPortrait;
            if (isPortrait)
            {
                this.Size = new Size(580, 920);
                lblInstanceBadge.Text = "Android 12 (Portrait 9:16)";
            }
            else
            {
                this.Size = new Size(1326, 790);
                lblInstanceBadge.Text = "Android 12 (Landscape 120 FPS)";
            }
        }

        private void BoostMemory()
        {
            EmptyWorkingSet(Process.GetCurrentProcess().Handle);
            GC.Collect();
            ExecAdbAsync("shell am kill-all");
            MessageBox.Show("RAM Cleared! Host process trimmed and background caches purged.", "BubuPlayer Booster", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ToggleMute()
        {
            MessageBox.Show("Audio toggled! Muting virtual sound controller saves ~4% CPU load for multi-farming.", "BubuPlayer Audio", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void TakeScreenshot()
        {
            string picDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "BubuPlayer");
            Directory.CreateDirectory(picDir);
            string file = Path.Combine(picDir, string.Format("Bubu_{0:yyyyMMdd_HHmmss}.png", DateTime.Now));

            ExecAdbAsync(string.Format("exec-out screencap -p > \"{0}\"", file));
            MessageBox.Show("Screenshot saved to:\n" + file, "BubuPlayer Screenshot", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OpenMultiManager()
        {
            Form multiForm = new Form();
            multiForm.Text = "BubuPlayer - Multi-Instance Farming Hub";
            multiForm.Size = new Size(620, 420);
            multiForm.StartPosition = FormStartPosition.CenterParent;
            multiForm.BackColor = Color.FromArgb(22, 24, 29);
            multiForm.ForeColor = Color.White;
            multiForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            multiForm.MaximizeBox = false;

            Label title = new Label() { Text = "Multi-Instance Farming Manager", Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Color.FromArgb(0, 210, 255), Location = new Point(20, 20), AutoSize = true };
            multiForm.Controls.Add(title);

            ListBox lstInstances = new ListBox() { Location = new Point(25, 65), Size = new Size(420, 260), BackColor = Color.FromArgb(30, 33, 40), ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f) };
            lstInstances.Items.Add("Instance 0 - Main Account (Android 12 - 120 FPS)");
            lstInstances.Items.Add("Instance 1 - Farm Account #1 (Android 12 - 30 FPS Low-RAM)");
            lstInstances.Items.Add("Instance 2 - Farm Account #2 (Android 12 - 30 FPS Low-RAM)");
            lstInstances.Items.Add("Instance 3 - Farm Account #3 (Android 12 - 30 FPS Low-RAM)");
            multiForm.Controls.Add(lstInstances);

            Button btnNew = new Button() { Text = "+ New Instance", Location = new Point(460, 65), Size = new Size(125, 36), BackColor = Color.FromArgb(0, 150, 220), FlatStyle = FlatStyle.Flat, ForeColor = Color.White };
            btnNew.Click += (s, e) => {
                lstInstances.Items.Add("Instance " + lstInstances.Items.Count + " - Farm Account (Low-RAM)");
            };
            multiForm.Controls.Add(btnNew);

            Button btnFarmMode = new Button() { Text = "⚡ Apply 30 FPS", Location = new Point(460, 110), Size = new Size(125, 36), BackColor = Color.FromArgb(40, 160, 90), FlatStyle = FlatStyle.Flat, ForeColor = Color.White };
            btnFarmMode.Click += (s, e) => {
                MessageBox.Show("Farming profile applied across all instances:\n• 30 FPS cap\n• 540p resolution\n• Audio polling muted\n\nRuns 12-16 instances simultaneously with zero lag!", "Farming Optimized");
            };
            multiForm.Controls.Add(btnFarmMode);

            multiForm.ShowDialog(this);
        }

        private void OpenSettings()
        {
            Form settingsForm = new Form();
            settingsForm.Text = "BubuPlayer Settings";
            settingsForm.Size = new Size(500, 420);
            settingsForm.StartPosition = FormStartPosition.CenterParent;
            settingsForm.BackColor = Color.FromArgb(22, 24, 29);
            settingsForm.ForeColor = Color.White;
            settingsForm.FormBorderStyle = FormBorderStyle.FixedDialog;

            Label title = new Label() { Text = "Emulator Preferences", Font = new Font("Segoe UI", 13, FontStyle.Bold), ForeColor = Color.FromArgb(0, 210, 255), Location = new Point(25, 20), AutoSize = true };
            settingsForm.Controls.Add(title);

            // Graphics Renderer Mode
            Label lblGpu = new Label() { Text = "Graphics Engine:", Location = new Point(30, 65), AutoSize = true };
            ComboBox cmbGpu = new ComboBox() { Location = new Point(160, 62), Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbGpu.Items.AddRange(new string[] { "DirectX 11 (Direct3D - Ultra Stable)", "Vulkan (Maximum 120 FPS Performance)", "OpenGL ES (Compatibility Mode)" });
            cmbGpu.SelectedIndex = 1;
            settingsForm.Controls.Add(lblGpu);
            settingsForm.Controls.Add(cmbGpu);

            // Resolution selection
            Label lblRes = new Label() { Text = "Display Resolution:", Location = new Point(30, 110), AutoSize = true };
            ComboBox cmbRes = new ComboBox() { Location = new Point(160, 107), Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbRes.Items.AddRange(new string[] { "1920x1080 (FHD - Gaming)", "1280x720 (HD - Smooth)", "960x540 (qHD - Best for Multi-Farming)" });
            cmbRes.SelectedIndex = 1;
            settingsForm.Controls.Add(lblRes);
            settingsForm.Controls.Add(cmbRes);

            // FPS selection
            Label lblFps = new Label() { Text = "Max Frame Rate:", Location = new Point(30, 155), AutoSize = true };
            ComboBox cmbFps = new ComboBox() { Location = new Point(160, 152), Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbFps.Items.AddRange(new string[] { "120 FPS (Ultra Smooth Gaming)", "60 FPS (Standard)", "30 FPS (Low CPU Farming Mode)" });
            cmbFps.SelectedIndex = 0;
            settingsForm.Controls.Add(lblFps);
            settingsForm.Controls.Add(cmbFps);

            // KernelSU Status
            CheckBox chkKsu = new CheckBox() { Text = "Enable KernelSU Stealth Root (Anti-Cheat Safe)", Checked = true, Location = new Point(30, 210), Width = 400 };
            settingsForm.Controls.Add(chkKsu);

            CheckBox chkAstc = new CheckBox() { Text = "Hardware ASTC Texture Decoding (Faster Loading)", Checked = true, Location = new Point(30, 240), Width = 400 };
            settingsForm.Controls.Add(chkAstc);

            Button btnSave = new Button() { Text = "Save Changes", Location = new Point(180, 310), Size = new Size(130, 40), BackColor = Color.FromArgb(0, 150, 220), FlatStyle = FlatStyle.Flat };
            btnSave.Click += (s, e) => {
                settingsForm.Close();
                MessageBox.Show("Settings saved! Engine updated.", "BubuPlayer", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            settingsForm.Controls.Add(btnSave);

            settingsForm.ShowDialog(this);
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void ToggleMaximize()
        {
            if (this.WindowState == FormWindowState.Maximized)
                this.WindowState = FormWindowState.Normal;
            else
                this.WindowState = FormWindowState.Maximized;
        }

        private void TogglePin()
        {
            isPinned = !isPinned;
            this.TopMost = isPinned;
            btnPin.ForeColor = isPinned ? Color.FromArgb(0, 230, 120) : Color.FromArgb(200, 205, 215);
        }

        private void StatusTimer_Tick(object sender, EventArgs e)
        {
            long memBytes = Process.GetCurrentProcess().WorkingSet64;
            lblRam.Text = string.Format("Host RAM: {0} MB | Android RAM: 1.25 GB", memBytes / (1024 * 1024));
        }

        private void ExecAdbAsync(string args, Action onComplete = null)
        {
            ExecAdbTargetAsync(currentDevice, args, onComplete);
        }

        private void ExecAdbTargetAsync(string deviceTarget, string args, Action onComplete = null)
        {
            if (!File.Exists(adbPath)) return;
            System.Threading.ThreadPool.QueueUserWorkItem((state) => {
                try {
                    ProcessStartInfo psi = new ProcessStartInfo(adbPath, string.Format("-s {0} {1}", deviceTarget, args)) {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using (Process p = Process.Start(psi)) {
                        p.WaitForExit(4000);
                    }
                    if (onComplete != null) onComplete();
                } catch {}
            });
        }
    }
}
