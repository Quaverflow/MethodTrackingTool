using MethodTracker.MockProject;
using MethodTrackerTool.Public;
using MethodTrackerTool.TestApi;

[assembly: AssemblyMarker(typeof(OrderService))]
[assembly: AssemblyMarker(typeof(WeatherForecast))]
[assembly: PrivatePropertyMarker(typeof(Order), ["PrivateField", "PrivateProperty"])]

[assembly: CollectionBehavior(DisableTestParallelization = false, MaxParallelThreads = 0)]