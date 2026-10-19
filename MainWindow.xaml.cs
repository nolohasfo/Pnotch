using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using static Pnotch.Native;

namespace Pnotch;

public enum NotchState { Resting, Closed, Open, Hidden }

public partial class MainWindow : Window
{
    // Dimensi DIP
    const double ClosedW = 220, ClosedH = 34, OpenW = 540, OpenH = 210;
    const double ThinW = 100, ThinH = 3;
    const int HoverOpenDelayMs = 90, HoverCloseDelayMs = 300;

    NotchState _state = NotchState.Closed;
    NotchState _stateBeforeHidden = NotchState.Closed;
    readonly DispatcherTimer _hoverTimer = new();
    Action? _pending;
    FullscreenWatcher? _fs;
    IdleMonitor? _idle;
    readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromSeconds(1) };
    
    readonly DispatcherTimer _occlusionTimer = new() { Interval = TimeSpan.FromMilliseconds(350) };
    bool _isOccluded = false;

    readonly DispatcherTimer _chiikawaTimer = new();
    readonly Random _random = new();

    // Deklarasi field modul
    private readonly List<INotchModule> _modules = new();
    private MediaModule _mediaModule = null!;
    private TodoModule _todoModule = null!;
    private ShelfModule _shelfModule = null!;
    private ClipboardModule _clipboardModule = null!;
    private int _moduleIndex = 0;

    [DllImport("kernel32.dll")]
    private static extern bool SetProcessWorkingSetSize(IntPtr proc, IntPtr min, IntPtr max);

    public MainWindow()
    {
        InitializeComponent();

        // Buat instance modul setelah InitializeComponent dipanggil
        _mediaModule = new MediaModule();
        _todoModule = new TodoModule();
        _shelfModule = new ShelfModule();
        _clipboardModule = new ClipboardModule();

        _modules.Add(_mediaModule);
        _modules.Add(_todoModule);
        _modules.Add(_shelfModule);
        _modules.Add(_clipboardModule);

        OpenHost.Content = _modules[_moduleIndex].View;
        UpdatePageDots();
        _todoModule.RequestShoulderTap += () =>
        {
            Dispatcher.Invoke(() =>
            {
                int todoIdx = _modules.IndexOf(_todoModule);
                if (todoIdx != -1) SwitchModule(todoIdx);
                Expand();
                var autoCloseTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                autoCloseTimer.Tick += (s, ev) => { autoCloseTimer.Stop(); Collapse(); };
                autoCloseTimer.Start();
            });
        };

        Notch.MouseWheel += Notch_MouseWheel;

        Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
        Top = 0;

        _hoverTimer.Tick += (_, _) => { _hoverTimer.Stop(); var a = _pending; _pending = null; a?.Invoke(); };

        Notch.MouseEnter += (_, _) => Schedule(Expand, HoverOpenDelayMs);
        Notch.MouseLeave += (_, _) => Schedule(Collapse, HoverCloseDelayMs);

        Notch.DragEnter += (s, e) =>
        {
            int shelfIdx = _modules.IndexOf(_shelfModule);
            if (e.Data.GetDataPresent(DataFormats.FileDrop) && shelfIdx != -1)
            {
                SwitchModule(shelfIdx);
                Schedule(Expand, 0);
            }
            else
            {
                Schedule(Expand, 0);
            }
        };
        Notch.DragLeave += (_, _) => Schedule(Collapse, HoverCloseDelayMs);

        _clock.Tick += (_, _) =>
        {
            ClockText.Text = DateTime.Now.ToString("HH:mm");
            UpdateBattery();
        };
        ClockText.Text = DateTime.Now.ToString("HH:mm");
        UpdateBattery();
        _clock.Start();

        _chiikawaTimer.Interval = TimeSpan.FromSeconds(3);
        _chiikawaTimer.Tick += (_, _) => ChangeChiikawaExpression();
        _chiikawaTimer.Start();

        _occlusionTimer.Tick += (_, _) => CheckAreaOcclusion();
        _occlusionTimer.Start();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;

        int ex = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, ex | WS_EX_TOOLWINDOW);

        _fs = new FullscreenWatcher(hwnd);
        _fs.Changed += isFs => Dispatcher.Invoke(() => OnFullscreenChanged(isFs));

        _idle = new IdleMonitor(TimeSpan.FromSeconds(60));
        _idle.WentIdle += () => Dispatcher.Invoke(EnterResting);
        _idle.Resumed  += () => Dispatcher.Invoke(ExitResting);
    }

    private void CheckAreaOcclusion()
    {
        IntPtr fgHwnd = GetForegroundWindow();
        var myHwnd = new WindowInteropHelper(this).Handle;

        if (fgHwnd == IntPtr.Zero || fgHwnd == myHwnd || fgHwnd == GetDesktopWindow() || fgHwnd == GetShellWindow())
        {
            SetOccluded(false);
            return;
        }

        if (IsIconic(fgHwnd))
        {
            SetOccluded(false);
            return;
        }

        if (GetWindowRect(fgHwnd, out RECT rect))
        {
            double screenW = SystemParameters.PrimaryScreenWidth;
            int notchLeft = (int)(screenW / 2 - 110);
            int notchRight = (int)(screenW / 2 + 110);
            int notchBottom = 35;

            bool overlaps = (rect.Left < notchRight && rect.Right > notchLeft && rect.Top < notchBottom && rect.Bottom > 0);
            SetOccluded(overlaps);
        }
    }

    private void SetOccluded(bool occluded)
    {
        if (_isOccluded == occluded) return;
        _isOccluded = occluded;

        if (_state == NotchState.Closed || _state == NotchState.Resting)
        {
            if (_isOccluded)
                EnterThinBar();
            else
                ExitThinBar();
        }
    }

    private void EnterThinBar()
    {
        Fade(ClosedView, 0, 150);
        Fade(EyesView, 0, 150);
        AnimateSize(ThinW, ThinH, 200);
    }

    private void ExitThinBar()
    {
        AnimateSize(ClosedW, ClosedH, 220, onDone: () =>
        {
            Fade(ClosedView, 1, 180);
            ClockText.Text = DateTime.Now.ToString("HH:mm");
            UpdateBattery();
        });
    }

    void Schedule(Action a, int ms)
    {
        _hoverTimer.Stop();
        _pending = a;
        _hoverTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(1, ms));
        _hoverTimer.Start();
    }

    void Expand()
    {
        if (_state is NotchState.Hidden or NotchState.Open) return;
        _state = NotchState.Open;
        _clock.Stop();
        _chiikawaTimer.Stop();
        AnimateSize(OpenW, OpenH, 260);
        Fade(ClosedView, 0, 120); Fade(EyesView, 0, 120);
        OpenContainer.Visibility = Visibility.Visible;
        Fade(OpenContainer, 1, 200, delayMs: 100);

        _modules[_moduleIndex].OnOpened();
    }

    void Collapse()
    {
        if (_state != NotchState.Open) return;
        if (Notch.IsMouseOver || Notch.IsMouseDirectlyOver) return;
        _state = NotchState.Closed;
        Fade(OpenContainer, 0, 120);

        if (_isOccluded)
        {
            AnimateSize(ThinW, ThinH, 220, onDone: () => 
            {
                OpenContainer.Visibility = Visibility.Collapsed;
                CompactMemory();
            });
        }
        else
        {
            AnimateSize(ClosedW, ClosedH, 220, onDone: () => 
            {
                OpenContainer.Visibility = Visibility.Collapsed;
                CompactMemory();
            });
            Fade(ClosedView, 1, 200, delayMs: 100);
            ClockText.Text = DateTime.Now.ToString("HH:mm"); 
            UpdateBattery();
        }

        _clock.Start();
        _chiikawaTimer.Start();
        _modules[_moduleIndex].OnClosed();
    }

    void EnterResting()
    {
        if (_state != NotchState.Closed) return;
        _state = NotchState.Resting;
        _clock.Stop();
        _chiikawaTimer.Stop();
        
        if (!_isOccluded)
        {
            Fade(ClosedView, 0, 600);
            Fade(EyesView, 1, 600);
        }
        CompactMemory();
    }

    void ExitResting()
    {
        if (_state != NotchState.Resting) return;
        _state = NotchState.Closed;
        
        if (!_isOccluded)
        {
            Fade(ClosedView, 1, 250);
            Fade(EyesView, 1, 0);
            ClockText.Text = DateTime.Now.ToString("HH:mm"); 
            UpdateBattery();
        }
        _clock.Start();
        _chiikawaTimer.Start();
    }

    void OnFullscreenChanged(bool isFs)
    {
        if (isFs) { _stateBeforeHidden = _state; _state = NotchState.Hidden; _clock.Stop(); _chiikawaTimer.Stop(); _occlusionTimer.Stop(); Hide(); }
        else      { _state = NotchState.Closed; Show(); _clock.Start(); _chiikawaTimer.Start(); _occlusionTimer.Start(); }
    }

    void UpdateBattery()
    {
        if (GetSystemPowerStatus(out var status))
        {
            if (status.BatteryLifePercent <= 100)
            {
                string chargingSymbol = status.ACLineStatus == 1 ? "⚡" : "";
                BatteryText.Text = $"{chargingSymbol}{status.BatteryLifePercent}%";

                if (status.BatteryLifePercent <= 20)
                    BatteryText.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                else if (status.ACLineStatus == 1)
                    BatteryText.Foreground = new SolidColorBrush(Color.FromRgb(255, 199, 44));
                else
                    BatteryText.Foreground = new SolidColorBrush(Color.FromRgb(80, 200, 255));
            }
            else
            {
                BatteryText.Text = "AC";
                BatteryText.Foreground = new SolidColorBrush(Color.FromRgb(255, 199, 44));
            }
        }
    }

    private void ChangeChiikawaExpression()
    {
        FaceNormal.Visibility = Visibility.Collapsed;
        FaceBlink.Visibility = Visibility.Collapsed;
        FaceCute.Visibility = Visibility.Collapsed;
        FaceHappy.Visibility = Visibility.Collapsed;

        int expr = _random.Next(0, 4);
        switch (expr)
        {
            case 0: FaceNormal.Visibility = Visibility.Visible; break;
            case 1: FaceBlink.Visibility = Visibility.Visible; break;
            case 2: FaceCute.Visibility = Visibility.Visible; break;
            case 3: FaceHappy.Visibility = Visibility.Visible; break;
        }

        _chiikawaTimer.Interval = TimeSpan.FromSeconds(_random.Next(3, 7));
    }

    public void SwitchModule(int newIndex)
    {
        if (_modules.Count == 0) return;

        if (newIndex < 0) newIndex = _modules.Count - 1;
        if (newIndex >= _modules.Count) newIndex = 0;

        if (_state == NotchState.Open)
        {
            _modules[_moduleIndex].OnClosed();
        }

        _moduleIndex = newIndex;
        OpenHost.Content = _modules[_moduleIndex].View;
        UpdatePageDots();

        if (_state == NotchState.Open)
        {
            _modules[_moduleIndex].OnOpened();
        }
    }

    private void BtnPrevModule_Click(object sender, RoutedEventArgs e) => SwitchModule(_moduleIndex - 1);
    private void BtnNextModule_Click(object sender, RoutedEventArgs e) => SwitchModule(_moduleIndex + 1);

    private void Notch_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (_state == NotchState.Open)
        {
            if (e.Delta < 0) SwitchModule(_moduleIndex + 1);
            else if (e.Delta > 0) SwitchModule(_moduleIndex - 1);
        }
    }

    private void UpdatePageDots()
    {
        PageDotsContainer.Children.Clear();
        for (int i = 0; i < _modules.Count; i++)
        {
            int idx = i;
            bool isActive = (idx == _moduleIndex);
            var dot = new Ellipse
            {
                Width = isActive ? 10 : 6,
                Height = 6,
                Fill = isActive 
                    ? new SolidColorBrush(Color.FromRgb(255, 199, 44))
                    : new SolidColorBrush(Color.FromRgb(30, 58, 96)),
                Margin = new Thickness(3, 0, 3, 0),
                Cursor = Cursors.Hand
            };
            dot.MouseDown += (s, e) => SwitchModule(idx);
            PageDotsContainer.Children.Add(dot);
        }
    }

    private void CompactMemory()
    {
        try
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, (IntPtr)(-1), (IntPtr)(-1));
        }
        catch { }
    }

    void AnimateSize(double w, double h, int ms, Action? onDone = null)
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var aw = new DoubleAnimation(w, TimeSpan.FromMilliseconds(ms)) { EasingFunction = ease };
        var ah = new DoubleAnimation(h, TimeSpan.FromMilliseconds(ms)) { EasingFunction = ease };
        if (onDone != null) ah.Completed += (_, _) => onDone();
        Notch.BeginAnimation(WidthProperty, aw);
        Notch.BeginAnimation(HeightProperty, ah);
    }

    static void Fade(UIElement e, double to, int ms, int delayMs = 0) =>
        e.BeginAnimation(OpacityProperty, new DoubleAnimation(to, TimeSpan.FromMilliseconds(ms))
        { BeginTime = TimeSpan.FromMilliseconds(delayMs) });

    protected override void OnClosed(EventArgs e) { _fs?.Dispose(); _idle?.Dispose(); base.OnClosed(e); }
}