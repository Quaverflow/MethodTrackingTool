using HarmonyLib;
using System.Reflection;
using System.Runtime.CompilerServices;

internal class Program
{
    static void Main()
    {
        var assembly = Assembly.GetExecutingAssembly();
        Console.WriteLine($"Loaded Assembly: {assembly.FullName}");
        // Apply Harmony patches to TestHook.SimpleMethod (use a similar logger patch as before)
        var harmony = new Harmony("com.example.test");
        harmony.Patch(
            original: typeof(TestHook).GetMethod("SimpleMethod"),
            prefix: new HarmonyMethod(typeof(MyPatches).GetMethod("Prefix")),
            postfix: new HarmonyMethod(typeof(MyPatches).GetMethod("Postfix"))
        );

        // Now call the method
        var result = TestHook.SimpleMethod("World");
        Console.WriteLine($"Result: {result}");
    }
}
public static class TestHook
{
    public static string SimpleMethod(string input)
    {
        Console.WriteLine("🔥 ORIGINAL: SimpleMethod called");
        return $"Hello, {input}";
    }
}
public static class MyPatches
{
    public static void Prefix()
    {
        Console.WriteLine("📌 (DEBUG) Prefix called");
    }
    public static void Postfix(string __result)
    {
        Console.WriteLine($"📌 (DEBUG) Postfix called, result: {__result}");
    }
}