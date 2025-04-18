using System.Text.Json;
using FluentAssertions;
using MethodTracker.MockProject;
using MethodTrackerTool;

namespace MethodTracker.Tests;

public class ResultTest
{
    private const string Name = "SampleTest";
    private static readonly string Path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MethodLogger");

    [Fact]
    public async Task TestMatchesSample()
    {
        string? matching = null;
        try
        {
            await MethodLogger.InitializeAsync(Name,
                async () => await new OrderService(Name, new DateTime(2025, 4, 17, 0, 0, 0)).ProcessOrderAsync(
                    new OrderRequest
                    {
                        UserId = 13,
                        ProductIds = [1, 4, 55, 342, 33, 334, 864, 268, 1042],
                        TotalAmount = 20
                    }));

            var binFolder = AppContext.BaseDirectory;
            var jsonPath = System.IO.Path.Combine(binFolder, "SampleTest.json");
            Assert.True(File.Exists(jsonPath), $"Could not find {jsonPath}");
            var payload = await File.ReadAllTextAsync(jsonPath);

            matching = Directory
                .GetFiles(Path, $"{Name}*")
                .Where(f => System.IO.Path.GetFileName(f).StartsWith(Name, StringComparison.OrdinalIgnoreCase))
                .OrderBy(File.GetLastWriteTimeUtc)
                .Last();

            var text = await File.ReadAllTextAsync(matching);

            var actual = JsonSerializer.Serialize(JsonSerializer.Deserialize<LogEntry[]>(text));
            var expected = JsonSerializer.Serialize(JsonSerializer.Deserialize<LogEntry[]>(payload));
            actual.Should().BeEquivalentTo(expected);
        }
        finally
        {
            if (matching != null)
            {
                File.Delete(matching);
            }
        }
    }

    public class LogEntry
    {
        public string MethodName { get; set; } = "";
        public Dictionary<string, object> Parameters { get; set; } = [];
        public string? ReturnType { get; set; }
        public object? ReturnValue { get; set; }

        public Exception[]? Exceptions { get; set; }

        public List<LogEntry> Children { get; set; } = [];

    }
}