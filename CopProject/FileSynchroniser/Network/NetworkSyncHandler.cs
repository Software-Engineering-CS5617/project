// Author : Eswar

using System.Collections.Concurrent;
using System.Net;
using System.Diagnostics;
using System.Text.Json;
using FileSynchroniser.Models;
using Networking;

namespace FileSynchroniser.Network;

/// <summary>
/// File sync protocol over the meeting's Networking communicator.
/// Networking is a star: clients can only talk to the host, so every message carries
/// an envelope (type, sender, target) and the host forwards messages not meant for itself.
/// The side that clicks Sync pulls files from the chosen peer.
/// </summary>
public sealed class NetworkSyncHandler : IMessageListener, IDisposable
{
    // must be unique among all modules; replace with the id the controller assigns
    public const int DefaultModuleId = 5;

    private const int ChunkSize = 64 * 1024;
    private const int Priority = 1;

    private const string GetManifestType = "GET_MANIFEST";
    private const string ManifestType = "MANIFEST";
    private const string RequestType = "REQUEST";
    private const string ChunkType = "CHUNK";
    private const string AckType = "SYNC_ACK";

    private readonly string _rootDir;
    private readonly ICommunicator _communicator;
    private readonly IPEndPoint _localEndpoint;
    private readonly bool _isHost;
    private readonly int _moduleId;
    private readonly IPEndPoint? _hostEndpoint;
    private readonly FileChunker _chunker = new();
    private readonly ConcurrentDictionary<string, List<FileChunk>> _incomingChunks = new();
    private bool _disposed;

    /// <summary>raised (on a network thread) when a file was assembled or nothing needed syncing</summary>
    public event Action? OnSyncCompleted;

    /// <summary>raised (on a network thread) with progress text</summary>
    public event Action<string>? OnLog;

    /// <param name="rootDir">the synchronised folder</param>
    /// <param name="communicator">the meeting's communicator, not a new one</param>
    /// <param name="isHost">true when this participant is the meeting host</param>
    /// <param name="localEndpoint">this user's Ip:Port as the meeting's user list shows it</param>
    /// <param name="hostEndpoint">the host's endpoint; required for a client, ignored for the host</param>
    /// <param name="moduleId">module id to subscribe on</param>
    public NetworkSyncHandler(
        string rootDir,
        ICommunicator communicator,
        bool isHost,
        IPEndPoint localEndpoint,
        IPEndPoint? hostEndpoint,
        int moduleId = DefaultModuleId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDir);
        ArgumentNullException.ThrowIfNull(communicator);
        ArgumentNullException.ThrowIfNull(localEndpoint);

        if (!isHost && hostEndpoint == null)
        {
            throw new ArgumentException("A client needs the host endpoint.", nameof(hostEndpoint));
        }

        _rootDir = Path.GetFullPath(rootDir);
        _communicator = communicator;
        _isHost = isHost;
        _moduleId = moduleId;
        _localEndpoint = localEndpoint;   // taken from CurrentUser, not from the communicator
        _hostEndpoint = hostEndpoint;

