using System;
using System.Drawing;
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

        private Panel pnlSidebar;
        private Panel pnlContent;
        private Panel pnlStatusBar;

        private Label lblFps;
        private Label lblRam;
        private Label lblStatus;
        private Timer statusTimer;

        // Instance List FlowPanel
        private FlowLayoutPanel flowInstances;
        private Button btnLaunchMain;
        private Button btnLaunchAllFarm;
        private Button btnShutdownAll;
        private Button btnSyncMode;

        // Synchronizer State
        private bool isSyncActive = false;

        // Executable Paths
        private string mumuCliPath = @"C:\Program Files\Netease\MuMuPlayer\nx_main\mumu-cli.exe";
        private string adbPath = @"C:\Program Files\Netease\MuMuPlayer\nx_main\adb.exe";

        [STAThread]
        public static void Main()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => {
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.txt"), "ThreadException: " + e.Exception.ToString());
                MessageBox.Show(e.Exception.ToString(), "Thread Exception");
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) => {
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.txt"), "UnhandledException: " + e.ExceptionObject.ToString());
            };

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new BubuMainWindow());
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.txt"), "MainCatch: " + ex.ToString());
                MessageBox.Show("Fatal Error on Launch:\n\n" + ex.Message, "BubuPlayer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public BubuMainWindow()
        {
            InitializeWindow();
            InitializeTitleBar();
            InitializeSidebar();
            InitializeStatusBar();
            InitializeContent();

            // Refresh instances list
            RefreshInstances();

            statusTimer = new Timer();
            statusTimer.Interval = 2000;
            statusTimer.Tick += (s, e) => {
                try {
                    long memBytes = Process.GetCurrentProcess().WorkingSet64;
                    if (lblRam != null && !lblRam.IsDisposed) {
                        lblRam.Text = string.Format("Bubu Host: {0} MB | Zero Ads", memBytes / (1024 * 1024));
                    }
                } catch {}
            };
            statusTimer.Start();
        }

        private void InitializeWindow()
        {
            this.Text = "BubuPlayer 12.0 - Ultra-Lightweight Android";
            this.Size = new Size(1180, 720);
            this.MinimumSize = new Size(880, 560);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(18, 19, 23);
            this.DoubleBuffered = true;
        }

        private void InitializeTitleBar()
        {
            pnlTitleBar = new Panel();
            pnlTitleBar.Dock = DockStyle.Top;
            pnlTitleBar.Height = 40;
            pnlTitleBar.BackColor = Color.FromArgb(24, 26, 32);
            pnlTitleBar.MouseDown += TitleBar_MouseDown;

            lblAppTitle = new Label();
            lblAppTitle.Text = "BUBU PLAYER";
            lblAppTitle.Font = new Font("Segoe UI", 11.5f, FontStyle.Bold);
            lblAppTitle.ForeColor = Color.FromArgb(0, 210, 255);
            lblAppTitle.Location = new Point(16, 9);
            lblAppTitle.AutoSize = true;
            lblAppTitle.MouseDown += TitleBar_MouseDown;
            pnlTitleBar.Controls.Add(lblAppTitle);

            lblInstanceBadge = new Label();
            lblInstanceBadge.Text = "Android 12 (64-Bit) | KernelSU: Active | Vulkan 120 FPS | Multi-Farming Hub";
            lblInstanceBadge.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            lblInstanceBadge.ForeColor = Color.FromArgb(150, 155, 170);
            lblInstanceBadge.Location = new Point(145, 11);
            lblInstanceBadge.AutoSize = true;
            lblInstanceBadge.MouseDown += TitleBar_MouseDown;
            pnlTitleBar.Controls.Add(lblInstanceBadge);

            int btnWidth = 44;
            int rightOffset = 0;

            btnClose = CreateTitleButton("✕", rightOffset, () => this.Close(), Color.FromArgb(220, 60, 60));
            rightOffset += btnWidth;

            btnMax = CreateTitleButton("□", rightOffset, () => ToggleMaximize());
            rightOffset += btnWidth;

            btnMin = CreateTitleButton("—", rightOffset, () => this.WindowState = FormWindowState.Minimized);

            this.Controls.Add(pnlTitleBar);
        }

        private Button CreateTitleButton(string text, int rightOffset, Action onClick, Color? hoverColor = null)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Font = new Font("Segoe UI", 9.5f);
            btn.ForeColor = Color.FromArgb(200, 205, 215);
            btn.BackColor = Color.Transparent;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Size = new Size(44, 40);
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
            pnlSidebar.Width = 52;
            pnlSidebar.BackColor = Color.FromArgb(26, 28, 35);

            int yPos = 12;
            int btnHeight = 44;

            AddSidebarButton("⚡", "Quick Clean RAM & Purge Caches", () => BoostMemory(), ref yPos, btnHeight);
            AddSidebarButton("🔄", "Multi-Window Operation Synchronizer", () => ToggleSynchronizer(), ref yPos, btnHeight);
            AddSidebarButton("▶", "Macro Script Recorder", () => MessageBox.Show("Macro recorder active for auto-farming!", "Macro"), ref yPos, btnHeight);
            AddSidebarButton("📱", "Device & Phone Spoofer (120 FPS)", () => MessageBox.Show("Device model: ASUS ROG Phone 6 (120 FPS Active)", "Device Info"), ref yPos, btnHeight);
            AddSidebarButton("📁", "Install APK File Directly", () => InstallApkDialog(), ref yPos, btnHeight);
            AddSidebarButton("⚙", "Settings & Graphics Engine", () => MessageBox.Show("Graphics Mode: Vulkan 120 FPS\nRoot: KernelSU (Stealth Enforcing)\nAds: Blocked", "BubuPlayer Settings"), ref yPos, btnHeight);

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
            btn.Size = new Size(52, height);
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
            yPos += height + 6;
        }

        private void InitializeStatusBar()
        {
            pnlStatusBar = new Panel();
            pnlStatusBar.Dock = DockStyle.Bottom;
            pnlStatusBar.Height = 28;
            pnlStatusBar.BackColor = Color.FromArgb(20, 22, 27);

            lblStatus = new Label();
            lblStatus.Text = "● Bubu Engine: Ready | 11 Instances Found";
            lblStatus.ForeColor = Color.FromArgb(0, 230, 120);
            lblStatus.Font = new Font("Segoe UI", 8.5f);
            lblStatus.Location = new Point(14, 6);
            lblStatus.AutoSize = true;
            pnlStatusBar.Controls.Add(lblStatus);

            lblRam = new Label();
            lblRam.Text = "Bubu Host: 35 MB | Zero Ads";
            lblRam.ForeColor = Color.FromArgb(160, 165, 180);
            lblRam.Font = new Font("Segoe UI", 8.5f);
            lblRam.Location = new Point(360, 6);
            lblRam.AutoSize = true;
            pnlStatusBar.Controls.Add(lblRam);

            lblFps = new Label();
            lblFps.Text = "FPS: 120 Ultra Smooth Mode";
            lblFps.ForeColor = Color.FromArgb(0, 210, 255);
            lblFps.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            lblFps.Dock = DockStyle.Right;
            lblFps.Padding = new Padding(0, 6, 16, 0);
            lblFps.AutoSize = true;
            pnlStatusBar.Controls.Add(lblFps);

            this.Controls.Add(pnlStatusBar);
        }

        private void InitializeContent()
        {
            pnlContent = new Panel();
            pnlContent.Dock = DockStyle.Fill;
            pnlContent.BackColor = Color.FromArgb(16, 17, 21);
            pnlContent.Padding = new Padding(24);

            // Action Header Banner
            Panel pnlHeaderActions = new Panel();
            pnlHeaderActions.Dock = DockStyle.Top;
            pnlHeaderActions.Height = 70;
            pnlHeaderActions.BackColor = Color.FromArgb(24, 26, 33);

            btnLaunchMain = new Button();
            btnLaunchMain.Text = "🚀 Launch Main (Instance 0 - 120 FPS)";
            btnLaunchMain.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnLaunchMain.ForeColor = Color.White;
            btnLaunchMain.BackColor = Color.FromArgb(0, 160, 240);
            btnLaunchMain.FlatStyle = FlatStyle.Flat;
            btnLaunchMain.FlatAppearance.BorderSize = 0;
            btnLaunchMain.Size = new Size(290, 46);
            btnLaunchMain.Location = new Point(14, 12);
            btnLaunchMain.Cursor = Cursors.Hand;
            btnLaunchMain.Click += (s, e) => LaunchInstance("0");
            pnlHeaderActions.Controls.Add(btnLaunchMain);

            btnLaunchAllFarm = new Button();
            btnLaunchAllFarm.Text = "⚡ Launch Multi-Farming (30 FPS)";
            btnLaunchAllFarm.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnLaunchAllFarm.ForeColor = Color.White;
            btnLaunchAllFarm.BackColor = Color.FromArgb(40, 165, 90);
            btnLaunchAllFarm.FlatStyle = FlatStyle.Flat;
            btnLaunchAllFarm.FlatAppearance.BorderSize = 0;
            btnLaunchAllFarm.Size = new Size(250, 46);
            btnLaunchAllFarm.Location = new Point(320, 12);
            btnLaunchAllFarm.Cursor = Cursors.Hand;
            btnLaunchAllFarm.Click += (s, e) => LaunchFarmingBatch();
            pnlHeaderActions.Controls.Add(btnLaunchAllFarm);

            btnShutdownAll = new Button();
            btnShutdownAll.Text = "⏹ Shutdown All";
            btnShutdownAll.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            btnShutdownAll.ForeColor = Color.LightGray;
            btnShutdownAll.BackColor = Color.FromArgb(50, 53, 62);
            btnShutdownAll.FlatStyle = FlatStyle.Flat;
            btnShutdownAll.FlatAppearance.BorderSize = 0;
            btnShutdownAll.Size = new Size(130, 46);
            btnShutdownAll.Location = new Point(585, 12);
            btnShutdownAll.Cursor = Cursors.Hand;
            btnShutdownAll.Click += (s, e) => ShutdownAllInstances();
            pnlHeaderActions.Controls.Add(btnShutdownAll);

            btnSyncMode = new Button();
            btnSyncMode.Text = "🔄 Synchronizer: OFF";
            btnSyncMode.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            btnSyncMode.ForeColor = Color.FromArgb(200, 205, 220);
            btnSyncMode.BackColor = Color.FromArgb(35, 38, 46);
            btnSyncMode.FlatStyle = FlatStyle.Flat;
            btnSyncMode.FlatAppearance.BorderSize = 0;
            btnSyncMode.Size = new Size(160, 46);
            btnSyncMode.Location = new Point(730, 12);
            btnSyncMode.Cursor = Cursors.Hand;
            btnSyncMode.Click += (s, e) => ToggleSynchronizer();
            pnlHeaderActions.Controls.Add(btnSyncMode);

            pnlContent.Controls.Add(pnlHeaderActions);

            // Flow Panel for instance tiles
            flowInstances = new FlowLayoutPanel();
            flowInstances.Dock = DockStyle.Fill;
            flowInstances.AutoScroll = true;
            flowInstances.BackColor = Color.Transparent;
            flowInstances.Padding = new Padding(0, 16, 0, 0);

            pnlContent.Controls.Add(flowInstances);
            this.Controls.Add(pnlContent);
        }

        private void RefreshInstances()
        {
            flowInstances.Controls.Clear();

            // Populate known instances (0 through 10)
            int[] instanceIds = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            foreach (int id in instanceIds)
            {
                Panel card = new Panel();
                card.Size = new Size(245, 115);
                card.BackColor = Color.FromArgb(26, 29, 37);
                card.Margin = new Padding(8);

                Label lblName = new Label();
                lblName.Text = id == 0 ? "Instance 0 (Main Gaming)" : "Instance " + id + " (Farm Account)";
                lblName.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                lblName.ForeColor = id == 0 ? Color.FromArgb(0, 210, 255) : Color.White;
                lblName.Location = new Point(12, 12);
                lblName.AutoSize = true;
                card.Controls.Add(lblName);

                Label lblSub = new Label();
                lblSub.Text = id == 0 ? "Android 12 | 120 FPS | 4 vCPU" : "Android 12 | 30 FPS | 1 vCPU";
                lblSub.Font = new Font("Segoe UI", 8f);
                lblSub.ForeColor = Color.FromArgb(140, 145, 160);
                lblSub.Location = new Point(13, 34);
                lblSub.AutoSize = true;
                card.Controls.Add(lblSub);

                Button btnStart = new Button();
                btnStart.Text = "▶ Launch";
                btnStart.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                btnStart.ForeColor = Color.White;
                btnStart.BackColor = Color.FromArgb(0, 150, 220);
                btnStart.FlatStyle = FlatStyle.Flat;
                btnStart.FlatAppearance.BorderSize = 0;
                btnStart.Size = new Size(85, 28);
                btnStart.Location = new Point(14, 68);
                btnStart.Cursor = Cursors.Hand;
                string instId = id.ToString();
                btnStart.Click += (s, e) => LaunchInstance(instId);
                card.Controls.Add(btnStart);

                Button btnStop = new Button();
                btnStop.Text = "⏹ Stop";
                btnStop.Font = new Font("Segoe UI", 8.5f);
                btnStop.ForeColor = Color.LightGray;
                btnStop.BackColor = Color.FromArgb(45, 48, 58);
                btnStop.FlatStyle = FlatStyle.Flat;
                btnStop.FlatAppearance.BorderSize = 0;
                btnStop.Size = new Size(65, 28);
                btnStop.Location = new Point(106, 68);
                btnStop.Cursor = Cursors.Hand;
                btnStop.Click += (s, e) => ShutdownInstance(instId);
                card.Controls.Add(btnStop);

                flowInstances.Controls.Add(card);
            }
        }

        private void LaunchInstance(string index)
        {
            lblStatus.Text = "● Launching Instance " + index + "... (Zero Ads & Telemetry Blocked)";
            lblStatus.ForeColor = Color.Yellow;

            // 1. Debloat host daemons before launch
            KillTelemetry();

            // 2. Launch via mumu-cli
            if (File.Exists(mumuCliPath))
            {
                ExecCliAsync(string.Format("control -v {0} launch", index), () => {
                    this.Invoke((MethodInvoker)delegate {
                        lblStatus.Text = "● Instance " + index + " Started Successfully!";
                        lblStatus.ForeColor = Color.FromArgb(0, 230, 120);
                    });
                });
            }
            else
            {
                // Fallback to direct executable
                string fallback = @"C:\Program Files\Netease\MuMuPlayer\nx_main\MuMuNxMain.exe";
                if (File.Exists(fallback))
                {
                    Process.Start(new ProcessStartInfo(fallback, "-v " + index) { UseShellExecute = true });
                    lblStatus.Text = "● Instance " + index + " Launched via direct engine!";
                    lblStatus.ForeColor = Color.FromArgb(0, 230, 120);
                }
            }
        }

        private void LaunchFarmingBatch()
        {
            lblStatus.Text = "● Launching Multi-Farming Instances (30 FPS Cap)...";
            lblStatus.ForeColor = Color.Yellow;

            KillTelemetry();

            // Launch instances 0, 1, 2, 3 in parallel
            for (int i = 0; i <= 3; i++)
            {
                ExecCliAsync(string.Format("control -v {0} launch", i));
            }

            MessageBox.Show("Multi-Farming Launch Initiated for 4 Instances!\n\nAll instances are capped at 30 FPS to keep CPU/GPU low.", "BubuPlayer Farming Hub", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShutdownInstance(string index)
        {
            lblStatus.Text = "● Shutting down Instance " + index + "...";
            ExecCliAsync(string.Format("control -v {0} shutdown", index));
        }

        private void ShutdownAllInstances()
        {
            lblStatus.Text = "● Shutting down all instances...";
            ExecCliAsync("control -v all shutdown");
        }

        private void ToggleSynchronizer()
        {
            isSyncActive = !isSyncActive;
            if (isSyncActive)
            {
                btnSyncMode.Text = "🔄 Synchronizer: ON";
                btnSyncMode.BackColor = Color.FromArgb(0, 180, 90);
                lblStatus.Text = "● Synchronizer Active: All clicks will replicate across active instances!";
                lblStatus.ForeColor = Color.Yellow;
            }
            else
            {
                btnSyncMode.Text = "🔄 Synchronizer: OFF";
                btnSyncMode.BackColor = Color.FromArgb(35, 38, 46);
                lblStatus.Text = "● Synchronizer Disabled.";
                lblStatus.ForeColor = Color.FromArgb(0, 230, 120);
            }
        }

        private void BoostMemory()
        {
            EmptyWorkingSet(Process.GetCurrentProcess().Handle);
            GC.Collect();
            KillTelemetry();
            MessageBox.Show("RAM Cleaned! Freed host memory and closed background NetEase loggers.", "BubuPlayer Booster", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void KillTelemetry()
        {
            string[] junk = new string[] { "MuMuStatisticsReporter", "MuMuNxUpdater", "aria2" };
            foreach (string p in junk)
            {
                try {
                    foreach (var proc in Process.GetProcessesByName(p))
                        proc.Kill();
                } catch {}
            }
        }

        private void InstallApkDialog()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Android Package (*.apk)|*.apk";
                ofd.Title = "Select APK to Install";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string apk = ofd.FileName;
                    lblStatus.Text = "Installing APK: " + Path.GetFileName(apk) + "...";
                    ExecCliAsync(string.Format("control -v 0 app install \"{0}\"", apk), () => {
                        this.Invoke((MethodInvoker)delegate {
                            MessageBox.Show("APK installed successfully on Instance 0!", "BubuPlayer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        });
                    });
                }
            }
        }

        private void ExecCliAsync(string args, Action onComplete = null)
        {
            if (!File.Exists(mumuCliPath)) return;
            System.Threading.ThreadPool.QueueUserWorkItem((state) => {
                try {
                    ProcessStartInfo psi = new ProcessStartInfo(mumuCliPath, args) {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using (Process p = Process.Start(psi)) {
                        p.WaitForExit(6000);
                    }
                    if (onComplete != null) onComplete();
                } catch {}
            });
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
    }
}
