using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using static MethodTrackerVisualizer.Views.DiffHelper;

namespace MethodTrackerVisualizer.Views
{
    public partial class ComparerView : UserControl
    {
        private List<DiffNode> _diffNodes = [];
        private int _currentDiffIndex = -1;

        public ComparerView()
        {
            InitializeComponent();
            Loaded += ComparerView_Loaded;
            LeftPanel.FileSelectionChanged += OnChildFileSelectionChanged;
            RightPanel.FileSelectionChanged += OnChildFileSelectionChanged;
        }

        private void ComparerView_Loaded(object sender, RoutedEventArgs e) => Load();

        private void OnChildFileSelectionChanged(object sender, EventArgs e) => Load();

        private void Load()
        {
            if (LeftPanel.Selected != null && RightPanel.Selected != null)
            {
                var diffRoot = DiffLogEntries(LeftPanel.Selected.Data.Single(), RightPanel.Selected.Data.Single());
                _diffNodes = [];
                FlattenDiffTree(diffRoot, _diffNodes);
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
            if (_diffNodes == null || _diffNodes.Count == 0)
            {
                MessageBox.Show("No differences found.");
                return;
            }
            _currentDiffIndex = (_currentDiffIndex + 1) % _diffNodes.Count;
            NavigateToDifference(_diffNodes[_currentDiffIndex]);
        }

        private void PreviousDifference_Click(object sender, RoutedEventArgs e)
        {
            if (_diffNodes == null || _diffNodes.Count == 0)
            {
                MessageBox.Show("No differences found.");
                return;
            }
            _currentDiffIndex = (_currentDiffIndex - 1 + _diffNodes.Count) % _diffNodes.Count;
            NavigateToDifference(_diffNodes[_currentDiffIndex]);
        }

        private void NavigateToDifference(DiffNode diffNode)
        {
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
}
