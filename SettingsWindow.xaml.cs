using System.IO;
using System.Text.Json;
using System.Windows;
using JISMemo.Models;
using JISMemo.Services;
using Microsoft.Win32;
using MessageBox = System.Windows.MessageBox;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace JISMemo;

public partial class SettingsWindow : Window
{
    public string? SelectedPath { get; private set; }
    public bool UseCustomPath { get; private set; }
    public bool PasswordRemoved { get; private set; }
    private readonly NoteService _noteService;

    public SettingsWindow(string currentPath, bool useCustomPath, NoteService noteService)
    {
        InitializeComponent();
        _noteService = noteService;
        
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
    }
    
    private void UpdateEncryptionStatus()
    {
        bool isEncrypted = _noteService.IsEncryptionEnabled();
        EncryptionStatusText.Text = isEncrypted ? "암호화 상태: 활성화" : "암호화 상태: 비활성화";
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
                SelectedPath = Path.Combine(CustomPathTextBox.Text, "JISMemo");
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
        var dialog = new SaveFileDialog
        {
            Filter = "JISMemo 백업 파일 (*.jmb)|*.jmb",
            FileName = $"JISMemo_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.jmb"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var userService = new UserService();
                var currentUser = userService.GetCurrentUser() ?? "";
                
                var backup = new UserBackup
                {
                    Username = currentUser,
                    Notes = await _noteService.LoadNotesAsync(null),
                    PasswordHash = _noteService.GetPasswordHash(),
                    PasswordHint = _noteService.GetPasswordHint(),
                    EncryptionEnabled = _noteService.IsEncryptionEnabled()
                };
                
                var json = JsonSerializer.Serialize(backup, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(dialog.FileName, json);
                
                MessageBox.Show("데이터가 내보내기 되었습니다.", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"내보내기 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "JISMemo 백업 파일 (*.jmb)|*.jmb"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var json = await File.ReadAllTextAsync(dialog.FileName);
                var backup = JsonSerializer.Deserialize<UserBackup>(json);
                
                if (backup == null)
                {
                    MessageBox.Show("잘못된 백업 파일입니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                var userService = new UserService();
                var users = userService.GetAllUsers();
                
                if (users.Any(u => u.Username == backup.Username))
                {
                    var result = MessageBox.Show(
                        $"사용자 '{backup.Username}'이(가) 이미 존재합니다.\n덮어쓰시겠습니까?",
                        "확인",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    
                    if (result != MessageBoxResult.Yes)
                        return;
                }
                else
                {
                    userService.AddUser(backup.Username);
                }
                
                var tempService = new NoteService(backup.Username);
                await tempService.SaveNotesAsync(backup.Notes, null);
                tempService.RestoreEncryptionSettings(backup.PasswordHash, backup.PasswordHint, backup.EncryptionEnabled);
                
                MessageBox.Show(
                    $"사용자 '{backup.Username}'의 데이터가 가져오기 되었습니다.\n사용자 전환으로 확인하세요.",
                    "성공",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"가져오기 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}