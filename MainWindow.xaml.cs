using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace TextToAudiobookCSharp;

public partial class MainWindow : Window
{
    private readonly string[] _encodings =
        ["auto", "utf-8", "utf-8-sig", "utf-16", "windows-1251", "cp866", "koi8-r", "iso-8859-5", "latin1"];

    private CancellationTokenSource? _cts;
    private PauseToken? _pauseToken;
    private HistoryEntry? _unfinished;
    private List<HistoryEntry> _history = [];
    private TtsMode _selectedTtsMode = TtsMode.Auto;

    // Объединённый список голосов (онлайн + оффлайн + piper)
    private List<(string DisplayName, string ShortName)> _allVoices = [];
    private List<OfflineVoiceInfo> _offlineVoices = [];
    private List<PiperVoiceInfo> _piperVoices = [];

    // Режимы (ключ локализации + режим)
    private static readonly (string LabelKey, TtsMode Mode)[] TtsModes =
    [
        ("Loc_ModeAuto", TtsMode.Auto),
        ("Loc_ModeOnline", TtsMode.Online),
        ("Loc_ModePiper", TtsMode.Piper),
        ("Loc_ModeOffline", TtsMode.Offline),
    ];

    public MainWindow()
    {
        InitializeComponent();
        InitControls();
        LoadHistoryUI();
    }

    // ── Инициализация контролов ──────────────────────────────────────

    private void InitControls()
    {
        // Оффлайн-голоса
        _offlineVoices = OfflineTtsEngine.GetInstalledVoices();
        _piperVoices = PiperTtsEngine.GetAvailableVoices();

        // Режим TTS
        ComboTtsMode.ItemsSource = TtsModes.Select(m => Localizer.L(m.LabelKey)).ToList();
        ComboTtsMode.SelectedIndex = 0;

        // Голоса — онлайн + оффлайн
        UpdateVoiceList(TtsMode.Auto);

        // Кодировки
        ComboEncoding.ItemsSource = _encodings;
        ComboEncoding.SelectedIndex = 0;

        // Шрифт лога — моноширинные шрифты
        var monoFonts = new[] { "Consolas", "Courier New", "Lucida Console", "Cascadia Mono", "Source Code Pro" };
        var availableFonts = Fonts.SystemFontFamilies.Select(f => f.Source).OrderBy(f => f).ToList();
        var logFonts = monoFonts.Where(f => availableFonts.Contains(f)).ToList();
        if (logFonts.Count == 0) logFonts.Add("Consolas");
        ComboLogFont.ItemsSource = logFonts;
        ComboLogFont.SelectedIndex = 0;

        // Размер шрифта лога
        var fontSizes = Enumerable.Range(11, 14).Select(s => s.ToString()).ToList(); // 11..24
        ComboLogFontSize.ItemsSource = fontSizes;
        ComboLogFontSize.SelectedIndex = fontSizes.IndexOf("14"); // default 14
        if (ComboLogFontSize.SelectedIndex < 0) ComboLogFontSize.SelectedIndex = 0;
    }

    private void UpdateVoiceList(TtsMode mode)
    {
        var online = BookConverter.VoicePresets
            .Select(v => (v.DisplayName, v.ShortName)).ToList();
        var offline = _offlineVoices
            .Select(v => (v.DisplayName, v.VoiceId)).ToList();
        var piper = _piperVoices
            .Select(v => (v.DisplayName, v.ModelPath)).ToList();

        _allVoices = mode switch
        {
            TtsMode.Piper => [.. piper, .. online, .. offline],
            TtsMode.Offline => [.. offline, .. piper, .. online],
            _ => [.. online, .. piper, .. offline],
        };

        ComboVoice.ItemsSource = _allVoices.Select(v => v.DisplayName).ToList();
        if (_allVoices.Count > 0)
            ComboVoice.SelectedIndex = 0;
    }

    private void ComboTtsMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        int idx = ComboTtsMode.SelectedIndex;
        if (idx < 0 || idx >= TtsModes.Length) return;
        _selectedTtsMode = TtsModes[idx].Mode;

        UpdateVoiceList(_selectedTtsMode);

