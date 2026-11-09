using AudioPlaylistPlayer.Core.Models;

namespace AudioPlaylistPlayer.Core.Playback;

public sealed class PlayerController
{
    private readonly IAudioOutput _output;
    private double _position;
    private Guid? _currentTrackId;
    private bool _trackLoaded;

    public PlayerController(PlaylistLibrary library, AppSettings settings, IAudioOutput output)
    {
        Library = library;
        Settings = settings;
        _output = output;
        Volume = Math.Clamp(settings.Volume, 0, 1);
        Mode = settings.DefaultPlaybackMode;
        _output.SetVolume(Volume);
        _output.PlaybackCompleted += OnPlaybackCompleted;
        Library.PlaylistRemoved += OnPlaylistRemoved;
    }

    public PlaylistLibrary Library { get; }

    public AppSettings Settings { get; }

    public Playlist? CurrentPlaylist { get; private set; }

    public int CurrentIndex { get; private set; } = -1;

    public PlaybackStatus Status { get; private set; } = PlaybackStatus.Stopped;

    public PlaybackMode Mode { get; private set; }

    public double Volume { get; private set; }

    public IReadOnlyList<int> ShuffleOrder { get; private set; } = [];

    public Track? CurrentTrack =>
        CurrentPlaylist is not null && CurrentIndex >= 0 && CurrentIndex < CurrentPlaylist.Tracks.Count
            ? CurrentPlaylist.Tracks[CurrentIndex]
            : null;

    public double PositionSeconds => Status == PlaybackStatus.Playing ? _output.PositionSeconds : _position;

    public double? DurationSeconds => _output.DurationSeconds;

    public bool ShouldResumePlayback { get; private set; }

    public event EventHandler? Changed;

    public void SelectPlaylist(Playlist playlist, int index = 0)
    {
        ArgumentNullException.ThrowIfNull(playlist);
        Attach(playlist);
        CurrentIndex = playlist.Tracks.Count == 0
            ? -1
            : Math.Clamp(index, 0, playlist.Tracks.Count - 1);
        RememberTrack();
        _position = 0;
        _trackLoaded = false;
        ShouldResumePlayback = false;
        RebuildShuffle();
        Status = PlaybackStatus.Stopped;
        _output.Stop();
        Notify();
    }

