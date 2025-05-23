﻿using System;
using System.ComponentModel;

namespace MethodTrackerVisualizer.Helpers;

public class FileItem : INotifyPropertyChanged
{
    private bool _selected;
    public event PropertyChangedEventHandler? PropertyChanged;

    public string? FileName { get; set; }
    public DateTime Updated { get; set; }

    public bool Selected
    {
        get => _selected;
        set
        {
            if (_selected != value)
            {
                _selected = value;
                OnPropertyChanged(nameof(Selected));
            }
        }
    }

    protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}