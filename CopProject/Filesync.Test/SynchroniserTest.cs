using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Security;
using System.Text;
using Filesync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: DoNotParallelize]

namespace Filesync.Test;

[TestClass]
public sealed class SynchroniserTest
{
    private Synchroniser _sync = null!;
    private string _testDir = null!;

    /// <summary>
    /// Initializing Synchroniser for testing Synchronise
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        _sync = new Synchroniser();
        _testDir = $"Tests/{Guid.NewGuid():N}";
    }

    /// <summary>
    /// Clean up once test is completed
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        string fullPath = Path.Combine(_sync.GetDirectory(), _testDir);

        if (Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, recursive: true);
        }
    }

    /// <summary>
    /// Sets the backing field of an event to null so the null branch can be tested
    /// </summary>
    private void ClearEvent(string eventName)
    {
        FieldInfo? field = typeof(Synchroniser).GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.IsNotNull(field);
        field.SetValue(_sync, null);
    }

    /// <summary>
    /// Calls the private IsExpectedFileError method
    /// </summary>
    private static bool InvokeIsExpectedFileError(Exception ex)
    {
        MethodInfo? method = typeof(Synchroniser).GetMethod("IsExpectedFileError", BindingFlags.Static | BindingFlags.NonPublic);

        Assert.IsNotNull(method);

        return (bool)method.Invoke(null, new object[] { ex })!;
    }

    /// <summary>
    /// 1. Synchronise raises OnSyncComplete when root is empty
    /// </summary>
    [TestMethod]
    public void Synchronise_EmptyRoot_RaisesOnSyncComplete()
    {
        bool completed = false;
        _sync.OnSyncComplete += () => completed = true;

        _sync.Synchronise();

        Assert.IsTrue(completed);
    }

    /// <summary>
    /// 2. Synchronise raises OnSyncComplete when files exist in root
    /// </summary>
    [TestMethod]
    public void Synchronise_WithFiles_RaisesOnSyncComplete()
    {
        string path1 = $"{_testDir}/file1.txt";
        string path2 = $"{_testDir}/file2.txt";

        _sync.SaveFile(path1, Encoding.UTF8.GetBytes("hello"));
        _sync.SaveFile(path2, Encoding.UTF8.GetBytes("world"));

        bool completed = false;
        _sync.OnSyncComplete += () => completed = true;

        _sync.Synchronise();

        Assert.IsTrue(completed);
    }

    /// <summary>
    /// 3. Synchronise raises OnSyncComplete when nested subdirectories exist
    /// </summary>
    [TestMethod]
    public void Synchronise_WithNestedFiles_RaisesOnSyncComplete()
    {
        string path = $"{_testDir}/level1/level2/deep.txt";
        _sync.SaveFile(path, Encoding.UTF8.GetBytes("deep content"));

        bool completed = false;
        _sync.OnSyncComplete += () => completed = true;

        _sync.Synchronise();

        Assert.IsTrue(completed);
    }

    /// <summary>
    /// 4. Synchronise does not raise OnSyncError on a clean run
    /// </summary>
    [TestMethod]
    public void Synchronise_CleanRun_DoesNotRaiseOnSyncError()
    {
        bool errorRaised = false;
        _sync.OnSyncError += _ => errorRaised = true;

        _sync.Synchronise();

        Assert.IsFalse(errorRaised);
    }

    /// <summary>
    /// 5. Synchronise can be called multiple times without failing
    /// </summary>
    [TestMethod]
    public void Synchronise_CalledTwice_RaisesOnSyncCompleteEachTime()
    {
        int completedCount = 0;
        _sync.OnSyncComplete += () => completedCount++;

        _sync.Synchronise();
        _sync.Synchronise();

        Assert.AreEqual(2, completedCount);
    }

    /// <summary>
    /// 6. Synchronise succeeds with an empty file in root
    /// </summary>
    [TestMethod]
    public void Synchronise_WithEmptyFile_RaisesOnSyncComplete()
    {
        string path = $"{_testDir}/empty.txt";
        _sync.SaveFile(path, Array.Empty<byte>());

        bool completed = false;
        _sync.OnSyncComplete += () => completed = true;

        _sync.Synchronise();

        Assert.IsTrue(completed);
    }

    /// <summary>
    /// 7. Synchronise handles multiple files in various subfolders
    /// </summary>
    [TestMethod]
    public void Synchronise_WithMultipleFolders_RaisesOnSyncComplete()
    {
        _sync.SaveFile($"{_testDir}/evidences/case1.txt", Encoding.UTF8.GetBytes("evidence 1"));
        _sync.SaveFile($"{_testDir}/logs/whiteboard.txt", Encoding.UTF8.GetBytes("board log"));
        _sync.SaveFile($"{_testDir}/top.txt", Encoding.UTF8.GetBytes("top level"));

        bool completed = false;
        _sync.OnSyncComplete += () => completed = true;

        _sync.Synchronise();

        Assert.IsTrue(completed);
    }

    /// <summary>
    /// 8. Synchronise succeeds after updating an existing file
    /// </summary>
    [TestMethod]
    public void Synchronise_AfterFileUpdate_RaisesOnSyncComplete()
    {
        string path = $"{_testDir}/update.txt";
        _sync.SaveFile(path, Encoding.UTF8.GetBytes("version 1"));
        _sync.UpdateFile(path, Encoding.UTF8.GetBytes("version 2"));

        bool completed = false;
        _sync.OnSyncComplete += () => completed = true;

        _sync.Synchronise();

        Assert.IsTrue(completed);
    }

    /// <summary>
    /// 9. Synchronise succeeds after deleting a file
    /// </summary>
    [TestMethod]
    public void Synchronise_AfterFileDelete_RaisesOnSyncComplete()
    {
        string path = $"{_testDir}/temp.txt";
        _sync.SaveFile(path, Encoding.UTF8.GetBytes("temp content"));
        _sync.DeleteFile(path);

        bool completed = false;
        _sync.OnSyncComplete += () => completed = true;

        _sync.Synchronise();

        Assert.IsTrue(completed);
    }

    /// <summary>
    /// 10. Synchronise raises OnSyncError when a failure happens during synchronisation
    /// </summary>
    [TestMethod]
    public void Synchronise_WhenFailureOccurs_RaisesOnSyncError()
    {
        string? message = null;

        _sync.OnSyncComplete += () => throw new InvalidOperationException("sync failed");
        _sync.OnSyncError += msg => message = msg;

        _sync.Synchronise();

        Assert.AreEqual("sync failed", message);
    }

    /// <summary>
    /// 11. Synchronise succeeds when nobody is subscribed to OnSyncComplete
    /// </summary>
    [TestMethod]
    public void Synchronise_WithNullCompleteEvent_DoesNotThrow()
    {
        bool errorRaised = false;
        _sync.OnSyncError += _ => errorRaised = true;

        ClearEvent("OnSyncComplete");

        _sync.Synchronise();

        Assert.IsFalse(errorRaised);
    }

    /// <summary>
    /// 12. Synchronise does not throw when a failure happens and nobody is subscribed to OnSyncError
    /// </summary>
    [TestMethod]
    public void Synchronise_WhenFailureOccursWithNullErrorEvent_DoesNotThrow()
    {
        _sync.OnSyncComplete += () => throw new InvalidOperationException("sync failed");

        ClearEvent("OnSyncError");

        _sync.Synchronise();
    }

    /// <summary>
    /// 13. IsExpectedFileError accepts IOException
    /// </summary>
    [TestMethod]
    public void IsExpectedFileError_IOException_ReturnsTrue()
    {
        Assert.IsTrue(InvokeIsExpectedFileError(new IOException()));
    }

    /// <summary>
    /// 14. IsExpectedFileError accepts UnauthorizedAccessException
    /// </summary>
    [TestMethod]
    public void IsExpectedFileError_UnauthorizedAccessException_ReturnsTrue()
    {
        Assert.IsTrue(InvokeIsExpectedFileError(new UnauthorizedAccessException()));
    }

    /// <summary>
    /// 15. IsExpectedFileError accepts ArgumentException
    /// </summary>
    [TestMethod]
    public void IsExpectedFileError_ArgumentException_ReturnsTrue()
    {
        Assert.IsTrue(InvokeIsExpectedFileError(new ArgumentException()));
    }

    /// <summary>
    /// 16. IsExpectedFileError accepts NotSupportedException
    /// </summary>
    [TestMethod]
    public void IsExpectedFileError_NotSupportedException_ReturnsTrue()
    {
        Assert.IsTrue(InvokeIsExpectedFileError(new NotSupportedException()));
    }

    /// <summary>
    /// 17. IsExpectedFileError accepts SecurityException
    /// </summary>
    [TestMethod]
    public void IsExpectedFileError_SecurityException_ReturnsTrue()
    {
        Assert.IsTrue(InvokeIsExpectedFileError(new SecurityException()));
    }

    /// <summary>
    /// 18. IsExpectedFileError rejects unrelated exceptions
    /// </summary>
    [TestMethod]
    public void IsExpectedFileError_UnrelatedException_ReturnsFalse()
    {
        Assert.IsFalse(InvokeIsExpectedFileError(new InvalidOperationException()));
    }
}
