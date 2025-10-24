using System.ComponentModel;

namespace JISMemo.Models;

public class StickyNote : INotifyPropertyChanged
{
    private string _content = "";
    private double _left;
    private double _top;
    private string _color = "#FFFF99";
    private string? _imageData;
    private double _fontSize = 14.0; // 메모 텍스트 확대/축소(줌) 값

    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string Content
    {
        get => _content;
        set
        {
            _content = value;
            OnPropertyChanged(nameof(Content));
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

    public string? ImageData
    {
        get => _imageData;
        set
        {
            _imageData = value;
            OnPropertyChanged(nameof(ImageData));
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

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}