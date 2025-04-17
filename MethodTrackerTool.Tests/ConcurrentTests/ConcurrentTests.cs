using MethodTracker.MockProject;
using MethodTrackerTool;

namespace MethodTracker.Tests.ConcurrentTests;

public class MethodLoggerTestsTest1
{
    [Fact]
    public async Task Test1() => await TestCall.Call("Test1");
}

public class MethodLoggerTestsTest2
{
    [Fact]
    public async Task Test2() => await TestCall.Call("Test2");
}

public class MethodLoggerTestsTest3
{
    [Fact]
    public async Task Test3() => await TestCall.Call("Test3");
}

public class MethodLoggerTestsTest4
{
    [Fact]
    public async Task Test4() => await TestCall.Call("Test4");
}

public class MethodLoggerTestsTest5
{
    [Fact]
    public async Task Test5() => await TestCall.Call("Test5");
}

public class MethodLoggerTestsTest6
{
    [Fact]
    public async Task Test6() => await TestCall.Call("Test6");
}

public class MethodLoggerTestsTest7
{
    [Fact]
    public async Task Test7() => await TestCall.Call("Test7");
}

public class MethodLoggerTestsTest8
{
    [Fact]
    public async Task Test8() => await TestCall.Call("Test8");
}

public class MethodLoggerTestsTest9
{
    [Fact]
    public async Task Test9() => await TestCall.Call("Test9");
}

public class MethodLoggerTestsTest10
{
    [Fact]
    public async Task Test10() => await TestCall.Call("Test10");
}

public static class TestCall
{
    private static readonly string _outputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MethodLogger");

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
            .GetFiles(_outputDirectory, $"{name}*")
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