    public async Task PlayAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentTrack is null)
        {
            var playlist = CurrentPlaylist ?? Library.Playlists.FirstOrDefault();
            if (playlist is null)
                return;

            if (!ReferenceEquals(playlist, CurrentPlaylist))
                SelectPlaylist(playlist);

            if (CurrentTrack is null && playlist.Tracks.Count > 0)
            {
                CurrentIndex = Mode == PlaybackMode.Shuffle && ShuffleOrder.Count > 0
                    ? ShuffleOrder[0]
                    : 0;
                RememberTrack();
            }
        }

        if (CurrentTrack is null)
            return;

        ShouldResumePlayback = false;
        if (Status == PlaybackStatus.Paused && _trackLoaded)
        {
            _output.Resume();
            Status = PlaybackStatus.Playing;
            Notify();
            return;
        }

        await _output.PlayAsync(CurrentTrack, _position, Volume, cancellationToken);
        _trackLoaded = true;
        Status = PlaybackStatus.Playing;
        Notify();
    }

    public void Pause()
    {
        if (ShouldResumePlayback)
        {
            ShouldResumePlayback = false;
            Status = PlaybackStatus.Paused;
            Notify();
            return;
        }

        if (Status != PlaybackStatus.Playing)
            return;

        _position = _output.PositionSeconds;
        _output.Pause();
        Status = PlaybackStatus.Paused;
        Notify();
    }

    public async Task ToggleAsync(CancellationToken cancellationToken = default)
    {
        if (Status == PlaybackStatus.Playing)
            Pause();
        else
            await PlayAsync(cancellationToken);
    }

    public void Stop()
    {
        _position = 0;
        _trackLoaded = false;
        ShouldResumePlayback = false;
        _output.Stop();
        Status = PlaybackStatus.Stopped;
        Notify();
    }

    public async Task NextAsync(bool automatic, CancellationToken cancellationToken = default)
    {
        if (CurrentPlaylist is null)
            return;

        var next = PlaybackCursor.NextIndex(
            Mode,
            CurrentIndex,
            CurrentPlaylist.Tracks.Count,
            ShuffleOrder,
            automatic);

        if (next < 0)
        {
            Stop();
            return;
        }

        var keepPlaying = Status == PlaybackStatus.Playing || automatic;
        await JumpToAsync(next, keepPlaying, cancellationToken);
    }

    public async Task PreviousAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentPlaylist is null || CurrentPlaylist.Tracks.Count == 0)
            return;

        if (PositionSeconds > 3)
        {
            await SeekAsync(0, cancellationToken);
            return;
        }

        var previous = PlaybackCursor.PreviousIndex(
            Mode,
            CurrentIndex,
            CurrentPlaylist.Tracks.Count,
            ShuffleOrder);
        await JumpToAsync(previous, Status == PlaybackStatus.Playing, cancellationToken);
    }

    public Task SeekAsync(double seconds, CancellationToken cancellationToken = default)
    {
        _position = Math.Max(0, seconds);
        if (Status == PlaybackStatus.Playing)
            _output.Seek(_position);

        Notify();
        return Task.CompletedTask;
    }

    public void SetVolume(double volume)
    {
        Volume = Math.Clamp(volume, 0, 1);
        Settings.Volume = Volume;
        _output.SetVolume(Volume);
        Notify();
    }

    public void SetMode(PlaybackMode mode)
    {
        Mode = mode;
        Settings.DefaultPlaybackMode = mode;
        if (mode == PlaybackMode.Shuffle)
            RebuildShuffle();

        Notify();
    }

    public SessionSnapshot CaptureSession() => new(
        CurrentPlaylist?.Id,
        Math.Max(CurrentIndex, 0),
        PositionSeconds,
        ShouldResumePlayback ? PlaybackStatus.Playing : Status,
        Mode,
        ShuffleOrder.ToArray(),
        Volume);

    public void ApplySession(SessionSnapshot session)
    {
        ArgumentNullException.ThrowIfNull(session);
        Volume = Math.Clamp(session.Volume, 0, 1);
        Settings.Volume = Volume;
        _output.SetVolume(Volume);
        Mode = session.Mode;
        Settings.DefaultPlaybackMode = session.Mode;

        var playlist = session.PlaylistId is Guid id ? Library.FindById(id) : null;
        if (playlist is null)
        {
            Detach();
            CurrentIndex = -1;
            _currentTrackId = null;
            _position = 0;
            _trackLoaded = false;
            ShouldResumePlayback = false;
            ShuffleOrder = [];
            Status = PlaybackStatus.Stopped;
            _output.Stop();
            Notify();
            return;
        }

        Attach(playlist);
        CurrentIndex = playlist.Tracks.Count == 0
            ? -1
            : Math.Clamp(session.TrackIndex, 0, playlist.Tracks.Count - 1);
        RememberTrack();
        _position = Math.Max(0, session.PositionSeconds);
        ShuffleOrder = session.ShuffleOrder.ToArray();
        if (Mode == PlaybackMode.Shuffle && ShuffleOrder.Count != playlist.Tracks.Count)
            RebuildShuffle();

        _output.Stop();
        _trackLoaded = false;
        ShouldResumePlayback = session.Status == PlaybackStatus.Playing;
        Status = session.Status == PlaybackStatus.Stopped
            ? PlaybackStatus.Stopped
            : PlaybackStatus.Paused;
        Notify();
    }

    private async Task JumpToAsync(int index, bool play, CancellationToken cancellationToken)
    {
        CurrentIndex = index;
        RememberTrack();
        _position = 0;
        _trackLoaded = false;
        ShouldResumePlayback = false;
        Status = PlaybackStatus.Stopped;
        _output.Stop();
        Notify();
        if (play)
            await PlayAsync(cancellationToken);
    }

    private void OnPlaybackCompleted(object? sender, EventArgs e)
    {
        _ = NextAsync(automatic: true);
    }

    private void OnPlaylistRemoved(object? sender, Playlist playlist)
    {
        if (CurrentPlaylist?.Id != playlist.Id)
            return;

        Detach();
        CurrentIndex = -1;
        _currentTrackId = null;
        _position = 0;
        _trackLoaded = false;
        ShouldResumePlayback = false;
        ShuffleOrder = [];
        Status = PlaybackStatus.Stopped;
        _output.Stop();
        Notify();
    }

    private void Attach(Playlist playlist)
    {
        if (ReferenceEquals(CurrentPlaylist, playlist))
            return;

        Detach();
        CurrentPlaylist = playlist;
        playlist.TrackAdded += OnTrackAdded;
        playlist.TrackRemoved += OnTrackRemoved;
        playlist.TrackMoved += OnTrackMoved;
    }

    private void Detach()
    {
        if (CurrentPlaylist is null)
            return;

        CurrentPlaylist.TrackAdded -= OnTrackAdded;
        CurrentPlaylist.TrackRemoved -= OnTrackRemoved;
        CurrentPlaylist.TrackMoved -= OnTrackMoved;
        CurrentPlaylist = null;
    }

    private void OnTrackAdded(object? sender, TrackEventArgs e)
    {
        if (Mode == PlaybackMode.Shuffle)
            RebuildShuffle();

        Notify();
    }

    private void OnTrackRemoved(object? sender, TrackEventArgs e)
    {
        if (e.Track.Id == _currentTrackId)
        {
            _position = 0;
            _trackLoaded = false;
            ShouldResumePlayback = false;
            Status = PlaybackStatus.Stopped;
            _output.Stop();
            if (CurrentPlaylist is { Tracks.Count: > 0 })
            {
                CurrentIndex = Math.Min(e.Index, CurrentPlaylist.Tracks.Count - 1);
                RememberTrack();
            }
            else
            {
                CurrentIndex = -1;
                _currentTrackId = null;
            }
        }
        else
        {
            FollowCurrentTrack();
        }

        if (Mode == PlaybackMode.Shuffle)
            RebuildShuffle();

        Notify();
    }

    private void OnTrackMoved(object? sender, TrackMovedEventArgs e)
    {
        FollowCurrentTrack();
        if (Mode == PlaybackMode.Shuffle)
            RebuildShuffle();

        Notify();
    }

    private void FollowCurrentTrack()
    {
        if (CurrentPlaylist is null || _currentTrackId is null)
        {
            CurrentIndex = -1;
            return;
        }

        for (var i = 0; i < CurrentPlaylist.Tracks.Count; i++)
        {
            if (CurrentPlaylist.Tracks[i].Id == _currentTrackId)
            {
                CurrentIndex = i;
                return;
            }
        }

        CurrentIndex = -1;
        _currentTrackId = null;
    }

    private void RememberTrack() => _currentTrackId = CurrentTrack?.Id;

    private void RebuildShuffle()
    {
        var count = CurrentPlaylist?.Tracks.Count ?? 0;
        if (count == 0)
        {
            ShuffleOrder = [];
            return;
        }

        var indices = Enumerable.Range(0, count).ToList();
        for (var i = indices.Count - 1; i > 0; i--)
        {
            var swap = Random.Shared.Next(i + 1);
            (indices[i], indices[swap]) = (indices[swap], indices[i]);
        }

        if (CurrentIndex >= 0)
        {
            indices.Remove(CurrentIndex);
            indices.Insert(0, CurrentIndex);
        }

        ShuffleOrder = indices;
    }

    private void Notify() => Changed?.Invoke(this, EventArgs.Empty);
}
