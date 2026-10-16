using System;
using System.IO;
using System.Text;
using Filesync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
        // save a couple of files first
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
}
