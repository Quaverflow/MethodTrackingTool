﻿namespace MethodTracker.Tests.ConcurrentTests.Api;

public class ApiTests4
{
    [Fact]
    public async Task Test4() => await new ApiTests().Get_Endpoint_ReturnsSuccessStatusCode("Test_4");
}