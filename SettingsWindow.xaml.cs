using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using JISMemo.Models;
using JISMemo.Services;
using Microsoft.Win32;
using MessageBox = System.Windows.MessageBox;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using RegistryKey = Microsoft.Win32.RegistryKey;

namespace JISMemo;

public partial class SettingsWindow : Window
{
    public string? SelectedPath { get; private set; }
    public bool UseCustomPath { get; private set; }
    public bool PasswordRemoved { get; private set; }
    public bool ColorChanged { get; private set; }
    public string? NewBackgroundColor { get; private set; }
    public bool NoteThemeChanged { get; private set; }
    public string? NewNoteColor { get; private set; }
    public string? NewNoteTextColor { get; private set; }
    public bool LanguageChanged { get; private set; }
    public string? NewLanguage { get; private set; }
    private readonly NoteService _noteService;
    private readonly SettingsService _settingsService = new();
    private readonly (string Name, string BgColor, string TextColor)[] _noteThemes = new[]
    {
        ("클래식 노랑", "#FFFF99", "#000000"),
        ("파스텔 핑크", "#FFB3D9", "#000000"),
        ("민트 그린", "#B3FFB3", "#000000"),
        ("스카이 블루", "#B3E5FF", "#000000"),
        ("라벤더", "#E6B3FF", "#000000"),
        ("피치", "#FFD9B3", "#000000"),
        ("다크 그레이", "#4A4A4A", "#FFFFFF"),
        ("네이비 블루", "#2C3E50", "#FFFFFF")
    };

    public SettingsWindow(string currentPath, bool useCustomPath, NoteService noteService, string currentBgColor, string currentNoteColor, string currentNoteTextColor)
    {
        InitializeComponent();
        _noteService = noteService;
        NewBackgroundColor = currentBgColor;
        NewNoteColor = currentNoteColor;
        NewNoteTextColor = currentNoteTextColor;
        
        CurrentPathTextBlock.Text = currentPath;
        
        if (useCustomPath)
        {
            CustomLocationRadio.IsChecked = true;
            CustomPathTextBox.Text = Path.GetDirectoryName(currentPath);
        }
        else
        {
            DefaultLocationRadio.IsChecked = true;
        }
        
        UpdateEncryptionStatus();
        UpdateColorPreview();
        InitializeNoteThemeComboBox();
        LoadAutoStartSetting();
        InitializeLanguageComboBox();
        UpdateUITexts();
    }
    
    private void UpdateUITexts()
    {
        Title = Localization.SettingsTitle;
        AutoStartCheckBox.Content = Localization.AutoStart;
        SetPasswordButton.Content = Localization.SetPassword;
        RemovePasswordButton.Content = Localization.RemovePassword;
        ChangeColorButton.Content = Localization.ChangeColor;
        ResetColorButton.Content = Localization.ResetToDefault;
        ExportButton.Content = Localization.ExportData;
        ImportButton.Content = Localization.ImportData;
        OkButton.Content = Localization.OK;
        CancelButton.Content = Localization.Cancel;
        BrowseButton.Content = Localization.Browse;
    }
    
    private void InitializeLanguageComboBox()
    {
        LanguageComboBox.Items.Add("한국어 (Korean)");
        LanguageComboBox.Items.Add("English");
        
        var settings = _settingsService.LoadSettings();
        LanguageComboBox.SelectedIndex = settings.Language == "en" ? 1 : 0;
        NewLanguage = settings.Language;
    }
    
    private void LanguageComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (LanguageComboBox.SelectedIndex < 0) return;
        
        var newLang = LanguageComboBox.SelectedIndex == 1 ? "en" : "ko";
        var settings = _settingsService.LoadSettings();
        
