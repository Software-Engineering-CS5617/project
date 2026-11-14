using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;

namespace Cloud.Functions;

/// <summary>
/// Entry point of the crash fingerprinting Azure Function app.
/// </summary>
public static class Program
{
    /// <summary>
    /// Starts the Function host.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    public static void Main(string[] args)
    {
        FunctionsApplicationBuilder builder = FunctionsApplication.CreateBuilder(args);
        builder.Build().Run();
    }
}
