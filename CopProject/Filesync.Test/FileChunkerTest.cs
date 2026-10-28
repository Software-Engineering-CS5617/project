using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Filesync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Filesync.Test;

[TestClass]
public class FileChunkerTest
{
    private string _tempDir = null!;
    private FileChunker _chunker = null!;

    /// <summary>
    /// Initializing FileChunker and temp directory for testing
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        _chunker = new FileChunker();
        _tempDir = Path.Combine(Path.GetTempPath(), $"ChunkerTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    /// <summary>
    /// Clean up temp directory once test is completed
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    /// <summary>
    /// 1. Splitting an empty file yields one empty chunk
    /// </summary>
    [TestMethod]
    public void SplitFile_EmptyFile_ReturnsSingleEmptyChunk()
    {
        string filePath = Path.Combine(_tempDir, "empty.txt");
        File.WriteAllBytes(filePath, Array.Empty<byte>());

        List<FileChunk> chunks = _chunker.SplitFile(filePath, "empty.txt");

        Assert.HasCount(1, chunks);
        Assert.AreEqual(0, chunks[0].ChunkNumber);
        Assert.AreEqual(1, chunks[0].TotalChunks);
        Assert.AreEqual("empty.txt", chunks[0].FileId);
        Assert.IsEmpty(chunks[0].Data);
    }

    /// <summary>
    /// 2. Splitting a small file smaller than chunk size returns single chunk
    /// </summary>
    [TestMethod]
    public void SplitFile_SmallerThanChunkSize_ReturnsSingleChunk()
    {
        string filePath = Path.Combine(_tempDir, "small.txt");
        byte[] expected = Encoding.UTF8.GetBytes("Small file content for chunking");
        File.WriteAllBytes(filePath, expected);

        List<FileChunk> chunks = _chunker.SplitFile(filePath, "small.txt", 1024);

        Assert.HasCount(1, chunks);
        Assert.AreEqual(0, chunks[0].ChunkNumber);
        Assert.AreEqual(1, chunks[0].TotalChunks);
        CollectionAssert.AreEqual(expected, chunks[0].Data);
    }

    /// <summary>
    /// 3. Splitting a file larger than chunk size returns correct number of chunks
    /// </summary>
    [TestMethod]
    public void SplitFile_LargerThanChunkSize_ReturnsMultipleChunks()
    {
        string filePath = Path.Combine(_tempDir, "multi.txt");
        byte[] content = new byte[2500];
        new Random(42).NextBytes(content);
        File.WriteAllBytes(filePath, content);

        List<FileChunk> chunks = _chunker.SplitFile(filePath, "multi.txt", 1000);

        Assert.HasCount(3, chunks);
        Assert.HasCount(1000, chunks[0].Data);
        Assert.HasCount(1000, chunks[1].Data);
        Assert.HasCount(500, chunks[2].Data);

        for (int i = 0; i < chunks.Count; i++)
        {
            Assert.AreEqual(i, chunks[i].ChunkNumber);
            Assert.AreEqual(3, chunks[i].TotalChunks);
            Assert.AreEqual("multi.txt", chunks[i].FileId);
        }
    }

    /// <summary>
    /// 4. MergeChunks reconstructs original file content exactly
    /// </summary>
    [TestMethod]
    public void MergeChunks_ReconstructsExactOriginalContent()
    {
        string originalPath = Path.Combine(_tempDir, "original.bin");
        string mergedPath = Path.Combine(_tempDir, "merged.bin");

        byte[] expected = new byte[5000];
        new Random(99).NextBytes(expected);
        File.WriteAllBytes(originalPath, expected);

        List<FileChunk> chunks = _chunker.SplitFile(originalPath, "original.bin", 1024);
        _chunker.MergeChunks(chunks, mergedPath);

        byte[] actual = File.ReadAllBytes(mergedPath);
        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// 5. MergeChunks handles chunks arriving out of order
    /// </summary>
    [TestMethod]
    public void MergeChunks_OutOfOrderChunks_ReconstructsCorrectly()
    {
        string originalPath = Path.Combine(_tempDir, "order.bin");
        string mergedPath = Path.Combine(_tempDir, "order_merged.bin");

        byte[] expected = new byte[3500];
        new Random(123).NextBytes(expected);
        File.WriteAllBytes(originalPath, expected);

        List<FileChunk> chunks = _chunker.SplitFile(originalPath, "order.bin", 1000);

        // Reverse chunk order to simulate out-of-order network arrival
        chunks.Reverse();

        _chunker.MergeChunks(chunks, mergedPath);

        byte[] actual = File.ReadAllBytes(mergedPath);
        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// 6. MergeChunks creates parent directory if it does not exist
    /// </summary>
    [TestMethod]
    public void MergeChunks_CreatesNestedDirectoryIfMissing()
    {
        string originalPath = Path.Combine(_tempDir, "source.txt");
        string mergedPath = Path.Combine(_tempDir, "deep", "nested", "output.txt");

        byte[] expected = Encoding.UTF8.GetBytes("Test directory auto-creation");
        File.WriteAllBytes(originalPath, expected);

        List<FileChunk> chunks = _chunker.SplitFile(originalPath, "source.txt");
        _chunker.MergeChunks(chunks, mergedPath);

        Assert.IsTrue(File.Exists(mergedPath));
        CollectionAssert.AreEqual(expected, File.ReadAllBytes(mergedPath));
    }
}
