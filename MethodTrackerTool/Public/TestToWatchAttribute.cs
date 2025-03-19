using System;

namespace MethodTrackerTool.Public;

[AttributeUsage(AttributeTargets.Method)]
public class TestToWatchAttribute() : Attribute
{
    
}