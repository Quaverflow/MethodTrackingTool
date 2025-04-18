namespace MethodTracker.Tests.ConcurrentTests.Api;

public class ApiTests7
{
    [Fact]
    public async Task Test7() => await new ApiTests().Get_Endpoint_ReturnsSuccessStatusCode("Test_7");
}