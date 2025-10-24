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
    private NotifyIcon? _notifyIcon;

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
        var notes = await _noteService.LoadNotesAsync();
        foreach (var note in notes)
        {
            _notes.Add(note);
            CreateNoteControl(note);
        }
    }

    private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        await _noteService.SaveNotesAsync(_notes.ToList());
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
    }

    private void CreateNoteControl(StickyNote note)
    {
        var noteControl = new System.Windows.Controls.Border
        {
            Width = 200,
            Height = 200,
            Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(note.Color)),
            CornerRadius = new System.Windows.CornerRadius(5),
            BorderBrush = System.Windows.Media.Brushes.Gray,
            BorderThickness = new System.Windows.Thickness(1)
        };

        var stackPanel = new System.Windows.Controls.StackPanel();
        
        var textBox = new System.Windows.Controls.TextBox
        {
            Text = note.Content,
            Background = System.Windows.Media.Brushes.Transparent,
            BorderThickness = new System.Windows.Thickness(0),
            TextWrapping = System.Windows.TextWrapping.Wrap,
            AcceptsReturn = true,
            Margin = new System.Windows.Thickness(5),
            Height = string.IsNullOrEmpty(note.ImageData) ? 170 : 80
        };

        textBox.TextChanged += (s, e) => note.Content = textBox.Text;

        System.Windows.DataObject.AddPastingHandler(textBox, (s, pastingArgs) =>
        {
            BitmapSource? imageSource = null;

            // 여러 형식에서 이미지 추출 시도
            imageSource = ExtractBitmapSourceFromDataObject(pastingArgs.DataObject);

            // 로그 남김
            try { LogClipboardFormats(pastingArgs.DataObject, note.Id ?? note.GetHashCode().ToString()); } catch { }

            // IDataObject에서 못찾으면 클립보드에서 직접 시도 (PrintScreen 등에서 CF_DIB가 올 때 안전한 경로)
            if (imageSource == null)
            {
                try
                {
                    if (System.Windows.Clipboard.ContainsImage())
                    {
                        imageSource = System.Windows.Clipboard.GetImage();
                    }
                }
                catch
                {
                    // 클립보드 접근 실패는 무시
                }
            }

            // WinForms 경로로도 시도
            if (imageSource == null)
            {
                try
                {
                    imageSource = ExtractFromWinFormsClipboard();
                }
                catch
                {
                }
            }

            if (imageSource != null)
            {
                note.ImageData = ConvertImageToBase64(imageSource);
                RefreshNoteControl(note, noteControl);
                pastingArgs.CancelCommand();
            }
        });

        // Ctrl+V가 AddPastingHandler로 잡히지 않는 경우를 보완: PreviewKeyDown에서 직접 처리
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
                catch
                {
                    // 클립보드 접근 실패 무시
                }

                if (imageSource != null)
                {
                    note.ImageData = ConvertImageToBase64(imageSource);
                    RefreshNoteControl(note, noteControl);
                    ke.Handled = true;
                }
            }
        };

        // Paste 명령(키/메뉴)을 확실히 가로채서 이미지 붙여넣기를 시도
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

                // 로그 남김
                try { LogClipboardFormats(dataObj, note.Id ?? note.GetHashCode().ToString()); } catch { }

                if (imageSource == null && System.Windows.Clipboard.ContainsImage())
                {
                    imageSource = System.Windows.Clipboard.GetImage();
                }
            }
            catch
            {
                // 클립보드 접근 실패 무시
            }

            if (imageSource == null)
            {
                try
                {
                    imageSource = ExtractFromWinFormsClipboard();
                }
                catch
                {
                }
            }

            if (imageSource != null)
            {
                note.ImageData = ConvertImageToBase64(imageSource);
                RefreshNoteControl(note, noteControl);
                e.Handled = true;
            }
        }));

        textBox.ContextMenu = CreateNoteContextMenu();

        stackPanel.Children.Add(textBox);

        if (!string.IsNullOrEmpty(note.ImageData))
        {
            var imageControl = CreateImageControl(note.ImageData);
            stackPanel.Children.Add(imageControl);
        }

        var deleteButton = new System.Windows.Controls.Button
        {
            Content = "X",
            Width = 20,
            Height = 20,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            VerticalAlignment = System.Windows.VerticalAlignment.Top,
            Margin = new System.Windows.Thickness(0, 2, 2, 0),
            Background = System.Windows.Media.Brushes.Red,
            Foreground = System.Windows.Media.Brushes.White
        };

        deleteButton.Click += (s, e) =>
        {
            _notes.Remove(note);
            NotesCanvas.Children.Remove(noteControl);
            _noteControls.Remove(noteControl);
        };

        var grid = new System.Windows.Controls.Grid();
        grid.Children.Add(stackPanel);
        grid.Children.Add(deleteButton);
        noteControl.Child = grid;

        System.Windows.Controls.Canvas.SetLeft(noteControl, note.Left);
        System.Windows.Controls.Canvas.SetTop(noteControl, note.Top);

        noteControl.MouseLeftButtonDown += (s, e) =>
        {
            noteControl.CaptureMouse();
            e.Handled = true;
        };

        noteControl.MouseMove += (s, e) =>
        {
            if (noteControl.IsMouseCaptured && e.LeftButton == MouseButtonState.Pressed)
            {
                var position = e.GetPosition(NotesCanvas);
                note.Left = position.X;
                note.Top = position.Y;
                System.Windows.Controls.Canvas.SetLeft(noteControl, note.Left);
                System.Windows.Controls.Canvas.SetTop(noteControl, note.Top);
            }
        };

        noteControl.MouseLeftButtonUp += (s, e) =>
        {
            noteControl.ReleaseMouseCapture();
        };

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
            await _noteService.SaveNotesAsync(_notes.ToList());
            
            _noteService.SetDataPath(settingsWindow.SelectedPath);
            
            foreach (var control in _noteControls.ToList())
            {
                NotesCanvas.Children.Remove(control);
            }
            _noteControls.Clear();
            _notes.Clear();
            
            var notes = await _noteService.LoadNotesAsync();
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

}

