using Cloud.Crash;

namespace Cloud.Test.Crash;

/// <summary>
/// Tests the mapping from exception type names to categories.
/// </summary>
[TestClass]
public class TestCrashCategorizer
{
    /// <summary>
    /// Known exception types map to their category, with or without a namespace.
    /// </summary>
    [TestMethod]
    [DataRow("System.NullReferenceException", CrashCategory.NullReference)]
    [DataRow("NullReferenceException", CrashCategory.NullReference)]
    [DataRow("System.ArgumentNullException", CrashCategory.InvalidArgument)]
    [DataRow("System.ArgumentOutOfRangeException", CrashCategory.InvalidArgument)]
    [DataRow("System.InvalidOperationException", CrashCategory.InvalidState)]
    [DataRow("System.ObjectDisposedException", CrashCategory.InvalidState)]
    [DataRow("System.IO.FileNotFoundException", CrashCategory.FileSystem)]
    [DataRow("System.UnauthorizedAccessException", CrashCategory.FileSystem)]
    [DataRow("System.Net.Sockets.SocketException", CrashCategory.Network)]
    [DataRow("System.Net.Http.HttpRequestException", CrashCategory.Network)]
    [DataRow("System.TimeoutException", CrashCategory.Network)]
    [DataRow("Microsoft.Azure.Cosmos.CosmosException", CrashCategory.Storage)]
    [DataRow("System.Threading.Tasks.TaskCanceledException", CrashCategory.Concurrency)]
    [DataRow("System.OperationCanceledException", CrashCategory.Concurrency)]
    [DataRow("System.Xml.XmlException", CrashCategory.DataFormat)]
    [DataRow("System.Text.Json.JsonException", CrashCategory.DataFormat)]
    [DataRow("System.InvalidCastException", CrashCategory.DataFormat)]
    [DataRow("System.OutOfMemoryException", CrashCategory.Resources)]
    [DataRow("System.Runtime.InteropServices.COMException", CrashCategory.Platform)]
    [DataRow("System.DllNotFoundException", CrashCategory.Platform)]
    [DataRow("System.Collections.Generic.KeyNotFoundException", CrashCategory.Logic)]
    [DataRow("System.IndexOutOfRangeException", CrashCategory.Logic)]
    [DataRow("System.DivideByZeroException", CrashCategory.Logic)]
    [DataRow("My.Custom.WeirdException", CrashCategory.Other)]
    public void KnownTypesMapToCategory(string exceptionType, CrashCategory expected)
    {
        Assert.AreEqual(expected, CrashCategorizer.Categorize(exceptionType));
    }

    /// <summary>
    /// Missing type names are categorized as Other.
    /// </summary>
    [TestMethod]
    public void NullOrBlankIsOther()
    {
        Assert.AreEqual(CrashCategory.Other, CrashCategorizer.Categorize(null));
        Assert.AreEqual(CrashCategory.Other, CrashCategorizer.Categorize("  "));
    }
}
