namespace MethodTracker.Tests.ConcurrentTests.Api;

public class ApiTests8
{
    [Fact]
    public async Task Test8() => await new ApiTests().Get_Endpoint_ReturnsSuccessStatusCode("Test_8");
}