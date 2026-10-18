using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace NusaNotch;

public class SmtcService
{
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;

    public event Action? MediaPropertiesChanged;
    public event Action? TimelinePropertiesChanged;

    public string Title { get; private set; } = string.Empty;
    public string Artist { get; private set; } = string.Empty;
    public Stream? ThumbnailStream { get; private set; }
    public bool IsPlaying { get; private set; }
    public TimeSpan Position { get; private set; }
    public TimeSpan EndTime { get; private set; }

    public async void Start()
    {
        try
        {
            _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            if (_manager != null)
            {
                _manager.CurrentSessionChanged += Manager_CurrentSessionChanged;
                HookCurrentSession();
            }
        }
        catch { }
    }

    private void Manager_CurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
    {
        HookCurrentSession();
    }

    private void HookCurrentSession()
    {
        if (_currentSession != null)
        {
            _currentSession.MediaPropertiesChanged -= CurrentSession_MediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged -= CurrentSession_PlaybackInfoChanged;
            _currentSession.TimelinePropertiesChanged -= CurrentSession_TimelinePropertiesChanged;
        }

        _currentSession = _manager?.GetCurrentSession();

        if (_currentSession != null)
        {
            _currentSession.MediaPropertiesChanged += CurrentSession_MediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged += CurrentSession_PlaybackInfoChanged;
            _currentSession.TimelinePropertiesChanged += CurrentSession_TimelinePropertiesChanged;
        }

        UpdateMediaProperties();
        UpdatePlaybackInfo();
        UpdateTimelineProperties();
    }

    private async void CurrentSession_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
    {
        await UpdateMediaPropertiesAsync();
    }

    private void CurrentSession_PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
    {
        UpdatePlaybackInfo();
    }

    private void CurrentSession_TimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
    {
        UpdateTimelineProperties();
    }

    private void UpdateMediaProperties()
    {
        _ = UpdateMediaPropertiesAsync();
    }

    private async Task UpdateMediaPropertiesAsync()
    {
        if (_currentSession != null)
        {
            try
            {
                var props = await _currentSession.TryGetMediaPropertiesAsync();
                Title = props?.Title ?? string.Empty;
                Artist = props?.Artist ?? string.Empty;

                if (props?.Thumbnail != null)
                {
                    var streamRef = props.Thumbnail;
                    IRandomAccessStreamWithContentType stream = await streamRef.OpenReadAsync();
                    ThumbnailStream = stream.AsStreamForRead();
                }
                else
                {
                    ThumbnailStream = null;
                }
            }
            catch
            {
                Title = string.Empty;
                Artist = string.Empty;
                ThumbnailStream = null;
            }
        }
        else
        {
            Title = string.Empty;
            Artist = string.Empty;
            ThumbnailStream = null;
        }

        MediaPropertiesChanged?.Invoke();
    }

    private void UpdatePlaybackInfo()
    {
        if (_currentSession != null)
        {
            try
            {
                var info = _currentSession.GetPlaybackInfo();
                IsPlaying = info?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
            }
            catch
            {
                IsPlaying = false;
            }
        }
        else
        {
            IsPlaying = false;
        }

        MediaPropertiesChanged?.Invoke();
    }

    private void UpdateTimelineProperties()
    {
        if (_currentSession != null)
        {
            try
            {
                var timeline = _currentSession.GetTimelineProperties();
                Position = timeline?.Position ?? TimeSpan.Zero;
                EndTime = timeline?.EndTime ?? TimeSpan.Zero;
            }
            catch
            {
                Position = TimeSpan.Zero;
                EndTime = TimeSpan.Zero;
            }
        }
        else
        {
            Position = TimeSpan.Zero;
            EndTime = TimeSpan.Zero;
        }

        TimelinePropertiesChanged?.Invoke();
    }

    public async Task TogglePlayPauseAsync()
    {
        if (_currentSession != null)
        {
            try
            {
                if (IsPlaying) await _currentSession.TryPauseAsync();
                else await _currentSession.TryPlayAsync();
            }
            catch { }
        }
    }

    public async Task SkipNextAsync()
    {
        if (_currentSession != null)
        {
            try { await _currentSession.TrySkipNextAsync(); } catch { }
        }
    }

    public async Task SkipPreviousAsync()
    {
        if (_currentSession != null)
        {
            try { await _currentSession.TrySkipPreviousAsync(); } catch { }
        }
    }
}