using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MethodTrackerVisualizer.Helpers;

namespace MethodTrackerVisualizer.Views;

public partial class FileSystemView
{
    public FileSystemView()
    {
        InitializeComponent();

        FilesDataGrid.ItemsSource = FileHelper.Data
            .Select(x => new FileItem { FileName = x.FileName, Updated = x.Updated, Selected = false })
            .ToList();

        FileSystemSearchBar.SearchTextChanged += SearchForText;
        FileHelper.Refresh += Refresh;
    }

    private void SearchForText(object _, string searchText)
    {
        searchText = searchText.Trim();
        if (!string.IsNullOrEmpty(searchText))
        {
            FilesDataGrid.ItemsSource = FileHelper.Data
                .Where(x => x.FileName.IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) != -1)
                .Select(x => new FileItem { FileName = x.FileName, Updated = x.Updated, Selected = false })
                .ToList();
        }
    }

    private void Refresh(object sender, EventArgs eventArgs) =>
        FilesDataGrid.ItemsSource = FileHelper.Data
            .Select(x => new FileItem { FileName = x.FileName, Updated = x.Updated, Selected = false })
            .ToList();

    private void ToggleButton_OnChecked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox btn && btn.DataContext is FileItem selectedItem)
        {
            if (btn.IsChecked == true)
            {
                var items = FilesDataGrid.ItemsSource
                    .Cast<FileItem>().ToList();

                foreach (var item in items)
                {
                    if (item != selectedItem)
                    {
                        item.Selected = false;
                    }
                }

                selectedItem.Selected = true;

                var selectedFile = FileHelper.Data.FirstOrDefault(x => x.FileName == selectedItem.FileName);
                if (selectedFile != null)
                {
                    FileHelper.Selected = selectedFile;
                }

                FilesDataGrid.ItemsSource = items.OrderByDescending(x => x.Selected);
            }
            else
            {
                selectedItem.Selected = false;
            }
        }
    }
}