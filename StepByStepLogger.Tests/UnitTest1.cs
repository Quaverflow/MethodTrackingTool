using System.Reflection;

namespace StepByStepLogger.Tests
{
    public class DummyService
    {
        public string DoWork()
        {
            return InnerWork();
        }

        private string InnerWork()
        {
            return "done";
        }
    }

    public class MethodLoggerTests
    {
        [Fact]
        public void LoggingOutput_IncludesNestedCallsAndPerformanceMetrics()
        {
            // Arrange: capture logger output.
            var outputLines = new List<string>();
            Action<string> logger = s => outputLines.Add(s);

            MethodLogger.Options.IncludePerformanceMetrics = true;
            MethodLogger.Options.DateTimeFormat = "HH:mm:ss:ff d/M/yyyy";
            MethodLogger.EnableLogging(Assembly.GetExecutingAssembly(), logger);

            // Act: call a method.
            var service = new DummyService();
            var result = service.DoWork();
            Assert.Equal("done", result);

            MethodLogger.DisableLogging();

            var jsonOutput = string.Join(Environment.NewLine, outputLines);

            // Assert that the JSON contains expected method names.
            Assert.Contains("DummyService.DoWork", jsonOutput);
            Assert.Contains("DummyService.InnerWork", jsonOutput);
            Assert.Contains("ms", jsonOutput);
            Assert.Matches(@"\d{2}:\d{2}:\d{2}:\d{2} \d+\/\d+\/\d+", jsonOutput);
        }

        [Fact]
        public void LoggingOutput_Minimal_WhenPerformanceMetricsDisabled()
        {
            // Arrange: capture output.
            var outputLines = new List<string>();
            Action<string> logger = s => outputLines.Add(s);
            MethodLogger.Options.IncludePerformanceMetrics = false;
            MethodLogger.EnableLogging(Assembly.GetExecutingAssembly(), logger);

            // Act.
            var service = new DummyService();
            var result = service.DoWork();
            Assert.Equal("done", result);
            MethodLogger.DisableLogging();
            var jsonOutput = string.Join(Environment.NewLine, outputLines);

            // Assert Minimal output should not contain timing properties (they won't be in the minimal output).
            Assert.DoesNotContain("StartTime", jsonOutput);
            Assert.DoesNotContain("EndTime", jsonOutput);
            Assert.DoesNotContain("ElapsedTime", jsonOutput);
            Assert.Contains("DummyService.DoWork", jsonOutput);
        }

        [Fact]
        public void MultipleAssemblies_ArePatchedSuccessfully()
        {
            // Arrange: capture output.
            var outputLines = new List<string>();
            Action<string> logger = s => outputLines.Add(s);
            var assemblies = new List<Assembly> { Assembly.GetExecutingAssembly(), Assembly.GetExecutingAssembly() };
            MethodLogger.Options.IncludePerformanceMetrics = true;
            MethodLogger.Options.DateTimeFormat = "HH:mm:ss:ff d/M/yyyy";
            MethodLogger.EnableLogging(assemblies, logger);

            // Act.
            var service = new DummyService();
            var result = service.DoWork();
            Assert.Equal("done", result);
            MethodLogger.DisableLogging();
            var jsonOutput = string.Join(Environment.NewLine, outputLines);
           
            // Assert that output contains the expected method names.
            Assert.Contains("DummyService.DoWork", jsonOutput);
        }

        [Fact]
        public void RealTimeLogging_FiresOnLogEntry()
        {
            // Arrange: capture real-time log events.
            var rtLogEntries = new List<string>();
            Action<string> logger = s => { };
            MethodLogger.Options.IncludePerformanceMetrics = true;
            MethodLogger.Options.DateTimeFormat = "HH:mm:ss:ff d/M/yyyy";
            MethodLogger.Options.EnableRealTimeLogging = true;
            MethodLogger.Options.ClearLogEntrySubscribers();
            MethodLogger.Options.OnLogEntry += entry => rtLogEntries.Add($"RT: {entry.MethodName}");
            MethodLogger.EnableLogging(Assembly.GetExecutingAssembly(), logger);

            // Act.
            var service = new DummyService();
            service.DoWork();
            MethodLogger.DisableLogging();

            // Assert that real-time events were fired.
            Assert.NotEmpty(rtLogEntries);
            Assert.Contains(rtLogEntries, s => s.StartsWith("RT:"));
        }
    }
}
