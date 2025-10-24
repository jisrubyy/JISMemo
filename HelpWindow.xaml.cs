using System.Windows;

namespace JISMemo;

public partial class HelpWindow : Window
{
    public string VersionInfo { get; set; }

    public HelpWindow()
    {
        InitializeComponent();
        VersionInfo = $"{AppInfo.AppName} v{AppInfo.Version}\n{AppInfo.Description}\n개발자: {AppInfo.Developer}\nContact: {AppInfo.ContactEmail1}\n{AppInfo.ContactEmail2}";
        DataContext = this;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}