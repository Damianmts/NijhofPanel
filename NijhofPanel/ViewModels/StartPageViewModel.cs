namespace NijhofPanel.ViewModels;

using System.IO;
using System.Reflection;
using System.Windows.Threading;

public class StartPageViewModel : ObservableObject
{
    private readonly MainUserControlViewModel _mainVm;

    public StartPageViewModel(MainUserControlViewModel mainVm)
    {
        _mainVm = mainVm;

        _saveTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(800)
        };
        _saveTimer.Tick += (s, e) =>
        {
            _saveTimer.Stop();
            TryEnsureNotesPath();
            SaveNotesSafe();
        };

        // Probeer notities te laden bij start
        TryEnsureNotesPath();
        LoadNotesSafe();
    }

    // Notities-tekst (auto-save met debounce)
    private string _notesText;
    public string NotesText
    {
        get => _notesText;
        set
        {
            if (SetProperty(ref _notesText, value))
            {
                // Start debounce voor auto-save
                _saveTimer.Stop();
                _saveTimer.Start();
            }
        }
    }

    private readonly DispatcherTimer _saveTimer;
    private string _notesFilePath;

    // Bepaalt en onthoudt het pad naar het notitiebestand
    private void TryEnsureNotesPath()
    {
        if (!string.IsNullOrWhiteSpace(_notesFilePath) && File.Exists(_notesFilePath))
            return;

        var projectFolder = ResolveProjectFolder();
        if (!string.IsNullOrWhiteSpace(projectFolder) && Directory.Exists(projectFolder))
        {
            _notesFilePath = Path.Combine(projectFolder, "NijhofPanel.Notes.txt");
            return;
        }

        // Fallback (indien geen projectmap vindbaar is); nog steeds functioneel
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appData, "NijhofPanel", "Notes");
        Directory.CreateDirectory(appFolder);
        var projectKey = ResolveProjectKey() ?? "DefaultProject";
        _notesFilePath = Path.Combine(appFolder, $"{SanitizeFileName(projectKey)}.txt");
    }

    private void LoadNotesSafe()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(_notesFilePath) && File.Exists(_notesFilePath))
            {
                NotesText = File.ReadAllText(_notesFilePath);
            }
        }
        catch
        {
            // Stil falen om UI niet te verstoren; logging kan hier toegevoegd worden
        }
    }

    private void SaveNotesSafe()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_notesFilePath))
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(_notesFilePath) ?? ".");
            File.WriteAllText(_notesFilePath, NotesText ?? string.Empty);
        }
        catch
        {
            // Stil falen; logging kan hier toegevoegd worden
        }
    }

    // Probeert het projectpad via veelvoorkomende property-namen op _mainVm
    private string ResolveProjectFolder()
    {
        if (_mainVm == null) return null;

        var candidates = new[]
        {
            "CurrentProjectPath",
            "ProjectPath",
            "SelectedProjectPath",
            "ProjectDirectory",
            "CurrentProjectDirectory",
            "WorkspacePath",
            "SolutionPath"
        };

        foreach (var name in candidates)
        {
            var prop = _mainVm.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.PropertyType == typeof(string))
            {
                var value = prop.GetValue(_mainVm) as string;
                if (!string.IsNullOrWhiteSpace(value) && Directory.Exists(value))
                    return value;
            }
        }

        // Eventueel: probeer een object met een Path/Directory eigenschap
        var objectCandidates = new[]
        {
            "CurrentProject",
            "SelectedProject",
            "Project"
        };

        foreach (var objName in objectCandidates)
        {
            var objProp = _mainVm.GetType().GetProperty(objName, BindingFlags.Public | BindingFlags.Instance);
            var objVal = objProp?.GetValue(_mainVm);
            if (objVal == null) continue;

            var innerPathProp = objVal.GetType().GetProperty("Path") ?? objVal.GetType().GetProperty("Directory");
            if (innerPathProp != null && innerPathProp.PropertyType == typeof(string))
            {
                var value = innerPathProp.GetValue(objVal) as string;
                if (!string.IsNullOrWhiteSpace(value) && Directory.Exists(value))
                    return value;
            }
        }

        return null;
    }

    // Bepaalt een sleutel/naam voor fallback-bestandsnaam
    private string ResolveProjectKey()
    {
        if (_mainVm == null) return null;

        var nameCandidates = new[] { "CurrentProjectName", "ProjectName", "SelectedProjectName", "Name", "Title" };
        foreach (var propName in nameCandidates)
        {
            var p = _mainVm.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            if (p != null && p.PropertyType == typeof(string))
            {
                var val = p.GetValue(_mainVm) as string;
                if (!string.IsNullOrWhiteSpace(val))
                    return val;
            }
        }

        // Anders: gebruik pad of type-naam als key
        return ResolveProjectFolder() ?? _mainVm.GetType().Name;
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
    }
}