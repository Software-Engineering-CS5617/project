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

    /// <summary>
    /// 8. ReadFile rejects an absolute path and reports an error.
    /// </summary>
    [TestMethod]
    public void ReadFile_AbsolutePath_ReportsErrorAndThrows()
    {
        string absolutePath = Path.Combine(
            Path.GetTempPath(), "FileSync_missing.txt");

        bool errorRaised = false;
        bool exceptionThrown = false;

        ((ISync)_fileOps).OnSyncError += _ => errorRaised = true;

        try
        {
            _fileOps.ReadFile(absolutePath);
        }
        catch (ArgumentException)
        {
            exceptionThrown = true;
        }

        Assert.IsTrue(exceptionThrown);
        Assert.IsTrue(errorRaised);
    }

    /// <summary>
    /// 9. ReadFile rejects a traversal path and reports an error.
    /// </summary>
    [TestMethod]
    public void ReadFile_PathTraversal_ReportsErrorAndThrows()
    {
        bool errorRaised = false;
        bool exceptionThrown = false;

        ((ISync)_fileOps).OnSyncError += _ => errorRaised = true;

        try
        {
            _fileOps.ReadFile("../outside.txt");
        }
        catch (ArgumentException)
        {
            exceptionThrown = true;
        }

        Assert.IsTrue(exceptionThrown);
        Assert.IsTrue(errorRaised);
    }


    /// <summary>
    /// 10. Reject an empty relative path.
    /// </summary>
    [TestMethod]
    public void SaveFile_RejectsEmptyPath()
    {
        bool errorRaised = false;
        ((ISync)_fileOps).OnSyncError += _ => errorRaised = true;

        Assert.IsFalse(_fileOps.SaveFile("", Encoding.UTF8.GetBytes("data")));
        Assert.IsTrue(errorRaised);
    }

    /// <summary>
    /// 11. Reject a whitespace-only path.
    /// </summary>
    [TestMethod]
    public void SaveFile_RejectsWhitespacePath()
    {
        bool errorRaised = false;
        ((ISync)_fileOps).OnSyncError += _ => errorRaised = true;

        Assert.IsFalse(_fileOps.SaveFile("   ", Encoding.UTF8.GetBytes("data")));
        Assert.IsTrue(errorRaised);
    }

    /// <summary>
    /// 12. Reject an absolute path.
    /// </summary>
    [TestMethod]
    public void SaveFile_RejectsAbsolutePath()
    {
        string outsidePath = Path.Combine(Path.GetTempPath(), $"FileSync_{Guid.NewGuid():N}.txt");

        bool errorRaised = false;
        ((ISync)_fileOps).OnSyncError += _ => errorRaised = true;

        try
        {
            Assert.IsFalse(_fileOps.SaveFile(outsidePath, Encoding.UTF8.GetBytes("data")));

            Assert.IsTrue(errorRaised);
            Assert.IsFalse(File.Exists(outsidePath));
        }
        finally
        {
            if (File.Exists(outsidePath))
            {
                File.Delete(outsidePath);
            }
        }
    }

    /// <summary>
    /// 13. Reject directory traversal outside the root.
    /// </summary>
    [TestMethod]
    public void SaveFile_RejectsPathTraversal()
    {
        string traversalPath = $"../FileSync_{Guid.NewGuid():N}.txt";
        bool errorRaised = false;

        ((ISync)_fileOps).OnSyncError += _ => errorRaised = true;

        Assert.IsFalse(_fileOps.SaveFile(traversalPath, Encoding.UTF8.GetBytes("data")));
        Assert.IsTrue(errorRaised);
    }

    /// <summary>
    /// 14. Reject paths that resolve to the root directory itself.
    /// </summary>
    [TestMethod]
    public void SaveFile_RejectsPathResolvingToRoot()
    {
        bool errorRaised = false;
        ((ISync)_fileOps).OnSyncError += _ => errorRaised = true;

        Assert.IsFalse(_fileOps.SaveFile("Tests/..", Encoding.UTF8.GetBytes("data")));
        Assert.IsTrue(errorRaised);
    }

    /// <summary>
    /// 15. Allow a valid nested relative path.
    /// </summary>
    [TestMethod]
    public void SaveFile_CreatesNestedDirectories()
    {
        string path = $"{_testDir}/level1/level2/nested.txt";
        byte[] expected = Encoding.UTF8.GetBytes("Nested content");

        Assert.IsTrue(_fileOps.SaveFile(path, expected));
        Assert.IsTrue(_fileOps.FileExists(path));
        CollectionAssert.AreEqual(expected, _fileOps.ReadFile(path));
    }


    /// <summary>
    /// 16. Update an existing file successfully.
    /// </summary>
    [TestMethod]
    public void UpdateFile_ReplacesExistingContent()
    {
        string path = $"{_testDir}/update.txt";
        byte[] original = Encoding.UTF8.GetBytes("Original");
        byte[] updated = Encoding.UTF8.GetBytes("Updated");

        Assert.IsTrue(_fileOps.SaveFile(path, original));
        Assert.IsTrue(_fileOps.UpdateFile(path, updated));

        CollectionAssert.AreEqual(updated, _fileOps.ReadFile(path));
    }

    /// <summary>
    /// 17. Updating a missing file reports an error.
    /// </summary>
    [TestMethod]
    public void UpdateFile_MissingFile_ReturnsFalseAndReportsError()
    {
        string path = $"{_testDir}/missing-update.txt";
        bool errorRaised = false;

        ((ISync)_fileOps).OnSyncError += _ => errorRaised = true;

        Assert.IsFalse(_fileOps.UpdateFile(path, Encoding.UTF8.GetBytes("Updated")));
        Assert.IsTrue(errorRaised);
    }

    /// <summary>
    /// 18. Updating with null content must fail.
    /// </summary>
    [TestMethod]
    public void UpdateFile_RejectsNullContent()
    {
        string path = $"{_testDir}/null-update.txt";
        byte[] original = Encoding.UTF8.GetBytes("Original");

        Assert.IsTrue(_fileOps.SaveFile(path, original));

        bool errorRaised = false;

        ((ISync)_fileOps).OnSyncError += _ => errorRaised = true;

        Assert.IsFalse(_fileOps.UpdateFile(path, null!));
        Assert.IsTrue(errorRaised);
        CollectionAssert.AreEqual(original, _fileOps.ReadFile(path));
    }

    /// <summary>
    /// 19. Updating a file outside the root must fail.
    /// </summary>
    [TestMethod]
    public void UpdateFile_RejectsPathTraversal()
    {
        bool errorRaised = false;
        ((ISync)_fileOps).OnSyncError += _ => errorRaised = true;

        Assert.IsFalse(_fileOps.UpdateFile("../outside.txt", Encoding.UTF8.GetBytes("Modified")));
        Assert.IsTrue(errorRaised);
    }


    /// <summary>
    /// 20. Delete an existing file successfully.
    /// </summary>
    [TestMethod]
    public void DeleteFile_RemovesExistingFile()
    {
        string path = $"{_testDir}/delete.txt";

        Assert.IsTrue(_fileOps.SaveFile(path, Encoding.UTF8.GetBytes("Delete me")));
        Assert.IsTrue(_fileOps.DeleteFile(path));
        Assert.IsFalse(_fileOps.FileExists(path));
    }

    /// <summary>
    /// 21. Deleting a missing file reports an error.
    /// </summary>
    [TestMethod]
    public void DeleteFile_MissingFile_ReturnsFalseAndReportsError()
    {
        string path = $"{_testDir}/missing-delete.txt";
        bool errorRaised = false;

        ((ISync)_fileOps).OnSyncError += _ => errorRaised = true;

        Assert.IsFalse(_fileOps.DeleteFile(path));
        Assert.IsTrue(errorRaised);
    }

    /// <summary>
    /// 22. Deleting a file outside the root must fail.
    /// </summary>
    [TestMethod]
    public void DeleteFile_RejectsPathTraversal()
    {
        bool errorRaised = false;
        ((ISync)_fileOps).OnSyncError += _ => errorRaised = true;

        Assert.IsFalse(_fileOps.DeleteFile("../outside.txt"));
        Assert.IsTrue(errorRaised);
    }


    /// <summary>
    /// 23. FileExists returns false for a missing file.
    /// </summary>
    [TestMethod]
    public void FileExists_MissingFile_ReturnsFalse()
    {
        Assert.IsFalse(_fileOps.FileExists($"{_testDir}/not-present.txt"));
    }

    /// <summary>
    /// 24. FileExists rejects an invalid path and reports an error.
    /// </summary>
    [TestMethod]
    public void FileExists_RejectsPathTraversalAndReportsError()
    {
        bool errorRaised = false;
        ((ISync)_fileOps).OnSyncError += _ => errorRaised = true;

        Assert.IsFalse(_fileOps.FileExists("../outside.txt"));
        Assert.IsTrue(errorRaised);
    }


}
