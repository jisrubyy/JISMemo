using System.ComponentModel;

namespace JISMemo.Models;

public class StickyNote : INotifyPropertyChanged
{
    private string _content = "";
    private double _left;
    private double _top;
    private string _color = "#FFFF99";
    private string? _imageData;

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

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}