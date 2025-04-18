namespace MethodTracker.Tests.ConcurrentTests.Api;

public class ApiTests2
{
    [Fact]
    public async Task Test2() => await new ApiTests().Get_Endpoint_ReturnsSuccessStatusCode("Test_2");
}