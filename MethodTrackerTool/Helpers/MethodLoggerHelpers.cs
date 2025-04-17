using System;
using System.Reflection;

namespace MethodTrackerTool.Helpers;

internal static class MethodLoggerHelpers
{
    public const BindingFlags CommonBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

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
}