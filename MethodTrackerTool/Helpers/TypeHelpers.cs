using System;
using System.Linq;
using System.Reflection;

namespace MethodTrackerTool.Helpers;

public static class TypeHelpers
{

    public static bool IsSystemType(Type type) =>
        type.Namespace?.StartsWith("System") == true ||
        type.Namespace?.StartsWith("Microsoft") == true;

    public static bool IsTestType(Type type) =>
        type.GetCustomAttributes().Any(attr => attr.GetType().Name.Contains("Test") ||
                                               attr.GetType().Name.Contains("Fact"));

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

}