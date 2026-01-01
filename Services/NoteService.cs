using System.IO;
using System.Text.Json;
using JISMemo.Models;
using Microsoft.Win32;

namespace JISMemo.Services;

public class NoteService
{
    private string _dataPath;
    private string _currentUser;
    private const string RegistryKey = @"SOFTWARE\JISMemo";
    private const string DataPathValue = "DataPath";
    private const string PasswordHashValue = "PasswordHash";
    private const string PasswordHintValue = "PasswordHint";
    private const string EncryptionEnabledValue = "EncryptionEnabled";

    public NoteService(string username = "")
    {
        _currentUser = username;
        _dataPath = GetDataPath();
    }

    private string GetDataPath()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(GetUserRegistryKey());
            var customPath = key?.GetValue(DataPathValue) as string;
            
            if (!string.IsNullOrEmpty(customPath) && Directory.Exists(Path.GetDirectoryName(customPath)))
            {
                return customPath;
            }
        }
        catch { }
        
        var fileName = string.IsNullOrEmpty(_currentUser) ? "notes.json" : $"notes_{_currentUser}.json";
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JISMemo", fileName);
    }
    
    private string GetUserRegistryKey()
    {
        return string.IsNullOrEmpty(_currentUser) ? RegistryKey : $"{RegistryKey}\\{_currentUser}";
    }

    public void SetDataPath(string? customPath)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(GetUserRegistryKey());
            
            if (string.IsNullOrEmpty(customPath))
            {
                key.DeleteValue(DataPathValue, false);
                var fileName = string.IsNullOrEmpty(_currentUser) ? "notes.json" : $"notes_{_currentUser}.json";
                _dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JISMemo", fileName);
            }
            else
            {
                var fileName = string.IsNullOrEmpty(_currentUser) ? "notes.json" : $"notes_{_currentUser}.json";
                var fullPath = Path.Combine(customPath, fileName);
                key.SetValue(DataPathValue, fullPath);
                _dataPath = fullPath;
            }
        }
        catch { }
    }

    public string GetCurrentDataPath() => _dataPath;
    
    public bool IsUsingCustomPath()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(GetUserRegistryKey());
            return key?.GetValue(DataPathValue) != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<StickyNote>> LoadNotesAsync(string? password = null)
    {
        try
        {
            if (!File.Exists(_dataPath))
                return new List<StickyNote>();

            var json = await File.ReadAllTextAsync(_dataPath);
            
            if (IsEncryptionEnabled() && !string.IsNullOrEmpty(password))
            {
                json = EncryptionService.Decrypt(json, password);
            }
            
            return JsonSerializer.Deserialize<List<StickyNote>>(json) ?? new List<StickyNote>();
        }
        catch (Exception ex)
        {
            LogService.Error($"메모 로드 실패 (경로: {_dataPath})", ex);
            return new List<StickyNote>();
        }
    }

    public async Task SaveNotesAsync(List<StickyNote> notes, string? password = null)
    {
        string tempPath = _dataPath + ".tmp";
        try
        {
            var directory = Path.GetDirectoryName(_dataPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(notes, new JsonSerializerOptions { WriteIndented = true });
            
            if (IsEncryptionEnabled() && !string.IsNullOrEmpty(password))
            {
                json = EncryptionService.Encrypt(json, password);
            }
            
            // 임시 파일에 쓰기
            await File.WriteAllTextAsync(tempPath, json);
            
            // 성공하면 기존 파일 교체
            if (File.Exists(_dataPath))
            {
                File.Replace(tempPath, _dataPath, null);
            }
            else
            {
                File.Move(tempPath, _dataPath);
            }
            
            LogService.Info($"메모 저장 성공: {notes.Count}개 (사용자: {_currentUser})");
        }
        catch (Exception ex)
        {
            LogService.Error($"메모 저장 중 치명적 오류 발생 (사용자: {_currentUser})", ex);
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { }
            }
        }
    }

    public void SetupPassword(string password, string hint)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(GetUserRegistryKey());
            var hash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(password)));
            key.SetValue(PasswordHashValue, hash);
            key.SetValue(PasswordHintValue, hint);
            key.SetValue(EncryptionEnabledValue, 1);
        }
        catch { }
    }

    public bool VerifyPassword(string password)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(GetUserRegistryKey());
            var storedHash = key?.GetValue(PasswordHashValue) as string;
            if (string.IsNullOrEmpty(storedHash)) return false;
            
            var hash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(password)));
            return hash == storedHash;
        }
        catch
        {
            return false;
        }
    }

    public string? GetPasswordHint()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(GetUserRegistryKey());
            return key?.GetValue(PasswordHintValue) as string;
        }
        catch
        {
            return null;
        }
    }

    public bool IsEncryptionEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(GetUserRegistryKey());
            var value = key?.GetValue(EncryptionEnabledValue);
            return value != null && (int)value == 1;
        }
        catch
        {
            return false;
        }
    }

    public void DisableEncryption()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(GetUserRegistryKey());
            key.DeleteValue(PasswordHashValue, false);
            key.DeleteValue(PasswordHintValue, false);
            key.DeleteValue(EncryptionEnabledValue, false);
        }
        catch { }
    }
    
    public string? GetPasswordHash()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(GetUserRegistryKey());
            return key?.GetValue(PasswordHashValue) as string;
        }
        catch
        {
            return null;
        }
    }
    
    public void RestoreEncryptionSettings(string? passwordHash, string? hint, bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(GetUserRegistryKey());
            if (enabled && !string.IsNullOrEmpty(passwordHash))
            {
                key.SetValue(PasswordHashValue, passwordHash);
                key.SetValue(PasswordHintValue, hint ?? "");
                key.SetValue(EncryptionEnabledValue, 1);
            }
        }
        catch { }
    }
}