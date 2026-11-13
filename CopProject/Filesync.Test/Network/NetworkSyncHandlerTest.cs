// Author : Eswar

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using FileSynchroniser.Models;
using FileSynchroniser.Network;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Networking;

namespace Filesync.Test.Network;

[TestClass]
public class NetworkSyncHandlerTest
{
    private string _tempDir = null!;
    private FakeCommunicator _communicator = null!;
    private NetworkSyncHandler _handler = null!;

    /// <summary>
    /// Simple fake communicator for testing NetworkSyncHandler without opening real sockets
    /// </summary>
    private class FakeCommunicator : ICommunicator
    {
        public int ListenPort => 8001;
        public IPAddress ListenIpAddress => IPAddress.Loopback;
        public IMessageListener? Subscriber { get; private set; }
        public int SubscribedModule { get; private set; }
        public List<(IPEndPoint? endpoint, byte[] data, int module, int priority)> SentMessages { get; } = new();
        public List<(byte[] data, int module, int priority)> BroadcastMessages { get; } = new();

        public void AddSubscriber(int module, IMessageListener subscriber)
        {
            SubscribedModule = module;
            Subscriber = subscriber;
        }

        public void RemoveSubscriber(int module)
        {
            if (SubscribedModule == module)
            {
                Subscriber = null;
            }
        }

        public void SendMessage(IPEndPoint ipEndPoint, byte[] message, int module, int priority)
        {
            SentMessages.Add((ipEndPoint, message, module, priority));
        }

        public void BroadcastMessage(byte[] message, int module, int priority)
        {
            BroadcastMessages.Add((message, module, priority));
        }

        public void MulticastMessage(int multicastGroup, byte[] message, int module, int priority) { }
        public void JoinMulticastGroup(int multicastGroup, IPEndPoint clientIp) { }
        public void LeaveMulticastGroup(int multicastGroup, IPEndPoint clientIp) { }
        public void AddLossySubscriber(int module, IMessageListenerLossy subscriber)
        {
        }

        public void RemoveLossySubscriber(int module)
        {
        }
        public void SendMessageLossy(IPEndPoint iPEndPoint, byte[] message, int module, int priority)
        {
        }

        public void BroadcastMessageLossy(byte[] message, int module, int priority)
        {
        }

        public void MulticastMessageLossy(int multicastGroup, byte[] message, int module, int priority)
        {
        }
    }

