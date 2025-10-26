using System.Windows;

namespace JISMemo;

public partial class PasswordWindow : Window
{
    public string Password { get; private set; } = "";
    public string? Hint { get; set; }

    public PasswordWindow(string? hint = null)
    {
        InitializeComponent();
        
        if (!string.IsNullOrEmpty(hint))
        {
            Hint = hint;
            HintLabel.Visibility = Visibility.Visible;
            HintText.Text = hint;
            HintText.Visibility = Visibility.Visible;
        }
        
        PasswordBox.Focus();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(PasswordBox.Password))
        {
            ErrorText.Text = "비밀번호를 입력해주세요.";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        Password = PasswordBox.Password;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
