namespace AudioPlaylistPlayer.Core.Help;

public static class HelpText
{
    public const string Text =
        """
        Проигрыватель аудиофайлов с редактором плейлистов

        Приложение хранит плейлисты, позицию и настройки. Сами аудиофайлы не копируются:
        в плейлисте записан путь к файлу (на Android — content URI).

        Графическое окно
          Пробел          воспроизведение или пауза
          Влево / вправо  предыдущий или следующий трек
          Вверх / вниз    громкость
          F1              эта справка
          Ctrl+S          сохранить состояние в файл
          Ctrl+O          загрузить состояние из файла

        Экраны: Плеер, Плейлисты, Настройки, Справка.
        Режимы: по порядку, повтор одного, повтор всех, перемешивание.
        Тема: как в системе, светлая, тёмная.

        Командная строка (AudioPlaylistPlayer.Console)
          --help
          playlist list
          playlist create <название>
          playlist delete <название>
          playlist rename <название> <новое-название>
          playlist add <название> <путь-к-файлу>
          playlist remove <название> <номер-с-единицы>
          mode <sequential|repeat-one|repeat-all|shuffle>
          play
          pause
          next
          prev
          state save <файл.json>
          state load <файл.json>

        Рабочий файл консоли — player-state.json в текущей папке.
        state load читает указанный файл и копирует его в рабочий.
        Команда без аргументов показывает эту справку.
        """;
}
