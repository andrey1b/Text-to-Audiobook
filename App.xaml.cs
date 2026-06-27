using System.Windows;

namespace TextToAudiobookCSharp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static AppSettings Settings { get; private set; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Загружаем сохранённые настройки и применяем тему + язык до показа окна
        Settings = AppSettings.Load();
        ThemeManager.Apply(Settings.Theme);
        Localizer.Apply(Settings.Language);

        var window = new MainWindow();
        window.Show();
    }
}
