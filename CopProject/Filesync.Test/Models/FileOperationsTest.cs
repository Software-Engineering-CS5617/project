// Author : Murali

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FileSynchroniser.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Filesync.Test.Models;

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
    /// Saves a single file through the multi-file SaveFile and returns its result.
    /// </summary>
    private bool Save(string path, byte[] content)
    {
        return _fileOps.SaveFile(new Dictionary<string, byte[]> { [path] = content })[path];
    }

    /// <summary>
    /// Reads a single file through the multi-file ReadFile; null when it could not be read.
    /// </summary>
    private byte[]? Read(string path)
    {
        return _fileOps.ReadFile(new[] { path })[path];
    }

    /// <summary>
    /// Updates a single file through the multi-file UpdateFile and returns its result.
    /// </summary>
    private bool Update(string path, byte[] content)
    {
        return _fileOps.UpdateFile(new Dictionary<string, byte[]> { [path] = content })[path];
    }

    /// <summary>
    /// Deletes a single file through the multi-file DeleteFile and returns its result.
    /// </summary>
    private bool Delete(string path)
    {
        return _fileOps.DeleteFile(new[] { path })[path];
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

        Assert.IsTrue(Save(path, content));
        Assert.IsTrue(_fileOps.FileExists(path));
        CollectionAssert.AreEqual(content, Read(path));
    }

    /// <summary>
    /// 3. Save an empty file
    /// </summary>
    [TestMethod]
    public void SaveFile_AllowsEmptyContent()
    {
        string path = $"{_testDir}/empty.txt";

        Assert.IsTrue(Save(path, Array.Empty<byte>()));
        Assert.IsTrue(_fileOps.FileExists(path));
        Assert.IsEmpty(Read(path)!);
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

        Assert.IsTrue(Save(path, original));

        Assert.IsFalse(Save(path, replacement));

        CollectionAssert.AreEqual(original, Read(path));
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

        Assert.IsFalse(Save(path, null!));
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

        Assert.IsTrue(Save(path, expected));
        CollectionAssert.AreEqual(expected, Read(path));
    }

    /// <summary>
    /// 7. Reading a missing file reports an error and maps the path to null.
    /// </summary>
    [TestMethod]
    public void ReadFile_MissingFile_ReturnsNullAndReportsError()
    {
        string path = $"{_testDir}/missing.txt";
        string directory = Path.Combine(_fileOps.GetDirectory(), _testDir);
        Directory.CreateDirectory(directory);

        bool errorRaised = false;

        ((ISync)_fileOps).OnSyncError += _ => errorRaised = true;

        IReadOnlyDictionary<string, byte[]?> results = _fileOps.ReadFile(new[] { path });

        Assert.IsTrue(results.ContainsKey(path));
        Assert.IsNull(results[path]);
        Assert.IsTrue(errorRaised);
    }

    /// <summary>
    /// 8. ReadFile rejects an absolute path and reports an error.
    /// </summary>
    [TestMethod]
    public void ReadFile_AbsolutePath_ReturnsNullAndReportsError()
    {
        string absolutePath = Path.Combine(
            Path.GetTempPath(), "FileSync_missing.txt");

        bool errorRaised = false;

        ((ISync)_fileOps).OnSyncError += _ => errorRaised = true;

        IReadOnlyDictionary<string, byte[]?> results = _fileOps.ReadFile(new[] { absolutePath });

        Assert.IsNull(results[absolutePath]);
        Assert.IsTrue(errorRaised);
    }

    /// <summary>
    /// 9. ReadFile rejects a traversal path and reports an error.
    /// </summary>
    [TestMethod]
    public void ReadFile_PathTraversal_ReturnsNullAndReportsError()
    {
        bool errorRaised = false;

        ((ISync)_fileOps).OnSyncError += _ => errorRaised = true;

        IReadOnlyDictionary<string, byte[]?> results = _fileOps.ReadFile(new[] { "../outside.txt" });

        Assert.IsNull(results["../outside.txt"]);
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

        Assert.IsFalse(Save("", Encoding.UTF8.GetBytes("data")));
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

        Assert.IsFalse(Save("   ", Encoding.UTF8.GetBytes("data")));
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
            Assert.IsFalse(Save(outsidePath, Encoding.UTF8.GetBytes("data")));

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

        Assert.IsFalse(Save(traversalPath, Encoding.UTF8.GetBytes("data")));
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

        Assert.IsFalse(Save("Tests/..", Encoding.UTF8.GetBytes("data")));
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

        Assert.IsTrue(Save(path, expected));
        Assert.IsTrue(_fileOps.FileExists(path));
        CollectionAssert.AreEqual(expected, Read(path));
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

        Assert.IsTrue(Save(path, original));
        Assert.IsTrue(Update(path, updated));

        CollectionAssert.AreEqual(updated, Read(path));
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

        Assert.IsFalse(Update(path, Encoding.UTF8.GetBytes("Updated")));
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

        Assert.IsTrue(Save(path, original));

        bool errorRaised = false;

        ((ISync)_fileOps).OnSyncError += _ => errorRaised = true;

        Assert.IsFalse(Update(path, null!));
        Assert.IsTrue(errorRaised);
        CollectionAssert.AreEqual(original, Read(path));
    }

    /// <summary>
    /// 19. Updating a file outside the root must fail.
    /// </summary>
    [TestMethod]
    public void UpdateFile_RejectsPathTraversal()
    {
        bool errorRaised = false;
        ((ISync)_fileOps).OnSyncError += _ => errorRaised = true;

        Assert.IsFalse(Update("../outside.txt", Encoding.UTF8.GetBytes("Modified")));
        Assert.IsTrue(errorRaised);
    }


    /// <summary>
    /// 20. Delete an existing file successfully.
    /// </summary>
    [TestMethod]
    public void DeleteFile_RemovesExistingFile()
    {
        string path = $"{_testDir}/delete.txt";

        Assert.IsTrue(Save(path, Encoding.UTF8.GetBytes("Delete me")));
        Assert.IsTrue(Delete(path));
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

        Assert.IsFalse(Delete(path));
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

        Assert.IsFalse(Delete("../outside.txt"));
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

    /// <summary>
    /// 25. Saving several files at once saves every file.
    /// </summary>
    [TestMethod]
    public void SaveFile_MultipleFiles_SavesAll()
    {
        var files = new Dictionary<string, byte[]> {
            [$"{_testDir}/a.txt"] = Encoding.UTF8.GetBytes("A"),
            [$"{_testDir}/sub/b.txt"] = Encoding.UTF8.GetBytes("B"),
            [$"{_testDir}/sub/deeper/c.txt"] = Encoding.UTF8.GetBytes("C")
        };

        IReadOnlyDictionary<string, bool> results = _fileOps.SaveFile(files);

        Assert.HasCount(3, results);
        Assert.IsTrue(results.Values.All(saved => saved));

        foreach (KeyValuePair<string, byte[]> file in files)
        {
            CollectionAssert.AreEqual(file.Value, Read(file.Key));
        }
    }

    /// <summary>
    /// 26. One failing file does not stop the others from being saved.
    /// </summary>
    [TestMethod]
    public void SaveFile_PartialFailure_SavesTheRest()
    {
        string existing = $"{_testDir}/existing.txt";
        string fresh = $"{_testDir}/fresh.txt";

        Assert.IsTrue(Save(existing, Encoding.UTF8.GetBytes("Original")));

        var files = new Dictionary<string, byte[]> {
            [existing] = Encoding.UTF8.GetBytes("Replacement"),
            [fresh] = Encoding.UTF8.GetBytes("Fresh")
        };

        IReadOnlyDictionary<string, bool> results = _fileOps.SaveFile(files);

        Assert.IsFalse(results[existing]);
        Assert.IsTrue(results[fresh]);
        Assert.IsTrue(_fileOps.FileExists(fresh));
        CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("Original"), Read(existing));
    }

    /// <summary>
    /// 27. Several failures in one call raise a single summary error event.
    /// </summary>
    [TestMethod]
    public void SaveFile_MultipleFailures_RaisesSingleSummaryError()
    {
        int errorCount = 0;
        string? message = null;

        ((ISync)_fileOps).OnSyncError += msg => {
            errorCount++;
            message = msg;
        };

        var files = new Dictionary<string, byte[]> {
            ["../bad1.txt"] = Encoding.UTF8.GetBytes("1"),
            ["../bad2.txt"] = Encoding.UTF8.GetBytes("2"),
            [$"{_testDir}/good.txt"] = Encoding.UTF8.GetBytes("ok")
        };

        _fileOps.SaveFile(files);

        Assert.AreEqual(1, errorCount);
        StringAssert.Contains(message, "2 file operation(s) failed");
        StringAssert.Contains(message, "bad1.txt");
        StringAssert.Contains(message, "bad2.txt");
    }

    /// <summary>
    /// 28. A successful call raises no error event.
    /// </summary>
    [TestMethod]
    public void SaveFile_AllSucceed_DoesNotRaiseError()
    {
        bool errorRaised = false;
        ((ISync)_fileOps).OnSyncError += _ => errorRaised = true;

        _fileOps.SaveFile(new Dictionary<string, byte[]> {
            [$"{_testDir}/one.txt"] = Encoding.UTF8.GetBytes("1"),
            [$"{_testDir}/two.txt"] = Encoding.UTF8.GetBytes("2")
        });

        Assert.IsFalse(errorRaised);
    }

    /// <summary>
    /// 29. A null collection throws.
    /// </summary>
    [TestMethod]
    public void SaveFile_NullCollection_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _fileOps.SaveFile(null!));
    }

    /// <summary>
    /// 30. An empty collection returns an empty result and raises no error.
    /// </summary>
    [TestMethod]
    public void SaveFile_EmptyCollection_ReturnsEmptyResult()
    {
        bool errorRaised = false;
        ((ISync)_fileOps).OnSyncError += _ => errorRaised = true;

        IReadOnlyDictionary<string, bool> results = _fileOps.SaveFile(new Dictionary<string, byte[]>());

        Assert.IsEmpty(results);
        Assert.IsFalse(errorRaised);
    }

    /// <summary>
    /// 31. Two paths in one call that resolve to the same file: only one is saved.
    /// </summary>
    [TestMethod]
    public void SaveFile_PathsResolvingToSameFile_OnlyFirstIsSaved()
    {
        bool errorRaised = false;
        ((ISync)_fileOps).OnSyncError += _ => errorRaised = true;

        var files = new Dictionary<string, byte[]> {
            [$"{_testDir}/same.txt"] = Encoding.UTF8.GetBytes("first"),
            [$"{_testDir}/other/../same.txt"] = Encoding.UTF8.GetBytes("second")
        };

        IReadOnlyDictionary<string, bool> results = _fileOps.SaveFile(files);

        Assert.HasCount(2, results);
        Assert.AreEqual(1, results.Values.Count(saved => saved));
        Assert.IsTrue(errorRaised);
        CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("first"), Read($"{_testDir}/same.txt"));
    }

    /// <summary>
    /// 32. More files than the batch limit: every file is reported as failed and none are saved.
    /// </summary>
    [TestMethod]
    public void SaveFile_TooManyFiles_FailsAllAndRaisesError()
    {
        bool errorRaised = false;
        ((ISync)_fileOps).OnSyncError += _ => errorRaised = true;

        var files = new Dictionary<string, byte[]>();

        for (int i = 0; i <= Synchroniser.MaxBatchFileCount; i++)
        {
            files[$"{_testDir}/limit/{i}.txt"] = new byte[] { 1 };
        }

        IReadOnlyDictionary<string, bool> results = _fileOps.SaveFile(files);

        Assert.HasCount(Synchroniser.MaxBatchFileCount + 1, results);
        Assert.IsFalse(results.Values.Any(saved => saved));
        Assert.IsTrue(errorRaised);
        Assert.IsFalse(_fileOps.FileExists($"{_testDir}/limit/0.txt"));
    }

    /// <summary>
    /// 33. Reading several files returns the content of each.
    /// </summary>
    [TestMethod]
    public void ReadFile_MultipleFiles_ReturnsAllContent()
    {
        string path1 = $"{_testDir}/r1.txt";
        string path2 = $"{_testDir}/r2.txt";
        byte[] content1 = Encoding.UTF8.GetBytes("one");
        byte[] content2 = Encoding.UTF8.GetBytes("two");

        Assert.IsTrue(Save(path1, content1));
        Assert.IsTrue(Save(path2, content2));

        IReadOnlyDictionary<string, byte[]?> results = _fileOps.ReadFile(new[] { path1, path2 });

        Assert.HasCount(2, results);
        CollectionAssert.AreEqual(content1, results[path1]);
        CollectionAssert.AreEqual(content2, results[path2]);
    }

    /// <summary>
    /// 34. One unreadable file maps to null while the others are still returned.
    /// </summary>
    [TestMethod]
    public void ReadFile_PartialFailure_ReturnsNullOnlyForFailedFile()
    {
        string good = $"{_testDir}/good.txt";
        string missing = $"{_testDir}/missing.txt";
        byte[] content = Encoding.UTF8.GetBytes("good");

        Assert.IsTrue(Save(good, content));

        int errorCount = 0;
        ((ISync)_fileOps).OnSyncError += _ => errorCount++;

        IReadOnlyDictionary<string, byte[]?> results = _fileOps.ReadFile(new[] { good, missing });

        CollectionAssert.AreEqual(content, results[good]);
        Assert.IsNull(results[missing]);
        Assert.AreEqual(1, errorCount);
    }

    /// <summary>
    /// 35. A null collection throws.
    /// </summary>
    [TestMethod]
    public void ReadFile_NullCollection_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _fileOps.ReadFile(null!));
    }

    /// <summary>
    /// 36. A null entry in the path list is skipped and reported.
    /// </summary>
    [TestMethod]
    public void ReadFile_NullPathEntry_IsSkippedAndReported()
    {
        bool errorRaised = false;
        ((ISync)_fileOps).OnSyncError += _ => errorRaised = true;

        IReadOnlyDictionary<string, byte[]?> results = _fileOps.ReadFile(new string[] { null! });

        Assert.IsEmpty(results);
        Assert.IsTrue(errorRaised);
    }

    /// <summary>
    /// 37. The same path listed twice is processed once and raises no error.
    /// </summary>
    [TestMethod]
    public void ReadFile_DuplicatePath_IsProcessedOnce()
    {
        string path = $"{_testDir}/dup-read.txt";
        byte[] content = Encoding.UTF8.GetBytes("dup");

        Assert.IsTrue(Save(path, content));

        bool errorRaised = false;
        ((ISync)_fileOps).OnSyncError += _ => errorRaised = true;

        IReadOnlyDictionary<string, byte[]?> results = _fileOps.ReadFile(new[] { path, path });

        Assert.HasCount(1, results);
        CollectionAssert.AreEqual(content, results[path]);
        Assert.IsFalse(errorRaised);
    }

    /// <summary>
    /// 38. Updating several files replaces the content of each.
    /// </summary>
    [TestMethod]
    public void UpdateFile_MultipleFiles_UpdatesAll()
    {
        string path1 = $"{_testDir}/u1.txt";
        string path2 = $"{_testDir}/u2.txt";

        Assert.IsTrue(Save(path1, Encoding.UTF8.GetBytes("old1")));
        Assert.IsTrue(Save(path2, Encoding.UTF8.GetBytes("old2")));

        IReadOnlyDictionary<string, bool> results = _fileOps.UpdateFile(new Dictionary<string, byte[]> {
            [path1] = Encoding.UTF8.GetBytes("new1"),
            [path2] = Encoding.UTF8.GetBytes("new2")
        });

        Assert.IsTrue(results.Values.All(updated => updated));
        CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("new1"), Read(path1));
        CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("new2"), Read(path2));
    }

    /// <summary>
    /// 39. A missing file does not stop the other updates.
    /// </summary>
    [TestMethod]
    public void UpdateFile_PartialFailure_UpdatesTheRest()
    {
        string existing = $"{_testDir}/u-existing.txt";
        string missing = $"{_testDir}/u-missing.txt";

        Assert.IsTrue(Save(existing, Encoding.UTF8.GetBytes("old")));

        IReadOnlyDictionary<string, bool> results = _fileOps.UpdateFile(new Dictionary<string, byte[]> {
            [missing] = Encoding.UTF8.GetBytes("x"),
            [existing] = Encoding.UTF8.GetBytes("new")
        });

        Assert.IsFalse(results[missing]);
        Assert.IsTrue(results[existing]);
        CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("new"), Read(existing));
    }

    /// <summary>
    /// 40. A null collection throws.
    /// </summary>
    [TestMethod]
    public void UpdateFile_NullCollection_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _fileOps.UpdateFile(null!));
    }

    /// <summary>
    /// 41. Deleting several files removes each of them.
    /// </summary>
    [TestMethod]
    public void DeleteFile_MultipleFiles_DeletesAll()
    {
        string path1 = $"{_testDir}/d1.txt";
        string path2 = $"{_testDir}/d2.txt";

        Assert.IsTrue(Save(path1, Encoding.UTF8.GetBytes("1")));
        Assert.IsTrue(Save(path2, Encoding.UTF8.GetBytes("2")));

        IReadOnlyDictionary<string, bool> results = _fileOps.DeleteFile(new[] { path1, path2 });

        Assert.IsTrue(results.Values.All(deleted => deleted));
        Assert.IsFalse(_fileOps.FileExists(path1));
        Assert.IsFalse(_fileOps.FileExists(path2));
    }

    /// <summary>
    /// 42. A missing file does not stop the other deletes.
    /// </summary>
    [TestMethod]
    public void DeleteFile_PartialFailure_DeletesTheRest()
    {
        string existing = $"{_testDir}/d-existing.txt";
        string missing = $"{_testDir}/d-missing.txt";

        Assert.IsTrue(Save(existing, Encoding.UTF8.GetBytes("x")));

        IReadOnlyDictionary<string, bool> results = _fileOps.DeleteFile(new[] { missing, existing });

        Assert.IsFalse(results[missing]);
        Assert.IsTrue(results[existing]);
        Assert.IsFalse(_fileOps.FileExists(existing));
    }

    /// <summary>
    /// 43. A null collection throws.
    /// </summary>
    [TestMethod]
    public void DeleteFile_NullCollection_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _fileOps.DeleteFile(null!));
    }

    /// <summary>
    /// 44. The same path listed twice is deleted once and raises no error.
    /// </summary>
    [TestMethod]
    public void DeleteFile_DuplicatePath_IsProcessedOnce()
    {
        string path = $"{_testDir}/dup-delete.txt";

        Assert.IsTrue(Save(path, Encoding.UTF8.GetBytes("x")));

        bool errorRaised = false;
        ((ISync)_fileOps).OnSyncError += _ => errorRaised = true;

        IReadOnlyDictionary<string, bool> results = _fileOps.DeleteFile(new[] { path, path });

        Assert.HasCount(1, results);
        Assert.IsTrue(results[path]);
        Assert.IsFalse(errorRaised);
    }
}
