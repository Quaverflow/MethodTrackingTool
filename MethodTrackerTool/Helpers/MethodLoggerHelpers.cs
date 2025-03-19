using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MethodTrackerTool.Helpers;

internal static class MethodLoggerHelpers
{
    public static bool IsValidMethod(MethodInfo method) =>
        !method.IsSpecialName && method is { IsAbstract: false, DeclaringType: not null } &&
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

    public static string BuildTypeName(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.FullName ?? type.Name;
        }

        var baseName = type.Name;
        var backTickIndex = baseName.IndexOf('`');
        if (backTickIndex > 0)
        {
            baseName = baseName[..backTickIndex];
        }

        var genericArgs = type.GetGenericArguments();
        var genericArgsString = string.Join(", ", genericArgs.Select(BuildTypeName));

        var ns = type.Namespace != null ? type.Namespace + "." : "";
        return $"{ns}{baseName}<{genericArgsString}>";
    }

    public static object ConvertToSerializableValue(object? result)
    {
        if (result is null)
        {
            return "null";
        }

        try
        {
            return result;
        }
        catch (Exception)
        {
            return $"Unserializable type: {result.GetType().FullName}";
        }
    }

    public static object? GetTaskResult(Task task)
    {
        try
        {
            var resultProperty = task.GetType().GetProperty("Result");
            return resultProperty?.GetValue(task);
        }
        catch
        {
            return null;
        }
    }

    public static string BuildReturnTypeString(MethodInfo method)
    {
        var returnType = method.ReturnType;
        return BuildTypeName(returnType);
    }

}