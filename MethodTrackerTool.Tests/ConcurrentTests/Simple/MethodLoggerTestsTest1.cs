namespace MethodTracker.Tests.ConcurrentTests.Simple;

public class MethodLoggerTestsTest1
{
    [Fact]
    public async Task Test1() => await TestCall.Call("Test1");
}