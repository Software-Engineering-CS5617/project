// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Cloud.Networking.Server.Models;
using Cloud.Networking.Server.ViewModels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace Cloud.Networking.Server.Test;

/// <summary>
/// Tests the startup wiring in Program (services, endpoints and the full application)
/// </summary>
[TestClass]
public class TestProgram
{
    private static bool HasMessageHubRoute(IEndpointRouteBuilder routes)
    {
        return routes.DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Any(endpoint => endpoint.RoutePattern.RawText == "/hub"
                && endpoint.Metadata.GetMetadata<HubMetadata>()?.HubType == typeof(MessageHub));
    }

    private static bool HasHealthRoute(IEndpointRouteBuilder routes)
    {
        return routes.DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Any(endpoint => endpoint.RoutePattern.RawText == "/health");
    }

    /// <summary>Tests that ConfigureServices registers the hub, one shared monitor and a view model per scope.</summary>
    [TestMethod]
    public void ConfigureServicesRegistersEverythingTheAppNeeds()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        Program.ConfigureServices(builder.Services);
        using ServiceProvider provider = builder.Services.BuildServiceProvider();

        // hub
        Assert.IsNotNull(provider.GetService<HubLifetimeManager<MessageHub>>(), "Hub is not resolvable.");

        // one shared monitor
        IServerMonitor monitor = provider.GetRequiredService<IServerMonitor>();
        Assert.IsInstanceOfType<ServerMonitor>(monitor);
        Assert.AreSame(monitor, provider.GetRequiredService<IServerMonitor>(), "Monitor is not a singleton.");

        // one view model per scope (browser tab)
        using IServiceScope firstScope = provider.CreateScope();
        using IServiceScope secondScope = provider.CreateScope();
        DashboardViewModel viewModel = firstScope.ServiceProvider.GetRequiredService<DashboardViewModel>();
        Assert.AreSame(viewModel, firstScope.ServiceProvider.GetRequiredService<DashboardViewModel>());
        Assert.AreNotSame(viewModel, secondScope.ServiceProvider.GetRequiredService<DashboardViewModel>());
    }

    /// <summary>
    /// Starts the app on a free local port with the real middlewares and endpoints, plus a /boom endpoint that always throws.
    /// </summary>
    private static async Task<(WebApplication App, HttpClient Client)> StartAppAsync(string environment)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(["--environment", environment, "--urls", "http://127.0.0.1:0"]);
        Program.ConfigureServices(builder.Services);
        WebApplication app = builder.Build();
        Program.ConfigureMiddlewares(app);
        Program.ConfigureEndpoints(app);
        app.MapGet("/boom", (Func<string>)(() => throw new InvalidOperationException("boom")));
        await app.StartAsync();

        return (app, new HttpClient { BaseAddress = new Uri(app.Urls.First()) });
    }

    /// <summary>Tests that outside Development an unhandled exception becomes a plain 500 without details.</summary>
    [TestMethod]
    public async Task UnhandledExceptionsBecomeAPlain500OutsideDevelopment()
    {
        (WebApplication app, HttpClient client) = await StartAppAsync("Production");
        await using WebApplication _ = app;
        using HttpClient __ = client;

        HttpResponseMessage response = await client.GetAsync("/boom");

        Assert.AreEqual(StatusCodes.Status500InternalServerError, (int)response.StatusCode);
        Assert.AreEqual("An unexpected error occurred.", await response.Content.ReadAsStringAsync());
    }

    /// <summary>Tests that in Development the custom error handler is not installed, so the error details are visible.</summary>
    [TestMethod]
    public async Task UnhandledExceptionsShowDetailsInDevelopment()
    {
        (WebApplication app, HttpClient client) = await StartAppAsync("Development");
        await using WebApplication _ = app;
        using HttpClient __ = client;

        HttpResponseMessage response = await client.GetAsync("/boom");

        Assert.AreEqual(StatusCodes.Status500InternalServerError, (int)response.StatusCode);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(), "boom");
    }

    /// <summary>Tests that the /health endpoint answers "ok".</summary>
    [TestMethod]
    public async Task HealthEndpointRespondsOk()
    {
        (WebApplication app, HttpClient client) = await StartAppAsync("Production");
        await using WebApplication _ = app;
        using HttpClient __ = client;

        HttpResponseMessage response = await client.GetAsync("/health");

        Assert.AreEqual(StatusCodes.Status200OK, (int)response.StatusCode);
        Assert.AreEqual("ok", await response.Content.ReadAsStringAsync());
    }

    /// <summary>Tests that ConfigureEndpoints maps the hub at /hub and the health check at /health.</summary>
    [TestMethod]
    public async Task ConfigureEndpointsMapsHubAndHealth()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        Program.ConfigureServices(builder.Services);
        await using WebApplication app = builder.Build();
        Program.ConfigureEndpoints(app);
        Assert.IsTrue(HasMessageHubRoute(app), "Expected MessageHub to be mapped at /hub.");
        Assert.IsTrue(HasHealthRoute(app), "Expected a /health endpoint.");
    }

    /// <summary>Tests that CreateApplication builds the app with services, endpoints and the given environment.</summary>
    [TestMethod]
    public async Task CreateApplicationWorksProperly()
    {
        await using WebApplication app = Program.CreateApplication(["--environment", "Staging"]);

        Assert.IsNotNull(app.Services.GetService<HubLifetimeManager<MessageHub>>());
        Assert.IsTrue(HasMessageHubRoute(app), "Expected MessageHub to be mapped at /hub.");
        Assert.AreEqual("Staging", app.Environment.EnvironmentName);
    }
}
