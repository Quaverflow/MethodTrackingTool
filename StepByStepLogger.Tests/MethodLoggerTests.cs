using System.Reflection;
using MethodTracker.MockProject;
using MethodTrackerTool;
using Xunit.Abstractions;

namespace MethodTracker.Tests;

public class MethodLoggerTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void LoggingOutput_IncludesNestedCallsAndPerformanceMetrics()
    {
        var outputLines = new List<string>();

        MethodLogger.EnableLogging(Logger, Assembly.GetExecutingAssembly());

        var service = new DummyService();
        var result = service.DoWork();
        Assert.Equal("done", result);

        var jsonOutput = string.Join(Environment.NewLine, outputLines);

        Assert.Contains("DummyService.DoWork", jsonOutput);
        Assert.Contains("DummyService.InnerWork", jsonOutput);
        Assert.Contains("ms", jsonOutput);
        Assert.Matches(@"\d{2}:\d{2}:\d{2}:\d{2} \d+\/\d+\/\d+", jsonOutput);
        return;

        void Logger(string s) => outputLines.Add(s);
    }

    [Fact]
    public void MultipleAssemblies_ArePatchedSuccessfully()
    {
        var outputLines = new List<string>();
        MethodLogger.EnableLogging(Logger, Assembly.GetExecutingAssembly(), Assembly.GetExecutingAssembly());

        var service = new DummyService();
        var result = service.DoWork();
        Assert.Equal("done", result);
        var jsonOutput = string.Join(Environment.NewLine, outputLines);

        Assert.Contains("DummyService.DoWork", jsonOutput);
        void Logger(string s) => outputLines.Add(s);
    }

    [Fact]
    public async Task Sample()
    {
        MethodLogger.EnableLogging(testOutputHelper.WriteLine, typeof(OrderService));

        var service = new OrderService();

        try
        {
            await service.ProcessOrderAsync(new OrderRequest
            {
                UserId = 13,
                ProductIds = [1, 4, 55, 342, 33, 334, 864, 268, 1042],
                TotalAmount = 20
            });
        }
        catch
        {
        }

        MethodLogger.PrintJson();
    }
}