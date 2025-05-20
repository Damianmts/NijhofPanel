using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace NijhofPanel.Models;

public class Mod_FileItem : INotifyPropertyChanged
{
    public Mod_FileItem(string path, bool isDirectory = false)
    {
        FullPath = path;
        ItemName = System.IO.Path.GetFileName(path);
        DisplayName = System.IO.Path.GetFileNameWithoutExtension(path);
        IsDirectory = isDirectory;
        SubFiles = new ObservableCollection<Mod_FileItem>();
    }

    public string ItemName { get; set; }
    public string DisplayName { get; set; }
    public string FullPath { get; set; }
    public bool IsDirectory { get; set; }
    public ObservableCollection<Mod_FileItem> SubFiles { get; set; }

    private BitmapImage _thumbnail;
    public BitmapImage Thumbnail
    {
        get => _thumbnail;
        set
        {
            _thumbnail = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}