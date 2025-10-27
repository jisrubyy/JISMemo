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
        
        if (!string.IsNullOrEmpty(note.DeviceType) || !string.IsNullOrEmpty(note.DeviceName))
        {
            DeviceInfoLabel.Visibility = Visibility.Visible;
            DeviceInfoText.Visibility = Visibility.Visible;
            
            var deviceInfo = new List<string>();
            if (!string.IsNullOrEmpty(note.DeviceType)) deviceInfo.Add(note.DeviceType);
            if (!string.IsNullOrEmpty(note.DeviceName)) deviceInfo.Add(note.DeviceName);
            DeviceInfoText.Text = string.Join(" - ", deviceInfo);
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
