using MethodTracker.MockProject;
using MethodTrackerTool.Public;
using MethodTrackerTool.TestApi;

[assembly: AssemblyMarker(typeof(OrderService))]
[assembly: AssemblyMarker(typeof(WeatherForecast))]

[assembly: CollectionBehavior(DisableTestParallelization = false, MaxParallelThreads = 0)]