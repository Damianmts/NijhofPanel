namespace NijhofPanel.ViewModels;

using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Models;
using Helpers.Tools;
using Helpers.Core;
using NijhofPanel.Services;

public class LibraryWindowViewModel : ObservableObject
{
    private ObservableCollection<FileItemModel>? _rootFiles;
    private FileItemModel? _selectedFolder;
    private ObservableCollection<FileItemModel>? _selectedFolderContent;

    private ICommand? _loadCommand;
    private ICommand? _placeCommand;
    private ICommand? _closeCommand;

    public ICommand LoadCommand => _loadCommand ??= new RelayCommand(ExecuteLoad);
    public ICommand PlaceCommand => _placeCommand ??= new RelayCommand(ExecutePlace);
    public ICommand CloseCommand => _closeCommand ??= new RelayCommand(ExecuteClose);

    private readonly ILibraryActions _actions;

    public LibraryWindowViewModel(ILibraryActions actions)
    {
        _actions = actions;

        RootFiles             = new ObservableCollection<FileItemModel>();
        SelectedFolderContent = new ObservableCollection<FileItemModel>();
        LoadFolderStructure();
    }

    private void ExecuteLoad()
    {
        if (SelectedFile == null ||
            !SelectedFile.FullPath.EndsWith(".rfa", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("Selecteer eerst een geldig .rfa-bestand.", "Waarschuwing",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _actions.LoadFamily(SelectedFile.FullPath);
    }

    private void ExecutePlace()
    {
        _actions.PlaceFamily();
    }

    private void ExecuteClose()
    {
        Application.Current.Windows.OfType<Window>()
            .FirstOrDefault(w => w.DataContext == this)?.Close();
    }

    public ObservableCollection<FileItemModel>? RootFiles
    {
        get => _rootFiles;
        set => SetProperty(ref _rootFiles, value);
    }

    public FileItemModel? SelectedFolder
    {
        get => _selectedFolder;
        set
        {
            if (SetProperty(ref _selectedFolder, value)) _ = LoadSelectedFolderContentAsync();
        }
    }

    public ObservableCollection<FileItemModel>? SelectedFolderContent
    {
        get => _selectedFolderContent;
        set => SetProperty(ref _selectedFolderContent, value);
    }

    private FileItemModel _selectedFile = null!;

    public FileItemModel SelectedFile
    {
        get => _selectedFile;
        set
        {
            _selectedFile = value;
            OnPropertyChanged();
        }
    }

    private async Task LoadSelectedFolderContentAsync()
    {
        SelectedFolderContent?.Clear();

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
                SelectedFolderContent?.Add(item);
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
                    SelectedFolderContent?.Add(item);
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
        var rootPath = @"F:\Stabiplan\Custom\Families";
        if (!Directory.Exists(rootPath)) return;

        var directories = Directory.GetDirectories(rootPath);
        foreach (var dir in directories)
        {
            var dirItem = new FileItemModel(dir, true);
            LoadSubFolders(dirItem);
            RootFiles?.Add(dirItem);
        }
    }

    private void LoadSubFolders(FileItemModel parentItemModel)
    {
        try
        {
            var directories = Directory.GetDirectories(parentItemModel.FullPath);
            foreach (var dir in directories)
            {
                var subItem = new FileItemModel(dir, true);
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