        if (newLang != settings.Language)
        {
            NewLanguage = newLang;
            LanguageChanged = true;
            LanguageChangeNote.Visibility = Visibility.Visible;
        }
        else
        {
            LanguageChanged = false;
            LanguageChangeNote.Visibility = Visibility.Collapsed;
        }
    }
    
    private void LoadAutoStartSetting()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
            var value = key?.GetValue("JISMemo");
            AutoStartCheckBox.IsChecked = value != null;
        }
        catch { }
    }
    
    private void AutoStartCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (key == null) return;
            
            if (AutoStartCheckBox.IsChecked == true)
            {
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue("JISMemo", $"\"{exePath}\"");
                }
            }
            else
            {
                key.DeleteValue("JISMemo", false);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"자동 시작 설정 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void InitializeNoteThemeComboBox()
    {
        var themeNames = new[] {
            Localization.ClassicYellow,
            Localization.PastelPink,
            Localization.MintGreen,
            Localization.SkyBlue,
            Localization.Lavender,
            Localization.Peach,
            Localization.DarkGray,
            Localization.NavyBlue
        };
        
        foreach (var name in themeNames)
        {
            NoteThemeComboBox.Items.Add(name);
        }
        
        var currentTheme = Array.FindIndex(_noteThemes, t => t.BgColor == NewNoteColor);
        NoteThemeComboBox.SelectedIndex = currentTheme >= 0 ? currentTheme : 0;
        UpdateNoteThemePreview();
    }
    
    private void UpdateColorPreview()
    {
        try
        {
            ColorPreview.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(NewBackgroundColor ?? "#F5F5F5"));
        }
        catch
        {
            ColorPreview.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.WhiteSmoke);
        }
    }
    
    private void UpdateEncryptionStatus()
    {
        bool isEncrypted = _noteService.IsEncryptionEnabled();
        EncryptionStatusText.Text = $"{Localization.EncryptionStatus}: {(isEncrypted ? Localization.Enabled : Localization.Disabled)}";
        SetPasswordButton.IsEnabled = !isEncrypted;
        RemovePasswordButton.IsEnabled = isEncrypted;
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "메모 저장 폴더 선택"
        };

        if (dialog.ShowDialog() == true)
        {
            CustomPathTextBox.Text = dialog.FolderName;
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        UseCustomPath = CustomLocationRadio.IsChecked == true;
        
        if (UseCustomPath)
        {
            if (string.IsNullOrWhiteSpace(CustomPathTextBox.Text))
            {
                MessageBox.Show("사용자 지정 경로를 선택해주세요.", "경고", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                Directory.CreateDirectory(CustomPathTextBox.Text);
                SelectedPath = CustomPathTextBox.Text;
            }
            catch
            {
                MessageBox.Show("선택한 경로에 접근할 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }
        else
        {
            SelectedPath = null;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
    
    private void SetPasswordButton_Click(object sender, RoutedEventArgs e)
    {
        var setupWindow = new PasswordSetupWindow();
        if (setupWindow.ShowDialog() == true)
        {
            _noteService.SetupPassword(setupWindow.Password, setupWindow.Hint);
            UpdateEncryptionStatus();
            MessageBox.Show("암호가 설정되었습니다.\n다음 실행부터 적용됩니다.", "완료", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
    
    private void RemovePasswordButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "암호를 제거하면 메모가 암호화되지 않습니다.\n정말 제거하시겠습니까?",
            "암호 제거 확인",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        
        if (result == MessageBoxResult.Yes)
        {
            _noteService.DisableEncryption();
            PasswordRemoved = true;
            UpdateEncryptionStatus();
            MessageBox.Show("암호가 제거되었습니다.", "완료", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
    
    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var userService = new UserService();
            var currentUser = userService.GetCurrentUser() ?? "Default";
            var dataPath = _noteService.GetCurrentDataPath();
            
            if (!File.Exists(dataPath))
            {
                MessageBox.Show("내보낼 데이터가 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            var notesJson = await File.ReadAllTextAsync(dataPath);
            List<StickyNote> notes;
            
            if (_noteService.IsEncryptionEnabled())
            {
                var passwordWindow = new PasswordWindow(_noteService.GetPasswordHint());
                if (passwordWindow.ShowDialog() != true) return;
                if (!_noteService.VerifyPassword(passwordWindow.Password))
                {
                    MessageBox.Show("비밀번호가 올바르지 않습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                var decrypted = EncryptionService.Decrypt(notesJson, passwordWindow.Password);
                notes = JsonSerializer.Deserialize<List<StickyNote>>(decrypted) ?? new();
            }
            else
            {
                notes = JsonSerializer.Deserialize<List<StickyNote>>(notesJson) ?? new();
            }
            
            var dialog = new SaveFileDialog
            {
                Filter = "JISMemo 백업 (*.jmb)|*.jmb",
                FileName = $"JISMemo_{currentUser}_{DateTime.Now:yyyyMMdd_HHmmss}.jmb"
            };
            
            if (dialog.ShowDialog() == true)
            {
                var backup = new UserBackup { Username = currentUser, Notes = notes };
                var json = JsonSerializer.Serialize(backup, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(dialog.FileName, json);
                MessageBox.Show($"{notes.Count}개 메모 내보내기 완료", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"내보내기 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "JISMemo 백업 (*.jmb)|*.jmb" };
        if (dialog.ShowDialog() != true) return;
        
        try
        {
            var json = await File.ReadAllTextAsync(dialog.FileName);
            var backup = JsonSerializer.Deserialize<UserBackup>(json);
            if (backup?.Notes == null)
            {
                MessageBox.Show("잘못된 백업 파일", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            var userService = new UserService();
            var existingUsers = string.Join(", ", userService.GetAllUsers().Select(u => u.Username));
            
            var inputWindow = new System.Windows.Window
            {
                Title = "사용자 이름",
                Width = 450,
                Height = 280,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };
            
            var grid = new System.Windows.Controls.Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
            
            var label = new System.Windows.Controls.TextBlock
            {
                Text = $"백업: {backup.Username}\n가져올 사용자 이름:",
                Margin = new Thickness(0, 0, 0, 10)
            };
            System.Windows.Controls.Grid.SetRow(label, 0);
            
            var textBox = new System.Windows.Controls.TextBox
            {
                Text = backup.Username,
                Margin = new Thickness(0, 0, 0, 10),
                Height = 30
            };
            System.Windows.Controls.Grid.SetRow(textBox, 1);
            
            var existingLabel = new System.Windows.Controls.TextBlock
            {
                Text = $"기존 사용자: {existingUsers}",
                Foreground = System.Windows.Media.Brushes.Black,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 15),
                TextWrapping = System.Windows.TextWrapping.Wrap
            };
            System.Windows.Controls.Grid.SetRow(existingLabel, 2);
            
            var panel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };
            System.Windows.Controls.Grid.SetRow(panel, 3);
            
            var okBtn = new System.Windows.Controls.Button { Content = "확인", Width = 80, Margin = new Thickness(0, 0, 10, 0) };
            okBtn.Click += (s, ev) => { inputWindow.DialogResult = true; inputWindow.Close(); };
            var cancelBtn = new System.Windows.Controls.Button { Content = "취소", Width = 80 };
            cancelBtn.Click += (s, ev) => { inputWindow.Close(); };
            
            panel.Children.Add(okBtn);
            panel.Children.Add(cancelBtn);
            grid.Children.Add(label);
            grid.Children.Add(textBox);
            grid.Children.Add(existingLabel);
            grid.Children.Add(panel);
            inputWindow.Content = grid;
            
            if (inputWindow.ShowDialog() != true || string.IsNullOrWhiteSpace(textBox.Text)) return;
            
            var targetUser = textBox.Text.Trim();
            
            if (userService.GetAllUsers().Any(u => u.Username == targetUser))
            {
                MessageBox.Show($"'{targetUser}' 사용자가 이미 존재합니다.\n다른 이름을 사용하세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            userService.AddUser(targetUser);
            
            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JISMemo");
            Directory.CreateDirectory(appData);
            var targetPath = Path.Combine(appData, $"notes_{targetUser}.json");
            
            var notesJson = JsonSerializer.Serialize(backup.Notes, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(targetPath, notesJson);
            userService.SetCurrentUser(targetUser);
            
            MessageBox.Show($"{backup.Notes.Count}개 메모 가져오기 완료\n재시작 후 '{targetUser}'로 자동 로그인", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"가져오기 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void ChangeColorButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.ColorDialog();
        
        try
        {
            var currentColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(NewBackgroundColor ?? "#F5F5F5");
            dialog.Color = System.Drawing.Color.FromArgb(currentColor.A, currentColor.R, currentColor.G, currentColor.B);
        }
        catch { }
        
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var color = dialog.Color;
            NewBackgroundColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            ColorChanged = true;
            UpdateColorPreview();
        }
    }
    
    private void ResetColorButton_Click(object sender, RoutedEventArgs e)
    {
        NewBackgroundColor = "#F5F5F5";
        ColorChanged = true;
        UpdateColorPreview();
    }
    
    private void NoteThemeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (NoteThemeComboBox.SelectedIndex >= 0 && NoteThemeComboBox.SelectedIndex < _noteThemes.Length)
        {
            var theme = _noteThemes[NoteThemeComboBox.SelectedIndex];
            NewNoteColor = theme.BgColor;
            NewNoteTextColor = theme.TextColor;
            NoteThemeChanged = true;
            UpdateNoteThemePreview();
        }
    }
    
    private void UpdateNoteThemePreview()
    {
        try
        {
            NoteThemePreview.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(NewNoteColor ?? "#FFFF99"));
            NoteThemePreviewText.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(NewNoteTextColor ?? "#000000"));
        }
        catch { }
    }
}