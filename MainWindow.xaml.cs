using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using JISMemo.Models;
using JISMemo.Services;
using WpfApplication = System.Windows.Application;
using WinFormsApplication = System.Windows.Forms.Application;
using NotifyIcon = System.Windows.Forms.NotifyIcon;
using ContextMenuStrip = System.Windows.Forms.ContextMenuStrip;
using SystemIcons = System.Drawing.SystemIcons;

namespace JISMemo;

public partial class MainWindow : Window
{
    private NoteService _noteService;
    private readonly ObservableCollection<StickyNote> _notes = new();
    private readonly List<System.Windows.Controls.Border> _noteControls = new();
    private readonly Dictionary<StickyNote, System.Windows.Shapes.Rectangle> _minimapRects = new();
    private NotifyIcon? _notifyIcon;
    private string? _currentPassword;
    private string _currentUser = "";
    private readonly SettingsService _settingsService = new();
    private AppSettings _appSettings;

    public MainWindow()
    {
        InitializeComponent();
        
        var userService = new UserService();
        userService.EnsureDefaultUser();
        
        var currentUser = userService.GetCurrentUser();
        if (string.IsNullOrEmpty(currentUser))
        {
            _currentUser = "Default";
            userService.SetCurrentUser(_currentUser);
        }
        else
        {
            _currentUser = currentUser;
        }
        
        _noteService = new NoteService(_currentUser);
        // 기본 언어를 영문으로 설정
        Localization.CurrentLanguage = "en";
        _appSettings = _settingsService.LoadSettings();
        // 설정 파일에 언어 설정이 있으면 사용
        if (!string.IsNullOrEmpty(_appSettings.Language))
        {
            Localization.CurrentLanguage = _appSettings.Language;
        }
        
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
        StateChanged += MainWindow_StateChanged;
        UpdateUITexts();
        InitializeSystemTray();
        UpdateCurrentUserDisplay();
        ApplyBackgroundColor();
    }
    
