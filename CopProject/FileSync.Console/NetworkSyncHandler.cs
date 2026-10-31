using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using Filesync;
using Networking;

namespace FileSync.ConsoleApp;

/// <summary>
/// handles file sync protocol over the networking communicator
/// </summary>
public class NetworkSyncHandler : IMessageListener
{
    // SubscriberId for now will be replaced once joined with controller
    public const int ModuleId = 5;
    private readonly string _rootDir;
    private readonly ICommunicator _communicator;
    private readonly FileChunker _chunker;
    private readonly ConcurrentDictionary<string, List<FileChunk>> _incomingChunks = new();

    public IPEndPoint? DefaultPeerEndpoint { get; set; }

    public event Action? OnSyncCompleted;
    public event Action<string>? OnLog;

    /// <summary>
    /// register this handler as the subscriber for the filesync module
    /// </summary>
    public NetworkSyncHandler(string rootDir, ICommunicator communicator)
    {
        _rootDir = Path.GetFullPath(rootDir);
        _communicator = communicator;
        _chunker = new FileChunker();

        if (!Directory.Exists(_rootDir))
        {
            Directory.CreateDirectory(_rootDir);
        }

        _communicator.AddSubscriber(ModuleId, this);
    }

    /// <summary>
    /// gets the root directory used for file synchronization
    /// </summary>
    public string RootDirectory => _rootDir;

    /// <summary>
    /// triggers synchronisation by requesting the remote manifest from the peer
    /// </summary>
    public void StartSync()
    {
        OnLog?.Invoke($"[sync] starting sync from folder: {_rootDir}");

        byte[] req = Encoding.UTF8.GetBytes("GET_MANIFEST");

        if (DefaultPeerEndpoint != null)
        {
            _communicator.SendMessage(DefaultPeerEndpoint, req, ModuleId, priority: 1);
        }
        else
        {
            _communicator.BroadcastMessage(req, ModuleId, priority: 1);
        }
    }

    /// <summary>
    /// handles incoming network messages from other peers
    /// </summary>
    /// <param name="message">the received network message</param>
    public void OnMessageReceived(byte[] message)
    {
        string raw = Encoding.UTF8.GetString(message);

        if (raw == "GET_MANIFEST")
        {
            SendLocalManifest();
        }
        else if (raw.StartsWith("MANIFEST:"))
        {
            HandleManifest(raw.Substring("MANIFEST:".Length));
        }
        else if (raw.StartsWith("REQUEST:"))
        {
            HandleFileRequest(raw.Substring("REQUEST:".Length));
        }
        else if (raw.StartsWith("CHUNK:"))
        {
            HandleChunk(raw.Substring("CHUNK:".Length));
        }
    }

    /// <summary>
    /// gathers all local files and timestamps in the root directory and sends the manifest to the peer
    /// </summary>
    private void SendLocalManifest()
    {
        string[] allFiles = Directory.GetFiles(_rootDir, "*", SearchOption.AllDirectories);
        var manifest = new Dictionary<string, DateTime>();

        foreach (string file in allFiles)
        {
            string rel = Path.GetRelativePath(_rootDir, file).Replace('\\', '/');
            manifest[rel] = File.GetLastWriteTimeUtc(file);
        }

        string json = JsonSerializer.Serialize(manifest);
        byte[] payload = Encoding.UTF8.GetBytes("MANIFEST:" + json);

        OnLog?.Invoke($"[sync] sending local manifest with {manifest.Count} file(s)");

        if (DefaultPeerEndpoint != null)
        {
            _communicator.SendMessage(DefaultPeerEndpoint, payload, ModuleId, priority: 1);
        }
        else
        {
            _communicator.BroadcastMessage(payload, ModuleId, priority: 1);
        }
    }

