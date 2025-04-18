using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace MethodTrackerTool.Helpers;

public static class MethodHelpers
{
    public static bool IsValidMethod(MethodInfo method) =>
        method.IsDeclaredMember() &&
        method is { IsSpecialName: false, DeclaringType: not null, IsAbstract: false } &&
        !IsLambdaOrStateMachine(method);

    private static bool IsLambdaOrStateMachine(MethodInfo method) =>
        method.DeclaringType?.Name.Contains('<') is true ||
        method.Name == "MoveNext" && method.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false);

}