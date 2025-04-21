using System;
using System.Text.Json;

namespace MethodTrackerTool.Models;

internal class UnexpectedMethodTrackerException(string testId) : Exception(Format(testId))
{
    private static string Format(string s)
    {
        var message = "An unexpected issue has occured. Please raise an issue here: https://github.com/Quaverflow/MethodTrackingTool/issues with the message content of this exception";
        try
        {
            var errors = JsonSerializer.Serialize(MethodPatches.ResultsByTest[s].UnexpectedIssues);
            return $"{message}{Environment.NewLine}{errors}";

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;

        }
    }
}