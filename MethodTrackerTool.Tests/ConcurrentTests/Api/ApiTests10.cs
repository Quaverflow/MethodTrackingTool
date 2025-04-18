namespace MethodTracker.Tests.ConcurrentTests.Api;

public class ApiTests10
{
    [Fact]
    public async Task Test10() => await new ApiTests().Get_Endpoint_ReturnsSuccessStatusCode("Test_10");
}