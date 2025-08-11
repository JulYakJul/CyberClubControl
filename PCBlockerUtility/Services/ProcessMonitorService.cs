using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PCBlockerUtility.Services
{
    public class ProcessMonitorService
    {
        private bool _isInitializing = true;
        private HashSet<string> allowedExecutables;
        private List<string> allowedFolders = new List<string>
        {
            @"C:\Program Files (x86)\Steam",
            @"C:\Program Files\Steam",
            @"C:\Program Files (x86)\Steam\steamapps\common",
            @"C:\Program Files\Microsoft Visual Studio",
            @"C:\Program Files (x86)\Microsoft VS Code",
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\Microsoft VS Code"
        };

        private ManagementEventWatcher processStartWatcher;

        public event Action<string, int> ProcessBlocked;

        public ProcessMonitorService()
        {
            allowedExecutables = new HashSet<string>
            {
                "PerfWatson2.exe",
                "sqlservr.exe",
                "msvsmon.exe",
                "steam.exe",
                "steamservice.exe",
                "explorer.exe",
                "steamwebhelper.exe",
                "gameoverlayui.exe",
                "steamerrorreporter.exe",
                "devenv.exe",
                "msbuild.exe",
                "ServiceHub.*.exe",
                "vstest.*.exe"
            };
        }

        public void StartProcessWatcher()
        {
            try
            {
                string query = "SELECT * FROM Win32_ProcessStartTrace";
                processStartWatcher = new ManagementEventWatcher(query);
                processStartWatcher.EventArrived += ProcessStarted;
                processStartWatcher.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to start process watcher: {ex.Message}");
            }
        }

        private void ProcessStarted(object sender, EventArrivedEventArgs e)
        {
            if (_isInitializing) return;

            try
            {
                string processName = e.NewEvent.Properties["ProcessName"].Value.ToString();
                int processId = Convert.ToInt32(e.NewEvent.Properties["ProcessID"].Value);

                Debug.WriteLine($"Проверка процесса: {processName} (ID: {processId})");

                if (processName.Equals(Process.GetCurrentProcess().ProcessName + ".exe", StringComparison.OrdinalIgnoreCase))
                    return;

                if (!IsProcessAllowed(processName, processId))
                {
                    Debug.WriteLine($"Блокировка процесса: {processName}");

                    ProcessBlocked?.Invoke(processName, processId);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка в ProcessStarted: {ex.Message}");
            }
        }

        private bool IsProcessAllowed(string processName, int processId)
        {
            if (allowedExecutables.Any(pattern =>
                WildcardMatch(processName, pattern)))
            {
                return true;
            }

            try
            {
                var process = Process.GetProcessById(processId);
                string processPath = process.MainModule.FileName.ToLower();

                if (processPath.Contains("steam"))
                    return true;

                foreach (var folder in allowedFolders)
                {
                    if (processPath.StartsWith(folder.ToLower()))
                        return true;
                }

                if (IsChildOfProcess(processId, "steam.exe"))
                    return true;

                foreach (var allowedProcess in allowedExecutables)
                {
                    if (IsChildOfProcess(processId, allowedProcess))
                        return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private bool IsChildOfProcess(int processId, string parentProcessName)
        {
            try
            {
                using (ManagementObject mo = new ManagementObject($"win32_process.handle='{processId}'"))
                {
                    mo.Get();
                    uint parentId = (uint)mo["ParentProcessId"];
                    var parent = Process.GetProcessById((int)parentId);

                    if (parent.ProcessName.Equals(parentProcessName, StringComparison.OrdinalIgnoreCase))
                        return true;

                    return IsChildOfProcess((int)parentId, parentProcessName);
                }
            }
            catch
            {
                return false;
            }
        }

        private bool WildcardMatch(string input, string pattern)
        {
            string regexPattern = "^" + Regex.Escape(pattern)
                                          .Replace("\\*", ".*")
                                          .Replace("\\?", ".") + "$";
            return Regex.IsMatch(input, regexPattern, RegexOptions.IgnoreCase);
        }
    }
}

