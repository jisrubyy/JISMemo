using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace JISMemo;

public partial class SettingsWindow : Window
{
    public string? SelectedPath { get; private set; }
    public bool UseCustomPath { get; private set; }

    public SettingsWindow(string currentPath, bool useCustomPath)
    {
        InitializeComponent();
        
        CurrentPathTextBlock.Text = currentPath;
        
        if (useCustomPath)
        {
            CustomLocationRadio.IsChecked = true;
            CustomPathTextBox.Text = Path.GetDirectoryName(currentPath);
        }
        else
        {
            DefaultLocationRadio.IsChecked = true;
        }
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "메모 저장 폴더 선택"
        };

        if (dialog.ShowDialog() == true)
        {
            CustomPathTextBox.Text = dialog.FolderName;
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        UseCustomPath = CustomLocationRadio.IsChecked == true;
        
        if (UseCustomPath)
        {
            if (string.IsNullOrWhiteSpace(CustomPathTextBox.Text))
            {
                System.Windows.MessageBox.Show("사용자 지정 경로를 선택해주세요.", "경고", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                Directory.CreateDirectory(CustomPathTextBox.Text);
                SelectedPath = Path.Combine(CustomPathTextBox.Text, "JISMemo");
            }
            catch
            {
                System.Windows.MessageBox.Show("선택한 경로에 접근할 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }
        else
        {
            SelectedPath = null;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}