    /// <summary>
    /// Initializing test temp folder and handler before each test
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"SyncHandlerTests_{Guid.NewGuid():N}");
        _communicator = new FakeCommunicator();
        _handler = new NetworkSyncHandler(_tempDir, _communicator);
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

    private void Send(string text)
    {
        _handler.OnMessageReceived(Encoding.UTF8.GetBytes(text));
    }

    private void SendManifest(Dictionary<string, DateTime> manifest)
    {
        Send("MANIFEST:" + JsonSerializer.Serialize(manifest));
    }

    private void SendChunk(string fileId, int number, int total, string content, DateTime? lastModified = null)
    {
        var dto = new {
            FileId = fileId,
            ChunkNumber = number,
            TotalChunks = total,
            Base64Data = Convert.ToBase64String(Encoding.UTF8.GetBytes(content)),
            LastModifiedUtc = lastModified
        };
        Send("CHUNK:" + JsonSerializer.Serialize(dto));
    }

    /// <summary>
    /// 1. Constructor creates root folder and subscribes to module 5
    /// </summary>
    [TestMethod]
    public void Constructor_CreatesDirectoryAndRegistersSubscriber()
    {
        Assert.IsTrue(Directory.Exists(_tempDir));
        Assert.AreEqual(Path.GetFullPath(_tempDir), _handler.RootDirectory);
        Assert.AreEqual(NetworkSyncHandler.ModuleId, _communicator.SubscribedModule);
        Assert.AreSame(_handler, _communicator.Subscriber);
    }

    /// <summary>
    /// 2. StartSync sends GET_MANIFEST to DefaultPeerEndpoint when specified
    /// </summary>
    [TestMethod]
    public void StartSync_WithDefaultPeer_SendsGetManifestToPeer()
    {
        var endpoint = new IPEndPoint(IPAddress.Loopback, 8002);
        _handler.DefaultPeerEndpoint = endpoint;

        _handler.StartSync();

        Assert.HasCount(1, _communicator.SentMessages);
        Assert.AreEqual(endpoint, _communicator.SentMessages[0].endpoint);
        string msg = Encoding.UTF8.GetString(_communicator.SentMessages[0].data);
        Assert.AreEqual("GET_MANIFEST", msg);
    }

    /// <summary>
    /// 3. StartSync broadcasts GET_MANIFEST when no default peer is set
    /// </summary>
    [TestMethod]
    public void StartSync_WithoutDefaultPeer_BroadcastsGetManifest()
    {
        _handler.DefaultPeerEndpoint = null;

        _handler.StartSync();

        Assert.HasCount(1, _communicator.BroadcastMessages);
        string msg = Encoding.UTF8.GetString(_communicator.BroadcastMessages[0].data);
        Assert.AreEqual("GET_MANIFEST", msg);
    }

    /// <summary>
    /// 4. Receiving GET_MANIFEST sends local files manifest
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_GetManifest_SendsLocalManifest()
    {
        var endpoint = new IPEndPoint(IPAddress.Loopback, 8002);
        _handler.DefaultPeerEndpoint = endpoint;

        // create a test file locally
        string testFile = Path.Combine(_tempDir, "sample.txt");
        File.WriteAllText(testFile, "hello filesync");

        Send("GET_MANIFEST");

        Assert.HasCount(1, _communicator.SentMessages);
        string response = Encoding.UTF8.GetString(_communicator.SentMessages[0].data);
        Assert.StartsWith("MANIFEST:", response);
        Assert.Contains("sample.txt", response);
    }

    /// <summary>
    /// 5. Receiving GET_MANIFEST without default peer broadcasts local manifest
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_GetManifest_WithoutPeer_BroadcastsLocalManifest()
    {
        _handler.DefaultPeerEndpoint = null;

        Send("GET_MANIFEST");

        Assert.HasCount(1, _communicator.BroadcastMessages);
        string response = Encoding.UTF8.GetString(_communicator.BroadcastMessages[0].data);
        Assert.StartsWith("MANIFEST:", response);
    }

    /// <summary>
    /// 6. HandleManifest requests files that are missing locally
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Manifest_RequestsMissingFile()
    {
        var endpoint = new IPEndPoint(IPAddress.Loopback, 8002);
        _handler.DefaultPeerEndpoint = endpoint;

        SendManifest(new Dictionary<string, DateTime> { { "missing.txt", DateTime.UtcNow } });

        Assert.HasCount(1, _communicator.SentMessages);
        string sent = Encoding.UTF8.GetString(_communicator.SentMessages[0].data);
        Assert.AreEqual("REQUEST:missing.txt", sent);
    }

    /// <summary>
    /// 7. HandleManifest requests files when remote version is newer
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Manifest_RequestsNewerFile()
    {
        _handler.DefaultPeerEndpoint = null;

        string localFile = Path.Combine(_tempDir, "older.txt");
        File.WriteAllText(localFile, "old content");
        File.SetLastWriteTimeUtc(localFile, DateTime.UtcNow.AddHours(-2));

        SendManifest(new Dictionary<string, DateTime> { { "older.txt", DateTime.UtcNow } });

        Assert.HasCount(1, _communicator.BroadcastMessages);
        string sent = Encoding.UTF8.GetString(_communicator.BroadcastMessages[0].data);
        Assert.AreEqual("REQUEST:older.txt", sent);
    }

    /// <summary>
    /// 8. HandleManifest triggers OnSyncCompleted when all files are already up-to-date
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Manifest_WhenUpToDate_InvokesSyncCompleted()
    {
        string localFile = Path.Combine(_tempDir, "current.txt");
        File.WriteAllText(localFile, "current content");
        DateTime fileTime = File.GetLastWriteTimeUtc(localFile);

        bool completed = false;
        _handler.OnSyncCompleted += () => completed = true;

        SendManifest(new Dictionary<string, DateTime> { { "current.txt", fileTime.AddHours(-1) } });

        Assert.IsTrue(completed);
        Assert.HasCount(0, _communicator.SentMessages);
    }

    /// <summary>
    /// 9. HandleManifest with invalid JSON does not throw or crash
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Manifest_InvalidJson_DoesNotThrow()
    {
        Send("MANIFEST:invalid json data");

        Assert.HasCount(0, _communicator.SentMessages);
    }

    /// <summary>
    /// 10. HandleFileRequest splits local file and sends chunks to peer
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Request_ValidFile_SendsChunksToPeer()
    {
        var endpoint = new IPEndPoint(IPAddress.Loopback, 8002);
        _handler.DefaultPeerEndpoint = endpoint;

        string localFile = Path.Combine(_tempDir, "data.txt");
        File.WriteAllText(localFile, "chunk file test data");

        Send("REQUEST:data.txt");

        Assert.IsGreaterThanOrEqualTo(1, _communicator.SentMessages.Count);
        string firstChunkPacket = Encoding.UTF8.GetString(_communicator.SentMessages[0].data);
        Assert.StartsWith("CHUNK:", firstChunkPacket);
    }

    /// <summary>
    /// 11. HandleFileRequest broadcasts chunks when DefaultPeerEndpoint is null
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Request_WithoutDefaultPeer_BroadcastsChunks()
    {
        _handler.DefaultPeerEndpoint = null;

        string localFile = Path.Combine(_tempDir, "broadcast.txt");
        File.WriteAllText(localFile, "broadcast chunk data");

        Send("REQUEST:broadcast.txt");

        Assert.IsGreaterThanOrEqualTo(1, _communicator.BroadcastMessages.Count);
        string packet = Encoding.UTF8.GetString(_communicator.BroadcastMessages[0].data);
        Assert.StartsWith("CHUNK:", packet);
    }

    /// <summary>
    /// 12. HandleFileRequest rejects non-existent files or path traversal attempts
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Request_InvalidOrOutsideRoot_RejectsRequest()
    {
        // non-existent file
        Send("REQUEST:ghost.txt");

        // path traversal attempt
        Send("REQUEST:../../etc/passwd");

        Assert.HasCount(0, _communicator.SentMessages);
        Assert.HasCount(0, _communicator.BroadcastMessages);
    }

    /// <summary>
    /// 13. HandleChunk reassembles incoming chunks into file and raises OnSyncCompleted
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Chunk_ReassemblesFileAndInvokesSyncCompleted()
    {
        bool completed = false;
        _handler.OnSyncCompleted += () => completed = true;

        SendChunk("received.txt", 0, 1, "full reconstructed file content");

        Assert.IsTrue(completed);
        string assembledPath = Path.Combine(_tempDir, "received.txt");
        Assert.IsTrue(File.Exists(assembledPath));
        Assert.AreEqual("full reconstructed file content", File.ReadAllText(assembledPath));
    }

    /// <summary>
    /// 14. HandleChunk with invalid JSON does not throw
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Chunk_InvalidJson_DoesNotThrow()
    {
        Send("CHUNK:not a json");

        Assert.IsFalse(File.Exists(Path.Combine(_tempDir, "invalid.txt")));
    }

    /// <summary>
    /// 15. OnLog fires event when log messages are emitted
    /// </summary>
    [TestMethod]
    public void OnLog_FiresLogEvent()
    {
        string? loggedMessage = null;
        _handler.OnLog += msg => loggedMessage = msg;

        _handler.StartSync();

        Assert.IsNotNull(loggedMessage);
        Assert.Contains("[sync]", loggedMessage);
    }

    /// <summary>
    /// 16. Constructor with an already existing directory skips directory creation
    /// </summary>
    [TestMethod]
    public void Constructor_ExistingDirectory_UsesItAndRegistersSubscriber()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"SyncHandlerExisting_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            var comm = new FakeCommunicator();
            var handler = new NetworkSyncHandler(dir, comm);

            Assert.AreEqual(Path.GetFullPath(dir), handler.RootDirectory);
            Assert.AreSame(handler, comm.Subscriber);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>
    /// 17. Unknown message prefix is ignored
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_UnknownMessage_IsIgnored()
    {
        Send("HELLO_WORLD");

        Assert.HasCount(0, _communicator.SentMessages);
        Assert.HasCount(0, _communicator.BroadcastMessages);
    }

    /// <summary>
    /// 18. Local manifest includes files from sub folders using forward slashes
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_GetManifest_IncludesNestedFiles()
    {
        string subDir = Path.Combine(_tempDir, "sub");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "inner.txt"), "inner");
        File.WriteAllText(Path.Combine(_tempDir, "top.txt"), "top");

        Send("GET_MANIFEST");

        Assert.HasCount(1, _communicator.BroadcastMessages);
        string response = Encoding.UTF8.GetString(_communicator.BroadcastMessages[0].data);
        Assert.Contains("sub/inner.txt", response);
        Assert.Contains("top.txt", response);
    }

    /// <summary>
    /// 19. Manifest JSON "null" deserializes to null and is ignored
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Manifest_NullJson_IsIgnored()
    {
        bool completed = false;
        _handler.OnSyncCompleted += () => completed = true;

        Send("MANIFEST:null");

        Assert.IsFalse(completed);
        Assert.HasCount(0, _communicator.SentMessages);
        Assert.HasCount(0, _communicator.BroadcastMessages);
    }

    /// <summary>
    /// 20. Manifest with several files requests only the missing and newer ones and does not complete
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Manifest_MixedFiles_RequestsOnlyNeeded()
    {
        bool completed = false;
        _handler.OnSyncCompleted += () => completed = true;

        string older = Path.Combine(_tempDir, "older.txt");
        File.WriteAllText(older, "old");
        File.SetLastWriteTimeUtc(older, DateTime.UtcNow.AddHours(-2));

        string current = Path.Combine(_tempDir, "current.txt");
        File.WriteAllText(current, "current");
        DateTime currentTime = File.GetLastWriteTimeUtc(current);

        SendManifest(new Dictionary<string, DateTime>
        {
            { "missing.txt", DateTime.UtcNow },
            { "older.txt", DateTime.UtcNow },
            { "current.txt", currentTime.AddHours(-1) }
        });

        Assert.HasCount(2, _communicator.BroadcastMessages);
        List<string> sent = _communicator.BroadcastMessages
            .Select(m => Encoding.UTF8.GetString(m.data))
            .ToList();
        CollectionAssert.Contains(sent, "REQUEST:missing.txt");
        CollectionAssert.Contains(sent, "REQUEST:older.txt");
        Assert.IsFalse(completed);
    }

    /// <summary>
    /// 21. Up-to-date manifest without any OnSyncCompleted subscriber does not throw
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Manifest_WhenUpToDate_NoSubscriber_DoesNotThrow()
    {
        SendManifest(new Dictionary<string, DateTime>());

        Assert.HasCount(0, _communicator.SentMessages);
        Assert.HasCount(0, _communicator.BroadcastMessages);
    }

    /// <summary>
    /// 22. Large file is split into several chunks
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Request_LargeFile_SendsMultipleChunks()
    {
        _handler.DefaultPeerEndpoint = new IPEndPoint(IPAddress.Loopback, 8002);

        byte[] big = new byte[200 * 1024];
        new Random(42).NextBytes(big);
        File.WriteAllBytes(Path.Combine(_tempDir, "big.bin"), big);

        Send("REQUEST:big.bin");

        Assert.IsGreaterThan(1, _communicator.SentMessages.Count);
        foreach ((IPEndPoint? _, byte[] data, int _, int _) in _communicator.SentMessages)
        {
            Assert.StartsWith("CHUNK:", Encoding.UTF8.GetString(data));
        }
    }

    /// <summary>
    /// 23. Request with an absolute path outside the root is rejected even if the file exists
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Request_AbsolutePathOutsideRoot_IsRejected()
    {
        string outsideFile = Path.Combine(Path.GetTempPath(), $"outside_{Guid.NewGuid():N}.txt");
        File.WriteAllText(outsideFile, "secret");
        try
        {
            Send("REQUEST:" + outsideFile);

            Assert.HasCount(0, _communicator.SentMessages);
            Assert.HasCount(0, _communicator.BroadcastMessages);
        }
        finally
        {
            File.Delete(outsideFile);
        }
    }

    /// <summary>
    /// 24. Chunks arriving one by one are merged only after the last chunk arrives
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Chunk_MultipleChunks_AssemblesAfterLast()
    {
        int completedCount = 0;
        _handler.OnSyncCompleted += () => completedCount++;
        string target = Path.Combine(_tempDir, "multi.txt");

        SendChunk("multi.txt", 0, 2, "AAAA");

        Assert.AreEqual(0, completedCount);
        Assert.IsFalse(File.Exists(target));

        SendChunk("multi.txt", 1, 2, "BBBB");

        Assert.AreEqual(1, completedCount);
        Assert.AreEqual("AAAABBBB", File.ReadAllText(target));
    }

    /// <summary>
    /// 25. Chunk JSON "null" deserializes to null and is ignored
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Chunk_NullJson_IsIgnored()
    {
        bool completed = false;
        _handler.OnSyncCompleted += () => completed = true;

        Send("CHUNK:null");

        Assert.IsFalse(completed);
        Assert.HasCount(0, Directory.GetFiles(_tempDir));
    }

    /// <summary>
    /// 26. Completed chunk assembly without any OnSyncCompleted subscriber does not throw
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Chunk_Complete_NoSubscriber_DoesNotThrow()
    {
        SendChunk("quiet.txt", 0, 1, "quiet content");

        Assert.IsTrue(File.Exists(Path.Combine(_tempDir, "quiet.txt")));
    }

    /// <summary>
    /// 27. With OnLog and OnSyncCompleted subscribed, every code path raises its event
    /// </summary>
    [TestMethod]
    public void AllEventsSubscribed_FullFlow_RaisesLogsAndCompletion()
    {
        var logs = new List<string>();
        int completedCount = 0;
        _handler.OnLog += msg => logs.Add(msg);
        _handler.OnSyncCompleted += () => completedCount++;

        File.WriteAllText(Path.Combine(_tempDir, "local.txt"), "local data");

        _handler.StartSync();
        Send("GET_MANIFEST");
        SendManifest(new Dictionary<string, DateTime> { { "remote.txt", DateTime.UtcNow } });
        SendManifest(new Dictionary<string, DateTime>());
        Send("REQUEST:ghost.txt");
        Send("REQUEST:local.txt");
        SendChunk("assembled.txt", 0, 1, "abc");

        Assert.AreEqual(2, completedCount);
        Assert.IsTrue(logs.Any(l => l.Contains("starting sync")));
        Assert.IsTrue(logs.Any(l => l.Contains("sending local manifest")));
        Assert.IsTrue(logs.Any(l => l.Contains("received remote manifest")));
        Assert.IsTrue(logs.Any(l => l.Contains("requesting newer file")));
        Assert.IsTrue(logs.Any(l => l.Contains("already up-to-date")));
        Assert.IsTrue(logs.Any(l => l.Contains("rejected invalid file request")));
        Assert.IsTrue(logs.Any(l => l.Contains("splitting and sending chunks")));
        Assert.IsTrue(logs.Any(l => l.Contains("sending chunk 1/1")));
        Assert.IsTrue(logs.Any(l => l.Contains("sent all 1 chunks")));
        Assert.IsTrue(logs.Any(l => l.Contains("received chunk 1/1")));
        Assert.IsTrue(logs.Any(l => l.Contains("successfully assembled file")));
    }

    /// <summary>
    /// 28. HandleChunk preserves the peer's last modified timestamp on reconstructed file
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Chunk_PreservesPeerLastModifiedTimestamp()
    {
        DateTime peerTime = new DateTime(2025, 8, 15, 14, 0, 0, DateTimeKind.Utc);
        SendChunk("timestamped.txt", 0, 1, "peer content", peerTime);

        string assembledPath = Path.Combine(_tempDir, "timestamped.txt");
        Assert.IsTrue(File.Exists(assembledPath));
        Assert.AreEqual(peerTime, File.GetLastWriteTimeUtc(assembledPath));
    }

    /// <summary>
    /// 29. HandleChunk preserves manifest timestamp if chunk DTO has null timestamp
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Chunk_FallsBackToManifestTimestamp()
    {
        DateTime manifestTime = new DateTime(2025, 9, 1, 12, 0, 0, DateTimeKind.Utc);
        SendManifest(new Dictionary<string, DateTime> { { "manifest_timed.txt", manifestTime } });

        SendChunk("manifest_timed.txt", 0, 1, "content without chunk timestamp");

        string assembledPath = Path.Combine(_tempDir, "manifest_timed.txt");
        Assert.IsTrue(File.Exists(assembledPath));
        Assert.AreEqual(manifestTime, File.GetLastWriteTimeUtc(assembledPath));
    }

    /// <summary>
    /// 30. Receiving SYNC_ACK invokes completion and logs peer confirmation
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_SyncAck_LogsAndInvokesCompletion()
    {
        string? logged = null;
        bool completed = false;
        _handler.OnLog += msg => logged = msg;
        _handler.OnSyncCompleted += () => completed = true;

        Send("SYNC_ACK:notes.txt");

        Assert.IsTrue(completed);
        Assert.IsNotNull(logged);
        Assert.Contains("peer confirmed receipt", logged);
        Assert.Contains("notes.txt", logged);
    }
}
