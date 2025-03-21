using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Newtonsoft.Json;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using MethodTrackerVisualizer.Helpers;

namespace MethodTrackerVisualizer.Views
{
    public partial class DiffViewer : UserControl
    {
        public DiffViewer()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Updates the diff view by comparing the details of the two LogEntry objects.
        /// </summary>
        /// <param name="leftEntry">The LogEntry from the left (previous) test run.</param>
        /// <param name="rightEntry">The LogEntry from the right (current) test run.</param>
        public void UpdateDiffView(LogEntry leftEntry, LogEntry rightEntry)
        {
            var leftText = FormatLogEntry(leftEntry);
            var rightText = FormatLogEntry(rightEntry);

            var diffDoc = BuildDiffDocument(leftText, rightText);
            DiffRichTextBox.Document = diffDoc;
        }


        private string FormatLogEntry(LogEntry entry)
        {
            if (entry == null)
            {
                return string.Empty;
            }

            var exceptionsText = (entry.Exceptions != null && entry.Exceptions.Length > 0)
                ? JsonConvert.SerializeObject(entry.Exceptions)
                : "None";
            return $"Method: {entry.MethodName}\n" +
                   $"Parameters: {JsonConvert.SerializeObject(entry.Parameters)}\n" +
                   $"Return Type: {entry.ReturnType}\n" +
                   $"Return Value: {entry.ReturnValue}\n" +
                   $"Exceptions: {exceptionsText}\n";
        }

        /// <summary>
        /// Uses DiffPlex to build a FlowDocument that highlights differences between two text blocks.
        /// Inserted lines are highlighted in LightGreen, deleted lines in LightCoral, and modified lines in LightBlue.
        /// </summary>
        /// <param name="leftText">The text from the left (previous) run.</param>
        /// <param name="rightText">The text from the right (current) run.</param>
        /// <returns>A FlowDocument with highlighted differences.</returns>
        private FlowDocument BuildDiffDocument(string leftText, string rightText)
        {
            // Create a diff builder and compute the diff.
            var diffBuilder = new InlineDiffBuilder(new Differ());
            var diffModel = diffBuilder.BuildDiffModel(leftText, rightText);

            // Create a FlowDocument to host the diff.
            var doc = new FlowDocument();
            var paragraph = new Paragraph();

            // Iterate over each diff line and create a Run with appropriate highlighting.
            foreach (var line in diffModel.Lines)
            {
                var run = new Run(line.Text);
                switch (line.Type)
                {
                    case ChangeType.Inserted:
                        run.Background = Brushes.DarkGreen;
                        run.Foreground = Brushes.White;
                        break;
                    case ChangeType.Deleted:
                        run.Background = Brushes.DarkRed;
                        run.Foreground = Brushes.White;
                        break;
                    case ChangeType.Modified:
                        run.Background = Brushes.DarkBlue;
                        run.Foreground = Brushes.White;
                        break;
                    default:
                        run.Foreground = Brushes.White;
                        break;
                }
                paragraph.Inlines.Add(run);
                paragraph.Inlines.Add(new LineBreak());
            }
            doc.Blocks.Add(paragraph);
            return doc;
        }
    }
}
