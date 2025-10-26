using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
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
    private readonly NoteService _noteService = new();
    private readonly ObservableCollection<StickyNote> _notes = new();
    private readonly List<System.Windows.Controls.Border> _noteControls = new();
    private readonly Dictionary<StickyNote, System.Windows.Shapes.Rectangle> _minimapRects = new();
    private NotifyIcon? _notifyIcon;
    private string? _currentPassword;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
        StateChanged += MainWindow_StateChanged;
        InitializeSystemTray();
    }

    private void InitializeSystemTray()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = CreateStickyNoteIcon(),
            Visible = true,
            Text = $"{AppInfo.AppName} v{AppInfo.Version} - 더블클릭으로 열기"
        };
        
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("열기", null, (s, e) => { Show(); WindowState = WindowState.Normal; Activate(); });
        contextMenu.Items.Add("설정", null, (s, e) => ShowSettings());
        contextMenu.Items.Add("도움말", null, (s, e) => ShowHelp());
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("종료", null, (s, e) => WpfApplication.Current.Shutdown());
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
        var bitmap = new System.Drawing.Bitmap(16, 16);
        using (var g = System.Drawing.Graphics.FromImage(bitmap))
        {
            g.Clear(System.Drawing.Color.Transparent);
            
            var rect = new System.Drawing.Rectangle(1, 1, 14, 14);
            g.FillRectangle(new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(255, 255, 153)), rect);
            g.DrawRectangle(new System.Drawing.Pen(System.Drawing.Color.FromArgb(200, 200, 100)), rect);
            
            var corner = new System.Drawing.Point[] {
                new System.Drawing.Point(12, 1),
                new System.Drawing.Point(15, 1),
                new System.Drawing.Point(15, 4)
            };
            g.FillPolygon(new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(230, 230, 120)), corner);
            g.DrawPolygon(new System.Drawing.Pen(System.Drawing.Color.FromArgb(200, 200, 100)), corner);
            
            var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(150, 150, 150));
            g.DrawLine(pen, 3, 5, 11, 5);
            g.DrawLine(pen, 3, 7, 10, 7);
            g.DrawLine(pen, 3, 9, 12, 9);
            g.DrawLine(pen, 3, 11, 9, 11);
        }
        
        var handle = bitmap.GetHicon();
        return new System.Drawing.Icon(System.Drawing.Icon.FromHandle(handle), 16, 16);
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
            _notifyIcon!.ShowBalloonTip(2000, $"{AppInfo.AppName} v{AppInfo.Version}", "시스템 트레이로 최소화되었습니다.", System.Windows.Forms.ToolTipIcon.Info);
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
        // 암호화가 활성화되어 있으면 비밀번호 입력 요청
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
        else
        {
            // 첫 실행 시 비밀번호 설정 여부 확인
            var result = System.Windows.MessageBox.Show("메모를 암호화하시겠습니까?\n(나중에 설정에서 변경 가능)", "암호화 설정", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                var setupWindow = new PasswordSetupWindow();
                if (setupWindow.ShowDialog() == true)
                {
                    _noteService.SetupPassword(setupWindow.Password, setupWindow.Hint);
                    _currentPassword = setupWindow.Password;
                }
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

    private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        await _noteService.SaveNotesAsync(_notes.ToList(), _currentPassword);
    }

    private void AddNoteButton_Click(object sender, RoutedEventArgs e)
    {
        var note = new StickyNote
        {
            Left = 200 + _notes.Count * 20,
            Top = 80 + _notes.Count * 20,
            Content = "새 메모"
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

        // Root grid with a header ("창틀") and content area
        var rootGrid = new System.Windows.Controls.Grid();
        rootGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(24) });
        rootGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });

        // Header bar (title bar for the note)
        var headerGrid = new System.Windows.Controls.Grid
        {
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60))
        };
        headerGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(24) });

        var titleText = new System.Windows.Controls.TextBlock
        {
            Text = "메모",
            Foreground = System.Windows.Media.Brushes.White,
            VerticalAlignment = System.Windows.VerticalAlignment.Center,
            Margin = new System.Windows.Thickness(6, 0, 0, 0),
            FontSize = 12
        };

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
        System.Windows.Controls.Grid.SetColumn(closeButton, 1);
        headerGrid.Children.Add(titleText);
        headerGrid.Children.Add(closeButton);
        System.Windows.Controls.Grid.SetRow(headerGrid, 0);

        // Content area (image preview + textbox)
        var contentPanel = new System.Windows.Controls.StackPanel();
        System.Windows.Controls.Grid.SetRow(contentPanel, 1);

        // 텍스트 영역 높이(이미지 유무에 따라 조정)
        var textHeight = string.IsNullOrEmpty(note.ImageData) ? 170 : 80;

        var textBox = new System.Windows.Controls.TextBox
        {
            Text = note.Content,
            Background = System.Windows.Media.Brushes.Transparent,
            BorderThickness = new System.Windows.Thickness(0),
            TextWrapping = System.Windows.TextWrapping.Wrap,
            AcceptsReturn = true,
            Margin = new System.Windows.Thickness(5, 2, 5, 5),
            Height = textHeight,
            VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
            FontSize = note.FontSize
        };

        // 텍스트 변경 시 모델에 반영
        textBox.TextChanged += (s, e) => note.Content = textBox.Text;

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

        // 이미지 붙여넣기 핸들링은 기존 로직 유지
        System.Windows.DataObject.AddPastingHandler(textBox, (s, pastingArgs) =>
        {
            BitmapSource? imageSource = null;

            imageSource = ExtractBitmapSourceFromDataObject(pastingArgs.DataObject);

            try { LogClipboardFormats(pastingArgs.DataObject, note.Id ?? note.GetHashCode().ToString()); } catch { }

            if (imageSource == null)
            {
                try
                {
                    if (System.Windows.Clipboard.ContainsImage())
                    {
                        imageSource = System.Windows.Clipboard.GetImage();
                    }
                }
                catch { }
            }

            if (imageSource == null)
            {
                try { imageSource = ExtractFromWinFormsClipboard(); } catch { }
            }

            if (imageSource != null)
            {
                note.ImageData = ConvertImageToBase64(imageSource);
                RefreshNoteControl(note, noteControl);
                pastingArgs.CancelCommand();
            }
        });

        textBox.PreviewKeyDown += (s, ke) =>
        {
            if (ke.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                BitmapSource? imageSource = null;
                try
                {
                    var dataObj = System.Windows.Clipboard.GetDataObject();
                    if (dataObj != null)
                    {
                        imageSource = ExtractBitmapSourceFromDataObject(dataObj);
                    }

                    if (imageSource == null && System.Windows.Clipboard.ContainsImage())
                    {
                        imageSource = System.Windows.Clipboard.GetImage();
                    }
                }
                catch { }

                if (imageSource != null)
                {
                    note.ImageData = ConvertImageToBase64(imageSource);
                    RefreshNoteControl(note, noteControl);
                    ke.Handled = true;
                }
            }
        };

        textBox.CommandBindings.Add(new System.Windows.Input.CommandBinding(System.Windows.Input.ApplicationCommands.Paste, (s, e) =>
        {
            BitmapSource? imageSource = null;

            try
            {
                var dataObj = System.Windows.Clipboard.GetDataObject();
                if (dataObj != null)
                {
                    imageSource = ExtractBitmapSourceFromDataObject(dataObj);
                }
                try { LogClipboardFormats(dataObj, note.Id ?? note.GetHashCode().ToString()); } catch { }
                if (imageSource == null && System.Windows.Clipboard.ContainsImage())
                {
                    imageSource = System.Windows.Clipboard.GetImage();
                }
            }
            catch { }

            if (imageSource == null)
            {
                try { imageSource = ExtractFromWinFormsClipboard(); } catch { }
            }

            if (imageSource != null)
            {
                note.ImageData = ConvertImageToBase64(imageSource);
                RefreshNoteControl(note, noteControl);
                e.Handled = true;
            }
        }));

        textBox.ContextMenu = CreateNoteContextMenu();

        // 이미지가 있으면 이미지 미리보기, 이후 텍스트 박스
        if (!string.IsNullOrEmpty(note.ImageData))
        {
            var imageControl = CreateImageControl(note.ImageData);
            contentPanel.Children.Add(imageControl);
        }
        contentPanel.Children.Add(textBox);

        // Close button removes the note
        closeButton.Click += (s, e) =>
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
                if (newWidth > 150) { noteControl.Width = newWidth; note.Width = newWidth; }
                if (newHeight > 100) { noteControl.Height = newHeight; note.Height = newHeight; }
            }
        };
        resizeHandle.MouseLeftButtonUp += (s, e) =>
        {
            resizeHandle.ReleaseMouseCapture();
        };

        // Assemble
        rootGrid.Children.Add(headerGrid);
        rootGrid.Children.Add(contentPanel);
        rootGrid.Children.Add(resizeHandle);
        noteControl.Child = rootGrid;

        System.Windows.Controls.Canvas.SetLeft(noteControl, note.Left);
        System.Windows.Controls.Canvas.SetTop(noteControl, note.Top);

        NotesCanvas.Children.Add(noteControl);
        _noteControls.Add(noteControl);
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
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

    private System.Windows.Controls.Image CreateImageControl(string base64Data)
    {
        var bytes = Convert.FromBase64String(base64Data);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.StreamSource = new MemoryStream(bytes);
        bitmap.EndInit();
        
        return new System.Windows.Controls.Image
        {
            Source = bitmap,
            MaxHeight = 100,
            Margin = new System.Windows.Thickness(5, 0, 5, 5),
            Stretch = System.Windows.Media.Stretch.Uniform
        };
    }

    private void RefreshNoteControl(StickyNote note, System.Windows.Controls.Border noteControl)
    {
        var index = _noteControls.IndexOf(noteControl);
        NotesCanvas.Children.Remove(noteControl);
        _noteControls.Remove(noteControl);
        CreateNoteControl(note);
    }

    private System.Windows.Controls.ContextMenu CreateNoteContextMenu()
    {
        var contextMenu = new System.Windows.Controls.ContextMenu();
        
        var settingsMenuItem = new System.Windows.Controls.MenuItem
        {
            Header = "설정"
        };
        settingsMenuItem.Click += (s, e) => ShowSettings();
        
        var helpMenuItem = new System.Windows.Controls.MenuItem
        {
            Header = "도움말"
        };
        helpMenuItem.Click += (s, e) => ShowHelp();
        
        contextMenu.Items.Add(settingsMenuItem);
        contextMenu.Items.Add(helpMenuItem);
        return contextMenu;
    }

    private void ShowHelp()
    {
        var helpWindow = new HelpWindow();
        helpWindow.ShowDialog();
    }

    private async void ShowSettings()
    {
        var settingsWindow = new SettingsWindow(_noteService.GetCurrentDataPath(), _noteService.IsUsingCustomPath());
        if (settingsWindow.ShowDialog() == true)
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
            
            System.Windows.MessageBox.Show("설정이 저장되고 새 위치에서 메모를 불러왔습니다.", "설정 완료", MessageBoxButton.OK, MessageBoxImage.Information);
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

}

