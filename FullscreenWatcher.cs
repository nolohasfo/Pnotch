using System;
using System.Runtime.InteropServices;
using static Pnotch.Native;

namespace Pnotch;

public class FullscreenWatcher : IDisposable
{
    public event Action<bool>? Changed;

    private IntPtr _hook;
    private readonly Native.WinEventProc _proc;

    public FullscreenWatcher(IntPtr targetHwnd)
    {
        _proc = OnWinEvent;
        _hook = SetWinEventHook(0x0003, 0x0003, IntPtr.Zero, _proc, 0, 0, 0);
    }

    private void OnWinEvent(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        if (hwnd == IntPtr.Zero) return;
        bool isFs = IsFullscreenWindow(hwnd);
        Changed?.Invoke(isFs);
    }

    private bool IsFullscreenWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero || hwnd == GetDesktopWindow() || hwnd == GetShellWindow()) return false;

        if (GetWindowRect(hwnd, out RECT rect))
        {
            int screenW = (int)System.Windows.SystemParameters.PrimaryScreenWidth;
            int screenH = (int)System.Windows.SystemParameters.PrimaryScreenHeight;

            return (rect.Left <= 0 && rect.Top <= 0 && rect.Right >= screenW && rect.Bottom >= screenH);
        }
        return false;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, Native.WinEventProc lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    public void Dispose()
    {
        if (_hook != IntPtr.Zero)
        {
            UnhookWinEvent(_hook);
            _hook = IntPtr.Zero;
        }
    }
}