using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace Pnotch;

public class TodoDisplayItem : INotifyPropertyChanged
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    private bool _done;
    public bool Done
    {
        get => _done;
        set
        {
            if (_done != value)
            {
                _done = value;
                OnPropertyChanged(nameof(Done));
            }
        }
    }

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

    public bool AlarmFired { get; set; }

    public void UpdateCountdown()
    {
        if (!Due.HasValue)
        {
            DueText = "No Alarm";
            return;
        }

        var diff = Due.Value - DateTime.Now;

        if (diff.TotalSeconds <= 0)
        {
            DueText = "00:00";
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

// Converters untuk template WPF XAML Custom Checkbox
public class BoolToVisConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        (value is bool b && b) ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class CheckBgConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        (value is bool b && b) ? System.Windows.Media.Brushes.MediumSeaGreen : System.Windows.Media.Brushes.Transparent;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public partial class TodoModule : UserControl, INotchModule
{
    private List<TodoItem> _items = new();
    private readonly ObservableCollection<TodoDisplayItem> _displayItems = new();
    private readonly DispatcherTimer _uiTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private string? _editingTaskId;

    public event Action? RequestShoulderTap;

    public string Id => "Todo";
    public UIElement View => this;

    public TodoModule()
    {
        // Register Converter di Resources secara programmatic jika belum ada
        if (!Resources.Contains("BoolToVisConverter")) Resources.Add("BoolToVisConverter", new BoolToVisConverter());
        if (!Resources.Contains("CheckBgConverter")) Resources.Add("CheckBgConverter", new CheckBgConverter());

        InitializeComponent();
        ListTodos.ItemsSource = _displayItems;

        _uiTimer.Tick += (_, _) => OnTimerTick();
        _uiTimer.Start();

        LoadData();
    }

    public void OnOpened()
    {
        LoadData();
        _uiTimer.Start();
    }

    public void OnClosed()
    {
        SaveData();
    }

    private void LoadData()
    {
        _items = JsonStore.Load<List<TodoItem>>("todos");
        RefreshDisplay();
    }

    private void SaveData()
    {
        JsonStore.Save("todos", _items);
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

        UpdateProgressStats();
    }

    private void UpdateProgressStats()
    {
        int doneCount = _items.Count(x => x.Done);
        int totalCount = _items.Count;
        TxtProgressFraction.Text = $"{doneCount}/{totalCount}";
        TxtHeaderDate.Text = DateTime.Now.ToString("ddd dd MMM yyyy HH:mm");
    }

    private void OnTimerTick()
    {
        TxtHeaderDate.Text = DateTime.Now.ToString("ddd dd MMM yyyy HH:mm");

        foreach (var item in _items.ToList())
        {
            item.UpdateCountdown();

            // Cek jika timer alarm habis
            if (item.Due.HasValue && !item.Done && !item.AlarmFired && item.Due.Value <= DateTime.Now)
            {
                item.AlarmFired = true;
                TriggerAlarmSound();
            }
        }
    }

    private void TriggerAlarmSound()
    {
        try
        {
            SystemSounds.Exclamation.Play();
        }
        catch { }

        RequestShoulderTap?.Invoke();
    }

    private void CheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox cb && cb.DataContext is TodoDisplayItem displayItem)
        {
            var item = _items.FirstOrDefault(x => x.Id == displayItem.Id);
            if (item != null)
            {
                item.Done = cb.IsChecked ?? false;
                displayItem.Done = item.Done;
                SaveData();
                UpdateProgressStats();
            }
        }
    }

    // --- DIALOG MODAL ADD / EDIT TASK ---
    private void BtnOpenAdd_Click(object sender, RoutedEventArgs e)
    {
        _editingTaskId = null;
        TxtModalTitle.Text = "Add New Task";
        TxtTaskTitle.Clear();
        CmbDue.SelectedIndex = 0;
        BtnDeleteTask.Visibility = Visibility.Collapsed;
        TaskOverlay.Visibility = Visibility.Visible;
    }

    private void BtnOpenEdit_Click(object sender, RoutedEventArgs e)
    {
        var firstItem = _items.FirstOrDefault();
        if (firstItem == null) return;

        _editingTaskId = firstItem.Id;
        TxtModalTitle.Text = "Edit Task";
        TxtTaskTitle.Text = firstItem.Title;
        CmbDue.SelectedIndex = 0;
        BtnDeleteTask.Visibility = Visibility.Visible;
        TaskOverlay.Visibility = Visibility.Visible;
    }

    private void BtnSaveTask_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtTaskTitle.Text)) return;

        DateTime? due = null;
        if (CmbDue.SelectedIndex == 1) due = DateTime.Now.AddMinutes(15);
        else if (CmbDue.SelectedIndex == 2) due = DateTime.Now.AddHours(1);
        else if (CmbDue.SelectedIndex == 3) due = DateTime.Now.AddHours(3);

        if (_editingTaskId == null)
        {
            // Tambah baru
            _items.Add(new TodoItem
            {
                Title = TxtTaskTitle.Text.Trim(),
                Done = false,
                Due = due
            });
        }
        else
        {
            // Edit item
            var item = _items.FirstOrDefault(x => x.Id == _editingTaskId);
            if (item != null)
            {
                item.Title = TxtTaskTitle.Text.Trim();
                if (due.HasValue) item.Due = due;
            }
        }

        SaveData();
        RefreshDisplay();
        TaskOverlay.Visibility = Visibility.Collapsed;
    }

    private void BtnDeleteTask_Click(object sender, RoutedEventArgs e)
    {
        if (_editingTaskId != null)
        {
            _items.RemoveAll(x => x.Id == _editingTaskId);
            SaveData();
            RefreshDisplay();
        }
        TaskOverlay.Visibility = Visibility.Collapsed;
    }

    private void BtnCancelOverlay_Click(object sender, RoutedEventArgs e)
    {
        TaskOverlay.Visibility = Visibility.Collapsed;
    }
}