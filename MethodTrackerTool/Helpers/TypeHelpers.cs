using System;
using System.Linq;

namespace MethodTrackerTool.Helpers;

public static class TypeHelpers
{
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