using System.Net.Sockets;
using System.Text.Json;

namespace FileSync;

/// <summary>Client side: connects to a peer and pulls anything newer or missing.</summary>
public sealed class SyncService : ISync
{
    private readonly string _root;

    public SyncService(string root)
    {
        _root = Path.GetFullPath(root);
        Directory.CreateDirectory(_root);
    }

    public string GetDirectory()
    {
        return _root;
    }

    public Task<int> SyncAsync(string peerHost, int peerPort)
    {
        return Task.Run(() => Sync(peerHost, peerPort));
    }


    private int Sync(string host, int port)
    {
        using var tcp = new TcpClient(host, port);
        using NetworkStream stream = tcp.GetStream();
        using var reader = new BinaryReader(stream);
        using var writer = new BinaryWriter(stream);

        writer.Write("LIST");
        writer.Flush();
        List<FileEntry> remote = JsonSerializer.Deserialize<List<FileEntry>>(reader.ReadString()) ?? new();

        int updated = 0;
        foreach (FileEntry r in remote)
        {
            string local = Path.GetFullPath(Path.Combine(_root, r.Path));
            if (!local.StartsWith(_root + Path.DirectorySeparatorChar))
            {
                continue;
            }

            // keep local copy if it is the same age or newer
            if (File.Exists(local) && File.GetLastWriteTimeUtc(local) >= r.LastWriteUtc)
            {
                continue;
            }

            writer.Write("GET");
            writer.Write(r.Path);
            writer.Flush();

            long len = reader.ReadInt64();
            if (len < 0)
            {
                continue;
            }

            byte[] data = reader.ReadBytes((int)len);
            Directory.CreateDirectory(Path.GetDirectoryName(local)!);
            File.WriteAllBytes(local, data);
            File.SetLastWriteTimeUtc(local, r.LastWriteUtc); // so both sides compare equal next time
            updated++;
        }

        writer.Write("BYE");
        writer.Flush();
        return updated;
    }
}
