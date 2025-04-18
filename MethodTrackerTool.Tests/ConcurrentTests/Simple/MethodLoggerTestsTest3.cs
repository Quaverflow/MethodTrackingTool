namespace MethodTracker.Tests.ConcurrentTests.Simple;

public class MethodLoggerTestsTest3
{
    [Fact]
    public async Task Test3() => await TestCall.Call("Test3");
}