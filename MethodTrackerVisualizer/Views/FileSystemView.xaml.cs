using System;
using System.Linq;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using MethodTrackerVisualizer.Helpers;
using System.Windows.Input;

namespace MethodTrackerVisualizer.Views;

public partial class FileSystemView : UserControl
{
    private readonly ObservableCollection<FileItem> _items = [];
    private EntryFile? _selected;

    public event EventHandler<EntryFile?>? FileSelectionChanged;
    public event EventHandler AllFiledDeleted;
    private void OnFileSelectionChanged(EntryFile? file)
    {
        _selected = file;
        FileSelectionChanged?.Invoke(this, file);
    }

    private void OnAllFilesDeleted() => AllFiledDeleted.Invoke(this, EventArgs.Empty);
    public FileSystemView()
    {
        InitializeComponent();
        FilesDataGrid.ItemsSource = _items;
        RebuildList();
        FileHelper.Refresh += (_, __) => Dispatcher.Invoke(RebuildList);
        FileSystemSearchBar.SearchTextChanged += (_, text) => Dispatcher.Invoke(() => ApplyFilter(text));
    }

    private void RebuildList()
    {
        _items.Clear();
        foreach (var entry in FileHelper.Data.OfType<EntryFile>())
        {
            _items.Add(new FileItem
            {
                FileName = entry.FileName,
                Updated = entry.Updated,
                Selected = false
            });
        }

        if (_selected != null)
        {
            var match = _items.FirstOrDefault(fi => fi.FileName == _selected.FileName);
            if (match != null)
            {
                FilesDataGrid.SelectedItem = match;
                return;
            }
        }

        FilesDataGrid.SelectedItem = null;
    }

    private void ApplyFilter(string raw)
    {
        var filter = raw?.Trim();
        if (string.IsNullOrEmpty(filter))
        {
            RebuildList();
        }
        else
        {
            var filtered = FileHelper.Data
                .OfType<EntryFile>()
                .Where(x => x.FileName.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0)
                .Select(x => new FileItem
                {
                    FileName = x.FileName,
                    Updated = x.Updated,
                    Selected = false
                });

            _items.Clear();
            foreach (var item in filtered)
            {
                _items.Add(item);
            }
        }
    }

    private void FilesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FilesDataGrid.SelectedItem is FileItem fi)
        {
            var entry = FileHelper.Data
                .OfType<EntryFile>()
                .FirstOrDefault(x => x.FileName == fi.FileName);
            if (entry != null)
            {
                OnFileSelectionChanged(entry);
            }
        }
    }

    private void DeleteAllButton_Click(object sender, RoutedEventArgs e)
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MethodLogger");
        try
        {
            foreach (var file in Directory.GetFiles(folder))
            {
                File.Delete(file);
            }
            OnAllFilesDeleted();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error deleting all files: {ex.Message}",
                "Delete All Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DeleteFileButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (sender is Button { Tag: string fileName })
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MethodLogger");
            var path = Path.Combine(folder, fileName);
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    if (_selected?.FileName == fileName)
                    {
                        OnFileSelectionChanged(null);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting file '{fileName}': {ex.Message}",
                    "Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}