using Cloud.Persistence;

namespace Cloud.Test;

[TestClass]

/// <summary>
/// Tests the CosmosSettings class.
/// </summary>
public class TestCosmosSettings
{
    [TestMethod]
    public void KeyReturnsConfiguredValueAndThrowsWhenMissing()
    {
        string? originalKey = Environment.GetEnvironmentVariable("COSMOS_KEY");

        try
        {
            Environment.SetEnvironmentVariable("COSMOS_KEY", "test-key");
            Assert.AreEqual("test-key", CosmosSettings.Key);

            Environment.SetEnvironmentVariable("COSMOS_KEY", null);
            InvalidOperationException error = Assert.ThrowsExactly<InvalidOperationException>(
                () => _ = CosmosSettings.Key);
            Assert.IsTrue(error.Message.Contains("COSMOS_KEY is not set", StringComparison.Ordinal));
        }
        finally
        {
            Environment.SetEnvironmentVariable("COSMOS_KEY", originalKey);
        }
    }
}
