using System.Windows;

namespace JISMemo;

public partial class HelpWindow : Window
{
    public string VersionInfo { get; set; }

    public HelpWindow()
    {
        InitializeComponent();
        Title = Localization.HelpTitle;
        var description = Localization.CurrentLanguage == "ko" ? AppInfo.Description : "Sticky Note Style Memo Application";
        var developer = Localization.CurrentLanguage == "ko" ? "개발자" : "Developer";
        VersionInfo = $"{AppInfo.AppName} v{AppInfo.FullVersion}\n{description}\n{developer}: {AppInfo.Developer}\nContact: {AppInfo.ContactEmail1}\n{AppInfo.ContactEmail2}";
        DataContext = this;
        OkButton.Content = Localization.OK;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}