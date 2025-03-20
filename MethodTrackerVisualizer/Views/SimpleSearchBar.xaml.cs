using System;
using System.Linq;
using System.Windows;

namespace MethodTrackerVisualizer.Views;

public partial class SimpleSearchBar
{
    public event EventHandler<string> SearchTextChanged;

    public SimpleSearchBar()
    {
        InitializeComponent();
        SearchTextBox.TextChanged += (_, _) =>
        {
            var text = SearchTextBox.Text.Trim();
            SearchTextChanged?.Invoke(this, text);
        };
    }
}