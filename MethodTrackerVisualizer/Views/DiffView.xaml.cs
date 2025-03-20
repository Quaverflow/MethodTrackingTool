using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using MethodTrackerVisualizer.Helpers;
using Newtonsoft.Json;

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
            string leftText = FormatLogEntry(leftEntry);
            string rightText = FormatLogEntry(rightEntry);

            FlowDocument diffDoc = BuildDiffDocument(leftText, rightText);
            DiffRichTextBox.Document = diffDoc;
        }

        /// <summary>
        /// Formats a LogEntry into a multi-line string representation.
        /// You can expand this method to include parameters, exceptions, etc.
        /// </summary>
        /// <param name="entry">The LogEntry to format.</param>
        /// <returns>A formatted string representing the LogEntry.</returns>
        private string FormatLogEntry(LogEntry entry)
        {
            if (entry == null)
                return string.Empty;

            // Using JsonSerializer for dictionaries and objects for consistency.
            return $"Method: {entry.MethodName}\n" +
                   $"Parameters: {JsonConvert.SerializeObject(entry.Parameters)}\n" +
                   $"Return Type: {entry.ReturnType}\n" +
                   $"Return Value: {entry.ReturnValue}\n" +
                   $"Start Time: {entry.StartTime}\n" +
                   $"End Time: {entry.EndTime}\n" +
                   $"Elapsed: {entry.ElapsedTime}\n" +
                   $"Exclusive Elapsed: {entry.ExclusiveElapsedTime}\n" +
                   $"Memory Before: {entry.MemoryBefore}\n" +
                   $"Memory After: {entry.MemoryAfter}\n" +
                   $"Memory Increase: {entry.MemoryIncrease}\n";
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
            DiffPaneModel diffModel = diffBuilder.BuildDiffModel(leftText, rightText);

            // Create a FlowDocument to host the diff.
            FlowDocument doc = new FlowDocument();
            Paragraph paragraph = new Paragraph();

            // Iterate over each diff line and create a Run with appropriate highlighting.
            foreach (var line in diffModel.Lines)
            {
                Run run = new Run(line.Text);
                switch (line.Type)
                {
                    case ChangeType.Inserted:
                        run.Background = Brushes.LightGreen;
                        break;
                    case ChangeType.Deleted:
                        run.Background = Brushes.LightCoral;
                        break;
                    case ChangeType.Modified:
                        run.Background = Brushes.LightBlue;
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
