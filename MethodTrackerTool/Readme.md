# MethodTracker Tool

## What it does

The **MethodTracker Tool** allows you to run individual unit tests while automatically capturing detailed runtime information about method calls in your codebase. It tracks execution in a hierarchical manner, storing the following information for each method call:

- **Method Name**: Name of the invoked method.
- **Parameters**: Method parameters and their values.
- **Return Type**: Type of the returned value.
- **Return Value**: The value returned from the method.
- **Start Time**: Timestamp of method entry.
- **End Time**: Timestamp of method exit.
- **Elapsed Time**: Total time spent executing the method (including nested calls).
- **Exclusive Elapsed Time**: Execution time excluding nested method calls.
- **Memory Usage**:
  - **Memory Before**: Memory usage before method execution.
  - **Memory After**: Memory usage after method execution.
  - **Memory Increase**: Difference in memory consumption due to the method call.
- **Exceptions**: List of any exceptions thrown during method execution.
- **Children**: Nested method calls made within this method call.

All method calls are captured and structured hierarchically, giving clear insights into your test's execution flow.

---

## Usage

To use **MethodTracker Tool**, annotate your test methods as follows:

```csharp
[Fact]
public async Task Sample()
{
    MethodLogger.Initialize("Sample");

    // Your test code here

    MethodLogger.PrintJson();
}
```

Then, specify which assemblies to instrument by adding the following attribute in an assembly-level file:

```csharp
[assembly: AssemblyMarker(typeof(A_SERVICE_FROM_TARGET_ASSEMBLY))]
```

Replace `A_SERVICE_FROM_TARGET_ASSEMBLY` with a type from the assembly you'd like to track.

**Important:**
- Run only one test at a time, as Harmony patches apply globally. Running tests in parallel will cause unexpected behavior.

Use this tool in conjunction with the [MethodTracker Visualizer](https://marketplace.visualstudio.com/items?itemName=MirkoSangrigoli.MethodTrackerVisualizer).

