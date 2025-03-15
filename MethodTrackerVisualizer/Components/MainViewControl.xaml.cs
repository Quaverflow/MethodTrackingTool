using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace MethodTrackerVisualizer.Components;

public partial class MainView : UserControl
{
    private List<LogEntry> _fullLogData = [];

    public MainView()
    {
        InitializeComponent();
        Loaded += MainView_Loaded;
    }

    private void MainView_Loaded(object sender, RoutedEventArgs e)
    {
        _fullLogData = FileHelper.LoadLogData();
    }

}