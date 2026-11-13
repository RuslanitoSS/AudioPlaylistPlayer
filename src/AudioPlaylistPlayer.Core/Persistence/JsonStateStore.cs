using System.Text.Json;
using AudioPlaylistPlayer.Core.Models;
using AudioPlaylistPlayer.Core.Playback;

namespace AudioPlaylistPlayer.Core.Persistence;

public sealed class JsonStateStore
{

    public const int CurrentVersion = 2;
    private static readonly JsonSerializerOptions _jsonOptions = new() 
    { 
        WriteIndented = true,
        IncludeFields = true // Важно для записи private полей, если они есть
    };

    public AppState Load(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var state = JsonSerializer.Deserialize<AppState>(json, _jsonOptions) ?? new AppState();
        
        // Гарантируем, что объекты не null
        state.MusicLibrary ??= new MusicLibrary();
        state.PlaylistLibrary ??= new PlaylistLibrary();
        state.Settings ??= new AppSettings();
        
        return state;
    }

    public void Save(string filePath, AppState state, PlayerController player)
    {
        // Обновляем сессию перед сохранением
        state.Session = player.CaptureSession();
        
        var json = JsonSerializer.Serialize(state, _jsonOptions);
        File.WriteAllText(filePath, json);
    }
}