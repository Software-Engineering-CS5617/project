// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Cloud.Networking.Server.Models;
using Cloud.Networking.Server.ViewModels;
using Microsoft.AspNetCore.Builder;
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

    /// <summary>Tests that middleware components are configured successfully.</summary>
    [TestMethod]
    public async Task ConfigureMiddlewaresRegistersSuccessfully()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        Program.ConfigureServices(builder.Services);
        await using WebApplication app = builder.Build();
        Program.ConfigureMiddlewares(app);
        Assert.IsNotNull(app);
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
