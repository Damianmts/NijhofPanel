namespace NijhofPanel.ViewModels;

using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using Models;
using Helpers.Tools;
using Services;

public class LibraryWindowViewModel : ObservableObject
{
    private ObservableCollection<FileItemModel>? _rootFiles;
    private FileItemModel? _selectedFolder;
    private ObservableCollection<FileItemModel>? _selectedFolderContent;

    private ICommand? _loadCommand;
    private ICommand? _placeCommand;
    private ICommand? _closeCommand;

    public Action? CloseAction { get; set; }

    public ICommand LoadCommand => _loadCommand ??= new RelayCommand(ExecuteLoad);
    public ICommand PlaceCommand => _placeCommand ??= new RelayCommand(ExecutePlace);
    public ICommand CloseCommand => _closeCommand ??= new RelayCommand(ExecuteClose);

    private readonly ILibraryActions _actions;

    public LibraryWindowViewModel(ILibraryActions actions)
    {
        _actions = actions;

        RootFiles = new ObservableCollection<FileItemModel>();
        SelectedFolderContent = new ObservableCollection<FileItemModel>();
        LoadFolderStructure();
    }

    private void ExecuteLoad()
    {
        if (SelectedFile == null ||
            !SelectedFile.FullPath.EndsWith(".rfa", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("Selecteer eerst een geldig family-bestand.", "Waarschuwing",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _actions.LoadFamily(SelectedFile.FullPath);
    }

    private void ExecutePlace()
    {
        if (SelectedFile == null || !File.Exists(SelectedFile.FullPath))
        {
            MessageBox.Show("Selecteer eerst een geldig family-bestand.", "Waarschuwing",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _actions.PlaceFamily(SelectedFile.FullPath);
    }

    private void ExecuteClose()
    {
        CloseAction?.Invoke();
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
            var files = Directory.GetFiles(SelectedFolder.FullPath, "*.rfa", SearchOption.AllDirectories);
            System.Diagnostics.Debug.WriteLine($"📁 Gevonden bestanden: {files.Length}");

            var itemDictionary = new Dictionary<string, FileItemModel>();

            foreach (var file in files)
            {
                var item = new FileItemModel(file);
                itemDictionary[file] = item;
                SelectedFolderContent?.Add(item);
            }

            System.Diagnostics.Debug.WriteLine($"🔄 Start laden van {files.Length} thumbnails...");

            var uiContext = SynchronizationContext.Current;

            // Parallel verwerken met maximaal 4 tegelijk (door je Semaphore in ThumbnailHelper)
            var tasks = files.Select(file => LoadThumbnailForFileAsync(file, itemDictionary, uiContext));
            await Task.WhenAll(tasks);

            System.Diagnostics.Debug.WriteLine($"🏁 Alle thumbnails geladen!");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"💥 Exception in LoadSelectedFolderContentAsync: {ex.Message}");
            MessageBox.Show($"Fout bij het laden van bestanden: {ex.Message}");
        }
    }

    private async Task LoadThumbnailForFileAsync(
        string file,
        Dictionary<string, FileItemModel> itemDictionary,
        SynchronizationContext? uiContext)
    {
        try
        {
            var bitmap = await ThumbnailHelper.GetThumbnailAsync(file);

            if (bitmap != null)
            {
                // Update op UI thread
                if (uiContext != null)
                {
                    uiContext.Post(_ =>
                    {
                        if (itemDictionary.TryGetValue(file, out var item))
                        {
                            item.Thumbnail = bitmap;
                        }
                    }, null);
                }
                else
                {
                    // Fallback: direct zetten (als we al op UI thread zijn)
                    if (itemDictionary.TryGetValue(file, out var item))
                    {
                        item.Thumbnail = bitmap;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error bij {Path.GetFileName(file)}: {ex.Message}");
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