using System;
using System.Reflection;

namespace MethodTrackerTool.Public;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class AssemblyToPatchAttribute(Assembly assembly) : Attribute
{
    public Assembly Assembly { get; } = assembly;
}