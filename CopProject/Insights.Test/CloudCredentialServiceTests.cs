using Insights.AI;
using Xunit;

namespace Insights.Test;

public sealed class CloudCredentialServiceTests
{
    [Fact]
    public async Task GetSecretAsync_WhenEnvironmentVariableExists_ReturnsValue()
    {
        string secretName = $"INSIGHTS_TEST_SECRET_{Guid.NewGuid():N}";
        const string expectedSecret = "test-secret-value";
        Environment.SetEnvironmentVariable(secretName, expectedSecret);

        try
        {
            var service = new CloudCredentialService();

            string result = await service.GetSecretAsync(secretName);

            Assert.Equal(expectedSecret, result);
        }
        finally
        {
            Environment.SetEnvironmentVariable(secretName, null);
        }
    }

    [Fact]
    public async Task GetSecretAsync_WhenEnvironmentVariableIsMissing_ThrowsInvalidOperationException()
    {
        string secretName = $"INSIGHTS_MISSING_SECRET_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(secretName, null);

        var service = new CloudCredentialService();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetSecretAsync(secretName));
    }

    [Fact]
    public async Task GetSecretAsync_WhenEnvironmentVariableIsEmpty_ThrowsInvalidOperationException()
    {
        string secretName = $"INSIGHTS_EMPTY_SECRET_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(secretName, string.Empty);

        try
        {
            var service = new CloudCredentialService();

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.GetSecretAsync(secretName));
        }
        finally
        {
            Environment.SetEnvironmentVariable(secretName, null);
        }
    }
}
