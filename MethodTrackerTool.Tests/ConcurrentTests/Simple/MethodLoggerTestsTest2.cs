namespace MethodTracker.Tests.ConcurrentTests.Simple;

public class MethodLoggerTestsTest2
{
    [Fact]
    public async Task Test2() => await TestCall.Call("Test2");
}