using MethodTracker.MockProject;
using MethodTrackerTool;

namespace MethodTracker.Tests;

public class MethodLoggerTests
{
    [Fact]
    public async Task Sample() =>
        await MethodLogger.InitializeAsync("Sample", async () =>
            await new OrderService().ProcessOrderAsync(new OrderRequest
            {
                UserId = 13,
                ProductIds = [1, 4, 55, 342, 33, 334, 864, 268, 1042],
                TotalAmount = 20
            }));
}