        Directory.CreateDirectory(_rootDir);
        _communicator.AddSubscriber(_moduleId, this);
        Trace.WriteLine($"[FileSync] NetworkSyncHandler initialized. Local={_localEndpoint}, Host={_isHost}, HostEndpoint={_hostEndpoint}");
    }

    /// <summary>the endpoint other participants use to reach this one</summary>
    public IPEndPoint LocalEndpoint => _localEndpoint;

    public string RootDirectory => _rootDir;

    /// <summary>
    /// pulls files from the given peer: asks for its manifest, then requests what is missing or newer
    /// </summary>
    public void StartSync(IPEndPoint peer)
    {
        ArgumentNullException.ThrowIfNull(peer);
        Trace.WriteLine($"[FileSync] StartSync: Local={_localEndpoint}, Peer={peer}, IsHost={_isHost}");

        if (SameEndpoint(peer, _localEndpoint))
        {
            Trace.WriteLine($"[FileSync] StartSync rejected: peer {peer} and the local endpoint are same.");
            OnLog?.Invoke("[sync] cannot sync with yourself");
            return;
        }

        OnLog?.Invoke($"[sync] requesting manifest from {peer}");
        Send(peer, GetManifestType, string.Empty);
    }

    public void OnMessageReceived(byte[] message)
    {
        Trace.WriteLine($"[FileSync] Message received. Bytes={message.Length}");
        Envelope? env;
        try
        {
            env = JsonSerializer.Deserialize<Envelope>(message);
            Trace.WriteLine($"[FileSync] Envelope received: Type={env?.Type}, From={env?.From}, To={env?.To}");
        }
        catch (JsonException ex)
        {
            Trace.WriteLine($"[FileSync] Failed to deserialize incoming message: {ex}");
            return;
        }

        if (env == null
            || !IPEndPoint.TryParse(env.To, out IPEndPoint? to)
            || !IPEndPoint.TryParse(env.From, out IPEndPoint? from))
        {
            Trace.WriteLine("[FileSync] Dropping message: invalid envelope or endpoint.");
            return;
        }

        if (!SameEndpoint(to, _localEndpoint))
        {
            Trace.WriteLine($"[FileSync] Routing check: MessageTo={to}, LocalEndpoint={_localEndpoint}, IsHost={_isHost}");
            // not for us: the host relays it, a client drops it
            if (_isHost)
            {
                Trace.WriteLine($"[FileSync] Host forwarding message: {from} -> {to}");

                _communicator.SendMessage(to, message, _moduleId, Priority);
            }
            else
            {
                Trace.WriteLine($"[FileSync] Client dropped message not addressed to local endpoint. To={to}");
            }

            return;
        }

        try
        {
            Trace.WriteLine($"[FileSync] Processing message: Type={env.Type}, From={from}, To={to}");
            switch (env.Type)
            {
                case GetManifestType:
                    SendLocalManifest(from);
                    break;
                case ManifestType:
                    HandleManifest(from, env.Payload);
                    break;
                case RequestType:
                    HandleFileRequest(from, env.Payload);
                    break;
                case ChunkType:
                    HandleChunk(from, env.Payload);
                    break;
                case AckType:
                    OnLog?.Invoke($"[sync] peer confirmed receipt of: {env.Payload}");
                    break;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Trace.WriteLine($"[FileSync] File operation error while processing {env.Type}: {ex}");
            OnLog?.Invoke($"[sync] file error: {ex.Message}");
        }
    }

    private void SendLocalManifest(IPEndPoint to)
    {
        var manifest = new Dictionary<string, DateTime>();
        Trace.WriteLine($"[FileSync] Building manifest for {to}. Root={_rootDir}");

        foreach (string file in Directory.GetFiles(_rootDir, "*", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(_rootDir, file).Replace('\\', '/');
            manifest[rel] = File.GetLastWriteTimeUtc(file);
        }

        Trace.WriteLine($"[FileSync] Manifest built. Files={manifest.Count}. Sending to {to}");

        OnLog?.Invoke($"[sync] sending manifest with {manifest.Count} file(s) to {to}");
        Send(to, ManifestType, JsonSerializer.Serialize(manifest));
    }

    private void HandleManifest(IPEndPoint from, string json)
    {
        Dictionary<string, DateTime>? remote;
        try
        {
            remote = JsonSerializer.Deserialize<Dictionary<string, DateTime>>(json);
        }
        catch (JsonException ex)
        {
            Trace.WriteLine($"[FileSync] Failed to deserialize manifest from {from}: {ex}");
            return;
        }

        if (remote == null)
        {
            Trace.WriteLine($"[FileSync] Received null manifest from {from}.");
            return;
        }

        OnLog?.Invoke($"[sync] received manifest with {remote.Count} file(s)");

        int requested = 0;

        foreach ((string relPath, DateTime remoteTime) in remote)
        {
            string? local = ResolveSafePath(relPath);
            if (local == null)
            {
                continue;
            }

            if (!File.Exists(local) || remoteTime > File.GetLastWriteTimeUtc(local))
            {
                Trace.WriteLine($"[FileSync] Requesting file '{relPath}' from {from}. RemoteTime={remoteTime:O}");
                OnLog?.Invoke($"[sync] requesting: {relPath}");
                Send(from, RequestType, relPath);
                requested++;
            }
            else
            {
                Trace.WriteLine($"[FileSync] No files need syncing from {from}.");
            }
        }

        if (requested == 0)
        {
            OnLog?.Invoke("[sync] all local files are already up-to-date");
            OnSyncCompleted?.Invoke();
        }
    }

    private void HandleFileRequest(IPEndPoint from, string relPath)
    {
        string? fullPath = ResolveSafePath(relPath);
        Trace.WriteLine($"[FileSync] File request received from {from}: '{relPath}'");

        if (fullPath == null || !File.Exists(fullPath))
        {
            Trace.WriteLine($"[FileSync] File request rejected. Path='{relPath}', ResolvedPath='{fullPath}'");
            OnLog?.Invoke($"[sync] rejected invalid file request: {relPath}");
            return;
        }

        DateTime lastModifiedUtc = File.GetLastWriteTimeUtc(fullPath);
        List<FileChunk> chunks = _chunker.SplitFile(fullPath, relPath, ChunkSize);

        foreach (FileChunk chunk in chunks)
        {
            var dto = new ChunkDto {
                FileId = chunk.FileId,
                ChunkNumber = chunk.ChunkNumber,
                TotalChunks = chunk.TotalChunks,
                Base64Data = Convert.ToBase64String(chunk.Data),
                LastModifiedUtc = lastModifiedUtc
            };

            Trace.WriteLine($"[FileSync] Sending chunk {chunk.ChunkNumber}/{chunk.TotalChunks} for '{chunk.FileId}' to {from}. Size={chunk.Data.Length} bytes");
            Send(from, ChunkType, JsonSerializer.Serialize(dto));
        }

        OnLog?.Invoke($"[sync] sent {chunks.Count} chunk(s) for {relPath}");
    }

    private void HandleChunk(IPEndPoint from, string json)
    {
        ChunkDto? dto;
        Trace.WriteLine($"[FileSync] Chunk received from {from}. JSON size={json.Length}");
        try
        {
            dto = JsonSerializer.Deserialize<ChunkDto>(json);
        }
        catch (JsonException ex)
        {
            Trace.WriteLine($"[FileSync] Failed to deserialize chunk from {from}: {ex}");
            return;
        }

        string? targetPath = dto == null ? null : ResolveSafePath(dto.FileId);
        if (dto == null || targetPath == null || dto.TotalChunks <= 0)
        {
            Trace.WriteLine($"[FileSync] Invalid chunk received from {from}. FileId='{dto?.FileId}', Chunk={dto?.ChunkNumber}, TotalChunks={dto?.TotalChunks}");
            return;
        }

        byte[] data;
        try
        {
            data = Convert.FromBase64String(dto.Base64Data);
        }
        catch (FormatException ex)
        {
            Trace.WriteLine($"[FileSync] Invalid Base64 chunk received from {from} for '{dto.FileId}': {ex}");
            return;
        }

        // keyed by sender as well, so chunks of the same file from two peers never mix
        string key = $"{from}|{dto.FileId}";
        List<FileChunk> list = _incomingChunks.GetOrAdd(key, _ => new List<FileChunk>());

        lock (list)
        {
            if (list.Any(c => c.ChunkNumber == dto.ChunkNumber))
            {
                Trace.WriteLine($"[FileSync] Duplicate chunk ignored: File='{dto.FileId}', Chunk={dto.ChunkNumber + 1}");
                return; // duplicate chunk
            }

            list.Add(new FileChunk {
                FileId = dto.FileId,
                ChunkNumber = dto.ChunkNumber,
                TotalChunks = dto.TotalChunks,
                Data = data,
                LastModifiedUtc = dto.LastModifiedUtc
            });

            OnLog?.Invoke($"[sync] received chunk {dto.ChunkNumber + 1}/{dto.TotalChunks} for {dto.FileId}");

            if (list.Count < dto.TotalChunks)
            {
                return;
            }

            // MergeChunks also restores the last-write time
            Trace.WriteLine($"[FileSync] All chunks received for '{dto.FileId}'. Starting merge to '{targetPath}'.");
            _chunker.MergeChunks(list, targetPath, dto.LastModifiedUtc);
            Trace.WriteLine($"[FileSync] Merge completed for '{dto.FileId}'. Sending ACK to {from}.");
            _incomingChunks.TryRemove(key, out _);
        }

        OnLog?.Invoke($"[sync] assembled file: {dto.FileId}");
        OnSyncCompleted?.Invoke();
        Send(from, AckType, dto.FileId);
    }

    /// <summary>
    /// wraps the message in an envelope and sends it. a client's message always travels to
    /// the host, which forwards it to "To"; the host sends straight to the client.
    /// </summary>
    private void Send(IPEndPoint to, string type, string payload)
    {
        var env = new Envelope {
            Type = type,
            From = _localEndpoint.ToString(),
            To = to.ToString(),
            Payload = payload
        };

        // a client can only talk to the host, which forwards by env.To;
        // the host sends straight to the client
        IPEndPoint transportDestination = _isHost ? to : _hostEndpoint!;
        Trace.WriteLine($"[FileSync] SEND: Type={type}, From={_localEndpoint}, To={to}, TransportDestination={transportDestination}, Module={_moduleId}, Priority={Priority}, PayloadBytes={payload.Length}");
        _communicator.SendMessage(transportDestination, JsonSerializer.SerializeToUtf8Bytes(env), _moduleId, Priority);
    }

    /// <summary>
    /// resolves a relative path inside the root; null if it is empty, rooted or escapes the root
    /// </summary>
    private string? ResolveSafePath(string relPath)
    {
        if (string.IsNullOrWhiteSpace(relPath) || Path.IsPathRooted(relPath))
        {
            Trace.WriteLine($"[FileSync] Rejected unsafe path: '{relPath}'");
            return null;
        }

        string full = Path.GetFullPath(Path.Combine(_rootDir, relPath));
        string prefix = _rootDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        + Path.DirectorySeparatorChar;

        if (!full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            Trace.WriteLine($"[FileSync] Rejected path escaping root: '{relPath}' -> '{full}'");
            return null;
        }
        else
        {
            return full;
        }
    }

    private static IPAddress ToPlainAddress(IPAddress address)
    {
        return address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
    }

    private static bool SameEndpoint(IPEndPoint a, IPEndPoint b)
    {
        return a.Port == b.Port && ToPlainAddress(a.Address).Equals(ToPlainAddress(b.Address));
    }

    /// <summary>
    /// only unsubscribes; the communicator belongs to the meeting and must not be disposed here
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            _communicator.RemoveSubscriber(_moduleId);
            Trace.WriteLine($"[FileSync] Disposing NetworkSyncHandler. ModuleId={_moduleId}, Local={_localEndpoint}");
        }
        catch (Exception ex)
        {
            // a client's communicator is already disposed when the host ends the meeting
            Trace.TraceWarning($"FileSync unsubscribe skipped: {ex.Message}");
        }
    }

    private sealed class Envelope
    {
        public string Type { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
    }

    private sealed class ChunkDto
    {
        public string FileId { get; set; } = string.Empty;
        public int ChunkNumber { get; set; }
        public int TotalChunks { get; set; }
        public string Base64Data { get; set; } = string.Empty;
        public DateTime? LastModifiedUtc { get; set; }
    }
}
