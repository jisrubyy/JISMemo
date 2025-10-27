using System.IO;
using System.Text.Json;
using JISMemo.Models;
using Microsoft.Win32;

namespace JISMemo.Services;

public class UserService
{
    private const string RegistryKey = @"SOFTWARE\JISMemo";
    private const string CurrentUserValue = "CurrentUser";
    private const string UsersFileName = "users.json";
    
    private string GetUsersFilePath()
    {
        var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JISMemo");
        Directory.CreateDirectory(appDataPath);
        return Path.Combine(appDataPath, UsersFileName);
    }

    public List<UserProfile> GetAllUsers()
    {
        try
        {
            var path = GetUsersFilePath();
            if (!File.Exists(path)) return new List<UserProfile>();
            
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<UserProfile>>(json) ?? new List<UserProfile>();
        }
        catch
        {
            return new List<UserProfile>();
        }
    }

    public void SaveUsers(List<UserProfile> users)
    {
        try
        {
            var path = GetUsersFilePath();
            var json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch { }
    }

    public void AddUser(string username)
    {
        var users = GetAllUsers();
        if (!users.Any(u => u.Username == username))
        {
            users.Add(new UserProfile { Username = username });
            SaveUsers(users);
        }
    }

    public void RemoveUser(string username)
    {
        var users = GetAllUsers();
        users.RemoveAll(u => u.Username == username);
        SaveUsers(users);
        
        // 사용자 데이터 파일도 삭제
        try
        {
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JISMemo");
            var userDataFile = Path.Combine(appDataPath, $"notes_{username}.json");
            if (File.Exists(userDataFile))
                File.Delete(userDataFile);
        }
        catch { }
    }

    public string? GetCurrentUser()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
            return key?.GetValue(CurrentUserValue) as string;
        }
        catch
        {
            return null;
        }
    }

    public void SetCurrentUser(string username)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKey);
            key.SetValue(CurrentUserValue, username);
        }
        catch { }
    }
}
