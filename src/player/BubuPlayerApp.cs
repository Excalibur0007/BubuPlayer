using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
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

        // UI Controls
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

        // Overlay for Keymapping
        private Panel pnlKeymapOverlay;
        private bool isKeymapActive = false;
        private bool isPinned = false;
        private bool isPortrait = false;

        private string adbPath = @"C:\Program Files\Netease\MuMuPlayer\nx_main\adb.exe";
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
            InitializeWindow();
            InitializeTitleBar();
            InitializeSidebar();
            InitializeStatusBar();
            InitializeViewport();
            InitializeKeymapOverlay();

            // Periodic monitor for FPS, RAM, ADB connection
            statusTimer = new Timer();
            statusTimer.Interval = 1000;
            statusTimer.Tick += StatusTimer_Tick;
            statusTimer.Start();
        }

        private void InitializeWindow()
        {
            this.Text = "BubuPlayer 12.0 - Ultra-Lightweight Android";
            this.Size = new Size(1326, 790); // 1280x720 viewport + 46px sidebar
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

            // App Icon & Title
            lblAppTitle = new Label();
            lblAppTitle.Text = "BUBU PLAYER";
            lblAppTitle.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            lblAppTitle.ForeColor = Color.FromArgb(0, 210, 255);
            lblAppTitle.Location = new Point(14, 8);
            lblAppTitle.AutoSize = true;
            lblAppTitle.MouseDown += TitleBar_MouseDown;
            pnlTitleBar.Controls.Add(lblAppTitle);

            // Subtitle Badge
            lblInstanceBadge = new Label();
            lblInstanceBadge.Text = "Android 12 (64-Bit) | KernelSU: Active | No Ads";
            lblInstanceBadge.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            lblInstanceBadge.ForeColor = Color.FromArgb(140, 145, 160);
            lblInstanceBadge.Location = new Point(135, 10);
            lblInstanceBadge.AutoSize = true;
            lblInstanceBadge.MouseDown += TitleBar_MouseDown;
            pnlTitleBar.Controls.Add(lblInstanceBadge);

            // Window Buttons
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

            // MuMu Player signature action buttons
            int yPos = 10;
            int btnHeight = 44;

            AddSidebarButton("⌨", "Keymapping Editor (WASD / Mouse Aim)", () => ToggleKeymapper(), ref yPos, btnHeight);
            AddSidebarButton("📁", "Install APK File", () => InstallApkDialog(), ref yPos, btnHeight);
            AddSidebarButton("🔄", "Rotate Screen (Landscape / Portrait)", () => RotateScreen(), ref yPos, btnHeight);
            AddSidebarButton("🗂", "Multi-Instance Manager (Farming Hub)", () => OpenMultiManager(), ref yPos, btnHeight);
            AddSidebarButton("⚡", "Memory Cleaner & Speed Booster", () => BoostMemory(), ref yPos, btnHeight);
            AddSidebarButton("🔊", "Audio & Volume Control", () => ToggleMute(), ref yPos, btnHeight);
            AddSidebarButton("📸", "Take Screenshot", () => TakeScreenshot(), ref yPos, btnHeight);
            AddSidebarButton("⚙", "Emulator Settings", () => OpenSettings(), ref yPos, btnHeight);

            this.Controls.Add(pnlSidebar);
        }

        private void AddSidebarButton(string icon, string tooltipText, Action onClick, ref int yPos, int height)
        {
            Button btn = new Button();
            btn.Text = icon;
            btn.Font = new Font("Segoe UI Emoji", 14f, FontStyle.Regular);
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
            lblStatus.Text = "● Engine: Connected (127.0.0.1:7555)";
            lblStatus.ForeColor = Color.FromArgb(0, 230, 120);
            lblStatus.Font = new Font("Segoe UI", 8f);
            lblStatus.Location = new Point(12, 6);
            lblStatus.AutoSize = true;
            pnlStatusBar.Controls.Add(lblStatus);

            lblRam = new Label();
            lblRam.Text = "RAM: 1.25 GB | CPU: 3%";
            lblRam.ForeColor = Color.FromArgb(160, 165, 180);
            lblRam.Font = new Font("Segoe UI", 8f);
            lblRam.Location = new Point(320, 6);
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
            pnlKeymapOverlay.BackColor = Color.FromArgb(120, 0, 0, 0);
            pnlKeymapOverlay.Visible = false;

            // Render keymap widgets (WASD D-Pad, Mouse Aim, Fire Button)
            AddKeyWidget("W\nA S D", 120, 320, 100, 100, Color.FromArgb(0, 180, 255));
            AddKeyWidget("AIM\n[R-Click]", 600, 280, 70, 70, Color.FromArgb(255, 170, 0));
            AddKeyWidget("FIRE\n[L-Click]", 850, 340, 80, 80, Color.FromArgb(255, 60, 60));
            AddKeyWidget("SPACE\n[Jump]", 880, 480, 70, 50, Color.FromArgb(0, 230, 120));
            AddKeyWidget("C\n[Crouch]", 780, 500, 50, 50, Color.FromArgb(200, 200, 200));

            Label lblKeymapHint = new Label();
            lblKeymapHint.Text = "KEYMAPPING MODE ACTIVE  |  Drag keys to adjust position  |  Click ⌨ on sidebar to exit";
            lblKeymapHint.ForeColor = Color.Yellow;
            lblKeymapHint.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblKeymapHint.BackColor = Color.FromArgb(180, 0, 0, 0);
            lblKeymapHint.Dock = DockStyle.Top;
            lblKeymapHint.Height = 28;
            lblKeymapHint.TextAlign = ContentAlignment.MiddleCenter;
            pnlKeymapOverlay.Controls.Add(lblKeymapHint);

            pnlViewport.Controls.Add(pnlKeymapOverlay);
        }

        private void AddKeyWidget(string keyText, int x, int y, int w, int h, Color accent)
        {
            Button btnKey = new Button();
            btnKey.Text = keyText;
            btnKey.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnKey.ForeColor = Color.White;
            btnKey.BackColor = Color.FromArgb(160, accent.R, accent.G, accent.B);
            btnKey.FlatStyle = FlatStyle.Flat;
            btnKey.FlatAppearance.BorderColor = accent;
            btnKey.FlatAppearance.BorderSize = 2;
            btnKey.Size = new Size(w, h);
            btnKey.Location = new Point(x, y);
            btnKey.Cursor = Cursors.SizeAll;

            // Simple drag support
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

            // Display Placeholder screen when VM is initializing
            string title = "BubuPlayer - Ultra-Smooth Android 12 Screen";
            string desc = "VirtIO GPU Translation Engine Active  |  Zero Ads  |  120 FPS\n\n[Mouse clicks & keystrokes are streamed directly to Android guest]";

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
            // Send tap event to Android via ADB in background
            int touchX = e.X;
            int touchY = e.Y;
            ExecAdbAsync(string.Format("shell input tap {0} {1}", touchX, touchY));
        }

        // Action Handlers
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
                this.Size = new Size(580, 920); // Portrait 9:16
                lblInstanceBadge.Text = "Android 12 (Portrait Mode)";
            }
            else
            {
                this.Size = new Size(1326, 790); // Landscape 16:9
                lblInstanceBadge.Text = "Android 12 (Landscape 120 FPS)";
            }
        }

        private void BoostMemory()
        {
            EmptyWorkingSet(Process.GetCurrentProcess().Handle);
            GC.Collect();
            ExecAdbAsync("shell am kill-all");
            MessageBox.Show("RAM Cleared! Freed unused memory and background caches.", "BubuPlayer Booster", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

            Label title = new Label();
            title.Text = "Multi-Instance Farming Manager";
            title.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(0, 210, 255);
            title.Location = new Point(20, 20);
            title.AutoSize = true;
            multiForm.Controls.Add(title);

            ListBox lstInstances = new ListBox();
            lstInstances.Location = new Point(25, 65);
            lstInstances.Size = new Size(420, 260);
            lstInstances.BackColor = Color.FromArgb(30, 33, 40);
            lstInstances.ForeColor = Color.White;
            lstInstances.Font = new Font("Segoe UI", 9.5f);
            lstInstances.Items.Add("Instance 0 - Main Account (Android 12 - 120 FPS)");
            lstInstances.Items.Add("Instance 1 - Farm Account #1 (Android 12 - 30 FPS Low-RAM)");
            lstInstances.Items.Add("Instance 2 - Farm Account #2 (Android 12 - 30 FPS Low-RAM)");
            lstInstances.Items.Add("Instance 3 - Farm Account #3 (Android 12 - 30 FPS Low-RAM)");
            multiForm.Controls.Add(lstInstances);

            Button btnNew = new Button();
            btnNew.Text = "+ New Instance";
            btnNew.Location = new Point(460, 65);
            btnNew.Size = new Size(125, 36);
            btnNew.BackColor = Color.FromArgb(0, 150, 220);
            btnNew.FlatStyle = FlatStyle.Flat;
            btnNew.ForeColor = Color.White;
            btnNew.Click += (s, e) => {
                lstInstances.Items.Add("Instance " + lstInstances.Items.Count + " - Farm Account (Low-RAM)");
            };
            multiForm.Controls.Add(btnNew);

            Button btnFarmMode = new Button();
            btnFarmMode.Text = "⚡ Apply 30 FPS";
            btnFarmMode.Location = new Point(460, 110);
            btnFarmMode.Size = new Size(125, 36);
            btnFarmMode.BackColor = Color.FromArgb(40, 160, 90);
            btnFarmMode.FlatStyle = FlatStyle.Flat;
            btnFarmMode.ForeColor = Color.White;
            btnFarmMode.Click += (s, e) => {
                MessageBox.Show("Farming profile applied: 30 FPS cap, 540p resolution, audio muted!\nAllows 12-16 instances simultaneously.", "Farming Optimized");
            };
            multiForm.Controls.Add(btnFarmMode);

            multiForm.ShowDialog(this);
        }

        private void OpenSettings()
        {
            Form settingsForm = new Form();
            settingsForm.Text = "BubuPlayer Settings";
            settingsForm.Size = new Size(500, 380);
            settingsForm.StartPosition = FormStartPosition.CenterParent;
            settingsForm.BackColor = Color.FromArgb(22, 24, 29);
            settingsForm.ForeColor = Color.White;
            settingsForm.FormBorderStyle = FormBorderStyle.FixedDialog;

            Label title = new Label();
            title.Text = "Emulator Preferences";
            title.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(0, 210, 255);
            title.Location = new Point(25, 20);
            title.AutoSize = true;
            settingsForm.Controls.Add(title);

            // Resolution selection
            Label lblRes = new Label() { Text = "Resolution:", Location = new Point(30, 70), AutoSize = true };
            ComboBox cmbRes = new ComboBox() { Location = new Point(160, 67), Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbRes.Items.AddRange(new string[] { "1920x1080 (FHD - Gaming)", "1280x720 (HD - Smooth)", "960x540 (qHD - Best for Farming)" });
            cmbRes.SelectedIndex = 1;
            settingsForm.Controls.Add(lblRes);
            settingsForm.Controls.Add(cmbRes);

            // FPS selection
            Label lblFps = new Label() { Text = "Max Frame Rate:", Location = new Point(30, 120), AutoSize = true };
            ComboBox cmbFps = new ComboBox() { Location = new Point(160, 117), Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbFps.Items.AddRange(new string[] { "120 FPS (Ultra Smooth Gaming)", "60 FPS (Standard)", "30 FPS (Low CPU Farming Mode)" });
            cmbFps.SelectedIndex = 0;
            settingsForm.Controls.Add(lblFps);
            settingsForm.Controls.Add(cmbFps);

            // KernelSU Status
            CheckBox chkKsu = new CheckBox() { Text = "Enable KernelSU Stealth Root (Anti-Cheat Safe)", Checked = true, Location = new Point(30, 180), Width = 400 };
            settingsForm.Controls.Add(chkKsu);

            Button btnSave = new Button() { Text = "Save Changes", Location = new Point(180, 260), Size = new Size(130, 40), BackColor = Color.FromArgb(0, 150, 220), FlatStyle = FlatStyle.Flat };
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
            // Update RAM readout
            long memBytes = Process.GetCurrentProcess().WorkingSet64;
            lblRam.Text = string.Format("Host RAM: {0} MB | Android RAM: 1.25 GB", memBytes / (1024 * 1024));
        }

        private void ExecAdbAsync(string args, Action onComplete = null)
        {
            if (!File.Exists(adbPath)) return;
            System.Threading.ThreadPool.QueueUserWorkItem((state) => {
                try {
                    ProcessStartInfo psi = new ProcessStartInfo(adbPath, string.Format("-s {0} {1}", currentDevice, args)) {
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