    /// <summary>
    /// compares the received remote manifest with local files and requests missing or newer files
    /// </summary>
    /// <param name="json">the serialized remote file manifest</param>
    private void HandleManifest(string json)
    {
        Dictionary<string, DateTime>? remoteManifest = JsonSerializer.Deserialize<Dictionary<string, DateTime>>(json);
        if (remoteManifest == null)
        {
            return;
        }

        OnLog?.Invoke($"[sync] received remote manifest with {remoteManifest.Count} file(s)");

        int requestedCount = 0;

        foreach ((string? relPath, DateTime remoteTime) in remoteManifest)
        {
            string localFullPath = Path.Combine(_rootDir, relPath);

            // check if we are missing the file or our local copy is older
            bool needFile = false;
            if (!File.Exists(localFullPath))
            {
                needFile = true;
            }
            else if (remoteTime > File.GetLastWriteTimeUtc(localFullPath))
            {
                needFile = true;
            }

            if (needFile)
            {
                OnLog?.Invoke($"[sync] requesting newer file from peer: {relPath}");
                byte[] req = Encoding.UTF8.GetBytes("REQUEST:" + relPath);

                if (DefaultPeerEndpoint != null)
                {
                    _communicator.SendMessage(DefaultPeerEndpoint, req, ModuleId, priority: 1);
                }
                else
                {
                    _communicator.BroadcastMessage(req, ModuleId, priority: 1);
                }
                requestedCount++;
            }
        }

        if (requestedCount == 0)
        {
            OnLog?.Invoke("[sync] all local files are already up-to-date");
            OnSyncCompleted?.Invoke();
        }
    }

    /// <summary>
    /// handles a file request by splitting the requested local file into chunks and sending them to the peer
    /// </summary>
    /// <param name="relPath">the relative path of the requested file</param>
    private void HandleFileRequest(string relPath)
    {
        string fullPath = Path.Combine(_rootDir, relPath);

        // security check to block path traversal outside root
        if (!fullPath.StartsWith(_rootDir) || !File.Exists(fullPath))
        {
            OnLog?.Invoke($"[sync] rejected invalid file request: {relPath}");
            return;
        }

        OnLog?.Invoke($"[sync] splitting and sending chunks for: {relPath}");

        // split file into 64kb chunks for network transfer
        List<FileChunk> chunks = _chunker.SplitFile(fullPath, relPath, chunkSize: 64 * 1024);

        foreach (FileChunk chunk in chunks)
        {
            var chunkDto = new ChunkDto {
                FileId = chunk.FileId,
                ChunkNumber = chunk.ChunkNumber,
                TotalChunks = chunk.TotalChunks,
                Base64Data = Convert.ToBase64String(chunk.Data)
            };

            string json = JsonSerializer.Serialize(chunkDto);
            byte[] packet = Encoding.UTF8.GetBytes("CHUNK:" + json);

            if (DefaultPeerEndpoint != null)
            {
                _communicator.SendMessage(DefaultPeerEndpoint, packet, ModuleId, priority: 1);
            }
            else
            {
                _communicator.BroadcastMessage(packet, ModuleId, priority: 1);
            }
        }
    }

    /// <summary>
    /// receives file chunks, stores them, and merges them when all chunks have arrived
    /// </summary>
    /// <param name="json">the serialized file chunk data</param>
    private void HandleChunk(string json)
    {
        ChunkDto? dto = JsonSerializer.Deserialize<ChunkDto>(json);
        if (dto == null)
        {
            return;
        }

        List<FileChunk> list = _incomingChunks.GetOrAdd(dto.FileId, _ => new List<FileChunk>());

        lock (list)
        {
            list.Add(new FileChunk {
                FileId = dto.FileId,
                ChunkNumber = dto.ChunkNumber,
                TotalChunks = dto.TotalChunks,
                Data = Convert.FromBase64String(dto.Base64Data)
            });

            OnLog?.Invoke($"[sync] received chunk {dto.ChunkNumber + 1}/{dto.TotalChunks} for {dto.FileId}");

            // assemble when all chunks arrive
            if (list.Count == dto.TotalChunks)
            {
                string targetPath = Path.Combine(_rootDir, dto.FileId);
                _chunker.MergeChunks(list, targetPath);
                _incomingChunks.TryRemove(dto.FileId, out _);

                OnLog?.Invoke($"[sync] successfully assembled file: {dto.FileId}");
                OnSyncCompleted?.Invoke();
            }
        }
    }

    /// <summary>
    /// represents the data structure used to serialize file chunks over the network
    /// </summary>
    private class ChunkDto
    {
        public string FileId { get; set; } = string.Empty;
        public int ChunkNumber { get; set; }
        public int TotalChunks { get; set; }
        public string Base64Data { get; set; } = string.Empty;
    }
}
