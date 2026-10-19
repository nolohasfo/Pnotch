using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Pnotch;

public partial class ClipboardModule : UserControl, INotchModule
{
    private const string StoreFileName = "snippets.json";
    private List<SnippetItem> _snippets = new();

    // Implementasi Interface INotchModule
    public string Id => "Clipboard";
    public UIElement View => this;

    public ClipboardModule()
    {
        InitializeComponent();
        SetPlaceholderText();
        LoadSnippets();
    }

    public void OnOpened()
    {
        LoadSnippets();
    }

    public void OnClosed()
    {
        CloseOverlay();
    }

    // ---------- LOAD & SAVE JSON ----------
    private void LoadSnippets()
    {
        _snippets = JsonStore.Load<List<SnippetItem>>(StoreFileName) ?? new List<SnippetItem>();

        // Preset default jika data masih kosong
        if (_snippets.Count == 0)
        {
            _snippets.Add(new SnippetItem { Label = "Terminal Run", Text = "dotnet run" });
            _snippets.Add(new SnippetItem { Label = "My Email", Text = "user@example.com" });
            _snippets.Add(new SnippetItem { Label = "Bank Account", Text = "BCA 1234567890 a.n Nusa" });
            SaveSnippets();
        }

        RenderSnippetList();
    }

    private void SaveSnippets()
    {
        JsonStore.Save(StoreFileName, _snippets);
    }

    // ---------- RENDER LIST UI ----------
    private void RenderSnippetList()
    {
        SnippetListPanel.Children.Clear();
        TxtTotalCount.Text = $"{_snippets.Count} Snippets";

        if (_snippets.Count == 0)
        {
            EmptyStatePanel.Visibility = Visibility.Visible;
            return;
        }

        EmptyStatePanel.Visibility = Visibility.Collapsed;

        foreach (var item in _snippets)
        {
            var card = CreateSnippetCard(item);
            SnippetListPanel.Children.Add(card);
        }
    }

    private Border CreateSnippetCard(SnippetItem item)
    {
        var card = new Border
        {
            Style = (Style)Resources["SnippetCardStyle"],
            Cursor = Cursors.Hand,
            Tag = item
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

        // Title / Label Snippet (Gold)
        var lbl = new TextBlock
        {
            Text = item.Label,
            Foreground = new SolidColorBrush(Color.FromRgb(255, 199, 44)), // #FFC72C
            FontWeight = FontWeights.Bold,
            FontSize = 11
        };

        // Preview Teks (Cyan) - WPF Single Line Ellipsis
        var content = new TextBlock
        {
            Text = item.Text,
            Foreground = new SolidColorBrush(Color.FromRgb(80, 200, 255)), // #50C8FF
            FontSize = 10,
            Margin = new Thickness(0, 2, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.NoWrap
        };

        textStack.Children.Add(lbl);
        textStack.Children.Add(content);
        Grid.SetColumn(textStack, 0);
        grid.Children.Add(textStack);

        // Action Panel (Tombol Hapus)
        var actionPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        var btnDelete = new Button
        {
            Content = "✕",
            Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            FontSize = 12,
            Padding = new Thickness(6, 2, 6, 2),
            Cursor = Cursors.Hand,
            ToolTip = "Hapus Snippet"
        };
        btnDelete.Click += (s, e) =>
        {
            e.Handled = true;
            _snippets.Remove(item);
            SaveSnippets();
            RenderSnippetList();
        };

        actionPanel.Children.Add(btnDelete);
        Grid.SetColumn(actionPanel, 1);
        grid.Children.Add(actionPanel);

        card.Child = grid;

        // Klik Kartu -> Menyalin Isi Teks ke Windows Clipboard
        card.MouseDown += (s, e) =>
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                CopyToClipboard(item.Text, item.Label);
            }
        };

        return card;
    }

    // ---------- CLIPBOARD COPY & TOAST FEEDBACK ----------
    private void CopyToClipboard(string text, string label)
    {
        try
        {
            Clipboard.SetText(text);
            ShowToast($"• Copied [{label}]!");
        }
        catch (Exception)
        {
            ShowToast("• Failed to copy!");
        }
    }

    private void ShowToast(string message)
    {
        ToastText.Text = message;

        var fadeInAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150));
        var fadeOutAnim = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200))
        {
            BeginTime = TimeSpan.FromMilliseconds(1100)
        };

        ToastBorder.BeginAnimation(OpacityProperty, fadeInAnim);

        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1100) };
        timer.Tick += (s, e) =>
        {
            timer.Stop();
            ToastBorder.BeginAnimation(OpacityProperty, fadeOutAnim);
        };
        timer.Start();
    }

    // ---------- MODAL OVERLAY LOGIC ----------
    private void BtnAddSnippet_Click(object sender, RoutedEventArgs e)
    {
        SetPlaceholderText();
        SnippetOverlay.Visibility = Visibility.Visible;
        TxtSnippetLabel.Focus();
    }

    private void BtnCancelOverlay_Click(object sender, RoutedEventArgs e)
    {
        CloseOverlay();
    }

    private void BtnSaveSnippet_Click(object sender, RoutedEventArgs e)
    {
        string label = TxtSnippetLabel.Text.Trim();
        string content = TxtSnippetContent.Text.Trim();

        if (string.IsNullOrEmpty(label) || label == "Label Snippet (misal: Bank BCA)" ||
            string.IsNullOrEmpty(content) || content == "Isi teks yang ingin disalin...")
        {
            return;
        }

        _snippets.Insert(0, new SnippetItem { Label = label, Text = content });
        SaveSnippets();
        RenderSnippetList();
        CloseOverlay();
    }

    private void CloseOverlay()
    {
        SnippetOverlay.Visibility = Visibility.Collapsed;
    }

    // ---------- PLACEHOLDER HELPER ----------
    private void SetPlaceholderText()
    {
        TxtSnippetLabel.Text = "Label Snippet (misal: Bank BCA)";
        TxtSnippetLabel.Foreground = new SolidColorBrush(Color.FromRgb(30, 58, 96));

        TxtSnippetContent.Text = "Isi teks yang ingin disalin...";
        TxtSnippetContent.Foreground = new SolidColorBrush(Color.FromRgb(30, 58, 96));
    }

    private void Input_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb)
        {
            if (tb.Text == "Label Snippet (misal: Bank BCA)" || tb.Text == "Isi teks yang ingin disalin...")
            {
                tb.Text = "";
                tb.Foreground = tb == TxtSnippetLabel 
                    ? new SolidColorBrush(Color.FromRgb(80, 200, 255)) 
                    : Brushes.White;
            }
        }
    }

    private void Input_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb && string.IsNullOrWhiteSpace(tb.Text))
        {
            if (tb == TxtSnippetLabel)
            {
                tb.Text = "Label Snippet (misal: Bank BCA)";
                tb.Foreground = new SolidColorBrush(Color.FromRgb(30, 58, 96));
            }
            else if (tb == TxtSnippetContent)
            {
                tb.Text = "Isi teks yang ingin disalin...";
                tb.Foreground = new SolidColorBrush(Color.FromRgb(30, 58, 96));
            }
        }
    }
}