using System;
using System.Collections.Generic;
using System.IO;

namespace Filesync;

/// <summary>
/// Represents a single chunk of a large file.
/// </summary>
public class FileChunk
{
    /// <summary>
    /// Relative path identifying the file.
    /// </summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// Zero-based index of this chunk.
    /// </summary>
    public int ChunkNumber { get; set; }

    /// <summary>
    /// Total count of chunks for the file.
    /// </summary>
    public int TotalChunks { get; set; }

    /// <summary>
    /// Binary data payload of this chunk.
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();
}

/// <summary>
/// Splits large files into chunks and merges chunks back into a file.
/// </summary>
public class FileChunker
{
    private const int DefaultChunkSize = 4 * 1024 * 1024;

    /// <summary>
    /// Reads a file and splits it into chunks of default size.
    /// </summary>
    public List<FileChunk> SplitFile(string fullPath, string relativePath)
    {
        return SplitFile(fullPath, relativePath, DefaultChunkSize);
    }

    /// <summary>
    /// Reads a file and splits it into chunks of a specified size.
    /// </summary>
    public List<FileChunk> SplitFile(string fullPath, string relativePath, int chunkSize)
    {
        var chunks = new List<FileChunk>();

        using FileStream fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read);

        if (fs.Length == 0)
        {
            chunks.Add(new FileChunk
            {
                FileId = relativePath,
                ChunkNumber = 0,
                TotalChunks = 1,
                Data = Array.Empty<byte>()
            });
            return chunks;
        }

        int totalChunks = (int)Math.Ceiling((double)fs.Length / chunkSize);
        int currentChunk = 0;

        byte[] buffer = new byte[chunkSize];
        int bytesRead;

        while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
        {
            byte[] actualData = new byte[bytesRead];
            Array.Copy(buffer, actualData, bytesRead);

            var chunk = new FileChunk
            {
                FileId = relativePath,
                ChunkNumber = currentChunk,
                TotalChunks = totalChunks,
                Data = actualData
            };

            chunks.Add(chunk);
            currentChunk++;
        }

        return chunks;
    }

    /// <summary>
    /// Merges chunks in chunk order and writes the full file.
    /// </summary>
    public void MergeChunks(List<FileChunk> receivedChunks, string savePath)
    {
        receivedChunks.Sort((a, b) => a.ChunkNumber.CompareTo(b.ChunkNumber));

        string? parent = Path.GetDirectoryName(savePath);
        if (parent != null)
        {
            Directory.CreateDirectory(parent);
        }

        using FileStream fs = new FileStream(savePath, FileMode.Create, FileAccess.Write);

        foreach (var chunk in receivedChunks)
        {
            fs.Write(chunk.Data, 0, chunk.Data.Length);
        }
    }
}
