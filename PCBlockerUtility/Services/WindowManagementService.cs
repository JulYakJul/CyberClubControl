//using System;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Runtime.InteropServices;

//namespace PCBlockerUtility.Services
//{
//    public class WindowManagementService
//    {
//        // Делаем делегат публичным, чтобы использовать его в других частях проекта
//        public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

//        // Объявление внешних функций из user32.dll
//        [DllImport("user32.dll")]
//        private static extern bool GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

//        [DllImport("user32.dll")]
//        private static extern IntPtr GetLastActivePopup(IntPtr hWnd);

//        [DllImport("user32.dll")]
//        private static extern bool SetForegroundWindow(IntPtr hWnd);

//        [DllImport("user32.dll")]
//        public static extern bool IsWindowVisible(IntPtr hwnd);

//        private HashSet<string> allowedExecutables;

//        public WindowManagementService()
//        {
//            allowedExecutables = new HashSet<string>
//            {
//                "steam.exe", "steamservice.exe", "explorer.exe"
//                // добавьте сюда другие разрешенные исполнимые файлы
//            };
//        }

//        public void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
//        {
//            if (hwnd == IntPtr.Zero)
//                return;

//            uint processId;
//            GetWindowThreadProcessId(hwnd, out processId);

//            try
//            {
//                var proc = Process.GetProcessById((int)processId);
//                string exeName = Path.GetFileName(proc.MainModule.FileName);

//                // Если это разрешенный exe — не трогаем
//                if (allowedExecutables.Contains(exeName))
//                {
//                    return;
//                }

//                // Убиваем запрещённый процесс
//                proc.Kill();

//                // Вызов метода для обработки окон
//                HandleWindowActivation();
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine("Ошибка в WinEventProc: " + ex.Message);
//            }
//        }

//        private void HandleWindowActivation()
//        {
//            // Пример логики обработки окон (аналогичная логика из Form1)
//            var steamProcesses = Process.GetProcessesByName("steam");
//            if (steamProcesses.Length > 0)
//            {
//                IntPtr steamHandle = steamProcesses[0].MainWindowHandle;
//                if (steamHandle != IntPtr.Zero)
//                {
//                    IntPtr popupHandle = GetLastActivePopup(steamHandle);
//                    if (popupHandle != IntPtr.Zero && popupHandle != steamHandle)
//                    {
//                        SetForegroundWindow(popupHandle);
//                        return;
//                    }

//                    SetForegroundWindow(steamHandle);
//                    return;
//                }
//            }

//            // Если Steam не найден, активируем лаунчер
//            // Похожие действия с переключением окон, которые были в Form1
//        }
//    }
//}
