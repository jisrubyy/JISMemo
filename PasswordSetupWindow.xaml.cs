using System.Windows;

namespace JISMemo;

public partial class PasswordSetupWindow : Window
{
    public string Password { get; private set; } = "";
    public string Hint { get; private set; } = "";

    public PasswordSetupWindow()
    {
        InitializeComponent();
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

        if (PasswordBox.Password.Length < 4)
        {
            ErrorText.Text = "비밀번호는 최소 4자 이상이어야 합니다.";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        if (PasswordBox.Password != ConfirmPasswordBox.Password)
        {
            ErrorText.Text = "비밀번호가 일치하지 않습니다.";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        Password = PasswordBox.Password;
        Hint = HintTextBox.Text.Trim();
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
