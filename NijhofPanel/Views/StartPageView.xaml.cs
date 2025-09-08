using System;
using System.Windows.Controls;
using System.Windows.Threading;
using NijhofPanel.ViewModels;

namespace NijhofPanel.Views;

public partial class StartPageView : Page
{
    private readonly DispatcherTimer _greetingTimer = new DispatcherTimer
    {
        Interval = TimeSpan.FromMinutes(1)
    };
    
    public StartPageView(MainUserControlViewModel mainVm)
    {
        InitializeComponent();
        DataContext = new StartPageViewModel(mainVm);
        
        // Stel initiale begroeting in en start timer
        UpdateGreeting();
        _greetingTimer.Tick += (s, e) => UpdateGreeting();
        _greetingTimer.Start();
    }
    
    private void UpdateGreeting()
    {
        if (GreetingTextBlock == null)
            return;

        GreetingTextBlock.Text = GetGreeting();
    }

    private static string GetGreeting()
    {
        var now = DateTime.Now.TimeOfDay;

        // Ochtend: 05:00 - 11:59
        if (now >= TimeSpan.FromHours(5) && now < TimeSpan.FromHours(12))
            return "Goedemorgen";

        // Middag: 12:00 - 17:59
        if (now >= TimeSpan.FromHours(12) && now < TimeSpan.FromHours(18))
            return "Goedemiddag";

        // Avond: 18:00 - 22:59
        if (now >= TimeSpan.FromHours(18) && now < TimeSpan.FromHours(23))
            return "Goedenavond";

        // Nacht: 23:00 - 04:59
        return "Goedenacht";
    }

}