using System.Reflection;
using MethodTracker.MockProject;
using MethodTrackerTool;
using Xunit.Abstractions;

namespace MethodTracker.Tests;

public class MethodLoggerTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public MethodLoggerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void LoggingOutput_IncludesNestedCallsAndPerformanceMetrics()
    {
        // Arrange: capture logger output.
        var outputLines = new List<string>();

        MethodLogger.EnableLogging(Logger, Assembly.GetExecutingAssembly());

        // Act: call a method.
        var service = new DummyService();
        var result = service.DoWork();
        Assert.Equal("done", result);

        var jsonOutput = string.Join(Environment.NewLine, outputLines);

        // Assert that the JSON contains expected method names.
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
        // Arrange: capture output.
        var outputLines = new List<string>();
        MethodLogger.EnableLogging(Logger, Assembly.GetExecutingAssembly(), Assembly.GetExecutingAssembly());

        // Act.
        var service = new DummyService();
        var result = service.DoWork();
        Assert.Equal("done", result);
        var jsonOutput = string.Join(Environment.NewLine, outputLines);
           
        // Assert that output contains the expected method names.
        Assert.Contains("DummyService.DoWork", jsonOutput);
        void Logger(string s) => outputLines.Add(s);
    }

    [Fact]
    public async Task Sample()
    {
        MethodLogger.EnableLogging(_testOutputHelper.WriteLine, typeof(OrderService));

        var service = new OrderService();
        await service.ProcessOrderAsync(new OrderRequest
        {
            UserId = 13,
            ProductIds = [1, 4, 55, 342, 33, 334, 864, 268, 1042],
            TotalAmount = 20
        });

        MethodLogger.PrintJson();
    }
}