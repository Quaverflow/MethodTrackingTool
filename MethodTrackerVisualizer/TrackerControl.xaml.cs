using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace MethodTrackerVisualizer
{
    /// <summary>
    /// Interaction logic for TrackerControl.
    /// </summary>
    public partial class TrackerControl : UserControl
    {
        private static readonly string FilePath = GetLogFilePath();
        public static string GetLogFilePath()
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MethodLogger");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            return Path.Combine(folder, "loggeroutput.json");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrackerControl"/> class.
        /// </summary>
        public TrackerControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            string json = File.ReadAllText(FilePath);
            MessageBox.Show(json,
                "Tracker");
        }
    }
}