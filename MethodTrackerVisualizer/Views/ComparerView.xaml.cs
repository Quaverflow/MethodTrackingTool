using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MethodTrackerVisualizer.Helpers;
using static MethodTrackerVisualizer.Views.DiffHelper;

namespace MethodTrackerVisualizer.Views
{
    public partial class ComparerView : UserControl
    {
        // Optionally, if you plan on navigating through a flattened diff,
        // you can store diff nodes here.
        private List<DiffNode> _diffNodes = new List<DiffNode>();
        private int _currentDiffIndex = 0;

        public ComparerView()
        {
            InitializeComponent();
            Loaded += ComparerView_Loaded;
        }

        private void ComparerView_Loaded(object sender, RoutedEventArgs e)
        {
            // Subscribe to file selection changed events if needed
            // (Assuming ComparerPanelView raises an event when its selection changes.)
            LeftPanel.FileSelectionChanged += OnFileSelectionChanged;
            RightPanel.FileSelectionChanged += OnFileSelectionChanged;

            // Initial diff update.
            UpdateDiffViewer();
        }

        private void OnFileSelectionChanged(object sender, System.EventArgs e)
        {
            UpdateDiffViewer();
        }

        private void UpdateDiffViewer()
        {
            if (LeftPanel.Selected != null && RightPanel.Selected != null)
            {
                LogEntry leftLog = LeftPanel.Selected.Data.Single();
                LogEntry rightLog = RightPanel.Selected.Data.Single();

                DiffViewerControl.UpdateDiffView(leftLog, rightLog);
            }
        }


        private void NextDifference_Click(object sender, RoutedEventArgs e)
        {
            if (_diffNodes.Any())
            {
                _currentDiffIndex = (_currentDiffIndex + 1) % _diffNodes.Count;
                var diff = _diffNodes[_currentDiffIndex];
                DiffViewerControl.UpdateDiffView(diff.Left, diff.Right);
            }
            else
            {
                UpdateDiffViewer();
            }
        }

        private void PreviousDifference_Click(object sender, RoutedEventArgs e)
        {
            if (_diffNodes.Any())
            {
                _currentDiffIndex = (_currentDiffIndex - 1 + _diffNodes.Count) % _diffNodes.Count;
                var diff = _diffNodes[_currentDiffIndex];
                DiffViewerControl.UpdateDiffView(diff.Left, diff.Right);
            }
            else
            {
                UpdateDiffViewer();
            }
        }
    }
}
