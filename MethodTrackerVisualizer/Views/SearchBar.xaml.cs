using System;
using System.Windows.Controls;

namespace MethodTrackerVisualizer.Views;

public partial class SearchBar : UserControl
{
    public event EventHandler<string> SearchTextChanged;
    public event EventHandler PreviousClicked;
    public event EventHandler NextClicked;


    public SearchBar()
    {
        InitializeComponent();
        SearchTextBox.TextChanged += (_, _) =>
        {
            var text = SearchTextBox.Text.Trim();
            SearchTextChanged?.Invoke(this, text);
        };
        PreviousButton.Click += (_, _) => PreviousClicked?.Invoke(this, EventArgs.Empty);
        NextButton.Click += (_, _) => NextClicked?.Invoke(this, EventArgs.Empty);
    }
}