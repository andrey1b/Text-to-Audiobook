using System.IO;
using System.Windows;
using System.Windows.Media;
using Newtonsoft.Json;

namespace TextToAudiobookCSharp;

public enum AppTheme { Light, Dark }
public enum AppLang { Ru, En }

// ── Сохраняемые настройки ────────────────────────────────────────────

public class AppSettings
{
    public AppTheme Theme { get; set; } = AppTheme.Light;
    public AppLang Language { get; set; } = AppLang.Ru;

    private static string SettingsPath
    {
        get
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TextToAudiobook");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "settings.json");
        }
    }

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
                return JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(SettingsPath))
                       ?? new AppSettings();
        }
        catch { }
        return new AppSettings();
    }

    public void Save()
    {
        try { File.WriteAllText(SettingsPath, JsonConvert.SerializeObject(this, Formatting.Indented)); }
        catch { }
    }
}

// ── Менеджер тем (две палитры) ───────────────────────────────────────

public static class ThemeManager
{
    public static AppTheme Current { get; private set; } = AppTheme.Light;

    // Светлая тема (стиль GardenPlanner)
    private static readonly Dictionary<string, string> Light = new()
    {
        ["AccentBrush"] = "#2C5F2D",
        ["AccentHoverBrush"] = "#234C24",
        ["SuccessBrush"] = "#3E8E41",
        ["WarningBrush"] = "#B8860B",
        ["DangerBrush"] = "#C0392B",
        ["BgDarkBrush"] = "#D4E8B8",
        ["BgCardBrush"] = "#E8F2D6",
        ["BgInputBrush"] = "#FFFFFF",
        ["BorderLightBrush"] = "#BCD79B",
        ["TextPrimaryBrush"] = "#2B2B2B",
        ["TextSecondaryBrush"] = "#4F5A41",
        ["LogBgBrush"] = "#FFFFFF",
        ["LogFgBrush"] = "#2C5F2D",
        ["ResumeBgBrush"] = "#E8F4EC",
        ["SplitterBrush"] = "#C9D9BC",
        ["DropDownBgBrush"] = "#FFFFFF",
        ["HeaderTextBrush"] = "#FFFFFF",
    };

    // Тёмная тема (палитра версии 14.0)
    private static readonly Dictionary<string, string> Dark = new()
    {
        ["AccentBrush"] = "#6C63FF",
        ["AccentHoverBrush"] = "#5A52D5",
        ["SuccessBrush"] = "#2ECC71",
        ["WarningBrush"] = "#E67E22",
        ["DangerBrush"] = "#E74C3C",
        ["BgDarkBrush"] = "#1A1A2E",
        ["BgCardBrush"] = "#16213E",
        ["BgInputBrush"] = "#0F3460",
        ["BorderLightBrush"] = "#3A3A55",
        ["TextPrimaryBrush"] = "#EAEAEA",
        ["TextSecondaryBrush"] = "#A0A0B8",
        ["LogBgBrush"] = "#0D1B2A",
        ["LogFgBrush"] = "#89CFF0",
        ["ResumeBgBrush"] = "#1B2838",
        ["SplitterBrush"] = "#2A2A40",
        ["DropDownBgBrush"] = "#1E2A45",
        ["HeaderTextBrush"] = "#FFFFFF",
    };

    public static void Apply(AppTheme theme)
    {
        Current = theme;
        var palette = theme == AppTheme.Dark ? Dark : Light;
        var res = Application.Current.Resources;
        foreach (var (key, hex) in palette)
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            res[key] = new SolidColorBrush(color);
        }
    }
}

// ── Локализатор (ru / en) ────────────────────────────────────────────

public static class Localizer
{
    public static AppLang Current { get; private set; } = AppLang.Ru;

