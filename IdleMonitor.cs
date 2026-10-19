using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Pnotch;

public class IdleMonitor : IDisposable
{
    public event Action? WentIdle;
    public event Action? Resumed;

    private Native.HookProc? _hookProc;
    private IntPtr _hookHandle = IntPtr.Zero;
    private System.Windows.Threading.DispatcherTimer? _timer;
    private readonly TimeSpan _timeout;
    private bool _isIdle;

    public IdleMonitor(TimeSpan timeout)
    {
        _timeout = timeout;
        _hookProc = HookCallback;

        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        IntPtr modHandle = GetModuleHandle(curModule?.ModuleName);

        _hookHandle = SetWindowsHookEx(14, _hookProc, modHandle, 0);

        _timer = new System.Windows.Threading.DispatcherTimer { Interval = _timeout };
        _timer.Tick += (s, e) =>
        {
            if (!_isIdle)
            {
                _isIdle = true;
                WentIdle?.Invoke();
            }
        };
        _timer.Start();
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            ResetIdle();
        }
        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private void ResetIdle()
    {
        _timer?.Stop();
        if (_isIdle)
        {
            _isIdle = false;
            Resumed?.Invoke();
        }
        _timer?.Start();
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, Native.HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    public void Dispose()
    {
        _timer?.Stop();
        if (_hookHandle != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }
    }
}