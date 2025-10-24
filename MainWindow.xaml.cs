using System.Collections.ObjectModel;
using System.IO;
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
            Text = "JISMemo - 더블클릭으로 열기"
        };
        
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("열기", null, (s, e) => { Show(); WindowState = WindowState.Normal; Activate(); });
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
            
            // 포스트잇 배경 (노란색)
            var rect = new System.Drawing.Rectangle(1, 1, 14, 14);
            g.FillRectangle(new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(255, 255, 153)), rect);
            g.DrawRectangle(new System.Drawing.Pen(System.Drawing.Color.FromArgb(200, 200, 100)), rect);
            
            // 접힌 모서리
            var corner = new System.Drawing.Point[] {
                new System.Drawing.Point(12, 1),
                new System.Drawing.Point(15, 1),
                new System.Drawing.Point(15, 4)
            };
            g.FillPolygon(new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(230, 230, 120)), corner);
            g.DrawPolygon(new System.Drawing.Pen(System.Drawing.Color.FromArgb(200, 200, 100)), corner);
            
            // 텍스트 라인들
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
            _notifyIcon!.ShowBalloonTip(2000, "JISMemo", "시스템 트레이로 최소화되었습니다.", System.Windows.Forms.ToolTipIcon.Info);
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
        textBox.KeyDown += (s, e) =>
        {
            if (e.Key == System.Windows.Input.Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (System.Windows.Clipboard.ContainsImage())
                {
                    var image = System.Windows.Clipboard.GetImage();
                    note.ImageData = ConvertImageToBase64(image);
                    RefreshNoteControl(note, noteControl);
                    e.Handled = true;
                }
            }
        };

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

    protected override void OnClosed(EventArgs e)
    {
        _notifyIcon?.Dispose();
        base.OnClosed(e);
    }
}