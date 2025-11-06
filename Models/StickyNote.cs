using System.ComponentModel;

namespace JISMemo.Models;

public sealed class StickyNote : INotifyPropertyChanged
{
    private string _title = "";
    private string _content = "";
    private double _left;
    private double _top;
    private string _color = "#FFFF99";
    private List<string> _imageDataList = new();
    private double _fontSize = 16.0; // 메모 텍스트 확대/축소(줌) 값
    private double _width = 250;
    private double _height = 250;

    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime ModifiedAt { get; set; } = DateTime.Now;
    public string? Owner { get; set; }
    public string? DeviceType { get; set; }
    public string? DeviceName { get; set; }
    
    public string Title
    {
        get => string.IsNullOrEmpty(_title) ? GetFirstLineAsTitle() : _title;
        set
        {
            _title = value;
            OnPropertyChanged(nameof(Title));
        }
    }
    
    private string GetFirstLineAsTitle()
    {
        if (string.IsNullOrEmpty(Content)) return "";
        var firstLine = Content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        return firstLine.Length > 20 ? firstLine.Substring(0, 20) + "..." : firstLine;
    }

    public string Content
    {
        get => _content;
        set
        {
            _content = value;
            ModifiedAt = DateTime.Now;
            OnPropertyChanged(nameof(Content));
            OnPropertyChanged(nameof(Title)); // Content changes can affect the default title
        }
    }

    public double Left
    {
        get => _left;
        set
        {
            _left = value;
            OnPropertyChanged(nameof(Left));
        }
    }

    public double Top
    {
        get => _top;
        set
        {
            _top = value;
            OnPropertyChanged(nameof(Top));
        }
    }

    public string Color
    {
        get => _color;
        set
        {
            _color = value;
            OnPropertyChanged(nameof(Color));
        }
    }

    public List<string> ImageDataList
    {
        get => _imageDataList;
        set
        {
            _imageDataList = value ?? new();
            OnPropertyChanged(nameof(ImageDataList));
        }
    }

    // 기존 코드 호환성을 위한 속성
    public string? ImageData
    {
        get => _imageDataList.FirstOrDefault();
        set
        {
            if (value != null)
            {
                _imageDataList.Clear();
                _imageDataList.Add(value);
            }
            else
            {
                _imageDataList.Clear();
            }
            OnPropertyChanged(nameof(ImageData));
            OnPropertyChanged(nameof(ImageDataList));
        }
    }

    // 텍스트 상자의 폰트 크기(줌). JSON에 저장되어 재시작 후에도 유지됩니다.
    public double FontSize
    {
        get => _fontSize;
        set
        {
            if (value < 8) value = 8;
            if (value > 48) value = 48;
            _fontSize = value;
            OnPropertyChanged(nameof(FontSize));
        }
    }

    public double Width
    {
        get => _width;
        set
        {
            _width = value;
            OnPropertyChanged(nameof(Width));
        }
    }

    public double Height
    {
        get => _height;
        set
        {
            _height = value;
            OnPropertyChanged(nameof(Height));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}