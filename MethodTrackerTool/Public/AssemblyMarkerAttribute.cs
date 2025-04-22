using System;
using System.Reflection;

namespace MethodTrackerTool.Public;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class AssemblyMarkerAttribute(Type type) : Attribute
{
    public Assembly Assembly { get; } = type.Assembly;
}

/// <summary>
/// Use this to tell the MethodTracker which private properties to export
/// </summary>
/// <param name="type"></param>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class PrivatePropertyMarkerAttribute(Type type, string[] propertyNames) : Attribute
{
    public Type Type { get; } = type;
    public string[] PropertyNames { get; } = propertyNames;
}