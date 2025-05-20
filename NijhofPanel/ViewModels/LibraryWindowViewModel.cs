using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using NijhofPanel.Models;
using NijhofPanel.Services;

namespace NijhofPanel.ViewModels;

public class LibraryWindowViewModel : ObservableObject
{
    private ObservableCollection<Mod_FileItem> _rootFiles;
    private Mod_FileItem _selectedFolder;
    private ObservableCollection<Mod_FileItem> _selectedFolderContent;

    public ObservableCollection<Mod_FileItem> RootFiles
    {
        get => _rootFiles;
        set => SetProperty(ref _rootFiles, value);
    }

    public Mod_FileItem SelectedFolder
    {
        get => _selectedFolder;
        set
        {
            if (SetProperty(ref _selectedFolder, value))
            {
                _ = LoadSelectedFolderContentAsync();
            }
        }
    }

    public ObservableCollection<Mod_FileItem> SelectedFolderContent
    {
        get => _selectedFolderContent;
        set => SetProperty(ref _selectedFolderContent, value);
    }

    public LibraryWindowViewModel()
    {
        RootFiles = new ObservableCollection<Mod_FileItem>();
        SelectedFolderContent = new ObservableCollection<Mod_FileItem>();
        LoadFolderStructure();
    }

    private async Task LoadSelectedFolderContentAsync()
    {
        SelectedFolderContent.Clear();

        if (SelectedFolder == null || !Directory.Exists(SelectedFolder.FullPath))
            return;

        try
        {
            var files = Directory.GetFiles(SelectedFolder.FullPath)
                .Where(f => Path.GetExtension(f).ToLower() == ".rfa");

            foreach (var file in files)
            {
                var item = new Mod_FileItem(file);
                item.Thumbnail = await Srv_Thumbnail.GetThumbnailAsync(file);
                SelectedFolderContent.Add(item);
            }

            await LoadFilesFromSubfoldersAsync(SelectedFolder.FullPath);
        }
        catch (IOException ex)
        {
            MessageBox.Show($"Fout bij het laden van bestanden: {ex.Message}");
        }
    }

    private async Task LoadFilesFromSubfoldersAsync(string folderPath)
    {
        try
        {
            foreach (var subDir in Directory.GetDirectories(folderPath))
            {
                var files = Directory.GetFiles(subDir)
                    .Where(f => Path.GetExtension(f).ToLower() == ".rfa");

                foreach (var file in files)
                {
                    var item = new Mod_FileItem(file);
                    item.Thumbnail = await Srv_Thumbnail.GetThumbnailAsync(file);
                    SelectedFolderContent.Add(item);
                }

                await LoadFilesFromSubfoldersAsync(subDir); // recursief
            }
        }
        catch (IOException ex)
        {
            MessageBox.Show($"Fout bij het laden van submappen: {ex.Message}");
        }
    }

    private void LoadFolderStructure()
    {
        string rootPath = @"F:\Stabiplan\Custom\Families";
        if (!Directory.Exists(rootPath)) return;

        var directories = Directory.GetDirectories(rootPath);
        foreach (var dir in directories)
        {
            var dirItem = new Mod_FileItem(dir, isDirectory: true);
            LoadSubFolders(dirItem);
            RootFiles.Add(dirItem);
        }
    }

    private void LoadSubFolders(Mod_FileItem parentItem)
    {
        try
        {
            var directories = Directory.GetDirectories(parentItem.FullPath);
            foreach (var dir in directories)
            {
                var subItem = new Mod_FileItem(dir, isDirectory: true);
                LoadSubFolders(subItem); // recursief
                parentItem.SubFiles.Add(subItem);
            }
        }
        catch (IOException ex)
        {
            MessageBox.Show($"Fout bij het laden van de map {parentItem.FullPath}: {ex.Message}");
        }
    }
}