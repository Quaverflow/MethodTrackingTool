using System.Reflection;

namespace MethodTrackerTool.Helpers;

public static class MethodLoggerHelpers
{


    public static bool IsValidMethod(MethodInfo method)
    {
        if (method.IsSpecialName || method.IsAbstract || method.DeclaringType == null ||
            method.DeclaringType.Namespace?.StartsWith("System") == true ||
            method.DeclaringType.Namespace?.StartsWith("Microsoft") == true ||
            IsLambdaOrStateMachine(method) ||
            IsTestMethod(method))
        {
            return false;
        }

        return true;
    }

    private static bool IsTestMethod(MethodInfo method) =>
        method.GetCustomAttributes().Any(attr => attr.GetType().Name.Contains("Fact") ||
                                                 attr.GetType().Name.Contains("Theory") ||
                                                 attr.GetType().Name.Contains("Test"));

    private static bool IsLambdaOrStateMachine(MethodInfo method) =>
        method.DeclaringType.Name.Contains('<') ||
        method.Name == "MoveNext" && method.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false);

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