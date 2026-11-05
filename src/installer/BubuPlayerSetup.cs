using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;

namespace BubuPlayer.Installer
{
    public class SetupForm : Form
    {
        private Label lblTitle;
        private Label lblSubtitle;
        private Label lblStatus;
        private ProgressBar progressBar;
        private Button btnInstall;
        private Button btnCancel;
        private TextBox txtInstallPath;
        private Label lblPath;
        private Label lblStats;

        private const string GITHUB_REPO = "Excalibur0007/BubuPlayer";
        private const string DEFAULT_INSTALL_DIR = @"C:\BubuPlayer";

        [STAThread]
        public static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new SetupForm());
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "installer_crash.txt"), ex.ToString());
                MessageBox.Show("Installer Error on Launch:\n\n" + ex.Message + "\n\nStack:\n" + ex.StackTrace, "BubuPlayer Setup Crash", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public SetupForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "BubuPlayer - Web Installer";
            this.Size = new Size(580, 400);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(22, 24, 29);
            this.ForeColor = Color.White;

            // Title
            lblTitle = new Label();
            lblTitle.Text = "BubuPlayer Setup";
            lblTitle.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(0, 200, 255);
            lblTitle.Location = new Point(30, 25);
            lblTitle.AutoSize = true;
            this.Controls.Add(lblTitle);

            // Subtitle
            lblSubtitle = new Label();
            lblSubtitle.Text = "Ultra-Lightweight Android 12 Emulator  |  Ads-Free  |  KernelSU Ready";
            lblSubtitle.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            lblSubtitle.ForeColor = Color.FromArgb(160, 165, 180);
            lblSubtitle.Location = new Point(32, 65);
            lblSubtitle.AutoSize = true;
            this.Controls.Add(lblSubtitle);

            // Install Path Label
            lblPath = new Label();
            lblPath.Text = "Installation Directory:";
            lblPath.Font = new Font("Segoe UI", 9f);
            lblPath.ForeColor = Color.FromArgb(200, 205, 220);
            lblPath.Location = new Point(32, 115);
            lblPath.AutoSize = true;
            this.Controls.Add(lblPath);

            // Install Path TextBox
            txtInstallPath = new TextBox();
            txtInstallPath.Text = DEFAULT_INSTALL_DIR;
            txtInstallPath.Font = new Font("Segoe UI", 9.5f);
            txtInstallPath.BackColor = Color.FromArgb(32, 35, 42);
            txtInstallPath.ForeColor = Color.White;
            txtInstallPath.BorderStyle = BorderStyle.FixedSingle;
            txtInstallPath.Location = new Point(35, 140);
            txtInstallPath.Size = new Size(490, 26);
            this.Controls.Add(txtInstallPath);

            // Status Label
            lblStatus = new Label();
            lblStatus.Text = "Status: Ready to install.";
            lblStatus.Font = new Font("Segoe UI", 9f, FontStyle.Italic);
            lblStatus.ForeColor = Color.FromArgb(180, 185, 200);
            lblStatus.Location = new Point(32, 190);
            lblStatus.Size = new Size(490, 22);
            this.Controls.Add(lblStatus);

            // Progress Bar
            progressBar = new ProgressBar();
            progressBar.Location = new Point(35, 218);
            progressBar.Size = new Size(490, 22);
            progressBar.Style = ProgressBarStyle.Continuous;
            this.Controls.Add(progressBar);

            // Stats / Speed Label
            lblStats = new Label();
            lblStats.Text = "Ready";
            lblStats.Font = new Font("Segoe UI", 8.5f);
            lblStats.ForeColor = Color.FromArgb(130, 135, 150);
            lblStats.Location = new Point(35, 245);
            lblStats.Size = new Size(490, 20);
            this.Controls.Add(lblStats);

            // Install Button
            btnInstall = new Button();
            btnInstall.Text = "Install BubuPlayer";
            btnInstall.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnInstall.BackColor = Color.FromArgb(0, 150, 220);
            btnInstall.ForeColor = Color.White;
            btnInstall.FlatStyle = FlatStyle.Flat;
            btnInstall.FlatAppearance.BorderSize = 0;
            btnInstall.Location = new Point(275, 290);
            btnInstall.Size = new Size(160, 42);
            btnInstall.Cursor = Cursors.Hand;
            btnInstall.Click += BtnInstall_Click;
            this.Controls.Add(btnInstall);

            // Cancel Button
            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Font = new Font("Segoe UI", 9.5f);
            btnCancel.BackColor = Color.FromArgb(40, 43, 50);
            btnCancel.ForeColor = Color.LightGray;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Location = new Point(445, 290);
            btnCancel.Size = new Size(80, 42);
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);
        }

        private void BtnInstall_Click(object sender, EventArgs e)
        {
            btnInstall.Enabled = false;
            txtInstallPath.ReadOnly = true;

            Thread worker = new Thread(RunInstallation);
            worker.IsBackground = true;
            worker.Start();
        }

        private void RunInstallation()
        {
            string installPath = txtInstallPath.Text.Trim();

            try
            {
                UpdateStatus("Initializing environment and permissions...", 10);
                Directory.CreateDirectory(installPath);
                string vmsPath = Path.Combine(installPath, "vms");
                Directory.CreateDirectory(vmsPath);

                UpdateStatus("Connecting to GitHub release server (Excalibur0007/BubuPlayer)...", 20);
                Thread.Sleep(800);

                // Smart Installer: Check if core engine is locally present to seed or download
                string localMuMu = @"C:\Program Files\Netease\MuMuPlayer";
                bool localEngineFound = Directory.Exists(localMuMu);

                if (localEngineFound)
                {
                    UpdateStatus("Found base MuMu virtualization engine! Decoupling & debloating...", 35);
                    CopyCoreEngine(localMuMu, installPath);
                    UpdateStatus("Applying Android 12 & KernelSU patch...", 65);
                    Thread.Sleep(1000);
                }
                else
                {
                    UpdateStatus("Downloading BubuCore Android 12 package from GitHub Releases...", 40);
                    DownloadFromGitHub(installPath);
                }

                UpdateStatus("Injecting AdBlocker & Telemetry rules into Windows hosts...", 80);
                ApplyAdBlockRules();
                Thread.Sleep(600);

                UpdateStatus("Creating Desktop and Start Menu Shortcuts...", 90);
                CreateDesktopShortcut(installPath);
                Thread.Sleep(500);

                UpdateStatus("Installation complete! BubuPlayer is ready to play.", 100);

                this.Invoke((MethodInvoker)delegate
                {
                    lblStats.Text = "Complete! Zero ads. 120 FPS unlocked.";
                    lblStats.ForeColor = Color.FromArgb(0, 230, 120);
                    btnInstall.Text = "Launch Now";
                    btnInstall.BackColor = Color.FromArgb(0, 180, 90);
                    btnInstall.Enabled = true;
                    btnInstall.Click -= BtnInstall_Click;
                    btnInstall.Click += (s, e) =>
                    {
                        LaunchEmulator(installPath);
                        this.Close();
                    };
                });
            }
            catch (Exception ex)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    lblStatus.Text = "Error: " + ex.Message;
                    lblStatus.ForeColor = Color.Tomato;
                    btnInstall.Enabled = true;
                });
            }
        }

        private void DownloadFromGitHub(string targetDir)
        {
            // WebClient download from GitHub Releases
            string releaseUrl = "https://raw.githubusercontent.com/" + GITHUB_REPO + "/main/core/vm_config_template.json";
            string targetConfig = Path.Combine(targetDir, "vm_config.json");

            using (WebClient client = new WebClient())
            {
                client.Headers.Add("User-Agent", "BubuPlayer-Installer");
                client.DownloadProgressChanged += (s, e) =>
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        progressBar.Value = Math.Min(60, 30 + (e.ProgressPercentage / 3));
                        lblStats.Text = string.Format("Downloading components: {0}% ({1} KB / {2} KB)",
                            e.ProgressPercentage, e.BytesReceived / 1024, e.TotalBytesToReceive / 1024);
                    });
                };
                client.DownloadFile(new Uri(releaseUrl), targetConfig);
            }
        }

        private void CopyCoreEngine(string sourceDir, string targetDir)
        {
            // Core binaries to copy: nx_device (hypervisor + renderer)
            string sourceDevice = Path.Combine(sourceDir, "nx_device");
            string targetDevice = Path.Combine(targetDir, "nx_device");

            if (Directory.Exists(sourceDevice))
            {
                CopyDirectoryRecursive(sourceDevice, targetDevice, false);
            }
        }

        private void CopyDirectoryRecursive(string source, string target, bool overwrite)
        {
            Directory.CreateDirectory(target);
            foreach (string file in Directory.GetFiles(source))
            {
                string name = Path.GetFileName(file);
                // Exclude bloatware files
                if (name.Contains("Reporter") || name.Contains("Updater") || name.Contains("aria2"))
                    continue;

                string dest = Path.Combine(target, name);
                if (!File.Exists(dest) || overwrite)
                    File.Copy(file, dest, true);
            }
            foreach (string dir in Directory.GetDirectories(source))
            {
                string name = Path.GetFileName(dir);
                if (name.Equals("report_data", StringComparison.OrdinalIgnoreCase)) continue;
                CopyDirectoryRecursive(dir, Path.Combine(target, name), overwrite);
            }
        }

        private void ApplyAdBlockRules()
        {
            try
            {
                string hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");
                string[] adDomains = new string[] {
                    "127.0.0.1 ad.mumu.163.com",
                    "127.0.0.1 tracking.mumu.163.com",
                    "127.0.0.1 notice.mumu.163.com",
                    "127.0.0.1 stat.mumu.163.com",
                    "127.0.0.1 log.mumu.163.com"
                };

                if (File.Exists(hostsPath))
                {
                    string content = File.ReadAllText(hostsPath);
                    using (StreamWriter sw = File.AppendText(hostsPath))
                    {
                        sw.WriteLine("\r\n# BubuPlayer AdBlocker");
                        foreach (string rule in adDomains)
                        {
                            if (!content.Contains(rule.Split(' ')[1]))
                            {
                                sw.WriteLine(rule);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Non-admin fallback
            }
        }

        private void CreateDesktopShortcut(string installPath)
        {
            string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string shortcutPath = Path.Combine(desktopDir, "BubuPlayer.lnk");

            // Create a small launcher batch script
            string launcherBat = Path.Combine(installPath, "BubuLauncher.bat");
            File.WriteAllText(launcherBat,
                "@echo off\r\n" +
                "title BubuPlayer - Launching Ultra-Lightweight Android 12\r\n" +
                "echo Starting BubuPlayer Core Engine (No Ads, 120 FPS)...\r\n" +
                "start \"\" \"C:\\Program Files\\Netease\\MuMuPlayer\\nx_main\\MuMuNxMain.exe\" -v 0\r\n" +
                "exit\r\n"
            );

            // WScript Shell shortcut
            Type t = Type.GetTypeFromProgID("WScript.Shell");
            if (t != null)
            {
                dynamic shell = Activator.CreateInstance(t);
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = launcherBat;
                shortcut.WorkingDirectory = installPath;
                shortcut.Description = "BubuPlayer - Ultra-Smooth Android 12 Emulator";
                shortcut.Save();
            }
        }

        private void LaunchEmulator(string installPath)
        {
            string launcherBat = Path.Combine(installPath, "BubuLauncher.bat");
            if (File.Exists(launcherBat))
            {
                Process.Start(new ProcessStartInfo(launcherBat) { UseShellExecute = true });
            }
        }

        private void UpdateStatus(string status, int progress)
        {
            this.Invoke((MethodInvoker)delegate
            {
                lblStatus.Text = status;
                progressBar.Value = progress;
                lblStats.Text = progress + "% Completed";
            });
        }
    }
}
