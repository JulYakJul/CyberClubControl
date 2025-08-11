using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Management;
using System.Text.RegularExpressions;
using System.Text;
using PCBlockerUtility.Services;
using PCBlockerUtility.Utilities;
using System.Drawing.Imaging;
using PCBlockerUtility.Interfaces;
using PCBlockerUtility.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace PCBlockerUtility
{
    public partial class Form1 : Form
    {
        private const int HOTKEY_ID = 1;
        private const int HOTKEY_ID_TEST_EXIT = 2;

        #region Native Methods
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetLastActivePopup(IntPtr hWnd);
        #endregion

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
                                             int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        #region Fields
        private bool _isInitializing;
        private bool steamWasVisible;
        private bool steamLaunchedFromLauncher;

        private SecurityService securityService;
        private ProcessMonitorService processMonitorService;
        private ProgramLauncherService programLauncherService;

        private Button exitButton;
        private Button testExitButton;

        private IntPtr hHook;
        private WinEventDelegate procDelegate;

        private Process runningGame;
        private HashSet<string> allowedExecutables;
        private ManagementEventWatcher processStartWatcher;
        private readonly string password = "123";

        private readonly List<string> allowedFolders = new List<string>
        {
            @"C:\Program Files (x86)\Steam",
            @"C:\Program Files\Steam",
            @"C:\Program Files (x86)\Steam\steamapps\common",
            @"C:\Program Files\Microsoft Visual Studio",
            @"C:\Program Files (x86)\Microsoft VS Code",
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\Microsoft VS Code"
        };

        private readonly string steamFolder = @"C:\Program Files (x86)\Steam";
        #endregion

        private PCBlockerService _signalRService;
        private DateTime _unlockUntil;
        private System.Windows.Forms.Timer _unlockTimer;
        private Label _unlockLabel;

        public static Form1 Instance { get; private set; }
        private IChatClient _chatClient;
        private List<ChatMessage> _chatHistory;
        private ChatForm _currentChatForm = null;
        private Button _steamButton;

        private ILogger<Form1> _logger;

        public Form1()
        {
            Instance = this;
            InitializeComponent();

            _chatClient = new OllamaChatClientService(new Uri("http://localhost:11434/"), "gemma2:2b");
            _chatHistory = new List<ChatMessage>();

            _unlockLabel = new Label
            {
                Text = "Компьютер заблокирован",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                BackColor = Color.Transparent,
                Visible = true,
                Location = new Point(650, 50)
            };
            this.Controls.Add(_unlockLabel);

            string imagePath = Path.Combine(Application.StartupPath, "Images", "BGImg.png");

            if (File.Exists(imagePath))
            {
                PictureBox backgroundPictureBox = new PictureBox
                {
                    Image = Image.FromFile(imagePath),
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    Dock = DockStyle.Fill
                };

                this.Controls.Add(backgroundPictureBox);
                backgroundPictureBox.SendToBack();
            }
            else
            {
                MessageBox.Show($"Фоновое изображение не найдено: {imagePath}");
            }

            try
            {
                _isInitializing = true;

                programLauncherService = new ProgramLauncherService(allowedFolders, steamFolder);
                programLauncherService.ProgramExited += OnProgramExited;

                WinAPI.WinEventDelegate del = new WinAPI.WinEventDelegate(WinEventProc);
                securityService = new SecurityService(this, del);
                securityService.InitializeSecurity();

                programLauncherService.LoadAllowedPrograms(this);

                processMonitorService = new ProcessMonitorService();
                processMonitorService.ProcessBlocked += OnProcessBlocked;
                processMonitorService.StartProcessWatcher();

                AddSteamButton();
                AddExitButton();
                AddTestExitButton();
                AddChatButton();

                _isInitializing = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Initialization failed: {ex.Message}");
                this.Close();
            }

            var serviceProvider = new ServiceCollection()
            .AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            })
            .BuildServiceProvider();

            var hubUrl = "https://192.168.1.92:8443/unlockhub";
            _logger = serviceProvider.GetRequiredService<ILogger<Form1>>();
            var loggerForService = serviceProvider.GetRequiredService<ILogger<PCBlockerService>>();

            _signalRService = new PCBlockerService(
                hubUrl,
                GetComputerId(),
                OnUnlockReceived,
                OnLockReceived,
                loggerForService);

            _ = TryInitializeSignalRAsync();
        

        _unlockTimer = new System.Windows.Forms.Timer { Interval = 60000 };
            _unlockTimer.Tick += CheckUnlockStatus;
            _unlockTimer.Start();
        }

        private async Task TryInitializeSignalRAsync()
        {
            try
            {
                await _signalRService.StartAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось подключиться к серверу: {ex.Message}");
            }
        }

        private string GetComputerId()
        {
            string ipAddress = GetLocalIPAddress();
            return $"{ipAddress}"; 
        }

        private string GetLocalIPAddress()
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "unknown";
        }

        private void CenterUnlockLabel()
        {
            _unlockLabel.Location = new Point(
                (this.ClientSize.Width - _unlockLabel.Width) / 2,
                (this.ClientSize.Height - _unlockLabel.Height) / 2);
        }

        private void OnUnlockReceived(DateTime unlockTime)
        {
            // Устанавливаем время разблокировки
            _unlockUntil = unlockTime;

            // Обновляем интерфейс
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateUIForUnlock()));
            }
            else
            {
                UpdateUIForUnlock();
            }
        }

        private void UpdateUIForUnlock()
        {
            try
            {
                // Форматируем дату и время для отображения
                string timeString = _unlockUntil.ToString("HH:mm");
                string dateString = _unlockUntil.ToString("dd.MM.yyyy");

                _unlockLabel.Text = $"Компьютер разблокирован до {timeString} ({dateString})";
                _unlockLabel.ForeColor = Color.Green;

                if (_steamButton != null)
                {
                    _steamButton.Visible = true;
                }
                else
                {
                    AddSteamButton();
                }

                // Разблокируем элементы управления
                UpdateLockState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления интерфейса: {ex.Message}");
            }
        }

        private void OnLockReceived()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateUIForLock()));
            }
            else
            {
                UpdateUIForLock();
            }
        }

        private void UpdateUIForLock()
        {
            // Обновляем текст метки
            _unlockLabel.Text = "Компьютер заблокирован";
            _unlockLabel.ForeColor = Color.Red;

            // Скрываем кнопку Steam
            if (_steamButton != null)
            {
                _steamButton.Visible = false;
            }
        }

        private void CheckUnlockStatus(object sender, EventArgs e)
        {
            try
            {
                if (DateTime.Now > _unlockUntil)
                {
                    OnLockReceived();
                }
                else
                {
                    // Обновляем время в реальном времени
                    string timeString = _unlockUntil.ToString("HH:mm");
                    string dateString = _unlockUntil.ToString("dd.MM.yyyy");

                    _unlockLabel.Text = $"Компьютер разблокирован до {timeString} ({dateString})";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка проверки статуса разблокировки");
            }
        }

        private void UpdateLockState()
        {
            bool shouldUnlock = DateTime.Now < _unlockUntil;

            foreach (Control control in this.Controls)
            {
                if (control != exitButton && control != testExitButton)
                {
                    control.Enabled = shouldUnlock;
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _unlockTimer?.Stop();
            _signalRService?.StopAsync().Wait();
            base.OnFormClosing(e);
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_CLOSE = 0xF060;

            if (m.Msg == WM_SYSCOMMAND && m.WParam.ToInt32() == SC_CLOSE)
            {
                return;
            }

            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == HOTKEY_ID_TEST_EXIT)
                {
                    SafeExit();
                    return;
                }
            }

            base.WndProc(ref m);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (hHook != IntPtr.Zero)
                {
                    UnhookWinEvent(hHook);
                    hHook = IntPtr.Zero;
                }
                procDelegate = null;
            }
            base.Dispose(disposing);
        }

        #region UI Methods
        private void AddExitButton()
        {
            exitButton = new Button
            {
                Text = "Выход",
                Width = 100,
                Height = 40,
                Font = new System.Drawing.Font("Arial", 10),
                ForeColor = System.Drawing.Color.White,
                BackColor = System.Drawing.Color.DarkRed,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new System.Drawing.Point(this.ClientSize.Width - 110, 10)
            };

            exitButton.Click += (s, e) => AskForPassword();
            this.Controls.Add(exitButton);
            exitButton.BringToFront();
        }

        private void AddTestExitButton()
        {
            testExitButton = new Button
            {
                Text = "X",
                Width = 40,
                Height = 40,
                Font = new System.Drawing.Font("Arial", 10),
                ForeColor = System.Drawing.Color.White,
                BackColor = System.Drawing.Color.Red,
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                Location = new System.Drawing.Point(10, 10)
            };

            testExitButton.Click += (s, e) => SafeExit();
            this.Controls.Add(testExitButton);
            testExitButton.BringToFront();
        }

        private void AddSteamButton()
        {
            string steamExePath = Path.Combine(steamFolder, "Steam.exe");

            if (!File.Exists(steamExePath))
            {
                Debug.WriteLine("Steam.exe не найден по пути: " + steamExePath);
                return;
            }

            if (_steamButton != null)
            {
                this.Controls.Remove(_steamButton);
                _steamButton.Dispose();
            }

            _steamButton = new Button
            {
                Text = "Перейти в Steam",
                Width = 400,
                Height = 90,
                Font = new Font("Arial", 18, FontStyle.Bold),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };

            string buttonImagePath = Path.Combine(Application.StartupPath, "Images", "ButtonImg.png");
            if (File.Exists(buttonImagePath))
            {
                _steamButton.BackgroundImage = Image.FromFile(buttonImagePath);
                _steamButton.BackgroundImageLayout = ImageLayout.Stretch;
                _steamButton.FlatAppearance.BorderSize = 0;
            }
            else
            {
                _steamButton.BackColor = Color.FromArgb(0, 100, 180);
            }

            _steamButton.Location = new Point(
                (this.ClientSize.Width - _steamButton.Width) / 2,
                (this.ClientSize.Height - _steamButton.Height) / 2);

            _steamButton.Anchor = AnchorStyles.None;

            _steamButton.Click += (s, e) =>
            {
                try
                {
                    Process.Start(steamExePath);
                    steamLaunchedFromLauncher = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка запуска Steam: {ex.Message}");
                }
            };

            this.Controls.Add(_steamButton);
            _steamButton.BringToFront();
        }

        private void AddChatButton()
        {
            var chatButton = new Button
            {
                Text = "AI чат",
                Size = new Size(180, 40),
                Location = new Point(20, 150),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            chatButton.Click += (sender, e) =>
            {
                if (_currentChatForm != null && !_currentChatForm.IsDisposed)
                {
                    _currentChatForm.BringToFront();
                    _currentChatForm.Focus();
                    return;
                }

                if (_chatClient == null)
                {
                    _chatClient = new OllamaChatClientService(new Uri("http://localhost:11434/"), "gemma2:2b");
                }

                if (_chatHistory == null)
                {
                    _chatHistory = new List<ChatMessage>();
                }

                _currentChatForm = new ChatForm(_chatClient, _chatHistory);
                _currentChatForm.FormClosed += (s, args) => _currentChatForm = null;
                _currentChatForm.Show(this);
            };

            this.Controls.Add(chatButton);
            chatButton.BringToFront();
        }

        private void AskForPassword()
        {
            using (PasswordForm passwordForm = new PasswordForm())
            {
                if (passwordForm.ShowDialog() == DialogResult.OK)
                {
                    if (passwordForm.Password == password)
                    {
                        SafeExit();
                    }
                    else
                    {
                        MessageBox.Show("Неверный пароль.", "Ошибка",
                                      MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }
        #endregion

        #region Business Logic
        private void SafeExit()
        {
            if (processStartWatcher != null)
            {
                processStartWatcher.Stop();
                processStartWatcher.Dispose();
                processStartWatcher = null;
            }

            UnregisterHotKey(this.Handle, HOTKEY_ID);
            UnregisterHotKey(this.Handle, HOTKEY_ID_TEST_EXIT);
            UnhookWinEvent(hHook);
            securityService.RestoreSystemState();

            this.Close();
        }

        private void OnProgramExited()
        {
            this.Invoke(new MethodInvoker(() =>
            {
                this.Show();
                this.Activate();
            }));
        }
        #endregion

        #region Event Handlers
        private void OnProcessBlocked(string processName, int processId)
        {
            try
            {
                var process = Process.GetProcessById(processId);
                Debug.WriteLine($"Убиваем процесс: {process.ProcessName}");
                process.Kill();

                var steamProcesses = Process.GetProcessesByName("steam");
                if (steamProcesses.Length > 0)
                {
                    IntPtr steamHandle = steamProcesses[0].MainWindowHandle;
                    if (steamHandle != IntPtr.Zero)
                    {
                        SetForegroundWindow(steamHandle);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при убийстве процесса: {ex.Message}");
            }
        }

        private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (hwnd == IntPtr.Zero)
                return;

            GetWindowThreadProcessId(hwnd, out uint processId);

            try
            {
                var proc = Process.GetProcessById((int)processId);
                string exeName = Path.GetFileName(proc.MainModule.FileName);

                if (programLauncherService.IsExecutableAllowed(exeName))
                    return;

                if (proc.ProcessName == Process.GetCurrentProcess().ProcessName)
                    return;

                proc.Kill();

                this.BeginInvoke((MethodInvoker)delegate
                {
                    var steam = Process.GetProcessesByName("steam").FirstOrDefault();
                    if (steam != null && steam.MainWindowHandle != IntPtr.Zero)
                    {
                        IntPtr popup = GetLastActivePopup(steam.MainWindowHandle);
                        if (popup != IntPtr.Zero && popup != steam.MainWindowHandle)
                        {
                            SetForegroundWindow(popup);
                            return;
                        }

                        SetForegroundWindow(steam.MainWindowHandle);
                        return;
                    }

                    this.Show();
                    this.SendToBack();
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Ошибка в WinEventProc: " + ex.Message);
            }
        }
        #endregion
    }
}