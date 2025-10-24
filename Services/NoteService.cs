using System.IO;
using System.Text.Json;
using JISMemo.Models;

namespace JISMemo.Services;

public class NoteService
{
    private readonly string _dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JISMemo", "notes.json");

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