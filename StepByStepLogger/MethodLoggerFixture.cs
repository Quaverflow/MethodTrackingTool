using System.Reflection;

namespace StepByStepLogger;

public class MethodLoggerFixture : IDisposable
{
    /// <summary>
    /// Creates a new fixture that patches the provided assemblies.
    /// </summary>
    /// <param name="targetAssemblies">The assemblies to patch.</param>
    /// <param name="outputAction">The action to output log messages (e.g. Console.WriteLine or ITestOutputHelper.WriteLine).</param>
    /// <param name="options">Configuration options for the logger.</param>
    public MethodLoggerFixture(IEnumerable<Assembly> targetAssemblies, Action<string> outputAction, MethodLoggerOptions options)
    {
        // Configure the logger using the provided options.
        MethodLogger.Options.IncludePerformanceMetrics = options.IncludePerformanceMetrics;
        MethodLogger.Options.DateTimeFormat = options.DateTimeFormat;
        MethodLogger.Options.OutputFormatter = options.OutputFormatter;
        MethodLogger.Options.EnableRealTimeLogging = options.EnableRealTimeLogging;
        if (options.EnableRealTimeLogging)
        {
            options.OnLogEntry += entry => outputAction($"RealTime: {entry.MethodName}");
        }

        MethodLogger.EnableLogging(targetAssemblies, outputAction);
    }

    public void Dispose()
    {
        MethodLogger.DisableLogging();
    }
}