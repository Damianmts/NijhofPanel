using System.Windows;
using NijhofPanel.Models;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Data;

namespace NijhofPanel.Views;

public partial class ScheduleSelectionWindowView : Window
{
    public class ScheduleItem
    {
        public string Name { get; set; }
        public bool IsSelected { get; set; }
        public ViewSchedule Schedule { get; set; } // Verander Schedule naar ViewSchedule
    }

    public List<ViewSchedule> SelectedSchedules { get; private set; } // Verander Schedule naar ViewSchedule

    public ScheduleSelectionWindowView(IEnumerable<ViewSchedule> schedules) // Verander Schedule naar ViewSchedule
    {
        InitializeComponent();

        // De rest van de code blijft hetzelfde
        ScheduleListBox.ItemsSource = schedules.Select(s => new ScheduleItem
        {
            Name = s.Name,
            Schedule = s,
            IsSelected = false
        }).ToList();
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        // Verzamel alle geselecteerde schedules
        SelectedSchedules = ScheduleListBox.Items
            .Cast<ScheduleItem>()
            .Where(item => item.IsSelected)
            .Select(item => item.Schedule)
            .ToList();

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}