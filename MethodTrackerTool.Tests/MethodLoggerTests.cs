using MethodTracker.MockProject;
using MethodTrackerTool;
using Xunit.Abstractions;

namespace MethodTracker.Tests;

public class MethodLoggerTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task Sample()
    {
        MethodLogger.EnableLogging(testOutputHelper.WriteLine, typeof(OrderService));

        try
        {
            await new OrderService().ProcessOrderAsync(new OrderRequest
            {
                UserId = 13,
                ProductIds = [1, 4, 55, 342, 33, 334, 864, 268, 1042],
                TotalAmount = 20
            });
        }
        catch
        {
            // ignore
        }

        MethodLogger.PrintJson();
    }
}