using System.Linq;
using System.Reflection;

namespace MethodTrackerTool.Helpers;

public static class MethodHelpers
{
    public static bool IsValidMethod(MethodInfo method) =>
        method is { IsSpecialName: false, IsAbstract: false, DeclaringType: not null } &&
        method.DeclaringType.Namespace?.StartsWith("System") != true &&
        method.DeclaringType.Namespace?.StartsWith("Microsoft") != true &&
        !IsLambdaOrStateMachine(method) &&
        !IsTestMethod(method);

    private static bool IsTestMethod(MethodInfo method) =>
        method.GetCustomAttributes().Any(attr => attr.GetType().Name.Contains("Fact") ||
                                                 attr.GetType().Name.Contains("Theory") ||
                                                 attr.GetType().Name.Contains("Test"));

    private static bool IsLambdaOrStateMachine(MethodInfo method) =>
        method.DeclaringType?.Name.Contains('<') is true ||
        method.Name == "MoveNext" && method.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false);

}