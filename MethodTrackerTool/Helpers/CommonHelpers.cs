using System.Reflection;
using System.Threading.Tasks;

namespace MethodTrackerTool.Helpers;

internal static class CommonHelpers
{
    public const BindingFlags CommonBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

    public static object? UnwrapTaskResult(object? result)
    {
        if (result is not Task task)
        {
            return result;
        }

        task.GetAwaiter().GetResult();

        var taskType = task.GetType();
        if (taskType.IsGenericType && taskType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultProp = taskType.GetProperty("Result");
            return resultProp?.GetValue(task);
        }

        return null;

    }
}