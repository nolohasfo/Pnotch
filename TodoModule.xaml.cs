using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace NusaNotch;

public class TodoDisplayItem : INotifyPropertyChanged
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool Done { get; set; }
    public DateTime? Due { get; set; }

    private string _dueText = string.Empty;
    public string DueText
    {
        get => _dueText;
        set
        {
            if (_dueText != value)
            {
                _dueText = value;
                OnPropertyChanged(nameof(DueText));
            }
        }
    }

    // Hitung sisa waktu mundur setiap detik
    public void UpdateCountdown()
    {
        if (!Due.HasValue)
        {
            DueText = "";
            return;
        }

        var diff = Due.Value - DateTime.Now;

        if (diff.TotalSeconds <= 0)
        {
            DueText = "Waktu Habis!";
        }
        else if (diff.TotalHours >= 1)
        {
            DueText = $"{((int)diff.TotalHours):D2}:{diff.Minutes:D2}:{diff.Seconds:D2}";
        }
        else
        {
            DueText = $"{diff.Minutes:D2}:{diff.Seconds:D2}";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
}

public partial class TodoModule : UserControl, INotchModule
{
    private List<TodoItem> _items = new();
    private readonly ObservableCollection<TodoDisplayItem> _displayItems = new();
    private readonly DispatcherTimer _scheduleTimer = new();
    
    // Timer 1 detik untuk meng-update teks hitungan mundur di UI
    private readonly DispatcherTimer _uiCountdownTimer = new() { Interval = TimeSpan.FromSeconds(1) };

    public event Action? RequestShoulderTap;

    public string Id => "Todo";
    public UIElement View => this;

    public TodoModule()
    {
        InitializeComponent();
        ListTodos.ItemsSource = _displayItems;

        _scheduleTimer.Tick += ScheduleTimer_Tick;
        _uiCountdownTimer.Tick += (_, _) => UpdateCountdowns();
        _uiCountdownTimer.Start();

        LoadData();
    }

    public void OnOpened()
    {
        LoadData();
        _uiCountdownTimer.Start();
    }

    public void OnClosed()
    {
        SaveData();
    }

    private void LoadData()
    {
        _items = JsonStore.Load<List<TodoItem>>("todos");
        RefreshDisplay();
        RescheduleTimer();
    }

    private void SaveData()
    {
        JsonStore.Save("todos", _items);
        RescheduleTimer();
    }

    private void RefreshDisplay()
    {
        _displayItems.Clear();
        foreach (var item in _items)
        {
            var displayItem = new TodoDisplayItem
            {
                Id = item.Id,
                Title = item.Title,
                Done = item.Done,
                Due = item.Due
            };
            displayItem.UpdateCountdown();
            _displayItems.Add(displayItem);
        }
    }

    private void UpdateCountdowns()
    {
        foreach (var item in _displayItems)
        {
            item.UpdateCountdown();
        }
    }

    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtTaskTitle.Text)) return;

        DateTime? due = null;
        if (CmbDue.SelectedIndex == 1) due = DateTime.Now.AddMinutes(15);
        else if (CmbDue.SelectedIndex == 2) due = DateTime.Now.AddHours(1);
        else if (CmbDue.SelectedIndex == 3) due = DateTime.Now.AddHours(3);

        _items.Add(new TodoItem
        {
            Title = TxtTaskTitle.Text.Trim(),
            Done = false,
            Due = due
        });

        TxtTaskTitle.Clear();
        CmbDue.SelectedIndex = 0;

        SaveData();
        RefreshDisplay();
    }

    private void CheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox cb && cb.DataContext is TodoDisplayItem displayItem)
        {
            var item = _items.FirstOrDefault(x => x.Id == displayItem.Id);
            if (item != null)
            {
                item.Done = cb.IsChecked ?? false;
                SaveData();
            }
        }
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string id)
        {
            _items.RemoveAll(x => x.Id == id);
            SaveData();
            RefreshDisplay();
        }
    }

    private void RescheduleTimer()
    {
        _scheduleTimer.Stop();

        var nextDue = _items
            .Where(x => !x.Done && x.Due.HasValue && x.Due.Value > DateTime.Now)
            .OrderBy(x => x.Due!.Value)
            .FirstOrDefault();

        if (nextDue?.Due is DateTime dueTime)
        {
            var timeSpan = dueTime - DateTime.Now;
            if (timeSpan.TotalMilliseconds > 0)
            {
                _scheduleTimer.Interval = timeSpan;
                _scheduleTimer.Start();
            }
        }
    }

    private void ScheduleTimer_Tick(object? sender, EventArgs e)
    {
        _scheduleTimer.Stop();
        RequestShoulderTap?.Invoke();
        RescheduleTimer();
    }
}