using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using NijhofPanel.Models;
using NijhofPanel.Services;
using NijhofPanel.Helpers;

namespace NijhofPanel.ViewModels;

public class LibraryWindowViewModel : ObservableObject
{
    private ObservableCollection<FileItemModel> _rootFiles;
    private FileItemModel _selectedFolder;
    private ObservableCollection<FileItemModel> _selectedFolderContent;

    public ObservableCollection<FileItemModel> RootFiles
    {
        get => _rootFiles;
        set => SetProperty(ref _rootFiles, value);
    }

    public FileItemModel SelectedFolder
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

    public ObservableCollection<FileItemModel> SelectedFolderContent
    {
        get => _selectedFolderContent;
        set => SetProperty(ref _selectedFolderContent, value);
    }

    public LibraryWindowViewModel()
    {
        RootFiles = new ObservableCollection<FileItemModel>();
        SelectedFolderContent = new ObservableCollection<FileItemModel>();
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
                var item = new FileItemModel(file);
                item.Thumbnail = await ThumbnailHelper.GetThumbnailAsync(file);
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
                    var item = new FileItemModel(file);
                    item.Thumbnail = await ThumbnailHelper.GetThumbnailAsync(file);
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
            var dirItem = new FileItemModel(dir, isDirectory: true);
            LoadSubFolders(dirItem);
            RootFiles.Add(dirItem);
        }
    }

    private void LoadSubFolders(FileItemModel parentItemModel)
    {
        try
        {
            var directories = Directory.GetDirectories(parentItemModel.FullPath);
            foreach (var dir in directories)
            {
                var subItem = new FileItemModel(dir, isDirectory: true);
                LoadSubFolders(subItem); // recursief
                parentItemModel.SubFiles.Add(subItem);
            }
        }
        catch (IOException ex)
        {
            MessageBox.Show($"Fout bij het laden van de map {parentItemModel.FullPath}: {ex.Message}");
        }
    }
}