using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using StepByStepLogger;

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
            this.Loaded += LoggerToolWindowControl_Loaded;
        }

        private void LoggerToolWindowControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadLogData();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadLogData();
        }

        private void LoadLogData()
        {
            if (!File.Exists(FilePath))
            {
                MessageBox.Show("Log file not found at: " + FilePath);
                return;
            }
            try
            {
                string json = File.ReadAllText(FilePath);
                var logs = JsonConvert.DeserializeObject<List<LogEntry>>(json, new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                });
                LogTreeView.ItemsSource = logs;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading log data: " + ex.Message);
            }
        }
    }
}
