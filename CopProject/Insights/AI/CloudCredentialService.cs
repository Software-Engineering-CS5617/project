using System.Diagnostics;

namespace Insights.AI;

public interface ICloudCredentialService
{
    Task<string> GetSecretAsync(string secretName);
}

public class CloudCredentialService : ICloudCredentialService
{
    public Task<string> GetSecretAsync(string secretName)
    {
        Trace.TraceInformation(
            "[Insights] Cloud credential lookup started for '{0}'.", secretName);

        string? secret = Environment.GetEnvironmentVariable(secretName);

        if (string.IsNullOrEmpty(secret))
        {
            Trace.TraceError(
                "[Insights] Required cloud credential '{0}' was not found.", secretName);

            throw new InvalidOperationException(
                $"Secret '{secretName}' not found.");
        }

        Trace.TraceInformation(
            "[Insights] Cloud credential '{0}' was retrieved successfully.", secretName);

        return Task.FromResult(secret);
    }
}
