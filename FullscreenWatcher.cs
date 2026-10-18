using System.Text;
using static NusaNotch.Native;
namespace NusaNotch;

/// Event-driven (tanpa polling): hanya bangun saat foreground window berubah / selesai dipindah.
sealed class FullscreenWatcher : IDisposable
{
    public event Action<bool>? Changed;
    readonly WinEventProc _proc;               // simpan referensi agar tidak di-GC
    readonly IntPtr _h1, _h2;
    readonly IntPtr _self;
    bool _last;

    public FullscreenWatcher(IntPtr selfHwnd)
    {
        _self = selfHwnd;
        _proc = (_, _, _, _, _, _, _) => Evaluate();
        _h1 = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, _proc, 0, 0, WINEVENT_OUTOFCONTEXT);
        _h2 = SetWinEventHook(EVENT_SYSTEM_MOVESIZEEND, EVENT_SYSTEM_MOVESIZEEND, IntPtr.Zero, _proc, 0, 0, WINEVENT_OUTOFCONTEXT);
    }

    void Evaluate()
    {
        bool fs = IsForegroundFullscreen();
        if (fs == _last) return;
        _last = fs; Changed?.Invoke(fs);
    }

    bool IsForegroundFullscreen()
    {
        var h = GetForegroundWindow();
        if (h == IntPtr.Zero || h == _self) return false;

        var sb = new StringBuilder(64); GetClassName(h, sb, 64);
        var cls = sb.ToString();
        if (cls is "Progman" or "WorkerW" or "Shell_TrayWnd") return false;   // desktop & taskbar

        if (!GetWindowRect(h, out var r)) return false;
        var mi = new MONITORINFO { cb = System.Runtime.InteropServices.Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(MonitorFromWindow(h, 2 /*NEAREST*/), ref mi)) return false;

        var m = mi.rcMonitor;
        return r.L <= m.L && r.T <= m.T && r.R >= m.R && r.B >= m.B;
    }

    public void Dispose() { UnhookWinEvent(_h1); UnhookWinEvent(_h2); }
}
