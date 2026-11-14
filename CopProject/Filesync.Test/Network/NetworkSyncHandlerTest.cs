// Author : Eswar

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using FileSynchroniser.Network;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Networking;

namespace Filesync.Test.Network;

[TestClass]
public class NetworkSyncHandlerTest
{
    // three participants of the fake meeting: the host and two clients
    private static readonly IPEndPoint s_hostEp = new(IPAddress.Loopback, 8001);
    private static readonly IPEndPoint s_epA = new(IPAddress.Loopback, 8002);
    private static readonly IPEndPoint s_epB = new(IPAddress.Loopback, 8003);

    private string _tempDir = null!;
    private FakeCommunicator _communicator = null!;

    // the handler under test, running on the host (local endpoint = HostEp)
    private NetworkSyncHandler _handler = null!;

    /// <summary>
    /// Fake communicator for testing NetworkSyncHandler without opening real sockets
    /// </summary>
    private sealed class FakeCommunicator : ICommunicator
    {
        // deliberately different from every endpoint used in the tests:
        // the handler must take its own endpoint from its constructor, not from these
        public int ListenPort => 9999;
        public IPAddress ListenIpAddress => IPAddress.Any;

        public IMessageListener? Subscriber { get; private set; }
        public int SubscribedModule { get; private set; }
        public int RemoveCalls { get; private set; }
        public bool ThrowOnRemove { get; set; }
        public List<(IPEndPoint? endpoint, byte[] data, int module, int priority)> SentMessages { get; } = new();
        public List<(byte[] data, int module, int priority)> BroadcastMessages { get; } = new();

        public void AddSubscriber(int module, IMessageListener subscriber)
        {
            SubscribedModule = module;
            Subscriber = subscriber;
        }

        public void RemoveSubscriber(int module)
        {
            RemoveCalls++;

            if (ThrowOnRemove)
            {
                throw new ObjectDisposedException("communicator");
            }

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
    /// A sent message split into where it travelled to and what its envelope says
    /// </summary>
    private sealed record Sent(IPEndPoint? Transport, string Type, string From, string To, string Payload, int Module, int Priority);

    /// <summary>
    /// Initializing test temp folder and a host-side handler before each test
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"SyncHandlerTests_{Guid.NewGuid():N}");
        _communicator = new FakeCommunicator();
        _handler = new NetworkSyncHandler(_tempDir, _communicator, isHost: true, s_hostEp, hostEndpoint: null);
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

    // ---------- helpers ----------

    private static byte[] MakeEnvelope(string type, string payload, IPEndPoint from, IPEndPoint to)
    {
        var env = new { Type = type, From = from.ToString(), To = to.ToString(), Payload = payload };
        return JsonSerializer.SerializeToUtf8Bytes(env);
    }

    /// <summary>delivers a message from EpA (or another sender) addressed to the host handler</summary>
    private void Receive(string type, string payload, IPEndPoint? from = null)
    {
        _handler.OnMessageReceived(MakeEnvelope(type, payload, from ?? s_epA, s_hostEp));
    }

    private void ReceiveManifest(Dictionary<string, DateTime> manifest, IPEndPoint? from = null)
    {
        Receive("MANIFEST", JsonSerializer.Serialize(manifest), from);
    }

    private void ReceiveChunk(string fileId, int number, int total, string content, DateTime? lastModified = null, IPEndPoint? from = null)
    {
        var dto = new {
            FileId = fileId,
            ChunkNumber = number,
            TotalChunks = total,
            Base64Data = Convert.ToBase64String(Encoding.UTF8.GetBytes(content)),
            LastModifiedUtc = lastModified
        };
        Receive("CHUNK", JsonSerializer.Serialize(dto), from);
    }

    private static List<Sent> ReadSent(FakeCommunicator comm)
    {
        return comm.SentMessages.Select(m => {
            using JsonDocument doc = JsonDocument.Parse(m.data);
            JsonElement root = doc.RootElement;
            return new Sent(
                m.endpoint,
                root.GetProperty("Type").GetString()!,
                root.GetProperty("From").GetString()!,
                root.GetProperty("To").GetString()!,
                root.GetProperty("Payload").GetString()!,
                m.module,
                m.priority);
        }).ToList();
    }

    private static (string FileId, int Number, int Total, string Text) ReadChunk(string payload)
    {
        using JsonDocument doc = JsonDocument.Parse(payload);
        JsonElement root = doc.RootElement;
        string text = Encoding.UTF8.GetString(Convert.FromBase64String(root.GetProperty("Base64Data").GetString()!));
        return (
            root.GetProperty("FileId").GetString()!,
            root.GetProperty("ChunkNumber").GetInt32(),
            root.GetProperty("TotalChunks").GetInt32(),
            text);
    }

