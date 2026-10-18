using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NusaNotch;

public class ShelfItem
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileInfoText { get; set; } = string.Empty;
    public bool IsFolder { get; set; }

    public Visibility IsFolderVisibility => IsFolder ? Visibility.Visible : Visibility.Collapsed;
    public Visibility IsFileVisibility => !IsFolder ? Visibility.Visible : Visibility.Collapsed;
}

public partial class ShelfModule : UserControl, INotchModule
{
    private List<ShelfItem> _items = new();
    private readonly ObservableCollection<ShelfItem> _displayItems = new();
    private Point _dragStartPoint;
    private bool _isDraggingOut = false;

    public string Id => "Shelf";
    public UIElement View => this;

    public ShelfModule()
    {
        InitializeComponent();
        ListShelfItems.ItemsSource = _displayItems;
        LoadData();
    }

    public void OnOpened()
    {
        LoadData();
    }

    public void OnClosed()
    {
        SaveData();
    }

    private void LoadData()
    {
        _items = JsonStore.Load<List<ShelfItem>>("shelf");
        RefreshDisplay();
    }

    private void SaveData()
    {
        JsonStore.Save("shelf", _items);
    }

    private void RefreshDisplay()
    {
        _displayItems.Clear();
        foreach (var item in _items)
        {
            _displayItems.Add(item);
        }
        EmptyPrompt.Visibility = _displayItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        _items.Clear();
        SaveData();
        RefreshDisplay();
    }

    private void BtnRemoveItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string path)
        {
            _items.RemoveAll(x => x.FilePath.Equals(path, StringComparison.OrdinalIgnoreCase));
            SaveData();
            RefreshDisplay();
        }
    }

    private void Shelf_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            DragOverlay.Visibility = Visibility.Visible;
        }
    }

    private void Shelf_DragLeave(object sender, DragEventArgs e)
    {
        DragOverlay.Visibility = Visibility.Collapsed;
    }

    private void Shelf_Drop(object sender, DragEventArgs e)
    {
        DragOverlay.Visibility = Visibility.Collapsed;

        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[]? files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null && files.Length > 0)
            {
                foreach (string path in files)
                {
                    if (_items.Any(x => x.FilePath.Equals(path, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    bool isDir = Directory.Exists(path);
                    bool isFile = File.Exists(path);

                    if (!isDir && !isFile) continue;

                    string infoText = "Folder";
                    if (isFile)
                    {
                        try
                        {
                            long bytes = new FileInfo(path).Length;
                            infoText = FormatBytes(bytes);
                        }
                        catch
                        {
                            infoText = "File";
                        }
                    }

                    _items.Add(new ShelfItem
                    {
                        FilePath = path,
                        FileName = Path.GetFileName(path),
                        IsFolder = isDir,
                        FileInfoText = infoText
                    });
                }

                SaveData();
                RefreshDisplay();
            }
        }
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes >= 1073741824) return $"{(bytes / 1073741824.0):F1} GB";
        if (bytes >= 1048576) return $"{(bytes / 1048576.0):F1} MB";
        if (bytes >= 1024) return $"{(bytes / 1024.0):F1} KB";
        return $"{bytes} B";
    }

    private void Item_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            _dragStartPoint = e.GetPosition(null);
            _isDraggingOut = false;
        }
    }

    private void Item_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && !_isDraggingOut)
        {
            Point mousePos = e.GetPosition(null);
            Vector diff = _dragStartPoint - mousePos;

            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                if (sender is FrameworkElement element && element.DataContext is ShelfItem item)
                {
                    if (File.Exists(item.FilePath) || Directory.Exists(item.FilePath))
                    {
                        _isDraggingOut = true;
                        var data = new DataObject(DataFormats.FileDrop, new string[] { item.FilePath });
                        
                        DragDropEffects result = DragDrop.DoDragDrop(element, data, DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link);

                        if (result != DragDropEffects.None)
                        {
                            _items.RemoveAll(x => x.FilePath.Equals(item.FilePath, StringComparison.OrdinalIgnoreCase));
                            SaveData();
                            RefreshDisplay();
                        }
                        _isDraggingOut = false;
                    }
                }
            }
        }
    }

    private void Item_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDraggingOut && sender is FrameworkElement element && element.DataContext is ShelfItem item)
        {
            try
            {
                if (File.Exists(item.FilePath) || Directory.Exists(item.FilePath))
                {
                    Process.Start(new ProcessStartInfo(item.FilePath) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Tidak dapat membuka file: {ex.Message}");
            }
        }
    }
}