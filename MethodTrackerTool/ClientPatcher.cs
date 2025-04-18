using System;
using HarmonyLib;
using Microsoft.AspNetCore.Builder;
using System.Net.Http;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace MethodTrackerTool;

internal static class ClientPatcher
{
    public static void Initialize()
    {
        var invokerType = typeof(HttpMessageInvoker);
        var sendAsync = invokerType.GetMethod(
            nameof(HttpMessageInvoker.SendAsync),
            [typeof(HttpRequestMessage), typeof(CancellationToken)]
        );
        if (sendAsync != null)
        {
            HarmonyInitializer.HarmonyInstance.Patch(
                sendAsync,
                prefix: new HarmonyMethod(typeof(ClientPatcher), nameof(SendAsync_Prefix))
            );
        }
    }

    public static void SendAsync_Prefix(HttpRequestMessage request)
    {
        var testId = MethodPatches.CurrentTestId.Value;
        if (!string.IsNullOrEmpty(testId) && !request.Headers.Contains("X-Test-Id"))
        {
            request.Headers.Add("X-Test-Id", testId);
        }
    }
}

public static class TestServerExtensions
{
    public static IServiceCollection AddMethodLoggerFilter(this IServiceCollection services) 
        => services.AddSingleton<IStartupFilter>(new LoggingStartupFilter());

    internal class LoggingStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
            app =>
            {
                app.Use(async (ctx, nextMiddleware) =>
                {
                    if (ctx.Request.Headers.TryGetValue("X-Test-Id", out var id))
                    {
                        MethodPatches.Initialize(id!);
                    }
                    await nextMiddleware();
                });
                next(app);
            };
    }
}