using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using MethodTrackerVisualizer.Helpers;
using System.Windows.Threading;

namespace MethodTrackerVisualizer.Views;

public partial class ComparerPanelView : INotifyPropertyChanged
{
    public event EventHandler? FileSelectionChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    public EntryFile? Selected = new() { Data = [] };
    private List<FileItem> _fileItems = [];
    private string _currentSearchText = string.Empty;

    public string CurrentSearchText
    {
        get => _currentSearchText;
        set
        {
            if (_currentSearchText != value)
            {
                _currentSearchText = value;
                OnPropertyChanged(nameof(CurrentSearchText));
            }
        }
    }

    public ComparerPanelView()
    {
        InitializeComponent();
        Loaded += Load;
        FileHelper.Refresh += Refresh;
        DataContext = this;
    }

    private void Refresh(object sender, EventArgs eventArgs)
    {
        _fileItems = FileHelper.Data
            .OfType<EntryFile>()
            .Select(x => new FileItem { FileName = x.FileName, Updated = x.Updated, Selected = false })
            .ToList();

        FilesDataGrid.ItemsSource = _fileItems;
        if (Selected != null)
        {
            HierarchicalTreeView.ItemsSource = Selected.Data;
        }
    }

    private void OnFileSelectionChanged() => FileSelectionChanged?.Invoke(this, EventArgs.Empty);

    public void Load(object sender, RoutedEventArgs e)
    {
        _fileItems = FileHelper.Data
            .OfType<EntryFile>()
            .Select(x => new FileItem { FileName = x.FileName, Updated = x.Updated, Selected = false })
            .OrderByDescending(x => x.Selected)
            .ToList();

        FilesDataGrid.ItemsSource = _fileItems;
        if (Selected != null)
        {
            HierarchicalTreeView.ItemsSource = Selected.Data;
        }
        FileSystemSearchBar.SearchTextChanged += (_, text) => CurrentSearchText = text;
    }

    private void ToggleButton_OnChecked(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox { DataContext: FileItem selectedItem } btn)
        {
            return;
        }

        if (btn.IsChecked != true)
        {
            selectedItem.Selected = false;
            return;
        }

        foreach (var item in _fileItems)
        {
            item.Selected = false;
        }

        selectedItem.Selected = true;
        var selectedFile = FileHelper.Data.FirstOrDefault(x => x?.FileName == selectedItem.FileName);

        if (selectedFile != null && Selected != null)
        {
            Selected = selectedFile;
            HierarchicalTreeView.ItemsSource = Selected.Data;
            OnFileSelectionChanged();
        }

        FilesDataGrid.ItemsSource = null;
        FilesDataGrid.ItemsSource = _fileItems.OrderByDescending(x => x.Selected);
    }

    public void NavigateToEntry(object dataItem)
    {
        HierarchicalTreeView.ExpandAllParents(dataItem);
        Dispatcher.BeginInvoke(new Action(() =>
        {
            var tvi = HierarchicalTreeView.GetTreeViewItem(dataItem);
            tvi?.ExpandExpanderForEntry();
            tvi?.BringIntoView();
        }), DispatcherPriority.Background);
    }

    private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}