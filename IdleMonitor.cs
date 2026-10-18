using System.Windows.Threading;
using static NusaNotch.Native;
namespace NusaNotch;

/// 0% CPU saat idle:
///  - Tidak ada polling. Satu timer one-shot yang hanya bangun saat batas idle kemungkinan terlampaui.
///  - Hook input low-level HANYA dipasang selama state Resting, dan dilepas begitu pengguna kembali.
sealed class IdleMonitor : IDisposable
{
    public event Action? WentIdle, Resumed;
    readonly TimeSpan _threshold;
    readonly DispatcherTimer _timer = new();
    readonly HookProc _mouse, _kbd;
    IntPtr _hMouse, _hKbd;
    public bool IsIdle { get; private set; }

    public IdleMonitor(TimeSpan threshold)
    {
        _threshold = threshold;
        _mouse = (c, w, l) => { OnInput(); return CallNextHookEx(_hMouse, c, w, l); };
        _kbd   = (c, w, l) => { OnInput(); return CallNextHookEx(_hKbd, c, w, l); };
        _timer.Tick += (_, _) => Check();
        Arm(_threshold);
    }

    void Arm(TimeSpan t) { _timer.Interval = t; _timer.Start(); }

    void Check()
    {
        _timer.Stop();
        var lii = new LASTINPUTINFO { cb = (uint)System.Runtime.InteropServices.Marshal.SizeOf<LASTINPUTINFO>() };
        GetLastInputInfo(ref lii);
        var idle = TimeSpan.FromMilliseconds(unchecked((uint)Environment.TickCount - lii.dwTime));
        if (idle < _threshold) { Arm(_threshold - idle); return; }   // belum idle: tidur lagi sampai waktunya

        IsIdle = true;
        var mod = GetModuleHandle(null);
        _hMouse = SetWindowsHookEx(WH_MOUSE_LL, _mouse, mod, 0);
        _hKbd   = SetWindowsHookEx(WH_KEYBOARD_LL, _kbd, mod, 0);
        WentIdle?.Invoke();
    }

    void OnInput()
    {
        if (!IsIdle) return;
        IsIdle = false;
        UnhookWindowsHookEx(_hMouse); UnhookWindowsHookEx(_hKbd);
        Resumed?.Invoke();
        Arm(_threshold);
    }

    public void Dispose() { _timer.Stop(); if (IsIdle) { UnhookWindowsHookEx(_hMouse); UnhookWindowsHookEx(_hKbd); } }
}
