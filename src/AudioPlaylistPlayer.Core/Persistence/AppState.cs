using AudioPlaylistPlayer.Core.Models;
using AudioPlaylistPlayer.Core.Playback; // <-- ДОБАВЛЕНО ЭТО


namespace AudioPlaylistPlayer.Core.Persistence;

public sealed class AppState
{
    public MusicLibrary MusicLibrary { get; set; } = new();
    public PlaylistLibrary PlaylistLibrary { get; set; } = new();
    public AppSettings Settings { get; set; } = new();
    public SessionSnapshot Session { get; set; } = new();

    // Для обратной совместимости со старым кодом, который ожидает свойство Library
    public PlaylistLibrary Library => PlaylistLibrary; 
}