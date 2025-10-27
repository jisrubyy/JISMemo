using System.Windows;
using JISMemo.Models;

namespace JISMemo;

public partial class NoteInfoWindow : Window
{
    public NoteInfoWindow(StickyNote note)
    {
        InitializeComponent();
        
        IdText.Text = note.Id;
        CreatedAtText.Text = note.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
        ModifiedAtText.Text = note.ModifiedAt.ToString("yyyy-MM-dd HH:mm:ss");
        SizeText.Text = $"{note.Width:F0} × {note.Height:F0}";
        PositionText.Text = $"X: {note.Left:F0}, Y: {note.Top:F0}";
        OwnerText.Text = string.IsNullOrEmpty(note.Owner) ? "알 수 없음" : note.Owner;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