    /// <summary>a handler running on client EpA, whose only route is the host</summary>
    private (NetworkSyncHandler handler, FakeCommunicator comm) CreateClient()
    {
        var comm = new FakeCommunicator();
        var handler = new NetworkSyncHandler(_tempDir, comm, isHost: false, s_epA, s_hostEp);
        return (handler, comm);
    }

    // ---------- constructor ----------

    /// <summary>
    /// 1. Constructor creates root folder and subscribes to the default module
    /// </summary>
    [TestMethod]
    public void Constructor_CreatesDirectoryAndRegistersSubscriber()
    {
        Assert.IsTrue(Directory.Exists(_tempDir));
        Assert.AreEqual(Path.GetFullPath(_tempDir), _handler.RootDirectory);
        Assert.AreEqual(NetworkSyncHandler.DefaultModuleId, _communicator.SubscribedModule);
        Assert.AreSame(_handler, _communicator.Subscriber);
    }

    /// <summary>
    /// 2. Constructor subscribes on a custom module id and sends on it
    /// </summary>
    [TestMethod]
    public void Constructor_CustomModuleId_SubscribesAndSendsOnIt()
    {
        var comm = new FakeCommunicator();
        var handler = new NetworkSyncHandler(_tempDir, comm, isHost: true, s_hostEp, null, moduleId: 42);

        handler.StartSync(s_epA);

        Assert.AreEqual(42, comm.SubscribedModule);
        Assert.HasCount(1, comm.SentMessages);
        Assert.AreEqual(42, comm.SentMessages[0].module);
    }

    /// <summary>
    /// 3. Constructor with an already existing directory uses it and registers the subscriber
    /// </summary>
    [TestMethod]
    public void Constructor_ExistingDirectory_UsesItAndRegistersSubscriber()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"SyncHandlerExisting_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            var comm = new FakeCommunicator();
            var handler = new NetworkSyncHandler(dir, comm, isHost: true, s_hostEp, null);

            Assert.AreEqual(Path.GetFullPath(dir), handler.RootDirectory);
            Assert.AreSame(handler, comm.Subscriber);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>
    /// 4. The local endpoint comes from the constructor argument, not from the communicator
    /// </summary>
    [TestMethod]
    public void Constructor_LocalEndpoint_ComesFromArgument()
    {
        Assert.AreEqual(s_hostEp, _handler.LocalEndpoint);
        Assert.AreNotEqual(_communicator.ListenPort, _handler.LocalEndpoint.Port);
    }

