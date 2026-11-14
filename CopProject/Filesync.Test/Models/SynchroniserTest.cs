// Author : Murali

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using FileSynchroniser.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: DoNotParallelize]

namespace Filesync.Test.Models;

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
        string fullPath = FullPath(_testDir);

        if (Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, recursive: true);
        }
    }

    // Helpers

    /// <summary>
    /// Resolves a relative path against the synchronised root.
    /// </summary>
    private string FullPath(string relativePath)
    {
        return Path.Combine(_sync.GetDirectory(), relativePath);
    }

    /// <summary>
    /// Saves a single file through the multi-file SaveFile.
    /// </summary>
    private IReadOnlyDictionary<string, bool> Save(string path, byte[] content)
    {
        return _sync.SaveFile(new Dictionary<string, byte[]> { [path] = content });
    }

    /// <summary>
    /// Updates a single file through the multi-file UpdateFile.
    /// </summary>
    private IReadOnlyDictionary<string, bool> Update(string path, byte[] content)
    {
        return _sync.UpdateFile(new Dictionary<string, byte[]> { [path] = content });
    }

    /// <summary>
    /// Deletes a single file through the multi-file DeleteFile.
    /// </summary>
    private IReadOnlyDictionary<string, bool> Delete(string path)
    {
        return _sync.DeleteFile(new[] { path });
    }

    /// <summary>
    /// Reads a single file through the multi-file ReadFile.
    /// </summary>
    private byte[]? Read(string path)
    {
        return _sync.ReadFile(new[] { path })[path];
    }

    /// <summary>
    /// Subscribes to OnSyncError and collects every message raised.
    /// </summary>
    private List<string> CaptureErrors()
    {
        var errors = new List<string>();
        _sync.OnSyncError += errors.Add;
        return errors;
    }

    /// <summary>
    /// Asserts that an action throws T and returns the exception.
    /// </summary>
    private static T AssertThrows<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T ex)
        {
            return ex;
        }

        Assert.Fail($"Expected {typeof(T).Name} was not thrown.");
        return null!;
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
    /// Calls the private generic ExecuteBatch method directly. Used for cases that
    /// are impractical through the public API (e.g. a 256 MB+ total payload).
    /// Exceptions thrown inside surface as TargetInvocationException.
    /// </summary>
    private Dictionary<string, T> InvokeExecuteBatch<T>(List<string> paths, long totalBytes, Func<string, T> operation)
    {
        MethodInfo? method = typeof(Synchroniser).GetMethod("ExecuteBatch", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.IsNotNull(method);

        return (Dictionary<string, T>)method
            .MakeGenericMethod(typeof(T))
            .Invoke(_sync, new object[] { paths, totalBytes, operation })!;
    }

    // Synchronise

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

        Save(path1, Encoding.UTF8.GetBytes("hello"));
        Save(path2, Encoding.UTF8.GetBytes("world"));

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
        Save(path, Encoding.UTF8.GetBytes("deep content"));

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
        Save(path, Array.Empty<byte>());

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
        _sync.SaveFile(new Dictionary<string, byte[]> {
            [$"{_testDir}/evidences/case1.txt"] = Encoding.UTF8.GetBytes("evidence 1"),
            [$"{_testDir}/logs/whiteboard.txt"] = Encoding.UTF8.GetBytes("board log"),
            [$"{_testDir}/top.txt"] = Encoding.UTF8.GetBytes("top level")
        });

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
        Save(path, Encoding.UTF8.GetBytes("version 1"));
        Update(path, Encoding.UTF8.GetBytes("version 2"));

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
        Save(path, Encoding.UTF8.GetBytes("temp content"));
        Delete(path);

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

    /// <summary>
    /// 19. GetDirectory returns the existing FileSync root directory
    /// </summary>
    [TestMethod]
    public void GetDirectory_ReturnsExistingRootDirectory()
    {
        string dir = _sync.GetDirectory();

        Assert.IsTrue(Directory.Exists(dir));
        Assert.IsTrue(dir.EndsWith(Path.Combine("FileSync", "Root"), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 20. SaveFile writes a new file and reports success
    /// </summary>
    [TestMethod]
    public void SaveFile_NewFile_WritesContentAndReturnsTrue()
    {
        string path = $"{_testDir}/new.txt";
        byte[] content = Encoding.UTF8.GetBytes("hello");
        List<string> errors = CaptureErrors();

        IReadOnlyDictionary<string, bool> result = Save(path, content);

        Assert.IsTrue(result[path]);
        Assert.IsEmpty(errors);
        CollectionAssert.AreEqual(content, File.ReadAllBytes(FullPath(path)));
    }

    /// <summary>
    /// 21. SaveFile creates missing parent directories
    /// </summary>
    [TestMethod]
    public void SaveFile_NestedPath_CreatesParentDirectories()
    {
        string path = $"{_testDir}/a/b/c/deep.txt";

        IReadOnlyDictionary<string, bool> result = Save(path, Encoding.UTF8.GetBytes("deep"));

        Assert.IsTrue(result[path]);
        Assert.IsTrue(File.Exists(FullPath(path)));
    }

    /// <summary>
    /// 22. SaveFile never overwrites an existing file
    /// </summary>
    [TestMethod]
    public void SaveFile_ExistingFile_ReturnsFalseAndKeepsOriginal()
    {
        string path = $"{_testDir}/existing.txt";
        byte[] original = Encoding.UTF8.GetBytes("original");
        Save(path, original);

        List<string> errors = CaptureErrors();

        IReadOnlyDictionary<string, bool> result = Save(path, Encoding.UTF8.GetBytes("replacement"));

        Assert.IsFalse(result[path]);
        Assert.HasCount(1, errors);
        CollectionAssert.AreEqual(original, File.ReadAllBytes(FullPath(path)));
    }

    /// <summary>
    /// 23. SaveFile maps a null content entry to false and reports an error
    /// </summary>
    [TestMethod]
    public void SaveFile_NullContent_ReturnsFalse()
    {
        string path = $"{_testDir}/null-content.txt";
        List<string> errors = CaptureErrors();

        IReadOnlyDictionary<string, bool> result = _sync.SaveFile(new Dictionary<string, byte[]> { [path] = null! });

        Assert.IsFalse(result[path]);
        Assert.HasCount(1, errors);
        Assert.Contains("content", errors[0]);
        Assert.IsFalse(File.Exists(FullPath(path)));
    }

    /// <summary>
    /// 24. SaveFile throws when the dictionary itself is null
    /// </summary>
    [TestMethod]
    public void SaveFile_NullDictionary_Throws()
    {
        AssertThrows<ArgumentNullException>(() => _sync.SaveFile(null!));
    }

    /// <summary>
    /// 25. SaveFile rejects a second path that resolves to the same file in one batch
    /// </summary>
    [TestMethod]
    public void SaveFile_DuplicateResolvedPath_FailsSecondEntry()
    {
        string first = $"{_testDir}/dup.txt";
        string second = $"{_testDir}/sub/../dup.txt";
        List<string> errors = CaptureErrors();

        IReadOnlyDictionary<string, bool> result = _sync.SaveFile(new Dictionary<string, byte[]> {
            [first] = Encoding.UTF8.GetBytes("first"),
            [second] = Encoding.UTF8.GetBytes("second")
        });

        Assert.IsTrue(result[first]);
        Assert.IsFalse(result[second]);
        Assert.HasCount(1, errors);
        Assert.Contains("Duplicate path in batch", errors[0]);
        CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("first"), File.ReadAllBytes(FullPath(first)));
    }


    /// <summary>
    /// 26. UpdateFile overwrites an existing file
    /// </summary>
    [TestMethod]
    public void UpdateFile_ExistingFile_OverwritesContent()
    {
        string path = $"{_testDir}/update-me.txt";
        Save(path, Encoding.UTF8.GetBytes("version 1"));
        byte[] updated = Encoding.UTF8.GetBytes("version 2");

        IReadOnlyDictionary<string, bool> result = Update(path, updated);

        Assert.IsTrue(result[path]);
        CollectionAssert.AreEqual(updated, File.ReadAllBytes(FullPath(path)));
    }

    /// <summary>
    /// 27. UpdateFile fails for a file that does not exist
    /// </summary>
    [TestMethod]
    public void UpdateFile_MissingFile_ReturnsFalse()
    {
        string path = $"{_testDir}/missing.txt";
        List<string> errors = CaptureErrors();

        IReadOnlyDictionary<string, bool> result = Update(path, Encoding.UTF8.GetBytes("data"));

        Assert.IsFalse(result[path]);
        Assert.HasCount(1, errors);
        Assert.IsFalse(File.Exists(FullPath(path)));
    }

    /// <summary>
    /// 28. UpdateFile maps a null content entry to false and leaves the file untouched
    /// </summary>
    [TestMethod]
    public void UpdateFile_NullContent_ReturnsFalseAndKeepsOriginal()
    {
        string path = $"{_testDir}/keep.txt";
        byte[] original = Encoding.UTF8.GetBytes("keep me");
        Save(path, original);

        List<string> errors = CaptureErrors();

        IReadOnlyDictionary<string, bool> result = _sync.UpdateFile(new Dictionary<string, byte[]> { [path] = null! });

        Assert.IsFalse(result[path]);
        Assert.HasCount(1, errors);
        CollectionAssert.AreEqual(original, File.ReadAllBytes(FullPath(path)));
    }

    /// <summary>
    /// 29. UpdateFile throws when the dictionary itself is null
    /// </summary>
    [TestMethod]
    public void UpdateFile_NullDictionary_Throws()
    {
        AssertThrows<ArgumentNullException>(() => _sync.UpdateFile(null!));
    }

    /// <summary>
    /// 30. ReadFile returns the content of every requested file
    /// </summary>
    [TestMethod]
    public void ReadFile_ExistingFiles_ReturnsContent()
    {
        string path1 = $"{_testDir}/r1.txt";
        string path2 = $"{_testDir}/r2.txt";
        byte[] content1 = Encoding.UTF8.GetBytes("one");
        byte[] content2 = Encoding.UTF8.GetBytes("two");
        Save(path1, content1);
        Save(path2, content2);
        List<string> errors = CaptureErrors();

        IReadOnlyDictionary<string, byte[]?> result = _sync.ReadFile(new[] { path1, path2 });

        Assert.HasCount(2, result);
        CollectionAssert.AreEqual(content1, result[path1]);
        CollectionAssert.AreEqual(content2, result[path2]);
        Assert.IsEmpty(errors);
    }

    /// <summary>
    /// 31. ReadFile maps a missing file to null and reports an error
    /// </summary>
    [TestMethod]
    public void ReadFile_MissingFile_ReturnsNullAndReportsError()
    {
        string path = $"{_testDir}/not-there.txt";
        List<string> errors = CaptureErrors();

        byte[]? data = Read(path);

        Assert.IsNull(data);
        Assert.HasCount(1, errors);
    }

    /// <summary>
    /// 32. ReadFile rejects a file larger than the batch size cap before loading it.
    /// The file is created with SetLength (sparse on most file systems), so no
    /// 256 MB buffer is ever allocated.
    /// </summary>
    [TestMethod]
    public void ReadFile_FileExceedsBatchLimit_ReturnsNullAndReportsError()
    {
        string path = $"{_testDir}/huge.bin";
        string fullPath = FullPath(path);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        using (var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            stream.SetLength(Synchroniser.MaxBatchTotalBytes + 1);
        }

        List<string> errors = CaptureErrors();

        byte[]? data = Read(path);

        Assert.IsNull(data);
        Assert.HasCount(1, errors);
        Assert.Contains("Batch size limit", errors[0]);
    }

    /// <summary>
    /// 33. ReadFile with no paths returns an empty result and no error
    /// </summary>
    [TestMethod]
    public void ReadFile_EmptyInput_ReturnsEmptyWithoutError()
    {
        List<string> errors = CaptureErrors();

        IReadOnlyDictionary<string, byte[]?> result = _sync.ReadFile(Array.Empty<string>());

        Assert.HasCount(0, result);
        Assert.HasCount(0, errors);
    }

    /// <summary>
    /// 34. ReadFile throws when the path collection is null
    /// </summary>
    [TestMethod]
    public void ReadFile_NullInput_Throws()
    {
        AssertThrows<ArgumentNullException>(() => _sync.ReadFile(null!));
    }


    /// <summary>
    /// 35. DeleteFile removes an existing file
    /// </summary>
    [TestMethod]
    public void DeleteFile_ExistingFile_RemovesFile()
    {
        string path = $"{_testDir}/delete-me.txt";
        Save(path, Encoding.UTF8.GetBytes("bye"));

        IReadOnlyDictionary<string, bool> result = Delete(path);

        Assert.IsTrue(result[path]);
        Assert.IsFalse(File.Exists(FullPath(path)));
    }

    /// <summary>
    /// 36. DeleteFile fails for a file that does not exist
    /// </summary>
    [TestMethod]
    public void DeleteFile_MissingFile_ReturnsFalse()
    {
        string path = $"{_testDir}/never-existed.txt";
        List<string> errors = CaptureErrors();

        IReadOnlyDictionary<string, bool> result = Delete(path);

        Assert.IsFalse(result[path]);
        Assert.HasCount(1, errors);
    }

    /// <summary>
    /// 37. DeleteFile throws when the path collection is null
    /// </summary>
    [TestMethod]
    public void DeleteFile_NullInput_Throws()
    {
        AssertThrows<ArgumentNullException>(() => _sync.DeleteFile(null!));
    }


    /// <summary>
    /// 38. FileExists returns true for an existing file
    /// </summary>
    [TestMethod]
    public void FileExists_ExistingFile_ReturnsTrue()
    {
        string path = $"{_testDir}/exists.txt";
        Save(path, Encoding.UTF8.GetBytes("here"));
        List<string> errors = CaptureErrors();

        Assert.IsTrue(_sync.FileExists(path));
        Assert.HasCount(0, errors);
    }

    /// <summary>
    /// 39. FileExists returns false, without an error, for a valid path with no file
    /// </summary>
    [TestMethod]
    public void FileExists_MissingFile_ReturnsFalseWithoutError()
    {
        List<string> errors = CaptureErrors();

        Assert.IsFalse(_sync.FileExists($"{_testDir}/absent.txt"));
        Assert.HasCount(0, errors);
    }

    /// <summary>
    /// 40. FileExists returns false and reports an error for empty, whitespace
    /// and out-of-root paths (including a sibling folder sharing the root's prefix)
    /// </summary>
    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("../outside.txt")]
    [DataRow("../Root-sibling/file.txt")]
    public void FileExists_InvalidPath_ReturnsFalseAndReportsError(string path)
    {
        List<string> errors = CaptureErrors();

        Assert.IsFalse(_sync.FileExists(path));
        Assert.HasCount(1, errors);
    }

    /// <summary>
    /// 41. FileExists rejects a null path
    /// </summary>
    [TestMethod]
    public void FileExists_NullPath_ReturnsFalseAndReportsError()
    {
        List<string> errors = CaptureErrors();

        Assert.IsFalse(_sync.FileExists(null!));
        Assert.HasCount(1, errors);
    }

    /// <summary>
    /// 42. FileExists rejects an absolute path
    /// </summary>
    [TestMethod]
    public void FileExists_AbsolutePath_ReturnsFalseAndReportsError()
    {
        string absolute = FullPath("absolute.txt");
        List<string> errors = CaptureErrors();

        Assert.IsFalse(_sync.FileExists(absolute));
        Assert.HasCount(1, errors);
        Assert.Contains("Absolute paths are not allowed", errors[0]);
    }


    /// <summary>
    /// 43. A null path in a batch is skipped and reported
    /// </summary>
    [TestMethod]
    public void ExecuteBatch_NullPath_IsSkippedAndReported()
    {
        List<string> errors = CaptureErrors();

        IReadOnlyDictionary<string, bool> result = _sync.DeleteFile(new string[] { null! });

        Assert.HasCount(0, result);
        Assert.HasCount(1, errors);
        Assert.Contains("A null path was skipped.", errors[0]);
    }

    /// <summary>
    /// 44. An exact duplicate path in a batch is processed only once
    /// </summary>
    [TestMethod]
    public void ExecuteBatch_ExactDuplicatePath_ProcessedOnce()
    {
        string path = $"{_testDir}/twice.txt";
        Save(path, Encoding.UTF8.GetBytes("x"));
        List<string> errors = CaptureErrors();

        IReadOnlyDictionary<string, bool> result = _sync.DeleteFile(new[] { path, path });

        Assert.HasCount(1, result);
        Assert.IsTrue(result[path]);
        Assert.HasCount(0, errors);
    }

    /// <summary>
    /// 45. A batch with more than MaxBatchFileCount paths fails every entry
    /// (a null entry is skipped when marking failures)
    /// </summary>
    [TestMethod]
    public void ExecuteBatch_TooManyFiles_FailsAll()
    {
        List<string> paths = Enumerable
            .Range(0, Synchroniser.MaxBatchFileCount + 1)
            .Select(i => $"{_testDir}/many{i}.txt")
            .ToList();
        paths.Add(null!);

        List<string> errors = CaptureErrors();

        IReadOnlyDictionary<string, bool> result = _sync.DeleteFile(paths);

        Assert.HasCount(Synchroniser.MaxBatchFileCount + 1, result);
        Assert.IsTrue(result.Values.All(r => !r));
        Assert.HasCount(1, errors);
        Assert.Contains($"the limit is {Synchroniser.MaxBatchFileCount}", errors[0]);
    }

    /// <summary>
    /// 46. A batch of exactly MaxBatchFileCount paths is accepted
    /// </summary>
    [TestMethod]
    public void ExecuteBatch_ExactlyMaxFiles_Processes()
    {
        List<string> paths = Enumerable
            .Range(0, Synchroniser.MaxBatchFileCount)
            .Select(i => $"{_testDir}/limit{i}.txt")
            .ToList();
        List<string> errors = CaptureErrors();

        IReadOnlyDictionary<string, bool> result = InvokeExecuteBatch<bool>(paths, 0, _ => true);

        Assert.HasCount(Synchroniser.MaxBatchFileCount, result);
        Assert.IsTrue(result.Values.All(r => r));
        Assert.HasCount(0, errors);
    }

    /// <summary>
    /// 47. A batch whose total size exceeds MaxBatchTotalBytes fails every entry
    /// and never runs the operation
    /// </summary>
    [TestMethod]
    public void ExecuteBatch_TotalBytesOverLimit_FailsAll()
    {
        bool invoked = false;
        List<string> errors = CaptureErrors();

        IReadOnlyDictionary<string, bool> result = InvokeExecuteBatch<bool>(
            new List<string> { "too-big.txt" },
            Synchroniser.MaxBatchTotalBytes + 1,
            _ => {
                invoked = true;
                return true;
            });

        Assert.IsFalse(invoked);
        Assert.HasCount(1, result);
        Assert.IsFalse(result["too-big.txt"]);
        Assert.HasCount(1, errors);
        Assert.Contains("exceeds the limit", errors[0]);
    }

    /// <summary>
    /// 48. A batch whose total size is exactly MaxBatchTotalBytes is accepted
    /// </summary>
    [TestMethod]
    public void ExecuteBatch_TotalBytesAtLimit_Processes()
    {
        bool invoked = false;
        List<string> errors = CaptureErrors();

        Dictionary<string, bool> result = InvokeExecuteBatch<bool>(
            new List<string> { "at-limit.txt" },
            Synchroniser.MaxBatchTotalBytes,
            _ => {
                invoked = true;
                return true;
            });

        Assert.IsTrue(invoked);
        Assert.IsTrue(result["at-limit.txt"]);
        Assert.HasCount(0, errors);
    }

    /// <summary>
    /// 49. An exception that is not an expected file error is not swallowed
    /// </summary>
    [TestMethod]
    public void ExecuteBatch_UnexpectedException_Propagates()
    {
        TargetInvocationException thrown = AssertThrows<TargetInvocationException>(() =>
            InvokeExecuteBatch<bool>(
                new List<string> { "unexpected.txt" },
                0,
                _ => throw new InvalidOperationException("boom")));

        Assert.IsTrue(thrown.InnerException is InvalidOperationException);
    }
}
