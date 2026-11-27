using AudioPlaylistPlayer.Core.Models;
using AudioPlaylistPlayer.Core.Playback;
using Plugin.Maui.Audio;

namespace AudioPlaylistPlayer.Mobile.Services;

public sealed class MauiAudioOutput : IAudioOutput
{
    private readonly IAudioManager _audioManager;
    private IAudioPlayer? _player;
    private double _currentVolume = 1.0;

    public event EventHandler? PlaybackCompleted;

    public double PositionSeconds => _player?.CurrentPosition ?? 0;
    public double? DurationSeconds => _player?.Duration;

    public MauiAudioOutput(IAudioManager audioManager)
    {
        _audioManager = audioManager;
    }

    public async Task PlayAsync(Track track, double startPosition, double volume, CancellationToken cancellationToken)
    {
        Stop();

        var fileSource = track.Source as FileTrackSource;
        if (fileSource == null)
        {
            throw new InvalidOperationException("Поддерживаются только файловые источники (FileTrackSource).");
        }

        var filePath = fileSource.Location;

        // Защита от падения: если файл был удалён из кэша (например, ОС очистила кэш),
        // воспроизводим из резервной копии или показываем понятную ошибку вместо краша.
        if (!File.Exists(filePath))
        {
            var backupPath = GetBackupPath(filePath);
            if (File.Exists(backupPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                File.Copy(backupPath, filePath, overwrite: true);
            }
            else
            {
                throw new FileNotFoundException($"Файл трека не найден: {Path.GetFileName(filePath)}. Добавьте трек заново.");
            }
        }

        FileStream fileStream;
        try
        {
            fileStream = File.OpenRead(filePath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Не удалось открыть файл «{Path.GetFileName(filePath)}»: {ex.Message}", ex);
        }

        _player = _audioManager.CreatePlayer(fileStream);
        
        _currentVolume = volume;
        _player.Volume = (float)volume;

        _player.PlaybackEnded += OnPlaybackEnded;

        if (startPosition > 0)
        {
            _player.Seek(startPosition);
        }

        _player.Play();
        
        System.Diagnostics.Debug.WriteLine($"[AUDIO] Play() вызван. Duration: {_player.Duration}, Position: {_player.CurrentPosition}");
        
        await Task.CompletedTask;
    }

    public void Pause()
    {
        _player?.Pause();
    }

    public void Resume()
    {
        _player?.Play();
    }

    public void Stop()
    {
        if (_player != null)
        {
            _player.PlaybackEnded -= OnPlaybackEnded;
            _player.Stop();
            _player.Dispose();
            _player = null;
        }
    }

    public void Seek(double seconds)
    {
        if (_player != null)
        {
            _player.Seek(Math.Max(0, seconds));
        }
    }

    public void SetVolume(double volume)
    {
        _currentVolume = volume;
        if (_player != null)
        {
            _player.Volume = (float)volume;
        }
    }

    private void OnPlaybackEnded(object? sender, EventArgs e)
    {
        PlaybackCompleted?.Invoke(this, EventArgs.Empty);
    }

    public static string GetBackupPath(string cacheFilePath)
    {
        var backupDir = Path.Combine(FileSystem.AppDataDirectory, "TrackBackup");
        return Path.Combine(backupDir, Path.GetFileName(cacheFilePath));
    }
}