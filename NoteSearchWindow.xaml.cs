using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using JISMemo.Models;

namespace JISMemo;

public partial class NoteSearchWindow : Window
{
    private readonly ObservableCollection<NoteSearchItem> _allItems = new();
    private readonly ObservableCollection<NoteSearchItem> _filteredItems = new();
    private readonly Action<StickyNote> _onNoteSelected;

    public NoteSearchWindow(IEnumerable<StickyNote> notes, Action<StickyNote> onNoteSelected)
    {
        InitializeComponent();
        _onNoteSelected = onNoteSelected;
        
        Title = Localization.SearchNotes;
        SearchBox.Text = Localization.SearchPlaceholder;
        SearchBox.Foreground = System.Windows.Media.Brushes.Gray;
        SearchBox.GotFocus += (s, e) =>
        {
            if (SearchBox.Text == Localization.SearchPlaceholder)
            {
                SearchBox.Text = "";
                SearchBox.Foreground = System.Windows.Media.Brushes.Black;
            }
        };
        SearchBox.LostFocus += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                SearchBox.Text = Localization.SearchPlaceholder;
                SearchBox.Foreground = System.Windows.Media.Brushes.Gray;
            }
        };

        foreach (var note in notes)
        {
            _allItems.Add(new NoteSearchItem
            {
                Note = note,
                Title = string.IsNullOrEmpty(note.Title) ? Localization.NewNote : note.Title,
                Preview = GetPreview(note.Content),
                ModifiedText = $"{Localization.LastModified} {note.ModifiedAt:yyyy-MM-dd HH:mm}"
            });
        }

        NotesListBox.ItemsSource = _filteredItems;
        UpdateFilter("");
    }

    private string GetPreview(string content)
    {
        if (string.IsNullOrEmpty(content)) return "";
        var cleaned = content.Replace("\r", "").Replace("\n", " ");
        return cleaned.Length > 100 ? cleaned.Substring(0, 100) + "..." : cleaned;
    }

    private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (SearchBox.Text == Localization.SearchPlaceholder) return;
        UpdateFilter(SearchBox.Text);
    }

    private void UpdateFilter(string searchText)
    {
        _filteredItems.Clear();
        var query = searchText.ToLower();
        
        foreach (var item in _allItems)
        {
            if (string.IsNullOrEmpty(query) || 
                item.Title.ToLower().Contains(query) || 
                item.Note.Content.ToLower().Contains(query))
            {
                _filteredItems.Add(item);
            }
        }

        ResultCount.Text = $"{_filteredItems.Count} {Localization.NotesFound}";
    }

    private void NotesListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (NotesListBox.SelectedItem is NoteSearchItem item)
        {
            _onNoteSelected(item.Note);
            Close();
        }
    }

    public class NoteSearchItem
    {
        public StickyNote Note { get; set; } = null!;
        public string Title { get; set; } = "";
        public string Preview { get; set; } = "";
        public string ModifiedText { get; set; } = "";
    }
}
