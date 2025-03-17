# MethodTracker (Beta)

**MethodTracker** is a lightweight debugging and logging tool for .NET that leverages Harmony to instrument your application at runtime—capturing method calls, performance metrics, and exceptions without modifying your source code.

In addition to the core logging functionality available via a NuGet package, a Visual Studio extension (VSIX) is available for interactive visualization, searching, and navigation of logged data.

## Features

- **Method Call Logging:** Automatically logs method invocations with parameters and return values.
- **Performance Metrics:** Measures execution time and memory usage before and after each call.
- **Exception Tracking:** Captures exceptions (even those caught internally) to aid in diagnosing issues.
- **Hierarchical Call Trees:** Displays a complete call hierarchy for easy drill-down analysis.
- **Visual Studio Integration:**  
  A VSIX extension provides a dedicated tool window for an interactive experience to visualize and navigate the log data.
  Download here [Method Tracker Visualizer GitHub Releases](https://marketplace.visualstudio.com/items?itemName=MirkoSangrigoli.MethodTrackerVisualizer) page and install it into Visual Studio.

## How It Works

MethodTracker uses Harmony to inject IL code into your application. It instruments method entry, exit, and exception paths to log detailed diagnostic data without requiring changes to your source code. This makes it ideal for debugging, testing, and performance analysis.

The VSIX extension integrates with Visual Studio, offering an interactive interface to:
- View the call tree.
- Filter log entries.
- Navigate directly to specific log entries.

## Installation

### NuGet Package

Install the beta NuGet package in your project:
