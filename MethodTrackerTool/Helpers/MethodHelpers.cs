using System.Linq;
using System.Reflection;

namespace MethodTrackerTool.Helpers;

public static class MethodHelpers
{
    public static bool IsValidMethod(MethodInfo method) =>
        method is { IsSpecialName: false, IsAbstract: false, DeclaringType: not null } &&
        !IsLambdaOrStateMachine(method);

    private static bool IsLambdaOrStateMachine(MethodInfo method) =>
        method.DeclaringType?.Name.Contains('<') is true ||
        method.Name == "MoveNext" && method.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false);

}