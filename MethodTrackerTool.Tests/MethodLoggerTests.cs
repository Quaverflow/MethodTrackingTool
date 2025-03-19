using MethodTracker.MockProject;
using MethodTrackerTool;
using MethodTrackerTool.Public;

namespace MethodTracker.Tests;

public class MethodLoggerTests
{
    [Fact]
    [TestToWatch]
    public async Task Sample()
    {
        try
        {
            MethodLogger.Initialize(typeof(OrderService).Assembly);
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
    }
}