namespace MethodTracker.Tests;

public class DummyService
{
    public string DoWork()
    {
        return InnerWork();
    }

    private string InnerWork()
    {
        return "done";
    }
}