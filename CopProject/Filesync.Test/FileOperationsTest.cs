using System;
using System.IO;
using System.Text;
using Filesync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Filesync.Test;

[TestClass]
public class FileOperationsTest
{
    private IFileOperations _fileOps = null!;
    private string _testDir = null!;

    /// <summary>
    /// Initializing Synchroniser for testing FileOperations
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        _fileOps = new Synchroniser();
        _testDir = $"Tests/{Guid.NewGuid():N}";
    }

    /// <summary>
    /// Clean up once Test in completed
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        string fullPath = Path.Combine(_fileOps.GetDirectory(), _testDir);

        if (Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, recursive: true);
        }
    }
    /// <summary>
    /// 1. Root directory exists and is absolute.
    /// </summary>
    [TestMethod]
    public void GetDirectory_ReturnsExistingAbsoluteDirectory()
    {
        string directory = _fileOps.GetDirectory();

        Assert.IsTrue(Path.IsPathFullyQualified(directory));
        Assert.IsTrue(Directory.Exists(directory));
    }


}
