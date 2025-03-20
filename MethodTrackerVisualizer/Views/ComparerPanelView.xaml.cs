using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using MethodTrackerVisualizer.Helpers;

namespace MethodTrackerVisualizer.Views;

public partial class ComparerPanelView
{

    public EntryFile Selected = FileHelper.Selected;
    public ComparerPanelView()
    {
        InitializeComponent();
        Loaded += Load;
    }

    public void Load(object sender, RoutedEventArgs e)
    {
        HierarchicalTreeView.ItemsSource = Selected.Data;
        FilesDataGrid.ItemsSource = FileHelper.Data.Select(x => new { x.FileName, x.Updated });
        FileSystemSearchBar.SearchTextChanged += SearchForText;
    }

    private void SearchForText(object _, string searchText)
    {
        searchText = searchText.Trim();
        if (!string.IsNullOrEmpty(searchText))
        {
            FilesDataGrid.ItemsSource = FileHelper.Data
                .Where(x => x.FileName.IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) != -1)
                .Select(x => new { x.FileName, x.Updated });

        }

    }
    private void FileNameButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            var dataContext = (dynamic)btn.DataContext;
            string fileName = dataContext.FileName;
            if (fileName != null)
            {
                Selected = FileHelper.Data.Single(x => x.FileName == fileName);
                HierarchicalTreeView.ItemsSource = Selected.Data;
            }
            else
            {
                MessageBox.Show("File not found.");
            }
        }
    }
}