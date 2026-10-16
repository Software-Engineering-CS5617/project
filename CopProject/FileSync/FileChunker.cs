using System;
using System.Collections.Generic;
using System.IO;

namespace Filesync;

// the data object that holds one piece of a file
public class FileChunk
{
    public string FileId { get; set; } = string.Empty; // e.g. "evidences/photo.png"
    public int ChunkNumber { get; set; }               // e.g. 0, 1, 2...
    public int TotalChunks { get; set; }               // so we know when to stop waiting
    public byte[] Data { get; set; } = Array.Empty<byte>(); // the actual bytes
}

// a simple utility class to cut up files and stitch them back together
public class FileChunker
{
    // 4MB default chunk size to prevent memory crashes over TCP
    private const int DefaultChunkSize = 4 * 1024 * 1024; 

    // reads a large file from the hard drive and breaks it into small pieces
    public List<FileChunk> SplitFile(string fullPath, string relativePath)
    {
        var chunks = new List<FileChunk>();
        
        // we use a FileStream here instead of File.ReadAllBytes()
        // so that if a 2GB video is dropped, it doesn't crash the RAM.
        using FileStream fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        
        int totalChunks = (int)Math.Ceiling((double)fs.Length / DefaultChunkSize);
        int currentChunk = 0;
        
        byte[] buffer = new byte[DefaultChunkSize];
        int bytesRead = 0;
        
        // keep reading chunks of 4MB until the stream is empty
        while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
        {
            // the very last chunk usually won't be exactly 4MB, 
            // so we copy only the actual bytes we read into a smaller array.
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
    
    // the other computer receives the chunks over the network and stitches them back together
    public void MergeChunks(List<FileChunk> receivedChunks, string savePath)
    {
        // network packets often arrive out of order! 
        // we must sort them by ChunkNumber before we stitch them together.
        receivedChunks.Sort((a, b) => a.ChunkNumber.CompareTo(b.ChunkNumber));
        
        // open a stream to write the final stitched file to the hard drive
        using FileStream fs = new FileStream(savePath, FileMode.Create, FileAccess.Write);
        
        foreach (var chunk in receivedChunks)
        {
            fs.Write(chunk.Data, 0, chunk.Data.Length);
        }
    }
}
