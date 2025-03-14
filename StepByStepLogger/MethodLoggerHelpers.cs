using System.Reflection;

namespace StepByStepLogger;

public static class MethodLoggerHelpers
{
    public static bool IsValidMethod(MethodInfo method)
    {
        if (method.IsSpecialName || method.IsAbstract || method.DeclaringType == null)
        {
            return false;
        }

        if (method.DeclaringType.Namespace?.StartsWith("System") == true ||
            method.DeclaringType.Namespace?.StartsWith("Microsoft") == true)
        {
            return false;
        }

        if (method.Name.StartsWith("<"))
        {
            return false;
        }

        return !method.GetCustomAttributes().Any(attr => attr.GetType().Name.Contains("Fact") ||
                                                         attr.GetType().Name.Contains("Test"));
    }

    public static bool IsSystemType(Type type)
    {
        return type.Namespace?.StartsWith("System") == true ||
               type.Namespace?.StartsWith("Microsoft") == true;
    }

    public static bool IsTestType(Type type)
    {
        return type.GetCustomAttributes().Any(attr => attr.GetType().Name.Contains("Test") ||
                                                      attr.GetType().Name.Contains("Fact"));
    }
}