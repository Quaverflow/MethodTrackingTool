using MethodTracker.MockProject;
using MethodTrackerTool;

namespace MethodTracker.Tests;

public class MethodLoggerTests
{
    [Fact]
    public async Task Sample()
    {
            MethodLogger.Initialize("Sample");
            await new OrderService().ProcessOrderAsync(new OrderRequest
            {
                UserId = 13,
                ProductIds = [1, 4, 55, 342, 33, 334, 864, 268, 1042],
                TotalAmount = 20
            });
            MethodLogger.PrintJson();
    }
}