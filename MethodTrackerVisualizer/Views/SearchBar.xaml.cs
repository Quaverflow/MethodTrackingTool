using System;

namespace MethodTrackerVisualizer.Views;

public partial class SearchBar
{
    public event EventHandler<string>? SearchTextChanged;
    public event EventHandler<string>? PreviousClicked;
    public event EventHandler<string>? NextClicked;

    public SearchBar()
    {
        InitializeComponent();
        SearchButton.Click += (_, _) => SearchTextChanged?.Invoke(this, SearchTextBox.Text.Trim());
        PreviousButton.Click += (_, _) => PreviousClicked?.Invoke(this, SearchTextBox.Text.Trim());
        NextButton.Click += (_, _) => NextClicked?.Invoke(this, SearchTextBox.Text.Trim());
    }
}