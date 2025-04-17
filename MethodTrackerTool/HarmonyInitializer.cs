using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MethodTrackerTool.Helpers;
using MethodTrackerTool.Public;

namespace MethodTrackerTool;

public static class HarmonyInitializer
{
    private static readonly Harmony _harmonyInstance = new("com.method.logger");

    public static void PatchAssemblies()
    {
        var assemblies = FindAssemblyMarkers();
        foreach (var assembly in assemblies)
        {
            PatchAssemblyMethods(assembly);
        }
    }

    private static IEnumerable<Assembly> FindAssemblyMarkers() =>
        AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetCustomAttributes(typeof(AssemblyMarkerAttribute), inherit: false)
                .Cast<AssemblyMarkerAttribute>()
                .Select(x => x.Assembly)
                .ToArray());

    private static void PatchAssemblyMethods(Assembly targetAssembly)
    {
        var methods = targetAssembly.GetTypes()
            .Where(type => !TypeHelpers.IsSystemType(type) && !TypeHelpers.IsTestType(type))
            .SelectMany(type => type.GetMethods(CommonHelpers.CommonBindingFlags))
            .Where(MethodHelpers.IsValidMethod);

        foreach (var method in methods)
        {
            var postfixMethodName = method.ReturnType == typeof(void)
                ? nameof(MethodPatches.VoidPostfix)
                : nameof(MethodPatches.Postfix);

            var prefix = new HarmonyMethod(typeof(MethodPatches).GetMethod(nameof(MethodPatches.Prefix), CommonHelpers.CommonBindingFlags));
            var postfix = new HarmonyMethod(typeof(MethodPatches).GetMethod(postfixMethodName, CommonHelpers.CommonBindingFlags));
            var finalizer = new HarmonyMethod(typeof(MethodPatches).GetMethod(nameof(MethodPatches.Finalizer), CommonHelpers.CommonBindingFlags));
            try
            {
                _harmonyInstance?.Patch(method, prefix: prefix, postfix: postfix, finalizer: finalizer);
            }
            catch
            {
                // ignore unpatched methods
            }
        }
    }
}