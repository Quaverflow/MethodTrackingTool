namespace MethodTracker.Tests.ConcurrentTests.Api;

public class ApiTests6
{
    [Fact]
    public async Task Test6() => await new ApiTests().Get_Endpoint_ReturnsSuccessStatusCode("Test_6");
}