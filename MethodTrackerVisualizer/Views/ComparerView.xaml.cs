using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using MethodTrackerVisualizer.Helpers;
using static MethodTrackerVisualizer.Helpers.DiffHelper;

namespace MethodTrackerVisualizer.Views;

public partial class ComparerView
{
    private readonly List<DiffNode> _diffNodes = [];
    private int _currentDiffIndex;

    public ComparerView()
    {
        InitializeComponent();
        Loaded += ComparerView_Loaded;
    }

    private void ComparerView_Loaded(object sender, RoutedEventArgs e)
    {
        LeftPanel.FileSystem.FileSelectionChanged += OnFileSelectionChanged;
        LeftPanel.FileSystem.AllFiledDeleted += OnAllDeleted;
      
        RightPanel.FileSystem.FileSelectionChanged += OnFileSelectionChanged;
        RightPanel.FileSystem.AllFiledDeleted += OnAllDeleted;
        UpdateDiffViewer();
    }

    private void OnFileSelectionChanged(object sender, EntryFile? e) => UpdateDiffViewer();

    private void OnAllDeleted(object sender, EventArgs e)
    {
        LeftPanel.Selected = null;
        RightPanel.Selected = null;
    }

    private void UpdateDiffViewer()
    {

        if (LeftPanel.Selected != null && RightPanel.Selected != null)
        {

            var leftLog = new LogEntry { MethodName = "Root", Children = LeftPanel.Selected?.Data };
            var rightLog = new LogEntry { MethodName = "Root", Children = RightPanel.Selected?.Data };
            var diffTree = DiffLogEntries(leftLog, rightLog);
            _diffNodes.Clear();
            FlattenDiffTree(diffTree, _diffNodes);

            _currentDiffIndex = 0;
            if (_diffNodes.Any())
            {
                NavigateToDifference(_diffNodes[_currentDiffIndex]);

                DiffViewerControl.UpdateDiffView(leftLog, rightLog);
            }
            else
            {
                DiffViewerControl.UpdateDiffView(null, null);
            }
        }
    }

    private void FlattenDiffTree(DiffNode node, List<DiffNode> list)
    {
        if (node.DiffType != DiffType.Unchanged)
        {
            list.Add(node);
        }

        foreach (var child in node.Children)
        {
            FlattenDiffTree(child, list);
        }
    }

    private void NextDifference_Click(object sender, RoutedEventArgs e)
    {
        if (!_diffNodes.Any())
        {
            MessageBox.Show("No differences found.");
            return;
        }
        _currentDiffIndex = (_currentDiffIndex + 1) % _diffNodes.Count;
        NavigateToDifference(_diffNodes[_currentDiffIndex]);
        var diff = _diffNodes[_currentDiffIndex];
        DiffViewerControl.UpdateDiffView(diff.Left, diff.Right);
    }

    private void PreviousDifference_Click(object sender, RoutedEventArgs e)
    {
        if (!_diffNodes.Any())
        {
            MessageBox.Show("No differences found.");
            return;
        }
        _currentDiffIndex = (_currentDiffIndex - 1 + _diffNodes.Count) % _diffNodes.Count;
        NavigateToDifference(_diffNodes[_currentDiffIndex]);
        var diff = _diffNodes[_currentDiffIndex];
        DiffViewerControl.UpdateDiffView(diff.Left, diff.Right);
    }

    private void NavigateToDifference(DiffNode diffNode)
    {
        System.Diagnostics.Debug.WriteLine($"Navigating to diff: DiffType={diffNode.DiffType}, Left Method={diffNode.Left?.MethodName}, Right Method={diffNode.Right?.MethodName}");

        if (diffNode.Left != null)
        {
            LeftPanel.NavigateToEntry(diffNode.Left);
        }
        if (diffNode.Right != null)
        {
            RightPanel.NavigateToEntry(diffNode.Right);
        }
    }
}