    private static readonly Dictionary<string, string> Ru = new()
    {
        ["Loc_Update"] = "Проверить обновления",
        ["Loc_Settings"] = "Настройки",
        ["Loc_SourceFile"] = "Исходный файл",
        ["Loc_OutputFolder"] = "Папка для сохранения",
        ["Loc_RecentBooks"] = "Недавние книги",
        ["Loc_Browse"] = "Обзор",
        ["Loc_Folder"] = "Папка",
        ["Loc_Mode"] = "Режим",
        ["Loc_Voice"] = "Голос",
        ["Loc_Speed"] = "Скорость",
        ["Loc_Encoding"] = "Кодировка",
        ["Loc_Merge"] = "Объединить в один файл",
        ["Loc_NoChapters"] = "Не разбивать на главы",
        ["Loc_DeleteFragments"] = "Удалить фрагменты после объединения",
        ["Loc_OpenFolder"] = "Открыть папку",
        ["Loc_Pause"] = "Пауза",
        ["Loc_Continue"] = "Продолжить",
        ["Loc_Stop"] = "Стоп",
        ["Loc_Start"] = "Начать конвертацию",
        ["Loc_Progress"] = "Прогресс",
        ["Loc_LogFont"] = "Шрифт лога",
        ["Loc_Size"] = "Размер",
        // Диалог настроек
        ["Loc_SettingsTitle"] = "Настройки",
        ["Loc_Language"] = "Язык",
        ["Loc_Russian"] = "Русский",
        ["Loc_English"] = "Английский",
        ["Loc_Theme"] = "Оформление",
        ["Loc_ThemeLight"] = "Светлый",
        ["Loc_ThemeDark"] = "Тёмный",
        ["Loc_Close"] = "Закрыть",
        // Режимы TTS
        ["Loc_ModeAuto"] = "Авто (онлайн → Piper → SAPI)",
        ["Loc_ModeOnline"] = "Онлайн (Edge TTS)",
        ["Loc_ModePiper"] = "Piper TTS (нейросеть, оффлайн)",
        ["Loc_ModeOffline"] = "Оффлайн (Windows SAPI)",
        // Статусы режима
        ["Loc_StatusAuto"] = "Автовыбор при старте",
        ["Loc_StatusOnline"] = "Требуется интернет",
        ["Loc_StatusPiperFound"] = "Piper моделей: {0}. Нейросетевое качество, без интернета.",
        ["Loc_StatusPiperNone"] = "Piper не найден! Поместите piper.exe и модели (.onnx) в папку 'piper'.",
        ["Loc_StatusSapiFound"] = "Найдено SAPI-голосов: {0}",
        ["Loc_StatusSapiNone"] = "SAPI-голоса не найдены!",
        // Сообщения / диалоги
        ["Loc_Warning"] = "Внимание",
        ["Loc_Error"] = "Ошибка",
        ["Loc_SelectSource"] = "Выберите исходный файл.",
        ["Loc_FileNotFound"] = "Файл не найден:\n{0}",
        ["Loc_SpecifyOutput"] = "Укажите папку для сохранения.",
        ["Loc_SelectFileFirst"] = "Сначала выберите файл книги.",
        ["Loc_FolderNotReady"] = "Папка ещё не выбрана или не создана.",
        ["Loc_FolderNotFound"] = "Папка не найдена.",
        ["Loc_ChooseTextFile"] = "Выберите текстовый файл",
        ["Loc_TextFilesFilter"] = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
        ["Loc_ChooseFolder"] = "Выберите папку для сохранения",
        // Обновления
        ["Loc_Checking"] = "Проверяю...",
        ["Loc_UpdateAvailTitle"] = "Доступно обновление",
        ["Loc_UpdateAvailMsg"] = "Доступна новая версия: {0}\nУ вас установлена: {1}\n\nСкачать обновление сейчас?",
        ["Loc_NoUpdateTitle"] = "Обновлений нет",
        ["Loc_NoUpdateMsg"] = "У вас установлена последняя версия ({0}).",
        ["Loc_UpdateErrTitle"] = "Ошибка проверки",
        ["Loc_UpdateErrMsg"] = "Не удалось проверить обновления.\nПроверьте подключение к интернету.\n\n{0}",
        // Диалог завершения
        ["Loc_CompletedTitle"] = "Конвертация завершена",
        ["Loc_CompletedHeader"] = "✔  Конвертация завершена!",
        ["Loc_Book"] = "Книга:",
        ["Loc_FilesCreated"] = "Файлов создано:",
        ["Loc_TotalSize"] = "Общий размер:",
        ["Loc_FinalFile"] = "Итоговый файл:",
        ["Loc_OpenAudio"] = "▶  Открыть аудиофайл",
        ["Loc_OpenFolderBtn"] = "📁  Открыть папку",
        // Прочее
        ["Loc_Ready"] = "готово",
        ["Loc_MB"] = "МБ",
        ["Loc_ResumeFmt"] = "Продолжить: {0}  ({1}/{2} — {3}%)",
        ["Loc_StoppedByUser"] = "\nОстановлено пользователем.",
        ["Loc_ErrorLogFmt"] = "\nОшибка: {0}",
    };

