using System;
using System.Reflection;

namespace MethodTrackerTool.Public;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class AssemblyMarkerAttribute(Type type) : Attribute
{
    public Assembly Assembly { get; } = type.Assembly;
}