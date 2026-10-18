using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace NusaNotch;

public partial class MediaModule : UserControl, INotchModule
{
    private readonly SmtcService _smtc = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(500) };

    public string Id => "Media";
    public UIElement View => this;

    public MediaModule()
    {
        InitializeComponent();

        _smtc.MediaPropertiesChanged += () => Dispatcher.Invoke(UpdateMediaUI);
        _smtc.TimelinePropertiesChanged += () => Dispatcher.Invoke(UpdateTimelineUI);

        _timer.Tick += (_, _) => UpdateTimelineUI();
    }

    public void OnOpened()
    {
        _smtc.Start();
        _timer.Start();
        UpdateMediaUI();
    }

    public void OnClosed()
    {
        _timer.Stop();
    }

    private void UpdateMediaUI()
    {
        TxtTitle.Text = string.IsNullOrEmpty(_smtc.Title) ? "Tidak Ada Media" : _smtc.Title;
        TxtArtist.Text = string.IsNullOrEmpty(_smtc.Artist) ? "-" : _smtc.Artist;
        LyricsTxtTitle.Text = TxtTitle.Text;

        if (_smtc.ThumbnailStream != null)
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = _smtc.ThumbnailStream;
                bmp.EndInit();
                bmp.Freeze();

                ImgCover.Source = bmp;
                LyricsSmallCover.Source = bmp;
                TxtNoArt.Visibility = Visibility.Collapsed;
            }
            catch
            {
                ImgCover.Source = null;
                LyricsSmallCover.Source = null;
                TxtNoArt.Visibility = Visibility.Visible;
            }
        }
        else
        {
            ImgCover.Source = null;
            LyricsSmallCover.Source = null;
            TxtNoArt.Visibility = Visibility.Visible;
        }

        BtnPlayPause.Content = _smtc.IsPlaying ? "⏸" : "▶";
    }

    private void UpdateTimelineUI()
    {
        if (_smtc.EndTime.TotalSeconds > 0)
        {
            ProgMedia.Value = (_smtc.Position.TotalSeconds / _smtc.EndTime.TotalSeconds) * 100;
        }
        else
        {
            ProgMedia.Value = 0;
        }
    }

    private async void BtnPlayPause_Click(object sender, RoutedEventArgs e) => await _smtc.TogglePlayPauseAsync();
    private async void BtnPrev_Click(object sender, RoutedEventArgs e) => await _smtc.SkipPreviousAsync();
    private async void BtnNext_Click(object sender, RoutedEventArgs e) => await _smtc.SkipNextAsync();

    private void BtnLyrics_Click(object sender, RoutedEventArgs e)
    {
        LyricsOverlay.Visibility = Visibility.Visible;
        TxtNoLyrics.Visibility = Visibility.Visible;
        TxtLyrics.Text = string.Empty;
    }

    private void BtnCloseLyrics_Click(object sender, RoutedEventArgs e)
    {
        LyricsOverlay.Visibility = Visibility.Collapsed;
    }
}