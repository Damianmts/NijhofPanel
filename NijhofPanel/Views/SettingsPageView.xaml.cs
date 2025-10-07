namespace NijhofPanel.Views;

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using ViewModels;

public partial class SettingsPageView
{
    public SettingsPageView(MainUserControlViewModel mainVm)
    {
        InitializeComponent();
        DataContext = new SettingsPageViewModel();
    }
    
    public SettingsPageView()
    {
        InitializeComponent();
        DataContext = new SettingsPageViewModel();
    }
}
