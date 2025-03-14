namespace StepByStepLogger.MockProject;

public class UserService
{
    public string GetUser(int id)
    {
        return $"User-{id}";
    }

    public void DoSomething()
    {
        Console.WriteLine("Doing something...");
    }
}