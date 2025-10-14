namespace NijhofPanel.Models;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

public class FileItemModel : INotifyPropertyChanged
{
    public FileItemModel(string path, bool isDirectory = false)
    {
        FullPath = path;
        ItemName = System.IO.Path.GetFileName(path);
        DisplayName = System.IO.Path.GetFileNameWithoutExtension(path);
        IsDirectory = isDirectory;
        SubFiles = new ObservableCollection<FileItemModel>();
    }

    public string ItemName { get; set; }
    public string DisplayName { get; set; }
    public string FullPath { get; set; }
    public bool IsDirectory { get; set; }
    public ObservableCollection<FileItemModel> SubFiles { get; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    private BitmapImage? _thumbnail;
    public BitmapImage? Thumbnail
    {
        get => _thumbnail;
        set 
        { 
            if (_thumbnail != value)
            {
                _thumbnail = value;
                System.Diagnostics.Debug.WriteLine($"🔔 PropertyChanged voor Thumbnail: {DisplayName} - Value: {value != null}");
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}