﻿using System;
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
        FilesDataGrid.ItemsSource = FileHelper.Data.Select(x => new {x.FileName, x.Updated});
    }
    private void FileNameButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            var dataContext = (dynamic)btn.DataContext;
            string fileName = dataContext.FileName;
            if (fileName != null)
            {
                FileHelper.Selected = FileHelper.Data.Single(x => x.FileName == fileName);
                MessageBox.Show("Selected file: " + fileName);
            }
            else
            {
                MessageBox.Show("File not found.");
            }
        }
    }
}