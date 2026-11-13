namespace AudioPlaylistPlayer.Core.Models;

public sealed class Track
{
    public Track(string title, TrackSource source, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Название трека не задано.", nameof(title));
        ArgumentNullException.ThrowIfNull(source);

        Id = id ?? Guid.NewGuid();
        Title = title.Trim();
        Source = source;
    }

    // --- Ключевые поля (неизменяемые) ---
    public Guid Id { get; }
    public string Title { get; }
    public TrackSource Source { get; }

    // --- Метаданные (изменяемые, заполняются при синхронизации с сервером) ---
    
    /// <summary>
    /// Название альбома. Пусто, если не задано.
    /// </summary>
    public string Album { get; set; } = string.Empty;

    /// <summary>
    /// Исполнитель. Пусто, если не задано.
    /// </summary>
    public string Artist { get; set; } = string.Empty;

    /// <summary>
    /// Длительность трека в секундах. 0, если неизвестна.
    /// </summary>
    public double DurationSeconds { get; set; }

    /// <summary>
    /// Путь к файлу обложки альбома. Пусто, если используется заглушка.
    /// </summary>
    public string CoverArtPath { get; set; } = string.Empty;

    /// <summary>
    /// Жанры трека. Пустой список, если не заданы.
    /// </summary>
    public List<string> Genres { get; set; } = new();

    /// <summary>
    /// Пользовательские теги. Пустой список, если не заданы.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Год выпуска. 0, если неизвестен.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Номер трека в альбоме. 0, если не задан.
    /// </summary>
    public int TrackNumber { get; set; }
}