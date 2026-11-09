namespace AudioPlaylistPlayer.Core.Models;

public static class PlaybackStatusNames
{
    public static string Display(PlaybackStatus status) => status switch
    {
        PlaybackStatus.Playing => "играет",
        PlaybackStatus.Paused => "пауза",
        _ => "остановлен"
    };
}
