using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace JISMemo;

public partial class CreditWindow : Window
{
    public CreditWindow()
    {
        InitializeComponent();
        UpdateUITexts();
    }
    
    private void UpdateUITexts()
    {
        Title = Localization.CurrentLanguage == "ko" ? "제작자 정보" : "Credit";
        VersionText.Text = $"Version {AppInfo.FullVersion}";
        DeveloperLabel.Text = Localization.CurrentLanguage == "ko" ? "개발자" : "Developer";
        ContactLabel.Text = Localization.CurrentLanguage == "ko" ? "연락처" : "Contact";
        WebsiteLabel.Text = Localization.CurrentLanguage == "ko" ? "개발 및 배포" : "Development & Distribution";
        DonationLabel.Text = Localization.CurrentLanguage == "ko" ? "기부" : "Donation";
        DonationButtonText.Text = Localization.CurrentLanguage == "ko" ? "☕ 커피 한잔 사주세요" : "☕ Buy me a coffee";
        CloseButton.Content = Localization.OK;
    }
    
    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }
        catch
        {
            System.Windows.MessageBox.Show(
                Localization.CurrentLanguage == "ko" ? 
                    "웹 브라우저를 열 수 없습니다." : 
                    "Cannot open web browser.",
                Localization.Error,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
    
    private void DonationButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://buymeacoffee.com/jisrubyy",
                UseShellExecute = true
            });
        }
        catch
        {
            System.Windows.MessageBox.Show(
                Localization.CurrentLanguage == "ko" ? 
                    "웹 브라우저를 열 수 없습니다." : 
                    "Cannot open web browser.",
                Localization.Error,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
    
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
