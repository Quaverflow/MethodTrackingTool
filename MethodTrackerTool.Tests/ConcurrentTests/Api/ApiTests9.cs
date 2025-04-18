namespace MethodTracker.Tests.ConcurrentTests.Api;

public class ApiTests9
{
    [Fact]
    public async Task Test9() => await new ApiTests().Get_Endpoint_ReturnsSuccessStatusCode("Test_9");
}