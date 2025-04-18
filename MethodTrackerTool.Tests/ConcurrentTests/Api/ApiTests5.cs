namespace MethodTracker.Tests.ConcurrentTests.Api;

public class ApiTests5
{
    [Fact]
    public async Task Test5() => await new ApiTests().Get_Endpoint_ReturnsSuccessStatusCode("Test_5");
}