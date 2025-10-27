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
        catch
        {
            return new List<StickyNote>();
        }
    }

    public async Task SaveNotesAsync(List<StickyNote> notes, string? password = null)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_dataPath)!);
            var json = JsonSerializer.Serialize(notes, new JsonSerializerOptions { WriteIndented = true });
            
            if (IsEncryptionEnabled() && !string.IsNullOrEmpty(password))
            {
                json = EncryptionService.Encrypt(json, password);
            }
            
            await File.WriteAllTextAsync(_dataPath, json);
        }
        catch
        {
            // 저장 실패 시 무시
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