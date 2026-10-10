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
    /// 1. Root directory exists and is absolute
    /// </summary>
    [TestMethod]
    public void GetDirectory_ReturnsExistingAbsoluteDirectory()
    {
        string directory = _fileOps.GetDirectory();

        Assert.IsTrue(Path.IsPathFullyQualified(directory));
        Assert.IsTrue(Directory.Exists(directory));
    }

    /// <summary>
    /// 2. Save a file successfully
    /// </summary>
    [TestMethod]
    public void SaveFile_CreatesNewFile()
    {
        string path = $"{_testDir}/sample.txt";
        byte[] content = Encoding.UTF8.GetBytes("Hello FileSync");

        Assert.IsTrue(_fileOps.SaveFile(path, content));
        Assert.IsTrue(_fileOps.FileExists(path));
        CollectionAssert.AreEqual(content, _fileOps.ReadFile(path));
    }

    /// <summary>
    /// 3. Save an empty file
    /// </summary>
    [TestMethod]
    public void SaveFile_AllowsEmptyContent()
    {
        string path = $"{_testDir}/empty.txt";

        Assert.IsTrue(_fileOps.SaveFile(path, Array.Empty<byte>()));
        Assert.IsTrue(_fileOps.FileExists(path));
        Assert.IsEmpty(_fileOps.ReadFile(path));
    }

    /// <summary>
    /// 4. Saving the same path twice must not overwrite
    /// </summary>
    [TestMethod]
    public void SaveFile_DoesNotOverwriteExistingFile()
    {
        string path = $"{_testDir}/duplicate.txt";
        byte[] original = Encoding.UTF8.GetBytes("Original");
        byte[] replacement = Encoding.UTF8.GetBytes("Replacement");

        Assert.IsTrue(_fileOps.SaveFile(path, original));

        Assert.IsFalse(_fileOps.SaveFile(path, replacement));

        CollectionAssert.AreEqual(original, _fileOps.ReadFile(path));
    }

    /// <summary>
    /// 5. Reject null content.
    /// </summary>
    [TestMethod]
    public void SaveFile_RejectsNullContent()
    {
        string path = $"{_testDir}/null.txt";
        bool errorRaised = false;

        var sync = (ISync)_fileOps;
        sync.OnSyncError += _ => errorRaised = true;

        Assert.IsFalse(_fileOps.SaveFile(path, null!));
        Assert.IsTrue(errorRaised);
        Assert.IsFalse(_fileOps.FileExists(path));
    }

    /// <summary>
    /// 6. Read a saved file
    /// </summary>
    [TestMethod]
    public void ReadFile_ReturnsExactContent()
    {
        string path = $"{_testDir}/read.txt";
        byte[] expected = Encoding.UTF8.GetBytes("File contents: 123");

        Assert.IsTrue(_fileOps.SaveFile(path, expected));
        CollectionAssert.AreEqual(expected, _fileOps.ReadFile(path));
    }

    /// <summary>
    /// 7. Reading a missing file reports an error.
    /// </summary>
    [TestMethod]
    public void ReadFile_MissingFile_ReportsErrorAndThrows()
    {
        string path = $"{_testDir}/missing.txt";
        string directory = Path.Combine(_fileOps.GetDirectory(), _testDir);
        Directory.CreateDirectory(directory);

        bool errorRaised = false;
        bool exceptionThrown = false;

        ((ISync)_fileOps).OnSyncError += _ => errorRaised = true;

        try
        {
            _fileOps.ReadFile(path);
        }
        catch (FileNotFoundException)
        {
            exceptionThrown = true;
        }

        Assert.IsTrue(exceptionThrown);
        Assert.IsTrue(errorRaised);
    }
}