    /// <summary>
    /// 5. A client without a host endpoint cannot be created
    /// </summary>
    [TestMethod]
    public void Constructor_ClientWithoutHostEndpoint_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            new NetworkSyncHandler(_tempDir, new FakeCommunicator(), isHost: false, s_epA, hostEndpoint: null));
    }

    /// <summary>
    /// 6. Missing arguments are rejected
    /// </summary>
    [TestMethod]
    public void Constructor_InvalidArguments_Throw()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            new NetworkSyncHandler(_tempDir, null!, isHost: true, s_hostEp, null));
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            new NetworkSyncHandler(_tempDir, new FakeCommunicator(), isHost: true, null!, null));
        Assert.ThrowsExactly<ArgumentException>(() =>
            new NetworkSyncHandler("   ", new FakeCommunicator(), isHost: true, s_hostEp, null));
    }

    // ---------- StartSync ----------

    /// <summary>
    /// 7. Host: StartSync sends GET_MANIFEST straight to the peer
    /// </summary>
    [TestMethod]
    public void StartSync_OnHost_SendsGetManifestDirectlyToPeer()
    {
        _handler.StartSync(s_epA);

        Assert.HasCount(1, _communicator.SentMessages);
        Assert.HasCount(0, _communicator.BroadcastMessages);

        Sent sent = ReadSent(_communicator)[0];
        Assert.AreEqual(s_epA, sent.Transport);
        Assert.AreEqual("GET_MANIFEST", sent.Type);
        Assert.AreEqual(s_hostEp.ToString(), sent.From);
        Assert.AreEqual(s_epA.ToString(), sent.To);
        Assert.AreEqual(NetworkSyncHandler.DefaultModuleId, sent.Module);
        Assert.AreEqual(1, sent.Priority);
    }

    /// <summary>
    /// 8. Client: StartSync travels to the host but names the peer as the target
    /// </summary>
    [TestMethod]
    public void StartSync_OnClient_SendsToHostWithPeerAsTarget()
    {
        (NetworkSyncHandler client, FakeCommunicator comm) = CreateClient();

        client.StartSync(s_epB);

        Assert.HasCount(1, comm.SentMessages);
        Sent sent = ReadSent(comm)[0];
        Assert.AreEqual(s_hostEp, sent.Transport);
        Assert.AreEqual("GET_MANIFEST", sent.Type);
        Assert.AreEqual(s_epA.ToString(), sent.From);
        Assert.AreEqual(s_epB.ToString(), sent.To);
    }

    /// <summary>
    /// 9. Syncing with yourself sends nothing and logs it
    /// </summary>
    [TestMethod]
    public void StartSync_WithOwnEndpoint_SendsNothing()
    {
        string? logged = null;
        _handler.OnLog += msg => logged = msg;

        _handler.StartSync(s_hostEp);

        Assert.HasCount(0, _communicator.SentMessages);
        Assert.IsNotNull(logged);
        Assert.Contains("cannot sync with yourself", logged);
    }

    /// <summary>
    /// 10. StartSync without a peer is rejected
    /// </summary>
    [TestMethod]
    public void StartSync_NullPeer_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _handler.StartSync(null!));
    }

    /// <summary>
    /// 11. OnLog fires when sync starts
    /// </summary>
    [TestMethod]
    public void OnLog_FiresLogEvent()
    {
        string? loggedMessage = null;
        _handler.OnLog += msg => loggedMessage = msg;

        _handler.StartSync(s_epA);

        Assert.IsNotNull(loggedMessage);
        Assert.Contains("[sync]", loggedMessage);
    }

    // ---------- GET_MANIFEST ----------

    /// <summary>
    /// 12. Receiving GET_MANIFEST sends the local manifest back to the sender
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_GetManifest_SendsLocalManifestToSender()
    {
        File.WriteAllText(Path.Combine(_tempDir, "sample.txt"), "hello filesync");

        Receive("GET_MANIFEST", string.Empty);

        Assert.HasCount(1, _communicator.SentMessages);
        Sent reply = ReadSent(_communicator)[0];
        Assert.AreEqual("MANIFEST", reply.Type);
        Assert.AreEqual(s_epA, reply.Transport);
        Assert.AreEqual(s_epA.ToString(), reply.To);
        Assert.AreEqual(s_hostEp.ToString(), reply.From);

        Dictionary<string, DateTime>? manifest = JsonSerializer.Deserialize<Dictionary<string, DateTime>>(reply.Payload);
        Assert.IsNotNull(manifest);
        Assert.IsTrue(manifest.ContainsKey("sample.txt"));
    }

    /// <summary>
    /// 13. Local manifest includes files from sub folders using forward slashes
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_GetManifest_IncludesNestedFiles()
    {
        string subDir = Path.Combine(_tempDir, "sub");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "inner.txt"), "inner");
        File.WriteAllText(Path.Combine(_tempDir, "top.txt"), "top");

        Receive("GET_MANIFEST", string.Empty);

        Dictionary<string, DateTime>? manifest = JsonSerializer.Deserialize<Dictionary<string, DateTime>>(ReadSent(_communicator)[0].Payload);
        Assert.IsNotNull(manifest);
        Assert.IsTrue(manifest.ContainsKey("sub/inner.txt"));
        Assert.IsTrue(manifest.ContainsKey("top.txt"));
    }

    /// <summary>
    /// 14. Client: the manifest reply goes through the host and names the original sender as target
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_GetManifest_OnClient_RepliesViaHost()
    {
        (NetworkSyncHandler client, FakeCommunicator comm) = CreateClient();

        client.OnMessageReceived(MakeEnvelope("GET_MANIFEST", string.Empty, s_epB, s_epA));

        Assert.HasCount(1, comm.SentMessages);
        Sent reply = ReadSent(comm)[0];
        Assert.AreEqual(s_hostEp, reply.Transport);
        Assert.AreEqual("MANIFEST", reply.Type);
        Assert.AreEqual(s_epB.ToString(), reply.To);
        Assert.AreEqual(s_epA.ToString(), reply.From);
    }

    // ---------- MANIFEST ----------

    /// <summary>
    /// 15. A file that is missing locally is requested from the sender
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Manifest_RequestsMissingFile()
    {
        ReceiveManifest(new Dictionary<string, DateTime> { { "missing.txt", DateTime.UtcNow } });

        Assert.HasCount(1, _communicator.SentMessages);
        Sent sent = ReadSent(_communicator)[0];
        Assert.AreEqual("REQUEST", sent.Type);
        Assert.AreEqual("missing.txt", sent.Payload);
        Assert.AreEqual(s_epA, sent.Transport);
        Assert.AreEqual(s_epA.ToString(), sent.To);
    }

    /// <summary>
    /// 16. A file whose remote version is newer is requested
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Manifest_RequestsNewerFile()
    {
        string localFile = Path.Combine(_tempDir, "older.txt");
        File.WriteAllText(localFile, "old content");
        File.SetLastWriteTimeUtc(localFile, DateTime.UtcNow.AddHours(-2));

        ReceiveManifest(new Dictionary<string, DateTime> { { "older.txt", DateTime.UtcNow } });

        Assert.HasCount(1, _communicator.SentMessages);
        Sent sent = ReadSent(_communicator)[0];
        Assert.AreEqual("REQUEST", sent.Type);
        Assert.AreEqual("older.txt", sent.Payload);
    }

    /// <summary>
    /// 17. Nothing to fetch completes the sync and requests nothing
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Manifest_WhenUpToDate_InvokesSyncCompleted()
    {
        string localFile = Path.Combine(_tempDir, "current.txt");
        File.WriteAllText(localFile, "current content");
        DateTime fileTime = File.GetLastWriteTimeUtc(localFile);

        bool completed = false;
        _handler.OnSyncCompleted += () => completed = true;

        ReceiveManifest(new Dictionary<string, DateTime> { { "current.txt", fileTime.AddHours(-1) } });

        Assert.IsTrue(completed);
        Assert.HasCount(0, _communicator.SentMessages);
    }

    /// <summary>
    /// 18. Manifest with invalid JSON is ignored
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Manifest_InvalidJson_DoesNotThrow()
    {
        bool completed = false;
        _handler.OnSyncCompleted += () => completed = true;

        Receive("MANIFEST", "invalid json data");

        Assert.IsFalse(completed);
        Assert.HasCount(0, _communicator.SentMessages);
    }

    /// <summary>
    /// 19. Manifest JSON "null" deserializes to null and is ignored
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Manifest_NullJson_IsIgnored()
    {
        bool completed = false;
        _handler.OnSyncCompleted += () => completed = true;

        Receive("MANIFEST", "null");

        Assert.IsFalse(completed);
        Assert.HasCount(0, _communicator.SentMessages);
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

        ReceiveManifest(new Dictionary<string, DateTime>
        {
            { "missing.txt", DateTime.UtcNow },
            { "older.txt", DateTime.UtcNow },
            { "current.txt", currentTime.AddHours(-1) }
        });

        List<string> requested = ReadSent(_communicator).Select(s => s.Payload).ToList();
        Assert.HasCount(2, requested);
        CollectionAssert.Contains(requested, "missing.txt");
        CollectionAssert.Contains(requested, "older.txt");
        Assert.IsFalse(completed);
    }

    /// <summary>
    /// 21. An empty manifest without any OnSyncCompleted subscriber does not throw
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Manifest_WhenUpToDate_NoSubscriber_DoesNotThrow()
    {
        ReceiveManifest(new Dictionary<string, DateTime>());

        Assert.HasCount(0, _communicator.SentMessages);
        Assert.HasCount(0, _communicator.BroadcastMessages);
    }

    /// <summary>
    /// 22. A manifest entry that escapes the root folder is never requested
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Manifest_PathOutsideRoot_IsNotRequested()
    {
        ReceiveManifest(new Dictionary<string, DateTime>
        {
            { "../evil.txt", DateTime.UtcNow },
            { "..\\..\\evil2.txt", DateTime.UtcNow }
        });

        Assert.HasCount(0, _communicator.SentMessages);
    }

    // ---------- REQUEST ----------

    /// <summary>
    /// 23. A valid request sends the file as CHUNK messages to the requester
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Request_ValidFile_SendsChunkToRequester()
    {
        File.WriteAllText(Path.Combine(_tempDir, "data.txt"), "chunk file test data");

        Receive("REQUEST", "data.txt");

        Assert.HasCount(1, _communicator.SentMessages);
        Sent sent = ReadSent(_communicator)[0];
        Assert.AreEqual("CHUNK", sent.Type);
        Assert.AreEqual(s_epA, sent.Transport);
        Assert.AreEqual(s_epA.ToString(), sent.To);

        (string FileId, int Number, int Total, string Text) chunk = ReadChunk(sent.Payload);
        Assert.AreEqual("data.txt", chunk.FileId);
        Assert.AreEqual(0, chunk.Number);
        Assert.AreEqual(1, chunk.Total);
        Assert.AreEqual("chunk file test data", chunk.Text);
    }

    /// <summary>
    /// 24. A large file is split into several consistent chunks
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Request_LargeFile_SendsMultipleChunks()
    {
        byte[] big = new byte[200 * 1024];
        new Random(42).NextBytes(big);
        File.WriteAllBytes(Path.Combine(_tempDir, "big.bin"), big);

        Receive("REQUEST", "big.bin");

        List<Sent> sent = ReadSent(_communicator);
        Assert.IsGreaterThan(1, sent.Count);

        foreach (Sent message in sent)
        {
            Assert.AreEqual("CHUNK", message.Type);
        }

        List<int> numbers = sent.Select(s => ReadChunk(s.Payload).Number).OrderBy(n => n).ToList();
        CollectionAssert.AreEqual(Enumerable.Range(0, sent.Count).ToList(), numbers);
        Assert.IsTrue(sent.All(s => ReadChunk(s.Payload).Total == sent.Count));
    }

    /// <summary>
    /// 25. Non-existent files and path traversal attempts are rejected
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Request_InvalidOrOutsideRoot_RejectsRequest()
    {
        var logs = new List<string>();
        _handler.OnLog += msg => logs.Add(msg);

        Receive("REQUEST", "ghost.txt");
        Receive("REQUEST", "../../etc/passwd");

        Assert.HasCount(0, _communicator.SentMessages);
        Assert.HasCount(0, _communicator.BroadcastMessages);
        Assert.AreEqual(2, logs.Count(l => l.Contains("rejected invalid file request")));
    }

    /// <summary>
    /// 26. A request with an absolute path outside the root is rejected even if the file exists
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Request_AbsolutePathOutsideRoot_IsRejected()
    {
        string outsideFile = Path.Combine(Path.GetTempPath(), $"outside_{Guid.NewGuid():N}.txt");
        File.WriteAllText(outsideFile, "secret");
        try
        {
            Receive("REQUEST", outsideFile);

            Assert.HasCount(0, _communicator.SentMessages);
            Assert.HasCount(0, _communicator.BroadcastMessages);
        }
        finally
        {
            File.Delete(outsideFile);
        }
    }

    /// <summary>
    /// 27. Client: chunks go through the host and are addressed to the requester
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Request_OnClient_SendsChunksViaHost()
    {
        (NetworkSyncHandler client, FakeCommunicator comm) = CreateClient();
        File.WriteAllText(Path.Combine(_tempDir, "shared.txt"), "client data");

        client.OnMessageReceived(MakeEnvelope("REQUEST", "shared.txt", s_epB, s_epA));

        Assert.HasCount(1, comm.SentMessages);
        Sent sent = ReadSent(comm)[0];
        Assert.AreEqual("CHUNK", sent.Type);
        Assert.AreEqual(s_hostEp, sent.Transport);
        Assert.AreEqual(s_epB.ToString(), sent.To);
    }

    // ---------- CHUNK ----------

    /// <summary>
    /// 28. A chunk is reassembled into a file, raises OnSyncCompleted and is acknowledged to the sender
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Chunk_ReassemblesFileInvokesSyncCompletedAndAcks()
    {
        bool completed = false;
        _handler.OnSyncCompleted += () => completed = true;

        ReceiveChunk("received.txt", 0, 1, "full reconstructed file content");

        Assert.IsTrue(completed);
        string assembledPath = Path.Combine(_tempDir, "received.txt");
        Assert.IsTrue(File.Exists(assembledPath));
        Assert.AreEqual("full reconstructed file content", File.ReadAllText(assembledPath));

        Assert.HasCount(1, _communicator.SentMessages);
        Sent ack = ReadSent(_communicator)[0];
        Assert.AreEqual("SYNC_ACK", ack.Type);
        Assert.AreEqual("received.txt", ack.Payload);
        Assert.AreEqual(s_epA, ack.Transport);
        Assert.AreEqual(s_epA.ToString(), ack.To);
    }

    /// <summary>
    /// 29. A chunk with invalid JSON is ignored
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Chunk_InvalidJson_DoesNotThrow()
    {
        Receive("CHUNK", "not a json");

        Assert.HasCount(0, Directory.GetFiles(_tempDir));
        Assert.HasCount(0, _communicator.SentMessages);
    }

    /// <summary>
    /// 30. Chunk JSON "null" deserializes to null and is ignored
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Chunk_NullJson_IsIgnored()
    {
        bool completed = false;
        _handler.OnSyncCompleted += () => completed = true;

        Receive("CHUNK", "null");

        Assert.IsFalse(completed);
        Assert.HasCount(0, Directory.GetFiles(_tempDir));
    }

    /// <summary>
    /// 31. Chunks arriving one by one are merged only after the last chunk arrives
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Chunk_MultipleChunks_AssemblesAfterLast()
    {
        int completedCount = 0;
        _handler.OnSyncCompleted += () => completedCount++;
        string target = Path.Combine(_tempDir, "multi.txt");

        ReceiveChunk("multi.txt", 0, 2, "AAAA");

        Assert.AreEqual(0, completedCount);
        Assert.IsFalse(File.Exists(target));
        Assert.HasCount(0, _communicator.SentMessages);

        ReceiveChunk("multi.txt", 1, 2, "BBBB");

        Assert.AreEqual(1, completedCount);
        Assert.AreEqual("AAAABBBB", File.ReadAllText(target));
        Assert.HasCount(1, _communicator.SentMessages);
    }

    /// <summary>
    /// 32. A duplicate chunk number is ignored and does not complete the file early
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Chunk_Duplicate_IsIgnored()
    {
        int completedCount = 0;
        _handler.OnSyncCompleted += () => completedCount++;
        string target = Path.Combine(_tempDir, "dup.txt");

        ReceiveChunk("dup.txt", 0, 2, "AAAA");
        ReceiveChunk("dup.txt", 0, 2, "AAAA");

        Assert.AreEqual(0, completedCount);
        Assert.IsFalse(File.Exists(target));

        ReceiveChunk("dup.txt", 1, 2, "BBBB");

        Assert.AreEqual(1, completedCount);
        Assert.AreEqual("AAAABBBB", File.ReadAllText(target));
    }

    /// <summary>
    /// 33. Chunks of the same file from two different senders are never mixed
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Chunk_SameFileFromTwoSenders_AreNotMixed()
    {
        int completedCount = 0;
        _handler.OnSyncCompleted += () => completedCount++;

        ReceiveChunk("shared.txt", 0, 2, "AAAA", from: s_epA);
        ReceiveChunk("shared.txt", 1, 2, "BBBB", from: s_epB);

        Assert.AreEqual(0, completedCount);
        Assert.IsFalse(File.Exists(Path.Combine(_tempDir, "shared.txt")));
    }

    /// <summary>
    /// 34. A chunk that claims zero total chunks is ignored
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Chunk_ZeroTotalChunks_IsIgnored()
    {
        bool completed = false;
        _handler.OnSyncCompleted += () => completed = true;

        ReceiveChunk("zero.txt", 0, 0, "data");

        Assert.IsFalse(completed);
        Assert.HasCount(0, Directory.GetFiles(_tempDir));
    }

    /// <summary>
    /// 35. A chunk with invalid base64 data is ignored
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Chunk_InvalidBase64_IsIgnored()
    {
        bool completed = false;
        _handler.OnSyncCompleted += () => completed = true;

        var dto = new { FileId = "bad.txt", ChunkNumber = 0, TotalChunks = 1, Base64Data = "***not base64***", LastModifiedUtc = (DateTime?)null };
        Receive("CHUNK", JsonSerializer.Serialize(dto));

        Assert.IsFalse(completed);
        Assert.HasCount(0, Directory.GetFiles(_tempDir));
        Assert.HasCount(0, _communicator.SentMessages);
    }

    /// <summary>
    /// 36. A chunk whose file id escapes the root folder is never written
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Chunk_PathOutsideRoot_IsIgnored()
    {
        bool completed = false;
        _handler.OnSyncCompleted += () => completed = true;

        ReceiveChunk("../escaped.txt", 0, 1, "should not be written");

        Assert.IsFalse(completed);
        Assert.HasCount(0, Directory.GetFiles(_tempDir));
        Assert.HasCount(0, _communicator.SentMessages);
    }

    /// <summary>
    /// 37. A completed chunk assembly without any OnSyncCompleted subscriber does not throw
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Chunk_Complete_NoSubscriber_DoesNotThrow()
    {
        ReceiveChunk("quiet.txt", 0, 1, "quiet content");

        Assert.IsTrue(File.Exists(Path.Combine(_tempDir, "quiet.txt")));
    }

    /// <summary>
    /// 38. The peer's last modified timestamp is kept on the reconstructed file
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Chunk_PreservesPeerLastModifiedTimestamp()
    {
        DateTime peerTime = new DateTime(2025, 8, 15, 14, 0, 0, DateTimeKind.Utc);

        ReceiveChunk("timestamped.txt", 0, 1, "peer content", peerTime);

        string assembledPath = Path.Combine(_tempDir, "timestamped.txt");
        Assert.IsTrue(File.Exists(assembledPath));
        Assert.AreEqual(peerTime, File.GetLastWriteTimeUtc(assembledPath));
    }

    /// <summary>
    /// 39. A chunk without a timestamp still produces the file
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Chunk_WithoutTimestamp_StillAssemblesFile()
    {
        ReceiveChunk("untimed.txt", 0, 1, "content without chunk timestamp", lastModified: null);

        string assembledPath = Path.Combine(_tempDir, "untimed.txt");
        Assert.IsTrue(File.Exists(assembledPath));
        Assert.AreEqual("content without chunk timestamp", File.ReadAllText(assembledPath));
    }

    // ---------- SYNC_ACK ----------

    /// <summary>
    /// 40. SYNC_ACK is only logged: it does not complete the receiving side's sync
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_SyncAck_LogsButDoesNotInvokeCompletion()
    {
        string? logged = null;
        bool completed = false;
        _handler.OnLog += msg => logged = msg;
        _handler.OnSyncCompleted += () => completed = true;

        Receive("SYNC_ACK", "notes.txt");

        Assert.IsFalse(completed);
        Assert.IsNotNull(logged);
        Assert.Contains("peer confirmed receipt", logged);
        Assert.Contains("notes.txt", logged);
        Assert.HasCount(0, _communicator.SentMessages);
    }

    // ---------- routing ----------

    /// <summary>
    /// 41. Host: a message addressed to another participant is forwarded unchanged
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Host_ForwardsMessageForSomeoneElse()
    {
        bool completed = false;
        _handler.OnSyncCompleted += () => completed = true;
        byte[] message = MakeEnvelope("GET_MANIFEST", string.Empty, s_epA, s_epB);

        _handler.OnMessageReceived(message);

        Assert.HasCount(1, _communicator.SentMessages);
        (IPEndPoint? endpoint, byte[] data, int module, int priority) forwarded = _communicator.SentMessages[0];
        Assert.AreEqual(s_epB, forwarded.endpoint);
        CollectionAssert.AreEqual(message, forwarded.data);
        Assert.AreEqual(NetworkSyncHandler.DefaultModuleId, forwarded.module);
        Assert.AreEqual(1, forwarded.priority);
        Assert.IsFalse(completed);
    }

    /// <summary>
    /// 42. Client: a message that is not addressed to it is dropped, never forwarded
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Client_DropsMessageForSomeoneElse()
    {
        (NetworkSyncHandler client, FakeCommunicator comm) = CreateClient();

        client.OnMessageReceived(MakeEnvelope("GET_MANIFEST", string.Empty, s_hostEp, s_epB));

        Assert.HasCount(0, comm.SentMessages);
        Assert.HasCount(0, comm.BroadcastMessages);
    }

    /// <summary>
    /// 43. An IPv4-mapped IPv6 target address is recognised as the local endpoint
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_MappedIpv4Target_IsHandledLocally()
    {
        byte[] message = JsonSerializer.SerializeToUtf8Bytes(new {
            Type = "GET_MANIFEST",
            From = s_epA.ToString(),
            To = "[::ffff:127.0.0.1]:8001",
            Payload = string.Empty
        });

        _handler.OnMessageReceived(message);

        Assert.HasCount(1, _communicator.SentMessages);
        Sent reply = ReadSent(_communicator)[0];
        Assert.AreEqual("MANIFEST", reply.Type);
        Assert.AreEqual(s_epA, reply.Transport);
    }

    /// <summary>
    /// 44. Messages that are not valid envelopes are ignored
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_MalformedEnvelope_IsIgnored()
    {
        _handler.OnMessageReceived(Encoding.UTF8.GetBytes("GET_MANIFEST"));
        _handler.OnMessageReceived(Encoding.UTF8.GetBytes("null"));
        _handler.OnMessageReceived(Array.Empty<byte>());

        Assert.HasCount(0, _communicator.SentMessages);
        Assert.HasCount(0, _communicator.BroadcastMessages);
    }

    /// <summary>
    /// 45. Envelopes with a missing or invalid sender or target are ignored
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_InvalidEndpointsInEnvelope_AreIgnored()
    {
        var noTarget = new { Type = "GET_MANIFEST", From = s_epA.ToString(), To = "", Payload = "" };
        var badTarget = new { Type = "GET_MANIFEST", From = s_epA.ToString(), To = "not an endpoint", Payload = "" };
        var noSender = new { Type = "GET_MANIFEST", From = "", To = s_hostEp.ToString(), Payload = "" };
        var badSender = new { Type = "GET_MANIFEST", From = "garbage", To = s_hostEp.ToString(), Payload = "" };

        _handler.OnMessageReceived(JsonSerializer.SerializeToUtf8Bytes(noTarget));
        _handler.OnMessageReceived(JsonSerializer.SerializeToUtf8Bytes(badTarget));
        _handler.OnMessageReceived(JsonSerializer.SerializeToUtf8Bytes(noSender));
        _handler.OnMessageReceived(JsonSerializer.SerializeToUtf8Bytes(badSender));

        Assert.HasCount(0, _communicator.SentMessages);
    }

    /// <summary>
    /// 46. An unknown message type is ignored
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_UnknownType_IsIgnored()
    {
        Receive("HELLO_WORLD", "anything");

        Assert.HasCount(0, _communicator.SentMessages);
        Assert.HasCount(0, _communicator.BroadcastMessages);
    }

    // ---------- events ----------

    /// <summary>
    /// 47. With OnLog and OnSyncCompleted subscribed, every code path raises its event
    /// </summary>
    [TestMethod]
    public void AllEventsSubscribed_FullFlow_RaisesLogsAndCompletion()
    {
        var logs = new List<string>();
        int completedCount = 0;
        _handler.OnLog += msg => logs.Add(msg);
        _handler.OnSyncCompleted += () => completedCount++;

        File.WriteAllText(Path.Combine(_tempDir, "local.txt"), "local data");

        _handler.StartSync(s_epA);
        Receive("GET_MANIFEST", string.Empty);
        ReceiveManifest(new Dictionary<string, DateTime> { { "remote.txt", DateTime.UtcNow } });
        ReceiveManifest(new Dictionary<string, DateTime>());
        Receive("REQUEST", "ghost.txt");
        Receive("REQUEST", "local.txt");
        ReceiveChunk("assembled.txt", 0, 1, "abc");
        Receive("SYNC_ACK", "assembled.txt");

        Assert.AreEqual(2, completedCount);
        Assert.IsTrue(logs.Any(l => l.Contains("requesting manifest from")));
        Assert.IsTrue(logs.Any(l => l.Contains("sending manifest with")));
        Assert.IsTrue(logs.Any(l => l.Contains("received manifest with")));
        Assert.IsTrue(logs.Any(l => l.Contains("requesting: remote.txt")));
        Assert.IsTrue(logs.Any(l => l.Contains("already up-to-date")));
        Assert.IsTrue(logs.Any(l => l.Contains("rejected invalid file request")));
        Assert.IsTrue(logs.Any(l => l.Contains("sent 1 chunk(s) for local.txt")));
        Assert.IsTrue(logs.Any(l => l.Contains("received chunk 1/1")));
        Assert.IsTrue(logs.Any(l => l.Contains("assembled file")));
        Assert.IsTrue(logs.Any(l => l.Contains("peer confirmed receipt")));
    }

    // ---------- dispose ----------

    /// <summary>
    /// 48. Dispose unsubscribes from the communicator without disposing it
    /// </summary>
    [TestMethod]
    public void Dispose_RemovesSubscriber()
    {
        _handler.Dispose();

        Assert.IsNull(_communicator.Subscriber);
        Assert.AreEqual(1, _communicator.RemoveCalls);
    }

    /// <summary>
    /// 49. Disposing twice unsubscribes only once
    /// </summary>
    [TestMethod]
    public void Dispose_CalledTwice_UnsubscribesOnce()
    {
        _handler.Dispose();
        _handler.Dispose();

        Assert.AreEqual(1, _communicator.RemoveCalls);
    }

    /// <summary>
    /// 50. Dispose does not throw when the meeting already disposed the communicator
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCommunicatorAlreadyGone_DoesNotThrow()
    {
        _communicator.ThrowOnRemove = true;

        _handler.Dispose();

        Assert.AreEqual(1, _communicator.RemoveCalls);
    }
}
