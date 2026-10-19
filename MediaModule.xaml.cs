using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Pnotch;

public partial class MediaModule : UserControl, INotchModule
{
    private readonly SmtcService _smtc = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(500) };

    private TimeSpan _basePosition = TimeSpan.Zero;
    private TimeSpan _knownEndTime = TimeSpan.Zero;
    private DateTime _lastSyncTime = DateTime.MinValue;

    public string Id => "Media";
    public UIElement View => this;

    public MediaModule()
    {
        InitializeComponent();

        _smtc.MediaPropertiesChanged += () => Dispatcher.Invoke(OnMediaPropertiesChanged);
        _smtc.TimelinePropertiesChanged += () => Dispatcher.Invoke(OnTimelinePropertiesChanged);

        _timer.Tick += (_, _) => UpdateTimelineUI();

        // Cukup jalankan SMTC 1x di sini saat modul dibuat
        _smtc.Start();
        _timer.Start();
    }

    public void OnOpened()
    {
        // Jangan panggil _smtc.Start() lagi di sini
        UpdateMediaUI();
        UpdateTimelineUI();
    }

    public void OnClosed()
    {
        // Biarkan timer tetap berjalan di background agar perhitungan detik posisi tidak terhenti
    }

    private void OnMediaPropertiesChanged()
    {
        UpdateMediaUI();
        
        // Reset timestamp saat ganti lagu
        _basePosition = _smtc.Position;
        _lastSyncTime = DateTime.Now;
        UpdateTimelineUI();
    }

    private void OnTimelinePropertiesChanged()
    {
        // Abaikan sinyal '0' dari Windows jika lagu sedang berjalan dan kita sudah punya posisi valid
        if (_smtc.Position > TimeSpan.Zero || !_smtc.IsPlaying)
        {
            _basePosition = _smtc.Position;
            _lastSyncTime = DateTime.Now;
        }

        if (_smtc.EndTime > TimeSpan.Zero)
        {
            _knownEndTime = _smtc.EndTime;
        }

        UpdateTimelineUI();
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
                bmp.DecodePixelWidth = 96;
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
        TxtHeaderDate.Text = DateTime.Now.ToString("ddd dd MMM yyyy HH:mm");

        TimeSpan endTime = _smtc.EndTime > TimeSpan.Zero ? _smtc.EndTime : _knownEndTime;
        TimeSpan currentPos = _basePosition;

        // Hitung estimasi real-time jika media sedang berputar
        if (_smtc.IsPlaying && _lastSyncTime != DateTime.MinValue)
        {
            var elapsed = DateTime.Now - _lastSyncTime;
            currentPos = _basePosition + elapsed;
        }

        if (endTime.TotalSeconds > 0)
        {
            if (currentPos > endTime) currentPos = endTime;
            if (currentPos < TimeSpan.Zero) currentPos = TimeSpan.Zero;

            ProgMedia.Value = (currentPos.TotalSeconds / endTime.TotalSeconds) * 100;
            TxtPosition.Text = currentPos.ToString(@"mm\:ss");

            var remaining = endTime - currentPos;
            TxtRemaining.Text = remaining.TotalSeconds > 0 
                ? $"-{remaining:mm\\:ss}" 
                : "-00:00";
        }
        else
        {
            ProgMedia.Value = 0;
            TxtPosition.Text = currentPos > TimeSpan.Zero ? currentPos.ToString(@"mm\:ss") : "00:00";
            TxtRemaining.Text = "-00:00";
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