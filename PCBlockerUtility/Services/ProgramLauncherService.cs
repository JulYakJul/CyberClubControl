using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PCBlockerUtility.Services
{
    public class ProgramLauncherService
    {
        private readonly List<string> allowedFolders;
        private readonly string steamFolder;
        private HashSet<string> allowedExecutables;
        private Process runningGame;
        private bool steamLaunchedFromLauncher;

        public event Action ProgramExited;

        public ProgramLauncherService(List<string> allowedFolders, string steamFolder)
        {
            this.allowedFolders = allowedFolders;
            this.steamFolder = steamFolder;
        }

        public void LoadAllowedPrograms(Control parentControl)
        {
            try
            {
                var allFiles = new List<string>();
                foreach (var folder in allowedFolders.Where(Directory.Exists))
                {
                    allFiles.AddRange(SafeGetFiles(folder, "*.exe", SearchOption.AllDirectories));
                }

                allowedExecutables = new HashSet<string>(
                    allFiles.Select(Path.GetFileName),
                    StringComparer.OrdinalIgnoreCase);

                var panel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    AutoScroll = true,
                    BackColor = Color.Gray,
                    WrapContents = true
                };

                if (Directory.Exists(steamFolder))
                {
                    string steamExePath = Path.Combine(steamFolder, "Steam.exe");
                    if (File.Exists(steamExePath))
                    {
                        var steamButton = new Button
                        {
                            Text = "Steam",
                            Width = 200,
                            Height = 50,
                            Font = new Font("Arial", 12),
                            ForeColor = Color.White,
                            BackColor = Color.DarkBlue,
                            Tag = steamExePath
                        };
                        steamButton.Click += LaunchProgram;
                        panel.Controls.Add(steamButton);
                    }
                }

                if (panel.Controls.Count == 0)
                {
                    MessageBox.Show("Не найдено ни одной разрешенной программы.", "Информация",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                parentControl.Controls.Add(panel);
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Нет доступа к папкам. Запустите от администратора.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке программ: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private List<string> SafeGetFiles(string path, string searchPattern, SearchOption searchOption)
        {
            var files = new List<string>();
            try
            {
                files.AddRange(Directory.GetFiles(path, searchPattern, SearchOption.TopDirectoryOnly));

                if (searchOption == SearchOption.AllDirectories)
                {
                    foreach (var dir in Directory.GetDirectories(path))
                    {
                        files.AddRange(SafeGetFiles(dir, searchPattern, searchOption));
                    }
                }
            }
            catch { }

            return files;
        }

        private void LaunchProgram(object sender, EventArgs e)
        {
            if (sender is not Button button || button.Tag is not string exePath || !File.Exists(exePath))
            {
                MessageBox.Show("Файл не найден.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exePath,
                        WindowStyle = ProcessWindowStyle.Normal,
                        UseShellExecute = true
                    },
                    EnableRaisingEvents = true
                };

                process.Exited += (s, args) => ProgramExited?.Invoke();

                process.Start();
                runningGame = process;

                if (Path.GetFileName(exePath).Equals("steam.exe", StringComparison.OrdinalIgnoreCase))
                {
                    steamLaunchedFromLauncher = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка запуска: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public bool IsExecutableAllowed(string exeName)
        {
            return allowedExecutables?.Contains(exeName) ?? false;
        }

        public bool SteamLaunchedFromLauncher => steamLaunchedFromLauncher;
    }
}