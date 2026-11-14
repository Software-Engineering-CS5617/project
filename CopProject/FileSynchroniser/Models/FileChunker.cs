// Author : Eswar

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;

namespace FileSynchroniser.Models;

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

    /// <summary>
    /// Optional last modified time (UTC) of the original file.
    /// </summary>
    public DateTime? LastModifiedUtc { get; set; }
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
        Trace.WriteLine($"[FileChunker] SplitFile started. File: '{fullPath}', RelativePath: '{relativePath}', ChunkSize: {chunkSize}");
        var chunks = new List<FileChunk>();
        DateTime? lastModifiedUtc = null;

        try
        {
            if (File.Exists(fullPath))
            {
                lastModifiedUtc = File.GetLastWriteTimeUtc(fullPath);
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[FileChunker] Failed to read last modified time for '{fullPath}': {ex}");
        }

        using FileStream fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        Trace.WriteLine($"[FileChunker] File opened. Path: '{fullPath}', Size: {fs.Length} bytes");

        if (fs.Length == 0)
        {
            chunks.Add(new FileChunk {
                FileId = relativePath,
                ChunkNumber = 0,
                TotalChunks = 1,
                Data = Array.Empty<byte>(),
                LastModifiedUtc = lastModifiedUtc
            });
            Trace.WriteLine($"[FileChunker] File is empty. Creating single empty chunk for '{relativePath}'.");
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

            var chunk = new FileChunk {
                FileId = relativePath,
                ChunkNumber = currentChunk,
                TotalChunks = totalChunks,
                Data = actualData,
                LastModifiedUtc = lastModifiedUtc
            };

            Trace.WriteLine($"[FileChunker] Created chunk {currentChunk}/{totalChunks} for '{relativePath}' ({bytesRead} bytes).");
            chunks.Add(chunk);
            currentChunk++;
        }

        Trace.WriteLine($"[FileChunker] SplitFile completed. '{relativePath}' produced {chunks.Count} chunks.");
        return chunks;
    }

    /// <summary>
    /// Merges chunks in chunk order and writes the full file.
    /// The caller's list is not modified.
    /// </summary>
    public void MergeChunks(List<FileChunk> receivedChunks, string savePath, DateTime? lastModifiedUtc = null)
    {
        ArgumentNullException.ThrowIfNull(receivedChunks);
        Trace.WriteLine($"[FileChunker] MergeChunks started. Chunks: {receivedChunks.Count}, SavePath: '{savePath}'.");

        // Sort a copy so the caller's list is left untouched.
        List<FileChunk> ordered = receivedChunks.OrderBy(c => c.ChunkNumber).ToList();

        string? parent = Path.GetDirectoryName(savePath);
        if (parent != null)
        {
            Directory.CreateDirectory(parent);
        }

        Trace.WriteLine($"[FileChunker] Writing merged file to '{savePath}'.");
        using (FileStream fs = new FileStream(savePath, FileMode.Create, FileAccess.Write))
        {
            foreach (FileChunk chunk in ordered)
            {
                Trace.WriteLine($"[FileChunker] Merging chunk {chunk.ChunkNumber}/{chunk.TotalChunks} ({chunk.Data.Length} bytes).");
                fs.Write(chunk.Data, 0, chunk.Data.Length);
            }
        }
        Trace.WriteLine($"[FileChunker] File merge completed. Saved to '{savePath}'.");

        DateTime? targetTime = lastModifiedUtc ?? ordered.FirstOrDefault(c => c.LastModifiedUtc.HasValue)?.LastModifiedUtc;
        if (targetTime.HasValue)
        {
            try
            {
                File.SetLastWriteTimeUtc(savePath, targetTime.Value);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[FileChunker] Failed to set last modified time for '{savePath}': {ex}");
            }
        }
    }
}
