using System;
using System.Linq;
using HarmonyLib;
using Microsoft.AspNetCore.Builder;
using System.Net.Http;
using System.Threading;
using MethodTrackerTool.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

#if NETFRAMEWORK
using System.Web;
#endif

namespace MethodTrackerTool;

internal static class ApiPatcher
{
    public static void Initialize()
    {
        var harmony = new Harmony("com.mycompany.methodlogger");

        // 1) Client‑side: inject X‑Test‑Id into HttpClient requests
        var invokerType = typeof(HttpMessageInvoker);
        var sendAsync = invokerType.GetMethod(
            nameof(HttpMessageInvoker.SendAsync),
            new[] { typeof(HttpRequestMessage), typeof(CancellationToken) }
        );
        if (sendAsync != null)
        {
            harmony.Patch(
                sendAsync,
                prefix: new HarmonyMethod(typeof(ApiPatcher), nameof(SendAsync_Prefix))
            );
        }
    }

    /// <summary>
    /// Prefix: adds X‑Test‑Id header to outgoing HTTP requests.
    /// </summary>
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
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
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
}