    private static readonly Dictionary<string, string> En = new()
    {
        ["Loc_Update"] = "Check for updates",
        ["Loc_Settings"] = "Settings",
        ["Loc_SourceFile"] = "Source file",
        ["Loc_OutputFolder"] = "Output folder",
        ["Loc_RecentBooks"] = "Recent books",
        ["Loc_Browse"] = "Browse",
        ["Loc_Folder"] = "Folder",
        ["Loc_Mode"] = "Mode",
        ["Loc_Voice"] = "Voice",
        ["Loc_Speed"] = "Speed",
        ["Loc_Encoding"] = "Encoding",
        ["Loc_Merge"] = "Merge into one file",
        ["Loc_NoChapters"] = "Don't split into chapters",
        ["Loc_DeleteFragments"] = "Delete fragments after merging",
        ["Loc_OpenFolder"] = "Open folder",
        ["Loc_Pause"] = "Pause",
        ["Loc_Continue"] = "Resume",
        ["Loc_Stop"] = "Stop",
        ["Loc_Start"] = "Start conversion",
        ["Loc_Progress"] = "Progress",
        ["Loc_LogFont"] = "Log font",
        ["Loc_Size"] = "Size",
        ["Loc_SettingsTitle"] = "Settings",
        ["Loc_Language"] = "Language",
        ["Loc_Russian"] = "Russian",
        ["Loc_English"] = "English",
        ["Loc_Theme"] = "Appearance",
        ["Loc_ThemeLight"] = "Light",
        ["Loc_ThemeDark"] = "Dark",
        ["Loc_Close"] = "Close",
        ["Loc_ModeAuto"] = "Auto (online → Piper → SAPI)",
        ["Loc_ModeOnline"] = "Online (Edge TTS)",
        ["Loc_ModePiper"] = "Piper TTS (neural, offline)",
        ["Loc_ModeOffline"] = "Offline (Windows SAPI)",
        ["Loc_StatusAuto"] = "Auto-select on start",
        ["Loc_StatusOnline"] = "Internet required",
        ["Loc_StatusPiperFound"] = "Piper models: {0}. Neural quality, no internet.",
        ["Loc_StatusPiperNone"] = "Piper not found! Put piper.exe and models (.onnx) into the 'piper' folder.",
        ["Loc_StatusSapiFound"] = "SAPI voices found: {0}",
        ["Loc_StatusSapiNone"] = "No SAPI voices found!",
        ["Loc_Warning"] = "Warning",
        ["Loc_Error"] = "Error",
        ["Loc_SelectSource"] = "Select a source file.",
        ["Loc_FileNotFound"] = "File not found:\n{0}",
        ["Loc_SpecifyOutput"] = "Specify an output folder.",
        ["Loc_SelectFileFirst"] = "Select the book file first.",
        ["Loc_FolderNotReady"] = "The folder is not selected or not created yet.",
        ["Loc_FolderNotFound"] = "Folder not found.",
        ["Loc_ChooseTextFile"] = "Choose a text file",
        ["Loc_TextFilesFilter"] = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
        ["Loc_ChooseFolder"] = "Choose an output folder",
        ["Loc_Checking"] = "Checking...",
        ["Loc_UpdateAvailTitle"] = "Update available",
        ["Loc_UpdateAvailMsg"] = "A new version is available: {0}\nInstalled: {1}\n\nDownload the update now?",
        ["Loc_NoUpdateTitle"] = "No updates",
        ["Loc_NoUpdateMsg"] = "You have the latest version ({0}).",
        ["Loc_UpdateErrTitle"] = "Update check failed",
        ["Loc_UpdateErrMsg"] = "Could not check for updates.\nCheck your internet connection.\n\n{0}",
        ["Loc_CompletedTitle"] = "Conversion complete",
        ["Loc_CompletedHeader"] = "✔  Conversion complete!",
        ["Loc_Book"] = "Book:",
        ["Loc_FilesCreated"] = "Files created:",
        ["Loc_TotalSize"] = "Total size:",
        ["Loc_FinalFile"] = "Final file:",
        ["Loc_OpenAudio"] = "▶  Open audio file",
        ["Loc_OpenFolderBtn"] = "📁  Open folder",
        ["Loc_Ready"] = "done",
        ["Loc_MB"] = "MB",
        ["Loc_ResumeFmt"] = "Resume: {0}  ({1}/{2} — {3}%)",
        ["Loc_StoppedByUser"] = "\nStopped by user.",
        ["Loc_ErrorLogFmt"] = "\nError: {0}",
    };

    /// <summary>Возвращает строку по ключу для текущего языка.</summary>
    public static string L(string key)
    {
        var dict = Current == AppLang.En ? En : Ru;
        return dict.TryGetValue(key, out var s) ? s : key;
    }

    /// <summary>Применяет язык: кладёт все строки в ресурсы приложения (для DynamicResource).</summary>
    public static void Apply(AppLang lang)
    {
        Current = lang;
        var dict = lang == AppLang.En ? En : Ru;
        var res = Application.Current.Resources;
        foreach (var (key, value) in dict)
            res[key] = value;
    }
}
