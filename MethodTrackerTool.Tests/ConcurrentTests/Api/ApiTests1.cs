namespace MethodTracker.Tests.ConcurrentTests.Api;

public class ApiTests1
{
    [Fact]
    public async Task Test1() => await new ApiTests().Get_Endpoint_ReturnsSuccessStatusCode("Test_1");
}