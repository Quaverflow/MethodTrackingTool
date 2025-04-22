using System;
using System.Linq;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using MethodTrackerVisualizer.Helpers;

namespace MethodTrackerVisualizer.Views;

public partial class FileSystemView : UserControl
{
    private readonly ObservableCollection<FileItem> _items = new();

    public event EventHandler<EntryFile>? FileSelectionChanged;
    private void OnFileSelectionChanged(EntryFile file)
        => FileSelectionChanged?.Invoke(this, file);

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
}