        UpdateModeStatus();
    }

    private void UpdateModeStatus()
    {
        if (LblModeStatus == null) return;

        if (_selectedTtsMode == TtsMode.Piper)
        {
            if (_piperVoices.Count > 0)
            {
                LblModeStatus.Text = string.Format(Localizer.L("Loc_StatusPiperFound"), _piperVoices.Count);
                LblModeStatus.Foreground = FindResource("SuccessBrush") as Brush;
            }
            else
            {
                LblModeStatus.Text = Localizer.L("Loc_StatusPiperNone");
                LblModeStatus.Foreground = FindResource("DangerBrush") as Brush;
            }
        }
        else if (_selectedTtsMode == TtsMode.Offline)
        {
            LblModeStatus.Text = _offlineVoices.Count > 0
                ? string.Format(Localizer.L("Loc_StatusSapiFound"), _offlineVoices.Count)
                : Localizer.L("Loc_StatusSapiNone");
            LblModeStatus.Foreground = _offlineVoices.Count > 0
                ? FindResource("SuccessBrush") as Brush
                : FindResource("DangerBrush") as Brush;
        }
        else if (_selectedTtsMode == TtsMode.Online)
        {
            LblModeStatus.Text = Localizer.L("Loc_StatusOnline");
            LblModeStatus.Foreground = FindResource("TextSecondaryBrush") as Brush;
        }
        else
        {
            LblModeStatus.Text = Localizer.L("Loc_StatusAuto");
            LblModeStatus.Foreground = FindResource("TextSecondaryBrush") as Brush;
        }
    }

    // ── Обработчики шрифта лога ────────────────────────────────────────

    private void ComboLogFont_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ComboLogFont.SelectedItem is string fontName && TxtLog != null)
            TxtLog.FontFamily = new FontFamily(fontName);
    }

    private void ComboLogFontSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ComboLogFontSize.SelectedItem is string sizeStr && int.TryParse(sizeStr, out int size) && TxtLog != null)
            TxtLog.FontSize = size;
    }

    // ── Загрузка истории ─────────────────────────────────────────────

    private void LoadHistoryUI()
    {
        _history = HistoryManager.LoadHistory();
        _unfinished = HistoryManager.GetLastUnfinished();

        // Баннер «Продолжить»
        if (_unfinished != null)
        {
            string bookName = _unfinished.BookName ?? Path.GetFileNameWithoutExtension(_unfinished.InputFile ?? "");
            int done = _unfinished.DoneChapters;
            int total = _unfinished.TotalChapters;
            int pct = total > 0 ? done * 100 / total : 0;
            ResumeLabel.Text = string.Format(Localizer.L("Loc_ResumeFmt"), bookName, done, total, pct);
            ResumeCard.Visibility = Visibility.Visible;
        }

        // Список истории
        if (_history.Count > 0)
        {
            HistoryCard.Visibility = Visibility.Visible;
            ComboHistory.ItemsSource = _history.Select(FormatHistoryEntry).ToList();
        }
    }

    private static string FormatHistoryEntry(HistoryEntry e)
    {
        string name = e.BookName ?? "?";
        string status = e.Finished ? Localizer.L("Loc_Ready") : $"{e.DoneChapters}/{e.TotalChapters}";
        return $"{name}  [{status}]  {e.LastUsed}";
    }

    private void LoadProject(HistoryEntry entry)
    {
        TxtInputFile.Text = entry.InputFile ?? "";
        TxtOutputDir.Text = entry.OutputDir ?? "";
        string? voice = entry.Voice;
        if (!string.IsNullOrEmpty(voice))
        {
            for (int i = 0; i < _allVoices.Count; i++)
            {
                if (_allVoices[i].ShortName == voice)
                {
                    ComboVoice.SelectedIndex = i;
                    break;
                }
            }
        }
    }

    // ── Обработчики кнопок ───────────────────────────────────────────

    private void BtnResume_Click(object sender, RoutedEventArgs e)
    {
        if (_unfinished != null) LoadProject(_unfinished);
    }

    private void ComboHistory_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        int idx = ComboHistory.SelectedIndex;
        if (idx >= 0 && idx < _history.Count)
            LoadProject(_history[idx]);
    }

    private void BtnBrowseInput_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = Localizer.L("Loc_ChooseTextFile"),
            Filter = Localizer.L("Loc_TextFilesFilter")
        };
        if (dlg.ShowDialog() == true)
        {
            TxtInputFile.Text = dlg.FileName;
            if (string.IsNullOrEmpty(TxtOutputDir.Text))
            {
                string dir = Path.GetDirectoryName(dlg.FileName) ?? "";
                string name = Path.GetFileNameWithoutExtension(dlg.FileName);
                TxtOutputDir.Text = Path.Combine(dir, name + "_audio");
            }
            // Автоопределение языка и голоса
            AutoSelectVoice(dlg.FileName);
        }
    }

    private void BtnOpenInputFolder_Click(object sender, RoutedEventArgs e)
    {
        string file = TxtInputFile.Text.Trim();
        string? dir = File.Exists(file) ? Path.GetDirectoryName(file) : null;
        if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            Process.Start("explorer.exe", dir);
        else
            MessageBox.Show(Localizer.L("Loc_SelectFileFirst"), Localizer.L("Loc_Warning"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void BtnBrowseOutput_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog { Title = Localizer.L("Loc_ChooseFolder") };
        if (dlg.ShowDialog() == true)
            TxtOutputDir.Text = dlg.FolderName;
    }

    // ── Проверка обновлений (через GitHub Releases) ──────────────────

    private const string UpdateApiUrl =
        "https://api.github.com/repos/andrey1b/Text-to-Audiobook/releases/latest";
    private const string ReleasesPageUrl =
        "https://github.com/andrey1b/Text-to-Audiobook/releases/latest";

    private async void BtnCheckUpdate_Click(object sender, RoutedEventArgs e)
    {
        BtnCheckUpdate.IsEnabled = false;
        string oldText = BtnCheckUpdate.Content as string ?? Localizer.L("Loc_Update");
        BtnCheckUpdate.Content = Localizer.L("Loc_Checking");
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            http.DefaultRequestHeaders.Add("User-Agent", "TextToAudiobook-UpdateCheck");
            string json = await http.GetStringAsync(UpdateApiUrl);
            var release = JObject.Parse(json);

            string tag = (string?)release["tag_name"] ?? "";
            string pageUrl = (string?)release["html_url"] ?? ReleasesPageUrl;

            // Ищем .exe-установщик среди файлов релиза
            string? downloadUrl = null;
            if (release["assets"] is JArray assets)
            {
                foreach (var asset in assets)
                {
                    string name = (string?)asset["name"] ?? "";
                    if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        downloadUrl = (string?)asset["browser_download_url"];
                        break;
                    }
                }
            }

            Version? latest = ParseVersion(tag);
            Version current = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0);

            if (latest != null && latest > current)
            {
                var res = MessageBox.Show(
                    string.Format(Localizer.L("Loc_UpdateAvailMsg"),
                        tag, $"{current.Major}.{current.Minor}"),
                    Localizer.L("Loc_UpdateAvailTitle"),
                    MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (res == MessageBoxResult.Yes)
                    Process.Start(new ProcessStartInfo(downloadUrl ?? pageUrl) { UseShellExecute = true });
            }
            else
            {
                MessageBox.Show(
                    string.Format(Localizer.L("Loc_NoUpdateMsg"), $"{current.Major}.{current.Minor}"),
                    Localizer.L("Loc_NoUpdateTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                string.Format(Localizer.L("Loc_UpdateErrMsg"), ex.Message),
                Localizer.L("Loc_UpdateErrTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            BtnCheckUpdate.Content = oldText;
            BtnCheckUpdate.IsEnabled = true;
        }
    }

    private static Version? ParseVersion(string tag)
    {
        var m = Regex.Match(tag, @"(\d+)\.(\d+)(?:\.(\d+))?");
        if (!m.Success) return null;
        int major = int.Parse(m.Groups[1].Value);
        int minor = int.Parse(m.Groups[2].Value);
        int patch = m.Groups[3].Success ? int.Parse(m.Groups[3].Value) : 0;
        return new Version(major, minor, patch, 0);
    }

    private void BtnOpenOutputFolder_Click(object sender, RoutedEventArgs e)
    {
        string dir = TxtOutputDir.Text.Trim();
        // Если папка ещё не создана — открываем родительскую
        if (!Directory.Exists(dir))
            dir = Path.GetDirectoryName(dir) ?? "";
        if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            Process.Start("explorer.exe", dir);
        else
            MessageBox.Show(Localizer.L("Loc_FolderNotReady"), Localizer.L("Loc_Warning"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
    {
        string dir = TxtOutputDir.Text.Trim();
        if (Directory.Exists(dir))
            Process.Start("explorer.exe", dir);
        else
            MessageBox.Show(Localizer.L("Loc_FolderNotFound"), Localizer.L("Loc_Warning"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void SliderSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        int val = (int)SliderSpeed.Value;
        if (LblSpeed != null)
            LblSpeed.Text = val >= 0 ? $"+{val}%" : $"{val}%";
    }

    // ── Автоопределение голоса ───────────────────────────────────────

    private void AutoSelectVoice(string filePath)
    {
        try
        {
            string enc = BookConverter.DetectEncoding(filePath);
            Encoding encoding;
            try { encoding = Encoding.GetEncoding(enc); }
            catch { encoding = Encoding.UTF8; }

            string sample = File.ReadAllText(filePath, encoding);
            if (sample.Length > 5000) sample = sample[..5000];
            string lang = BookConverter.DetectLanguage(sample);

            if (!BookConverter.DefaultVoices.TryGetValue(lang, out string? defaultVoice))
                return;

            // 1. Точное совпадение ShortName (Edge TTS)
            for (int i = 0; i < _allVoices.Count; i++)
            {
                if (_allVoices[i].ShortName == defaultVoice)
                {
                    ComboVoice.SelectedIndex = i;
                    return;
                }
            }

            // 2. ShortName начинается с языкового кода (ru-... / en-...)
            string langCode = lang + "-";
            for (int i = 0; i < _allVoices.Count; i++)
            {
                if (_allVoices[i].ShortName.StartsWith(langCode, StringComparison.OrdinalIgnoreCase))
                {
                    ComboVoice.SelectedIndex = i;
                    return;
                }
            }

            // 3. Поиск по ключевым словам в DisplayName (offline / piper)
            string[] markers = lang == "ru"
                ? ["рус", "russian", "irina", "pavel", "dmitry", "svetlana"]
                : ["англ", "english", "zira", "david", "guy", "jenny"];
            for (int i = 0; i < _allVoices.Count; i++)
            {
                string dn = _allVoices[i].DisplayName.ToLowerInvariant();
                if (markers.Any(m => dn.Contains(m)))
                {
                    ComboVoice.SelectedIndex = i;
                    return;
                }
            }
        }
        catch { }
    }

    private string GetCurrentVoice()
    {
        int idx = -1;
        Dispatcher.Invoke(() => idx = ComboVoice.SelectedIndex);
        if (idx >= 0 && idx < _allVoices.Count)
            return _allVoices[idx].ShortName;
        return "";
    }

    // ── Пауза / Стоп ────────────────────────────────────────────────

    private void BtnPause_Click(object sender, RoutedEventArgs e)
    {
        if (_pauseToken == null) return;

        if (_pauseToken.IsPaused)
        {
            _pauseToken.Resume();
            BtnPause.Content = Localizer.L("Loc_Pause");
            BtnPause.Background = FindResource("WarningBrush") as Brush;
        }
        else
        {
            _pauseToken.Pause();
            BtnPause.Content = Localizer.L("Loc_Continue");
            BtnPause.Background = FindResource("SuccessBrush") as Brush;
        }
    }

    private void BtnStop_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
    }

    // ── Запуск конвертации ───────────────────────────────────────────

    private void SetButtonsConverting(bool converting)
    {
        BtnStart.IsEnabled = !converting;
        BtnStop.IsEnabled = converting;
        BtnPause.IsEnabled = converting;
        if (!converting)
        {
            BtnPause.Content = Localizer.L("Loc_Pause");
            BtnPause.Background = FindResource("WarningBrush") as Brush;
        }
    }

    private async void BtnStart_Click(object sender, RoutedEventArgs e)
    {
        string inputFile = TxtInputFile.Text.Trim();
        string outputDir = TxtOutputDir.Text.Trim();

        if (string.IsNullOrEmpty(inputFile))
        {
            MessageBox.Show(Localizer.L("Loc_SelectSource"), Localizer.L("Loc_Warning"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!File.Exists(inputFile))
        {
            MessageBox.Show(string.Format(Localizer.L("Loc_FileNotFound"), inputFile), Localizer.L("Loc_Error"),
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        if (string.IsNullOrEmpty(outputDir))
        {
            MessageBox.Show(Localizer.L("Loc_SpecifyOutput"), Localizer.L("Loc_Warning"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int voiceIdx = ComboVoice.SelectedIndex;
        string voice = voiceIdx >= 0 && voiceIdx < _allVoices.Count
            ? _allVoices[voiceIdx].ShortName : "ru-RU-DmitryNeural";

        int speedVal = (int)SliderSpeed.Value;
        string speed = speedVal >= 0 ? $"+{speedVal}%" : $"{speedVal}%";

        string encoding = ComboEncoding.SelectedItem as string ?? "auto";
        bool merge = ChkMerge.IsChecked == true;
        bool noChapters = ChkNoChapters.IsChecked == true;
        bool deleteFragments = ChkDeleteFragments.IsChecked == true;

        // Очищаем лог
        TxtLog.Clear();
        SetProgress(0, 1);

        _cts = new CancellationTokenSource();
        _pauseToken = new PauseToken();
        SetButtonsConverting(true);

        var converter = new BookConverter();
        var opts = new ConversionOptions
        {
            InputFile = inputFile,
            OutputDir = outputDir,
            Voice = voice,
            Speed = speed,
            Merge = merge,
            NoChapters = noChapters,
            DeleteFragments = deleteFragments,
            Encoding = encoding,
            Log = msg => Dispatcher.BeginInvoke(() => AppendLog(msg)),
            Progress = (cur, total) => Dispatcher.BeginInvoke(() => SetProgress(cur, total)),
            TimeEstimate = text => Dispatcher.BeginInvoke(() => LblEta.Text = text),
            CancelToken = _cts.Token,
            PauseToken = _pauseToken,
            VoiceFunc = GetCurrentVoice,
            TtsMode = _selectedTtsMode,
        };

        ConversionResult? result = null;
        try
        {
            result = await Task.Run(() => converter.ConvertBookAsync(opts));
        }
        catch (OperationCanceledException)
        {
            AppendLog(Localizer.L("Loc_StoppedByUser"));
        }
        catch (Exception ex)
        {
            AppendLog(string.Format(Localizer.L("Loc_ErrorLogFmt"), ex.Message));
        }
        finally
        {
            SetButtonsConverting(false);
        }

        if (result != null)
            ShowCompletionDialog(result);
    }

    // ── Лог и прогресс ──────────────────────────────────────────────

    private void AppendLog(string message)
    {
        TxtLog.AppendText(message + "\n");
        TxtLog.ScrollToEnd();
    }

    private void SetProgress(int current, int total)
    {
        double frac = total > 0 ? (double)current / total : 0;
        int pct = (int)(frac * 100);

        // Ширина прогресс-бара
        double maxWidth = ProgressFill.Parent is FrameworkElement parent ? parent.ActualWidth : 0;
        if (maxWidth <= 0) maxWidth = 600;
        ProgressFill.Width = maxWidth * frac;

        LblPercent.Text = $"{pct}%";

        if (current >= total && total > 0)
        {
            ProgressFill.Background = FindResource("SuccessBrush") as Brush;
            LblPercent.Foreground = FindResource("SuccessBrush") as Brush;
            LblEta.Text = "";
        }
        else
        {
            ProgressFill.Background = FindResource("AccentBrush") as Brush;
            LblPercent.Foreground = FindResource("AccentBrush") as Brush;
        }
    }

    // ── Диалог завершения ────────────────────────────────────────────

    private void ShowCompletionDialog(ConversionResult result)
    {
        var dlg = new Window
        {
            Title = Localizer.L("Loc_CompletedTitle"),
            Width = 470,
            Height = 330,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            Background = FindResource("BgDarkBrush") as Brush,
        };

        var stack = new StackPanel { Margin = new Thickness(28, 24, 28, 20) };

        // Заголовок
        stack.Children.Add(new TextBlock
        {
            Text = Localizer.L("Loc_CompletedHeader"),
            FontSize = 18, FontWeight = FontWeights.Bold,
            Foreground = FindResource("SuccessBrush") as Brush,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 16),
        });

        // Информация
        var infoBorder = new Border
        {
            Background = FindResource("BgCardBrush") as Brush,
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16, 12, 16, 12),
            Margin = new Thickness(0, 0, 0, 16),
        };

        var infoStack = new StackPanel();
        var lines = new List<(string Label, string Value)>
        {
            (Localizer.L("Loc_Book"), result.BookName),
            (Localizer.L("Loc_FilesCreated"), result.TotalFiles.ToString()),
            (Localizer.L("Loc_TotalSize"), $"{result.TotalSizeMb} {Localizer.L("Loc_MB")}"),
        };
        if (!string.IsNullOrEmpty(result.MergedFile))
            lines.Add((Localizer.L("Loc_FinalFile"), Path.GetFileName(result.MergedFile)));

        foreach (var (label, value) in lines)
        {
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var lblBlock = new TextBlock
            {
                Text = label, FontSize = 13,
                Foreground = FindResource("TextSecondaryBrush") as Brush,
                Margin = new Thickness(0, 2, 0, 2),
            };
            Grid.SetColumn(lblBlock, 0);

            var valBlock = new TextBlock
            {
                Text = value, FontSize = 13, FontWeight = FontWeights.Bold,
                Foreground = FindResource("TextPrimaryBrush") as Brush,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 2, 0, 2),
            };
            Grid.SetColumn(valBlock, 1);

            row.Children.Add(lblBlock);
            row.Children.Add(valBlock);
            infoStack.Children.Add(row);
        }
        infoBorder.Child = infoStack;
        stack.Children.Add(infoBorder);

        // Кнопки
        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };

        var accentBrush = FindResource("AccentBrush") as Brush;
        var successBrush = FindResource("SuccessBrush") as Brush;
        var inputBrush = FindResource("BgInputBrush") as Brush;

        if (!string.IsNullOrEmpty(result.MergedFile) && File.Exists(result.MergedFile))
        {
            string mergedFile = result.MergedFile;
            var btnOpen = CreateDialogButton(Localizer.L("Loc_OpenAudio"), accentBrush);
            btnOpen.Click += (_, _) =>
            {
                Process.Start(new ProcessStartInfo(mergedFile) { UseShellExecute = true });
                dlg.Close();
            };
            btnPanel.Children.Add(btnOpen);
        }

        if (Directory.Exists(result.OutputDir))
        {
            string outDir = result.OutputDir;
            var btnFolder = CreateDialogButton(Localizer.L("Loc_OpenFolderBtn"), successBrush);
            btnFolder.Margin = new Thickness(8, 0, 0, 0);
            btnFolder.Click += (_, _) =>
            {
                Process.Start("explorer.exe", outDir);
                dlg.Close();
            };
            btnPanel.Children.Add(btnFolder);
        }

        var btnClose = CreateDialogButton(Localizer.L("Loc_Close"), inputBrush);
        btnClose.Foreground = FindResource("TextPrimaryBrush") as Brush;
        btnClose.FontWeight = FontWeights.Normal;
        btnClose.Margin = new Thickness(8, 0, 0, 0);
        btnClose.Click += (_, _) => dlg.Close();
        btnPanel.Children.Add(btnClose);

        stack.Children.Add(btnPanel);
        dlg.Content = stack;
        dlg.ShowDialog();
    }

    private static Button CreateDialogButton(string text, Brush? bg)
    {
        var btn = new Button
        {
            Content = text,
            Height = 38,
            Padding = new Thickness(16, 0, 16, 0),
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
            FontSize = 13,
            Cursor = System.Windows.Input.Cursors.Hand,
            BorderThickness = new Thickness(0),
        };

        var template = new ControlTemplate(typeof(Button));
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.BackgroundProperty, bg ?? Brushes.Gray);
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
        border.SetValue(Border.PaddingProperty, new Thickness(16, 8, 16, 8));
        border.AppendChild(new FrameworkElementFactory(typeof(ContentPresenter)));
        template.VisualTree = border;
        btn.Template = template;

        return btn;
    }

    // ── Диалог настроек (язык + тема) ────────────────────────────────

    private void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Window
        {
            Title = Localizer.L("Loc_SettingsTitle"),
            Width = 420, Height = 330,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            Background = FindResource("BgDarkBrush") as Brush,
        };

        var root = new StackPanel { Margin = new Thickness(24, 20, 24, 20) };

        root.Children.Add(new TextBlock
        {
            Text = Localizer.L("Loc_SettingsTitle"),
            FontSize = 20, FontWeight = FontWeights.Bold,
            Foreground = FindResource("AccentBrush") as Brush,
            Margin = new Thickness(0, 0, 0, 16),
        });

        // Язык
        root.Children.Add(SectionLabel(Localizer.L("Loc_Language")));
        var rbRu = MakeRadio(Localizer.L("Loc_Russian"), "lang", Localizer.Current == AppLang.Ru);
        var rbEn = MakeRadio(Localizer.L("Loc_English"), "lang", Localizer.Current == AppLang.En);
        rbRu.Checked += (_, _) => ApplyLanguage(AppLang.Ru);
        rbEn.Checked += (_, _) => ApplyLanguage(AppLang.En);
        root.Children.Add(rbRu);
        root.Children.Add(rbEn);

        // Тема
        var themeLabel = SectionLabel(Localizer.L("Loc_Theme"));
        themeLabel.Margin = new Thickness(0, 14, 0, 4);
        root.Children.Add(themeLabel);
        var rbLight = MakeRadio(Localizer.L("Loc_ThemeLight"), "theme", ThemeManager.Current == AppTheme.Light);
        var rbDark = MakeRadio(Localizer.L("Loc_ThemeDark"), "theme", ThemeManager.Current == AppTheme.Dark);
        rbLight.Checked += (_, _) => ApplyTheme(AppTheme.Light);
        rbDark.Checked += (_, _) => ApplyTheme(AppTheme.Dark);
        root.Children.Add(rbLight);
        root.Children.Add(rbDark);

        var btnClose = CreateDialogButton(Localizer.L("Loc_Close"), FindResource("AccentBrush") as Brush);
        btnClose.Margin = new Thickness(0, 20, 0, 0);
        btnClose.HorizontalAlignment = HorizontalAlignment.Right;
        btnClose.Click += (_, _) => dlg.Close();
        root.Children.Add(btnClose);

        dlg.Content = root;
        dlg.ShowDialog();
    }

    private TextBlock SectionLabel(string text) => new()
    {
        Text = text, FontSize = 14, FontWeight = FontWeights.Bold,
        Foreground = FindResource("TextPrimaryBrush") as Brush,
        Margin = new Thickness(0, 0, 0, 4),
    };

    private RadioButton MakeRadio(string text, string group, bool isChecked) => new()
    {
        Content = text, GroupName = group, IsChecked = isChecked,
        FontSize = 14,
        Foreground = FindResource("TextPrimaryBrush") as Brush,
        Margin = new Thickness(8, 3, 0, 3),
        Cursor = System.Windows.Input.Cursors.Hand,
    };

    // ── Живое применение языка и темы ────────────────────────────────

    private void ApplyLanguage(AppLang lang)
    {
        Localizer.Apply(lang);             // строки в XAML (DynamicResource) обновятся сами
        App.Settings.Language = lang;
        App.Settings.Save();
        RefreshLocalizedCode();            // тексты, выставляемые из кода
    }

    private void ApplyTheme(AppTheme theme)
    {
        ThemeManager.Apply(theme);         // кисти (DynamicResource) обновятся сами
        App.Settings.Theme = theme;
        App.Settings.Save();
        // Освежаем цвета, заданные из кода
        ProgressFill.Background = FindResource("AccentBrush") as Brush;
        LblPercent.Foreground = FindResource("AccentBrush") as Brush;
        if (_pauseToken == null || !_pauseToken.IsPaused)
            BtnPause.Background = FindResource("WarningBrush") as Brush;
        UpdateModeStatus();                // перекрашиваем статус режима
    }

    private void RefreshLocalizedCode()
    {
        int modeIdx = ComboTtsMode.SelectedIndex;
        int voiceIdx = ComboVoice.SelectedIndex;
        ComboTtsMode.ItemsSource = TtsModes.Select(m => Localizer.L(m.LabelKey)).ToList();
        ComboTtsMode.SelectedIndex = modeIdx >= 0 ? modeIdx : 0;
        if (voiceIdx >= 0 && voiceIdx < _allVoices.Count)
            ComboVoice.SelectedIndex = voiceIdx;

        UpdateModeStatus();

        if (_pauseToken == null || !_pauseToken.IsPaused)
            BtnPause.Content = Localizer.L("Loc_Pause");

        // Баннер «Продолжить»
        if (_unfinished != null)
        {
            string bookName = _unfinished.BookName ?? Path.GetFileNameWithoutExtension(_unfinished.InputFile ?? "");
            int done = _unfinished.DoneChapters, total = _unfinished.TotalChapters;
            int pct = total > 0 ? done * 100 / total : 0;
            ResumeLabel.Text = string.Format(Localizer.L("Loc_ResumeFmt"), bookName, done, total, pct);
        }

        // Список истории (без авто-перезагрузки проекта)
        if (_history.Count > 0)
        {
            ComboHistory.ItemsSource = _history.Select(FormatHistoryEntry).ToList();
            ComboHistory.SelectedIndex = -1;
        }
    }
}
