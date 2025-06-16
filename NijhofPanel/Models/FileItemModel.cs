using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace NijhofPanel.Models;

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

    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public string ItemName { get; set; }
    public string DisplayName { get; set; }
    public string FullPath { get; set; }
    public bool IsDirectory { get; set; }
    public ObservableCollection<FileItemModel> SubFiles { get; }

    private BitmapImage? _thumbnail;

    public BitmapImage? Thumbnail
    {
        get => _thumbnail;
        set
        {
            _thumbnail = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}