using MethodTracker.MockProject;
using MethodTrackerTool;

namespace MethodTracker.Tests.ConcurrentTests.Simple;

public static class TestCall
{
    private static readonly string OutputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MethodLogger");

    public static async Task Call(string name)
    {
        await MethodLogger.InitializeAsync(name,
            async () => await new OrderService(name, new DateTime(2025, 4, 17, 0, 0, 0)).ProcessOrderAsync(
                new OrderRequest
                {
                    UserId = 13,
                    ProductIds = [1, 4, 55, 342, 33, 334, 864, 268, 1042],
                    TotalAmount = 20
                }));

        var matching = Directory
            .GetFiles(OutputDirectory, $"{name}*")
            .Where(f => Path.GetFileName(f).StartsWith(name, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.NotEmpty(matching);

        // Take the most recently written one:
        var last = matching
            .OrderBy(File.GetLastWriteTimeUtc)
            .Last();

        // Read and check it contains the test name:
        var text = await File.ReadAllTextAsync(last);
        Assert.Contains(name, text);
    }
}