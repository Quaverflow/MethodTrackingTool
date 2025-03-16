using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using MethodTrackerVisualizer.Helpers;

namespace MethodTrackerVisualizer.Components;

public partial class MethodDumpReader : UserControl
{
    private List<LogEntry> _fullLogData = [];

    public MethodDumpReader()
    {
        InitializeComponent();
        Loaded += MethodDumpReader_Loaded;
    }

    private void MethodDumpReader_Loaded(object sender, RoutedEventArgs e)
    {
        _fullLogData = FileHelper.LoadLogData();
    }

}