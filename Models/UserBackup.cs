namespace JISMemo.Models;

public class UserBackup
{
    public string Username { get; set; } = "";
    public List<StickyNote> Notes { get; set; } = new();
    public string? PasswordHash { get; set; }
    public string? PasswordHint { get; set; }
    public bool EncryptionEnabled { get; set; }
}
