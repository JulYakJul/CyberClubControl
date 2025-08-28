using System;

namespace PCBlockerUtility.Utilities
{
    public class WinAPI
    {
        // Публичный делегат, доступный для использования в других частях проекта
        public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
    }
}
