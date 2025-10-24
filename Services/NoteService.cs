using System.IO;
using System.Text.Json;
using JISMemo.Models;
using Microsoft.Win32;

namespace JISMemo.Services;

public class NoteService
{
    private string _dataPath;
    private const string RegistryKey = @"SOFTWARE\JISMemo";
    private const string DataPathValue = "DataPath";

    public NoteService()
    {
        _dataPath = GetDataPath();
    }

    private string GetDataPath()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
            var customPath = key?.GetValue(DataPathValue) as string;
            
            if (!string.IsNullOrEmpty(customPath) && Directory.Exists(Path.GetDirectoryName(customPath)))
            {
                return customPath;
            }
        }
        catch { }
        
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JISMemo", "notes.json");
    }

    public void SetDataPath(string? customPath)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKey);
            
            if (string.IsNullOrEmpty(customPath))
            {
                key.DeleteValue(DataPathValue, false);
                _dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JISMemo", "notes.json");
            }
            else
            {
                var fullPath = Path.Combine(customPath, "notes.json");
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
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
            return key?.GetValue(DataPathValue) != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<StickyNote>> LoadNotesAsync()
    {
        try
        {
            if (!File.Exists(_dataPath))
                return new List<StickyNote>();

            var json = await File.ReadAllTextAsync(_dataPath);
            return JsonSerializer.Deserialize<List<StickyNote>>(json) ?? new List<StickyNote>();
        }
        catch
        {
            return new List<StickyNote>();
        }
    }

    public async Task SaveNotesAsync(List<StickyNote> notes)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_dataPath)!);
            var json = JsonSerializer.Serialize(notes, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_dataPath, json);
        }
        catch
        {
            // 저장 실패 시 무시
        }
    }
}