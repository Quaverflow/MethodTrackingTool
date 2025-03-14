using System.Reflection;
using StepByStepLogger;
using StepByStepLogger.MockProject;
using Xunit.Abstractions;

public class HarmonyTests : IDisposable
{
    private readonly ITestOutputHelper _outputHelper;

    public HarmonyTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;

        string mockAssemblyPath = Path.Combine(Directory.GetCurrentDirectory(), "StepByStepLogger.MockProject.dll");
        if (!File.Exists(mockAssemblyPath))
        {
            throw new FileNotFoundException($"Mock assembly not found: {mockAssemblyPath}");
        }

        var mockAssembly = typeof(UserService).Assembly;

        Console.WriteLine($"🔥 DEBUG: Patching Assembly: {mockAssembly.FullName}");
        MethodLogger.EnableLogging(mockAssembly, _outputHelper.WriteLine);
    }

    [Fact]
    public void MethodCall_Should_BeLogged()
    {
        var service = new StepByStepLogger.MockProject.UserService();
        var result = service.GetUser(42);
        service.DoSomething();

        Assert.Equal("User-42", result);
    }

    public void Dispose()
    {
        MethodLogger.DisableLogging();
    }
}