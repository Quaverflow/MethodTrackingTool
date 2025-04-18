namespace MethodTracker.Tests.ConcurrentTests.Api;

public class ApiTests3
{
    [Fact]
    public async Task Test3() => await new ApiTests().Get_Endpoint_ReturnsSuccessStatusCode("Test_3");
}