    private void ApplyBackgroundColor()
    {
        try
        {
            NotesCanvas.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_appSettings.BackgroundColor));
        }
        catch
        {
            NotesCanvas.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.WhiteSmoke);
        }
    }
    
    private void UpdateCurrentUserDisplay()
    {
        CurrentUserText.Text = $"{Localization.CurrentUser}: {_currentUser}";
    }
    
    private void UpdateUITexts()
    {
        AddNoteButton.Content = Localization.AddNote;
        FindNotesButton.Content = Localization.FindNotes;
        ArrangeNotesButton.Content = Localization.ArrangeNotes;
        SwitchUserButton.Content = Localization.SwitchUser;
        SettingsButton.Content = Localization.Settings;
        HelpButton.Content = Localization.Help;
        CreditButton.Content = Localization.Credit;
        MinimizeButton.Content = Localization.Minimize;
        ExitButton.Content = Localization.Exit;
        UIScaleLabel.Text = Localization.UIScale;
        UpdateCurrentUserDisplay();
    }

    private void InitializeSystemTray()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = CreateStickyNoteIcon(),
            Visible = true,
            Text = $"{AppInfo.AppName} v{AppInfo.FullVersion} - {Localization.DoubleClickToOpen}"
        };
        
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add(Localization.Open, null, (s, e) => { Show(); WindowState = WindowState.Normal; Activate(); });
        contextMenu.Items.Add(Localization.SwitchUser, null, (s, e) => SwitchUser());
        contextMenu.Items.Add(Localization.Settings, null, (s, e) => ShowSettings());
        contextMenu.Items.Add(Localization.Help, null, (s, e) => ShowHelp());
        contextMenu.Items.Add(Localization.Credit, null, (s, e) => { var creditWindow = new CreditWindow(); creditWindow.ShowDialog(); });
        contextMenu.Items.Add("-");
        contextMenu.Items.Add(Localization.Exit, null, (s, e) => WpfApplication.Current.Shutdown());
        _notifyIcon.ContextMenuStrip = contextMenu;
        
        _notifyIcon.DoubleClick += (s, e) => { Show(); WindowState = WindowState.Normal; Activate(); };
        _notifyIcon.Click += (s, e) => 
        {
            if (((System.Windows.Forms.MouseEventArgs)e).Button == System.Windows.Forms.MouseButtons.Left)
            {
                Show(); 
                WindowState = WindowState.Normal; 
                Activate();
            }
        };
    }

    private System.Drawing.Icon CreateStickyNoteIcon()
    {
        try
        {
            var uri = new Uri("pack://application:,,,/Resources/JISMemo_Icon.ico");
            var streamInfo = WpfApplication.GetResourceStream(uri);
            if (streamInfo != null)
            {
                return new System.Drawing.Icon(streamInfo.Stream, 16, 16);
            }
        }
        catch { }
        
        return SystemIcons.Application;
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
            _notifyIcon!.ShowBalloonTip(2000, $"{AppInfo.AppName} v{AppInfo.FullVersion}", Localization.MinimizedToTray, System.Windows.Forms.ToolTipIcon.Info);
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (_noteService.IsEncryptionEnabled())
        {
            var hint = _noteService.GetPasswordHint();
            var passwordWindow = new PasswordWindow(hint);
            
            while (true)
            {
                if (passwordWindow.ShowDialog() != true)
                {
                    WpfApplication.Current.Shutdown();
                    return;
                }
                
                if (_noteService.VerifyPassword(passwordWindow.Password))
                {
                    _currentPassword = passwordWindow.Password;
                    break;
                }
                
                System.Windows.MessageBox.Show("비밀번호가 올바르지 않습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                passwordWindow = new PasswordWindow(hint);
            }
        }
        
        var notes = await _noteService.LoadNotesAsync(_currentPassword);
        foreach (var note in notes)
        {
            _notes.Add(note);
            CreateNoteControl(note);
        }
        UpdateMinimap();
    }

    private async void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
        await _noteService.SaveNotesAsync(_notes.ToList(), _currentPassword);
    }

    private void AddNoteButton_Click(object sender, RoutedEventArgs e)
    {
        var note = new StickyNote
        {
            Left = 200 + _notes.Count * 20,
            Top = 80 + _notes.Count * 20,
            Content = Localization.NewNote,
            Owner = _currentUser,
            DeviceType = Environment.OSVersion.Platform.ToString(),
            DeviceName = Environment.MachineName,
            Color = _appSettings.DefaultNoteColor
        };
        _notes.Add(note);
        CreateNoteControl(note);
        UpdateMinimap();
    }

    private void CreateNoteControl(StickyNote note)
    {
        var noteControl = new System.Windows.Controls.Border
        {
            Width = note.Width,
            Height = note.Height,
            Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(note.Color)),
            CornerRadius = new System.Windows.CornerRadius(6),
            BorderBrush = System.Windows.Media.Brushes.Gray,
            BorderThickness = new System.Windows.Thickness(1)
        };

        // Root grid with a header ("창틀"), content area, and status bar
        var rootGrid = new System.Windows.Controls.Grid();
        rootGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(24) });
        rootGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        rootGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(20) });

        // Header bar (title bar for the note)
        var headerGrid = new System.Windows.Controls.Grid
        {
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60))
        };
        headerGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(24) });
        headerGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(24) });

        var titleText = new System.Windows.Controls.TextBlock
        {
            Foreground = System.Windows.Media.Brushes.White,
            VerticalAlignment = System.Windows.VerticalAlignment.Center,
            Margin = new System.Windows.Thickness(6, 0, 0, 0),
            FontSize = 12
        };
        var titleBinding = new System.Windows.Data.Binding("Title") { Source = note };
        titleText.SetBinding(System.Windows.Controls.TextBlock.TextProperty, titleBinding);

        var infoButton = new System.Windows.Controls.Button
        {
            Content = "ℹ",
            Width = 18,
            Height = 18,
            Margin = new System.Windows.Thickness(0, 3, 3, 3),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            VerticalAlignment = System.Windows.VerticalAlignment.Center,
            Background = System.Windows.Media.Brushes.Transparent,
            Foreground = System.Windows.Media.Brushes.White,
            BorderBrush = System.Windows.Media.Brushes.Transparent,
            Padding = new System.Windows.Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
            FontSize = 14
        };
        System.Windows.Controls.Grid.SetColumn(infoButton, 1);

        var closeButton = new System.Windows.Controls.Button
        {
            Content = "×",
            Width = 18,
            Height = 18,
            Margin = new System.Windows.Thickness(0, 3, 3, 3),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            VerticalAlignment = System.Windows.VerticalAlignment.Center,
            Background = System.Windows.Media.Brushes.Transparent,
            Foreground = System.Windows.Media.Brushes.White,
            BorderBrush = System.Windows.Media.Brushes.Transparent,
            Padding = new System.Windows.Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        System.Windows.Controls.Grid.SetColumn(closeButton, 2);
        headerGrid.Children.Add(titleText);
        headerGrid.Children.Add(infoButton);
        headerGrid.Children.Add(closeButton);
        System.Windows.Controls.Grid.SetRow(headerGrid, 0);

        // Info button shows note information
        infoButton.Click += (s, e) =>
        {
            var infoWindow = new NoteInfoWindow(note);
            infoWindow.ShowDialog();
        };

        // Content area with ScrollViewer
        var scrollViewer = new System.Windows.Controls.ScrollViewer
        {
            VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Disabled
        };
        var contentPanel = new System.Windows.Controls.StackPanel();
        scrollViewer.Content = contentPanel;
        System.Windows.Controls.Grid.SetRow(scrollViewer, 1);

        // 텍스트 영역 높이 조정
        var textHeight = Math.Max(80, 220 - note.ImageDataList.Count * 25);

        var textColor = (note.Color == "#4A4A4A" || note.Color == "#2C3E50") ? "#FFFFFF" : "#000000";
        
        var textBox = new System.Windows.Controls.TextBox
        {
            Text = note.Content,
            Background = System.Windows.Media.Brushes.Transparent,
            Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(textColor)),
            BorderThickness = new System.Windows.Thickness(0),
            TextWrapping = System.Windows.TextWrapping.Wrap,
            AcceptsReturn = true,
            Margin = new System.Windows.Thickness(5, 2, 5, 5),
            MinHeight = textHeight,
            FontSize = note.FontSize
        };

        // 텍스트 변경 시 모델에 반영
        textBox.TextChanged += (s, e) => 
        {
            note.Content = textBox.Text;
        };

        // Ctrl + 마우스 휠로 폰트 크기 확대/축소 (줌)
        textBox.PreviewMouseWheel += (s, e) =>
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                e.Handled = true;
                // 휠 한 칸(±120) 당 1pt 변경. 더 부드럽게 하려면 0.5 등으로 조정 가능
                double step = e.Delta > 0 ? 1.0 : -1.0;
                double newSize = note.FontSize + step;
                if (newSize < 8) newSize = 8;
                if (newSize > 48) newSize = 48;
                note.FontSize = newSize;
                textBox.FontSize = newSize;
            }
        };







        // 이미지 붙여넣기 처리
        textBox.PreviewKeyDown += (s, ke) =>
        {
            if (ke.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (System.Windows.Clipboard.ContainsImage())
                {
                    var image = System.Windows.Clipboard.GetImage();
                    if (image != null)
                    {
                        var cursorPos = textBox.CaretIndex;
                        var beforeText = textBox.Text.Substring(0, cursorPos);
                        var afterText = textBox.Text.Substring(cursorPos);
                        
                        var imageData = ConvertImageToBase64(image);
                        var imageIndex = note.ImageDataList.Count;
                        note.ImageDataList.Add(imageData);
                        
                        var imageMarker = $"[IMG{imageIndex}]";
                        textBox.Text = beforeText + imageMarker + afterText;
                        textBox.CaretIndex = cursorPos + imageMarker.Length;
                        
                        RefreshNoteControl(note, noteControl);
                        ke.Handled = true;
                    }
                }
            }
        };

        textBox.ContextMenu = CreateNoteContextMenu(note, noteControl);

        // 텍스트와 이미지를 순서대로 배치
        if (note.Content.Contains("[IMG"))
        {
            var textParts = SplitTextByImageMarkers(note.Content);
            
            for (int i = 0; i < textParts.Count; i++)
            {
                var part = textParts[i];
                
                if (part.StartsWith("[IMG") && part.EndsWith("]"))
                {
                    var indexStr = part.Substring(4, part.Length - 5);
                    if (int.TryParse(indexStr, out var imgIndex) && imgIndex < note.ImageDataList.Count)
                    {
                        var imageControl = CreateImageControl(note.ImageDataList[imgIndex], note, noteControl, imgIndex);
                        contentPanel.Children.Add(imageControl);
                    }
                }
                else if (!string.IsNullOrEmpty(part))
                {
                    var partTextBox = CreateTextBoxPart(part, note, textColor);
                    contentPanel.Children.Add(partTextBox);
                }
            }
            
            // 마커가 있을 때는 편집용 텍스트박스를 마지막에 추가
            var editTextBox = new System.Windows.Controls.TextBox
            {
                Text = "",
                Background = System.Windows.Media.Brushes.Transparent,
                Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(textColor)),
                BorderThickness = new System.Windows.Thickness(0),
                TextWrapping = System.Windows.TextWrapping.Wrap,
                AcceptsReturn = true,
                Margin = new System.Windows.Thickness(5, 2, 5, 5),
                MinHeight = 40,
                FontSize = note.FontSize
            };
            

            
            // 편집용 텍스트박스에도 이미지 붙여넣기 기능 추가
            editTextBox.PreviewKeyDown += (s, ke) =>
            {
                if (ke.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    if (System.Windows.Clipboard.ContainsImage())
                    {
                        var image = System.Windows.Clipboard.GetImage();
                        if (image != null)
                        {
                            // 현재 편집 중인 텍스트를 메인 컨텐트에 추가
                            if (!string.IsNullOrEmpty(editTextBox.Text))
                            {
                                note.Content += editTextBox.Text;
                            }
                            
                            var imageData = ConvertImageToBase64(image);
                            var imageIndex = note.ImageDataList.Count;
                            note.ImageDataList.Add(imageData);
                            
                            var imageMarker = $"[IMG{imageIndex}]";
                            note.Content += imageMarker;
                            
                            RefreshNoteControl(note, noteControl);
                            ke.Handled = true;
                        }
                    }
                }
            };
            
            // 편집용 텍스트박스에서 포커스를 잃을 때 텍스트를 메인 컨텐트에 추가
            editTextBox.LostFocus += (s, e) =>
            {
                if (!string.IsNullOrEmpty(editTextBox.Text))
                {
                    note.Content += editTextBox.Text;
                    editTextBox.Text = "";
                }
            };
            
            contentPanel.Children.Add(editTextBox);
        }
        else
        {
            contentPanel.Children.Add(textBox);
        }

        // Close button removes the note
        closeButton.Click += (s, e) =>
        {
            var result = System.Windows.MessageBox.Show(
                Localization.DeleteNoteMessage,
                Localization.DeleteNoteTitle,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                _notes.Remove(note);
                NotesCanvas.Children.Remove(noteControl);
                _noteControls.Remove(noteControl);
                if (_minimapRects.TryGetValue(note, out var rect))
                {
                    MinimapCanvas.Children.Remove(rect);
                    _minimapRects.Remove(note);
                }
                UpdateMinimap();
            }
        };

        // Header drag (drag only by header, with offset so it feels like a title bar)
        System.Windows.Point clickOffset = new System.Windows.Point(0, 0);
        headerGrid.MouseLeftButtonDown += (s, e) =>
        {
            headerGrid.CaptureMouse();
            clickOffset = e.GetPosition(noteControl);
            e.Handled = true;
        };
        headerGrid.MouseMove += (s, e) =>
        {
            if (headerGrid.IsMouseCaptured && e.LeftButton == MouseButtonState.Pressed)
            {
                var pos = e.GetPosition(NotesCanvas);
                note.Left = pos.X - clickOffset.X;
                note.Top = pos.Y - clickOffset.Y;
                System.Windows.Controls.Canvas.SetLeft(noteControl, note.Left);
                System.Windows.Controls.Canvas.SetTop(noteControl, note.Top);
                UpdateMinimapRect(note);
            }
        };
        headerGrid.MouseLeftButtonUp += (s, e) =>
        {
            headerGrid.ReleaseMouseCapture();
        };

        // 크기 조절 핸들 (우측 하단)
        var resizeHandle = new System.Windows.Controls.Border
        {
            Width = 12,
            Height = 12,
            Background = System.Windows.Media.Brushes.DarkGray,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            VerticalAlignment = System.Windows.VerticalAlignment.Bottom,
            Cursor = System.Windows.Input.Cursors.SizeNWSE,
            Margin = new System.Windows.Thickness(0, 0, 2, 2)
        };
        System.Windows.Controls.Grid.SetRowSpan(resizeHandle, 2);

        System.Windows.Point resizeStart = new System.Windows.Point();
        double startWidth = 0, startHeight = 0;
        resizeHandle.MouseLeftButtonDown += (s, e) =>
        {
            resizeHandle.CaptureMouse();
            resizeStart = e.GetPosition(NotesCanvas);
            startWidth = noteControl.Width;
            startHeight = noteControl.Height;
            e.Handled = true;
        };
        resizeHandle.MouseMove += (s, e) =>
        {
            if (resizeHandle.IsMouseCaptured)
            {
                var pos = e.GetPosition(NotesCanvas);
                var newWidth = startWidth + (pos.X - resizeStart.X);
                var newHeight = startHeight + (pos.Y - resizeStart.Y);
                if (newWidth > 200) { noteControl.Width = newWidth; note.Width = newWidth; }
                if (newHeight > 150) { noteControl.Height = newHeight; note.Height = newHeight; }
            }
        };
        resizeHandle.MouseLeftButtonUp += (s, e) =>
        {
            resizeHandle.ReleaseMouseCapture();
        };

        // Status bar at the bottom
        var statusBar = new System.Windows.Controls.Border
        {
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240)),
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 200, 200)),
            BorderThickness = new System.Windows.Thickness(0, 1, 0, 0)
        };
        var statusText = new System.Windows.Controls.TextBlock
        {
            Text = $"{Localization.LastModified} {note.ModifiedAt:yyyy-MM-dd HH:mm:ss}",
            FontSize = 10,
            Foreground = System.Windows.Media.Brushes.Gray,
            VerticalAlignment = System.Windows.VerticalAlignment.Center,
            Margin = new System.Windows.Thickness(5, 0, 5, 0)
        };
        statusBar.Child = statusText;
        System.Windows.Controls.Grid.SetRow(statusBar, 2);

        // Update status bar when content changes
        textBox.TextChanged += (s, e) => 
        {
            statusText.Text = $"{Localization.LastModified} {note.ModifiedAt:yyyy-MM-dd HH:mm:ss}";
        };

        // Assemble
        rootGrid.Children.Add(headerGrid);
        rootGrid.Children.Add(scrollViewer);
        rootGrid.Children.Add(statusBar);
        rootGrid.Children.Add(resizeHandle);
        noteControl.Child = rootGrid;

        System.Windows.Controls.Canvas.SetLeft(noteControl, note.Left);
        System.Windows.Controls.Canvas.SetTop(noteControl, note.Top);

        NotesCanvas.Children.Add(noteControl);
        _noteControls.Add(noteControl);
    }

    private async void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        await _noteService.SaveNotesAsync(_notes.ToList(), _currentPassword);
        WpfApplication.Current.Shutdown();
    }

    private string ConvertImageToBase64(BitmapSource image)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return Convert.ToBase64String(stream.ToArray());
    }

    private System.Windows.Controls.Image CreateImageControl(string base64Data, StickyNote note, System.Windows.Controls.Border noteControl, int imageIndex = -1)
    {
        var bytes = Convert.FromBase64String(base64Data);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.StreamSource = new MemoryStream(bytes);
        bitmap.EndInit();
        
        var image = new System.Windows.Controls.Image
        {
            Source = bitmap,
            MaxHeight = 100,
            Margin = new System.Windows.Thickness(5, 0, 5, 5),
            Stretch = System.Windows.Media.Stretch.Uniform
        };
        
        // 이미지에 우클릭 컨텍스트 메뉴 추가
        var contextMenu = new System.Windows.Controls.ContextMenu();
        var deleteMenuItem = new System.Windows.Controls.MenuItem
        {
            Header = "이 이미지 삭제"
        };
        deleteMenuItem.Click += (s, e) =>
        {
            if (imageIndex >= 0)
            {
                var marker = $"[IMG{imageIndex}]";
                note.Content = note.Content.Replace(marker, "");
            }
            note.ImageDataList.Remove(base64Data);
            RefreshNoteControl(note, noteControl);
        };
        contextMenu.Items.Add(deleteMenuItem);
        image.ContextMenu = contextMenu;
        
        return image;
    }

    private List<string> SplitTextByImageMarkers(string text)
    {
        var parts = new List<string>();
        var currentPos = 0;
        
        while (currentPos < text.Length)
        {
            var nextMarker = text.IndexOf("[IMG", currentPos);
            if (nextMarker == -1)
            {
                if (currentPos < text.Length)
                {
                    parts.Add(text.Substring(currentPos));
                }
                break;
            }
            
            if (nextMarker > currentPos)
            {
                parts.Add(text.Substring(currentPos, nextMarker - currentPos));
            }
            
            var markerEnd = text.IndexOf("]", nextMarker);
            if (markerEnd != -1)
            {
                parts.Add(text.Substring(nextMarker, markerEnd - nextMarker + 1));
                currentPos = markerEnd + 1;
            }
            else
            {
                currentPos = nextMarker + 1;
            }
        }
        
        return parts;
    }
    
    private System.Windows.Controls.TextBox CreateTextBoxPart(string text, StickyNote note, string textColor)
    {
        return new System.Windows.Controls.TextBox
        {
            Text = text,
            Background = System.Windows.Media.Brushes.Transparent,
            Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(textColor)),
            BorderThickness = new System.Windows.Thickness(0),
            TextWrapping = System.Windows.TextWrapping.Wrap,
            IsReadOnly = true,
            Margin = new System.Windows.Thickness(5, 2, 5, 2),
            FontSize = note.FontSize
        };
    }

    private void RefreshNoteControl(StickyNote note, System.Windows.Controls.Border noteControl)
    {
        var index = _noteControls.IndexOf(noteControl);
        NotesCanvas.Children.Remove(noteControl);
        _noteControls.Remove(noteControl);
        CreateNoteControl(note);
    }

    private System.Windows.Controls.ContextMenu CreateNoteContextMenu(StickyNote note, System.Windows.Controls.Border noteControl)
    {
        var contextMenu = new System.Windows.Controls.ContextMenu();
        
        var setTitleMenuItem = new System.Windows.Controls.MenuItem
        {
            Header = "제목 입력"
        };
        setTitleMenuItem.Click += (s, e) =>
        {
            var infoWindow = new NoteInfoWindow(note);
            infoWindow.ShowDialog();
        };
        contextMenu.Items.Add(setTitleMenuItem);
        
        // 모든 이미지 삭제 메뉴 (이미지가 있을 때만 표시)
        if (note.ImageDataList.Count > 0)
        {
            var removeAllImagesMenuItem = new System.Windows.Controls.MenuItem
            {
                Header = "모든 이미지 삭제"
            };
            removeAllImagesMenuItem.Click += (s, e) =>
            {
                note.ImageDataList.Clear();
                RefreshNoteControl(note, noteControl);
            };
            contextMenu.Items.Add(removeAllImagesMenuItem);
        }
        
        contextMenu.Items.Add(new System.Windows.Controls.Separator());

        var colorMenuItem = new System.Windows.Controls.MenuItem
        {
            Header = Localization.ColorTheme
        };
        
        var themes = new[]
        {
            new { Name = Localization.ClassicYellow, BgColor = "#FFFF99", TextColor = "#000000" },
            new { Name = Localization.PastelPink, BgColor = "#FFB3D9", TextColor = "#000000" },
            new { Name = Localization.MintGreen, BgColor = "#B3FFB3", TextColor = "#000000" },
            new { Name = Localization.SkyBlue, BgColor = "#B3E5FF", TextColor = "#000000" },
            new { Name = Localization.Lavender, BgColor = "#E6B3FF", TextColor = "#000000" },
            new { Name = Localization.Peach, BgColor = "#FFD9B3", TextColor = "#000000" },
            new { Name = Localization.DarkGray, BgColor = "#4A4A4A", TextColor = "#FFFFFF" },
            new { Name = Localization.NavyBlue, BgColor = "#2C3E50", TextColor = "#FFFFFF" }
        };
        
        foreach (var theme in themes)
        {
            var themeItem = new System.Windows.Controls.MenuItem
            {
                Header = theme.Name
            };
            themeItem.Click += (s, e) =>
            {
                note.Color = theme.BgColor;
                ApplyNoteTheme(noteControl, theme.BgColor, theme.TextColor);
            };
            colorMenuItem.Items.Add(themeItem);
        }
        
        contextMenu.Items.Add(colorMenuItem);
        contextMenu.Items.Add(new System.Windows.Controls.Separator());
        
        var switchUserMenuItem = new System.Windows.Controls.MenuItem
        {
            Header = Localization.SwitchUser
        };
        switchUserMenuItem.Click += (s, e) => SwitchUser();
        
        var settingsMenuItem = new System.Windows.Controls.MenuItem
        {
            Header = Localization.Settings
        };
        settingsMenuItem.Click += (s, e) => ShowSettings();
        
        var helpMenuItem = new System.Windows.Controls.MenuItem
        {
            Header = Localization.Help
        };
        helpMenuItem.Click += (s, e) => ShowHelp();
        
        var creditMenuItem = new System.Windows.Controls.MenuItem
        {
            Header = Localization.Credit
        };
        creditMenuItem.Click += (s, e) => { var creditWindow = new CreditWindow(); creditWindow.ShowDialog(); };
        
        contextMenu.Items.Add(switchUserMenuItem);
        contextMenu.Items.Add(settingsMenuItem);
        contextMenu.Items.Add(helpMenuItem);
        contextMenu.Items.Add(creditMenuItem);
        return contextMenu;
    }
    
    private void ApplyNoteTheme(System.Windows.Controls.Border noteControl, string bgColor, string textColor)
    {
        noteControl.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(bgColor));
        
        if (noteControl.Child is System.Windows.Controls.Grid grid)
        {
            foreach (var child in grid.Children)
            {
                if (child is System.Windows.Controls.StackPanel contentPanel)
                {
                    foreach (var item in contentPanel.Children)
                    {
                        if (item is System.Windows.Controls.TextBox textBox)
                        {
                            textBox.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(textColor));
                        }
                    }
                }
            }
        }
    }
    
    private async void SwitchUser()
    {
        await _noteService.SaveNotesAsync(_notes.ToList(), _currentPassword);
        
        var userSelection = new UserSelectionWindow();
        if (userSelection.ShowDialog() == true && !string.IsNullOrEmpty(userSelection.SelectedUser))
        {
            _currentUser = userSelection.SelectedUser;
            var userService = new UserService();
            userService.SetCurrentUser(_currentUser);
            
            _noteService = new NoteService(_currentUser);
            _currentPassword = null;
            
            foreach (var control in _noteControls.ToList())
            {
                NotesCanvas.Children.Remove(control);
            }
            _noteControls.Clear();
            _notes.Clear();
            _minimapRects.Clear();
            
            if (_noteService.IsEncryptionEnabled())
            {
                var hint = _noteService.GetPasswordHint();
                var passwordWindow = new PasswordWindow(hint);
                
                while (true)
                {
                    if (passwordWindow.ShowDialog() != true)
                    {
                        return;
                    }
                    
                    if (_noteService.VerifyPassword(passwordWindow.Password))
                    {
                        _currentPassword = passwordWindow.Password;
                        break;
                    }
                    
                    System.Windows.MessageBox.Show("비밀번호가 올바르지 않습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    passwordWindow = new PasswordWindow(hint);
                }
            }
            
            var notes = await _noteService.LoadNotesAsync(_currentPassword);
            foreach (var note in notes)
            {
                _notes.Add(note);
                CreateNoteControl(note);
            }
            UpdateMinimap();
            UpdateCurrentUserDisplay();
        }
    }

    private void ShowHelp()
    {
        var helpWindow = new HelpWindow();
        helpWindow.ShowDialog();
    }

    private async void ShowSettings()
    {
        await _noteService.SaveNotesAsync(_notes.ToList(), _currentPassword);
        
        var settingsWindow = new SettingsWindow(_noteService.GetCurrentDataPath(), _noteService.IsUsingCustomPath(), _noteService, 
            _appSettings.BackgroundColor, _appSettings.DefaultNoteColor, _appSettings.DefaultNoteTextColor);
        if (settingsWindow.ShowDialog() == true)
        {
            if (settingsWindow.PasswordRemoved)
            {
                await _noteService.SaveNotesAsync(_notes.ToList(), null);
                _currentPassword = null;
            }
            
            if (settingsWindow.SelectedPath != null)
            {
                await _noteService.SaveNotesAsync(_notes.ToList(), _currentPassword);
                _noteService.SetDataPath(settingsWindow.SelectedPath);
                
                foreach (var control in _noteControls.ToList())
                {
                    NotesCanvas.Children.Remove(control);
                }
                _noteControls.Clear();
                _notes.Clear();
                
                var notes = await _noteService.LoadNotesAsync(_currentPassword);
                foreach (var note in notes)
                {
                    _notes.Add(note);
                    CreateNoteControl(note);
                }
                UpdateMinimap();
            }
            
            if (settingsWindow.ColorChanged)
            {
                _appSettings.BackgroundColor = settingsWindow.NewBackgroundColor ?? "#F5F5F5";
                ApplyBackgroundColor();
            }
            
            if (settingsWindow.NoteThemeChanged)
            {
                _appSettings.DefaultNoteColor = settingsWindow.NewNoteColor ?? "#FFFF99";
                _appSettings.DefaultNoteTextColor = settingsWindow.NewNoteTextColor ?? "#000000";
            }
            
            if (settingsWindow.LanguageChanged)
            {
                _appSettings.Language = settingsWindow.NewLanguage ?? "ko";
            }
            
            if (settingsWindow.ColorChanged || settingsWindow.NoteThemeChanged || settingsWindow.LanguageChanged)
            {
                _settingsService.SaveSettings(_appSettings);
            }
            
            if (settingsWindow.LanguageChanged)
            {
                System.Windows.MessageBox.Show(
                    Localization.CurrentLanguage == "ko" ? 
                        "언어 변경은 프로그램 재시작 후 적용됩니다." : 
                        "Language change will be applied after restart.",
                    Localization.Information,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        ShowSettings();
    }

    private void HelpButton_Click(object sender, RoutedEventArgs e)
    {
        ShowHelp();
    }
    
    private void CreditButton_Click(object sender, RoutedEventArgs e)
    {
        var creditWindow = new CreditWindow();
        creditWindow.ShowDialog();
    }
    
    private void SwitchUserButton_Click(object sender, RoutedEventArgs e)
    {
        SwitchUser();
    }

    private void FindNotesButton_Click(object sender, RoutedEventArgs e)
    {
        var searchWindow = new NoteSearchWindow(_notes, note =>
        {
            var noteControl = _noteControls[_notes.IndexOf(note)];
            MainScrollViewer.ScrollToHorizontalOffset(note.Left - 100);
            MainScrollViewer.ScrollToVerticalOffset(note.Top - 100);
            HighlightNote(noteControl);
        });
        searchWindow.ShowDialog();
    }

    private async void HighlightNote(System.Windows.Controls.Border noteControl)
    {
        var originalBrush = noteControl.BorderBrush;
        var originalThickness = noteControl.BorderThickness;
        
        noteControl.BorderBrush = System.Windows.Media.Brushes.Red;
        noteControl.BorderThickness = new System.Windows.Thickness(3);
        
        await System.Threading.Tasks.Task.Delay(1000);
        
        noteControl.BorderBrush = originalBrush;
        noteControl.BorderThickness = originalThickness;
    }

    private void ArrangeNotesButton_Click(object sender, RoutedEventArgs e)
    {
        const double startX = 50;
        const double startY = 50;
        const double spacing = 20;
        
        var availableWidth = MainScrollViewer.ViewportWidth - startX - 50;
        
        double currentX = startX;
        double currentY = startY;
        double rowHeight = 0;
        
        for (int i = 0; i < _notes.Count; i++)
        {
            var note = _notes[i];
            
            if (currentX > startX && currentX + note.Width > availableWidth)
            {
                currentX = startX;
                currentY += rowHeight + spacing;
                rowHeight = 0;
            }
            
            note.Left = currentX;
            note.Top = currentY;
            
            var noteControl = _noteControls[i];
            System.Windows.Controls.Canvas.SetLeft(noteControl, note.Left);
            System.Windows.Controls.Canvas.SetTop(noteControl, note.Top);
            UpdateMinimapRect(note);
            
            currentX += note.Width + spacing;
            rowHeight = Math.Max(rowHeight, note.Height);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _notifyIcon?.Dispose();
        base.OnClosed(e);
    }

    private bool IsImageFile(string filePath)
    {
        var extension = Path.GetExtension(filePath)?.ToLowerInvariant();
        return extension == ".png" || extension == ".jpg" || extension == ".jpeg" || extension == ".gif" || extension == ".bmp";
    }

    private BitmapSource? LoadImageFromFile(string filePath)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(filePath);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private void EnsureLogDirectory(out string logPath)
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JISMemo");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        logPath = Path.Combine(dir, "paste_log.txt");
    }

    private void LogClipboardFormats(System.Windows.IDataObject dataObject, string noteId = "")
    {
        try
        {
            EnsureLogDirectory(out var path);
            var formats = dataObject?.GetFormats() ?? Array.Empty<string>();
            using var sw = new StreamWriter(path, append: true);
            sw.WriteLine($"[{DateTime.Now:O}] Paste attempt for note {noteId}");
            foreach (var f in formats)
            {
                sw.WriteLine("  " + f);
            }
            sw.WriteLine();
        }
        catch
        {
            // 로그 실패는 무시
        }
    }

    // DIB(byte[])를 BMP 스트림으로 감싸서 BitmapSource로 변환
    private BitmapSource? ConvertDibToBitmapSource(byte[] dib)
    {
        try
        {
            // BITMAPFILEHEADER (14 bytes)
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);

            // bfType 'BM'
            bw.Write((byte)'B');
            bw.Write((byte)'M');

            // bfSize = 14 + dib.Length
            bw.Write((int)(14 + dib.Length));
            bw.Write((short)0);
            bw.Write((short)0);

            // bfOffBits = 14 + BITMAPINFOHEADER size (assume 40)
            bw.Write((int)(14 + 40));

            // write dib
            bw.Write(dib);
            bw.Flush();
            ms.Seek(0, SeekOrigin.Begin);

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = ms;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    // 새 헬퍼: IDataObject에서 가능한 이미지 타입들을 추출하여 BitmapSource로 반환
    private BitmapSource? ExtractBitmapSourceFromDataObject(System.Windows.IDataObject dataObject)
    {
        if (dataObject == null) return null;

        try
        {
            // 새로운 시도: CF_DIB (DataFormats.Dib) 처리
            if (dataObject.GetDataPresent(System.Windows.DataFormats.Dib))
            {
                var dibObj = dataObject.GetData(System.Windows.DataFormats.Dib);
                if (dibObj is byte[] dibBytes)
                {
                    var bs = ConvertDibToBitmapSource(dibBytes);
                    if (bs != null) return bs;
                }
                else if (dibObj is MemoryStream dibStream)
                {
                    var arr = dibStream.ToArray();
                    var bs = ConvertDibToBitmapSource(arr);
                    if (bs != null) return bs;
                }
            }

            // PNG 포맷으로 클립보드에 들어오는 경우가 있어 시도
            if (dataObject.GetDataPresent("PNG"))
            {
                var pngObj = dataObject.GetData("PNG");
                if (pngObj is MemoryStream pngStream)
                {
                    pngStream.Seek(0, SeekOrigin.Begin);
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = pngStream;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }
                if (pngObj is byte[] pngBytes)
                {
                    using var ms = new MemoryStream(pngBytes);
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = ms;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }
            }

            if (dataObject.GetDataPresent(System.Windows.DataFormats.Bitmap))
            {
                var obj = dataObject.GetData(System.Windows.DataFormats.Bitmap);
                if (obj is BitmapSource bs)
                {
                    return bs;
                }

                if (obj is System.Drawing.Bitmap db)
                {
                    using var ms = new MemoryStream();
                    try
                    {
                        db.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        ms.Seek(0, SeekOrigin.Begin);
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = ms;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        return bitmap;
                    }
                    catch
                    {
                        // 변환 실패 시 계속 진행
                    }
                }

                if (obj is MemoryStream msObj)
                {
                    try
                    {
                        msObj.Seek(0, SeekOrigin.Begin);
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = msObj;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        return bitmap;
                    }
                    catch
                    {
                        // 변환 실패 시 계속 진행
                    }
                }

                if (obj is Stream s)
                {
                    try
                    {
                        s.Seek(0, SeekOrigin.Begin);
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = s;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        return bitmap;
                    }
                    catch
                    {
                        // 변환 실패 시 계속 진행
                    }
                }
            }

            if (dataObject.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                if (dataObject.GetData(System.Windows.DataFormats.FileDrop) is string[] files)
                {
                    var imageFile = files.FirstOrDefault(f => IsImageFile(f));
                    if (imageFile != null)
                    {
                        return LoadImageFromFile(imageFile);
                    }
                }
            }

            // 디버그용 로그: 어떤 포맷이 있는지 남김
            try
            {
                LogClipboardFormats(dataObject);
            }
            catch
            {
            }
        }
        catch
        {
            // 예외는 흡수
        }

        return null;
    }

    // WinForms 클립보드 경로에서 이미지 얻기 시도
    private BitmapSource? ExtractFromWinFormsClipboard()
    {
        try
        {
            // System.Windows.Forms.Clipboard는 STA에서 동작해야 함 (WPF 앱의 UI 스레드는 STA)
            if (System.Windows.Forms.Clipboard.ContainsImage())
            {
                var img = System.Windows.Forms.Clipboard.GetImage(); // System.Drawing.Image
                if (img is System.Drawing.Bitmap bmp)
                {
                    using var ms = new MemoryStream();
                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    ms.Seek(0, SeekOrigin.Begin);
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = ms;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }
            }

            var dataObj = System.Windows.Forms.Clipboard.GetDataObject();
            if (dataObj != null)
            {
                // Try CF_DIB via WinForms IDataObject
                if (dataObj.GetDataPresent(System.Windows.Forms.DataFormats.Dib))
                {
                    var dib = dataObj.GetData(System.Windows.Forms.DataFormats.Dib);
                    if (dib is MemoryStream ms)
                    {
                        var arr = ms.ToArray();
                        var bs = ConvertDibToBitmapSource(arr);
                        if (bs != null) return bs;
                    }
                    else if (dib is byte[] bArr)
                    {
                        var bs = ConvertDibToBitmapSource(bArr);
                        if (bs != null) return bs;
                    }
                }

                if (dataObj.GetDataPresent(System.Windows.Forms.DataFormats.Bitmap))
                {
                    var obj = dataObj.GetData(System.Windows.Forms.DataFormats.Bitmap);
                    if (obj is System.Drawing.Bitmap db)
                    {
                        using var ms2 = new MemoryStream();
                        db.Save(ms2, System.Drawing.Imaging.ImageFormat.Png);
                        ms2.Seek(0, SeekOrigin.Begin);
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = ms2;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        return bitmap;
                    }
                }
            }
        }
        catch
        {
            // 무시
        }

        return null;
    }

    private void UpdateMinimap()
    {
        const double scale = 200.0 / 3000.0;
        MinimapCanvas.Children.Clear();
        _minimapRects.Clear();

        foreach (var note in _notes)
        {
            var rect = new System.Windows.Shapes.Rectangle
            {
                Width = note.Width * scale,
                Height = note.Height * scale,
                Fill = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(note.Color)),
                Stroke = System.Windows.Media.Brushes.Gray,
                StrokeThickness = 0.5
            };
            System.Windows.Controls.Canvas.SetLeft(rect, note.Left * scale);
            System.Windows.Controls.Canvas.SetTop(rect, note.Top * scale);
            MinimapCanvas.Children.Add(rect);
            _minimapRects[note] = rect;
        }

        MinimapCanvas.Children.Add(ViewportRect);
        UpdateViewportRect();
    }

    private void UpdateMinimapRect(StickyNote note)
    {
        if (_minimapRects.TryGetValue(note, out var rect))
        {
            const double scale = 200.0 / 3000.0;
            System.Windows.Controls.Canvas.SetLeft(rect, note.Left * scale);
            System.Windows.Controls.Canvas.SetTop(rect, note.Top * scale);
        }
    }

    private void UpdateViewportRect()
    {
        const double scale = 200.0 / 3000.0;
        var x = MainScrollViewer.HorizontalOffset * scale;
        var y = MainScrollViewer.VerticalOffset * scale;
        var w = MainScrollViewer.ViewportWidth * scale;
        var h = MainScrollViewer.ViewportHeight * scale;

        System.Windows.Controls.Canvas.SetLeft(ViewportRect, x);
        System.Windows.Controls.Canvas.SetTop(ViewportRect, y);
        ViewportRect.Width = w;
        ViewportRect.Height = h;
    }

    private void MainScrollViewer_ScrollChanged(object sender, System.Windows.Controls.ScrollChangedEventArgs e)
    {
        UpdateViewportRect();
    }

    private void MinimapCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        MinimapCanvas.CaptureMouse();
        ScrollToMinimapPosition(e.GetPosition(MinimapCanvas));
        e.Handled = true;
    }

    private void ScrollToMinimapPosition(System.Windows.Point pos)
    {
        const double scale = 3000.0 / 200.0;
        var targetX = pos.X * scale - MainScrollViewer.ViewportWidth / 2;
        var targetY = pos.Y * scale - MainScrollViewer.ViewportHeight / 2;
        MainScrollViewer.ScrollToHorizontalOffset(Math.Max(0, targetX));
        MainScrollViewer.ScrollToVerticalOffset(Math.Max(0, targetY));
    }

    private void MinimapCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (MinimapCanvas.IsMouseCaptured && e.LeftButton == MouseButtonState.Pressed)
        {
            ScrollToMinimapPosition(e.GetPosition(MinimapCanvas));
        }
    }

    private void MinimapCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        MinimapCanvas.ReleaseMouseCapture();
    }

    private void UIScaleSlider_ValueChanged(object sender, RoutedEventArgs e)
    {
        if (UIScaleValue == null || RootGrid == null) return;
        
        var scale = UIScaleSlider.Value;
        UIScaleValue.Text = $"{(int)(scale * 100)}%";
        
        RootGrid.LayoutTransform = new System.Windows.Media.ScaleTransform(scale, scale);
    }

}
