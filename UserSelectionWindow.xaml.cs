using System.Windows;
using System.Windows.Input;
using JISMemo.Services;
using MessageBox = System.Windows.MessageBox;

namespace JISMemo;

public partial class UserSelectionWindow : Window
{
    private readonly UserService _userService = new();
    public string? SelectedUser { get; private set; }

    public UserSelectionWindow()
    {
        InitializeComponent();
        LoadUsers();
    }

    private void LoadUsers()
    {
        UserListBox.Items.Clear();
        var users = _userService.GetAllUsers();
        foreach (var user in users)
        {
            UserListBox.Items.Add(user.Username);
        }
    }

    private void AddUserButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Window
        {
            Title = "사용자 추가",
            Width = 350,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ResizeMode = ResizeMode.NoResize
        };

        var grid = new System.Windows.Controls.Grid { Margin = new Thickness(20) };
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

        var label = new System.Windows.Controls.TextBlock { Text = "사용자 이름:", Margin = new Thickness(0, 0, 0, 10) };
        System.Windows.Controls.Grid.SetRow(label, 0);

        var textBox = new System.Windows.Controls.TextBox { Height = 25, Margin = new Thickness(0, 0, 0, 10) };
        System.Windows.Controls.Grid.SetRow(textBox, 1);

        var buttonPanel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right };
        System.Windows.Controls.Grid.SetRow(buttonPanel, 3);

        var okButton = new System.Windows.Controls.Button { Content = "확인", Width = 80, Height = 30, Margin = new Thickness(0, 0, 10, 0) };
        okButton.Click += (s, args) =>
        {
            if (!string.IsNullOrWhiteSpace(textBox.Text))
            {
                _userService.AddUser(textBox.Text.Trim());
                LoadUsers();
                dialog.Close();
            }
            else
            {
                MessageBox.Show("사용자 이름을 입력하세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        };

        var cancelButton = new System.Windows.Controls.Button { Content = "취소", Width = 80, Height = 30 };
        cancelButton.Click += (s, args) => dialog.Close();

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);

        grid.Children.Add(label);
        grid.Children.Add(textBox);
        grid.Children.Add(buttonPanel);

        dialog.Content = grid;
        dialog.ShowDialog();
    }

    private void RemoveUserButton_Click(object sender, RoutedEventArgs e)
    {
        if (UserListBox.SelectedItem is string username)
        {
            var result = MessageBox.Show(
                $"'{username}' 사용자와 모든 데이터를 삭제하시겠습니까?",
                "사용자 제거",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _userService.RemoveUser(username);
                LoadUsers();
            }
        }
        else
        {
            MessageBox.Show("제거할 사용자를 선택하세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        if (UserListBox.SelectedItem is string username)
        {
            SelectedUser = username;
            DialogResult = true;
            Close();
        }
        else
        {
            MessageBox.Show("사용자를 선택하세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void UserListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (UserListBox.SelectedItem is string username)
        {
            SelectedUser = username;
            DialogResult = true;
            Close();
        }
    }
}
