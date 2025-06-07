using Autodesk.Revit.UI;
using NijhofPanel.Views;
using NijhofPanel.Services;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace NijhofPanel.ViewModels
{
    public class MainUserControlViewModel : INotifyPropertyChanged
    {
        private static MainWindowView _windowInstance;
        private bool _isDarkMode;

        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (_isDarkMode != value)
                {
                    _isDarkMode = value;
                    OnPropertyChanged(nameof(IsDarkMode));
                    UpdateTheme();
                }
            }
        }

        public ICommand Com_ToggleTheme { get; }

        public MainUserControlViewModel()
        {
            Com_ToggleTheme = new RelayCommand(ExecuteToggleTheme);
            IsDarkMode = false;
        }

        public void ToggleWindowMode(MainUserControlView userControl, UIApplication uiApp)
        {
            if (_windowInstance == null)
            {
                var dockablePane = GetDockablePane(uiApp);
                if (dockablePane != null)
                    dockablePane.Hide();

                _windowInstance = new MainWindowView();
                _windowInstance.MainContent.Content = userControl;
                _windowInstance.Closed += (s, e) =>
                {
                    _windowInstance = null;
                    dockablePane?.Show();
                };
                _windowInstance.Show();
            }
            else
            {
                _windowInstance.Close();
                _windowInstance = null;
            }
        }

        private DockablePane GetDockablePane(UIApplication uiApp)
        {
            var paneId = new DockablePaneId(new Guid("e54d1236-371d-4b8b-9c93-30c9508f2fb9"));
            return uiApp.GetDockablePane(paneId);
        }

        private void ExecuteToggleTheme()
        {
            IsDarkMode = !IsDarkMode;
        }

        private void UpdateTheme()
        {
            if (_windowInstance != null)
            {
                ThemeManagerService.UpdateTheme(IsDarkMode, _windowInstance);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
