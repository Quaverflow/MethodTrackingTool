using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using MethodTrackerVisualizer.Helpers;

namespace MethodTrackerVisualizer.Components;

public partial class MethodTrackerReader : UserControl
{
    private List<LogEntry> _fullLogData = [];

    public MethodTrackerReader()
    {
        InitializeComponent();
        Loaded += MethodTrackerReader_Loaded;
    }

    private void MethodTrackerReader_Loaded(object sender, RoutedEventArgs e)
    {
        _fullLogData = FileHelper.LoadLogData();
    }

}