﻿using System;
using System.Linq;
using System.Text.Json;
using MethodTrackerTool.Helpers;

namespace MethodTrackerTool;

internal class UnexpectedMethodTrackerException() : Exception(Format())
{
    private static string Format()
    {
        var message = "An unexpected issue has occured. Please raise an issue here: https://github.com/Quaverflow/MethodTrackingTool/issues with the message content of this exception";
        var errors = JsonSerializer.Serialize(MethodPatches.Result.UnexpectedIssues);

        return $"{message}{Environment.NewLine}{errors}";
    }
}