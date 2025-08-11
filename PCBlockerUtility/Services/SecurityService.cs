using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using PCBlockerUtility.Utilities;

namespace PCBlockerUtility.Services
{
    public class SecurityService
    {
        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint EVENT_SYSTEM_FOREGROUND = 3;
        private const int HOTKEY_ID = 1;
        private const int HOTKEY_ID_TEST_EXIT = 2;
        private const int MOD_CONTROL = 0x0002;
        private const int VK_Q = 0x51;

        private IntPtr hHook;
        private readonly WinAPI.WinEventDelegate procDelegate;
        private readonly Form ownerForm;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinAPI.WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        public SecurityService(Form ownerForm, WinAPI.WinEventDelegate procDelegate)
        {
            this.ownerForm = ownerForm;
            this.procDelegate = procDelegate;
        }

        // Инициализация безопасности
        public void InitializeSecurity()
        {
            SetTaskManagerState(false);
            KillSystemTools();

            // Регистрация горячих клавиш
            if (!RegisterHotKey(ownerForm.Handle, HOTKEY_ID_TEST_EXIT, MOD_CONTROL, VK_Q))
            {
                MessageBox.Show("Не удалось зарегистрировать горячую клавишу Ctrl+Q.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Установка хука для событий активации окон
            hHook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, procDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);
            if (hHook == IntPtr.Zero)
            {
                MessageBox.Show("Не удалось установить хук для событий активации окон.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void RestoreSystemState()
        {
            try
            {
                SetTaskManagerState(true);

                if (hHook != IntPtr.Zero)
                {
                    UnhookWinEvent(hHook);
                    hHook = IntPtr.Zero;
                }

                UnregisterHotKey(ownerForm.Handle, HOTKEY_ID);
                UnregisterHotKey(ownerForm.Handle, HOTKEY_ID_TEST_EXIT);

                if (!Process.GetProcessesByName("explorer").Any())
                {
                    Process.Start("explorer.exe");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при восстановлении системы: " + ex.Message);
            }
        }

        // Установка состояния диспетчера задач
        private void SetTaskManagerState(bool enable)
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    if (key != null)
                    {
                        key.SetValue("DisableTaskMgr", enable ? 0 : 1);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Ошибка при изменении состояния диспетчера задач: " + ex.Message);
            }
        }

        // Завершение процессов системных инструментов
        private void KillSystemTools()
        {
            var forbidden = new[] { "explorer", "regedit" };
            foreach (var name in forbidden)
            {
                foreach (var proc in Process.GetProcessesByName(name))
                {
                    try { proc.Kill(); } catch { }
                }
            }
        }
    }
}
