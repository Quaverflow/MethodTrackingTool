using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using HarmonyLib;
using MethodTrackerTool.Helpers;
using MethodTrackerTool.Public;

namespace MethodTrackerTool;

internal static class HarmonyInitializer
{
    public static readonly Harmony HarmonyInstance = new("com.method.logger");

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

            var errors = new List<string>();
            try
            {
                HarmonyInstance?.Patch(method, prefix: prefix, postfix: postfix, finalizer: finalizer);
            }
            catch
            {
                // A patch failing shouldn't block the flow.
            }
        }
    }
}