using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileSync.ConsoleApp;

internal static class Program
{
    /// <summary>
    /// serves as the entry point of the console application and processes command-line arguments
    /// </summary>
    private static async Task Main(string[] args)
    {
        Console.WriteLine("\n FileSync & Networking Simulation Console App \n");

        // if arguments are provided, run as an interactive single node
        if (args.Length == 4)
        {
            int listenPort = int.Parse(args[0]);
            int peerPort = int.Parse(args[1]);
            string peerIp = args.Length > 2 ? args[2] : "127.0.0.1";
            string folder = args.Length > 3 ? args[3] : $"Node_{listenPort}";

            RunInteractiveNode(listenPort, peerPort, peerIp, folder);
            return;
        }
        else
        {
            Console.WriteLine("Exactly 4 arguments in the order of <listenPort, peerPort, peerIp, folder> should be given");
        }
    }

    /// <summary>
    /// runs an interactive node that connects to a peer machine over TCP and handles file synchronization commands
    /// </summary>
    /// <param name="listenPort">the port on which the node listens for incoming connections</param>
    /// <param name="peerPort">the port of the remote peer</param>
    /// <param name="peerIp">the IP address of the remote peer</param>
    /// <param name="folder">the local directory used for file synchronization</param>
    private static void RunInteractiveNode(int listenPort, int peerPort, string peerIp, string folder)
    {
        Console.WriteLine($"[node] listening on port {listenPort}");
        Console.WriteLine($"[node] targeting peer at {peerIp}:{peerPort}");
        Console.WriteLine($"[node] shared folder: {Path.GetFullPath(folder)}");

        using var communicator = new SimulatedCommunicator(listenPort);
        var peerEndpoint = new IPEndPoint(IPAddress.Parse(peerIp), peerPort);
        communicator.RegisterPeer(peerEndpoint);

        var handler = new NetworkSyncHandler(folder, communicator) {
            DefaultPeerEndpoint = peerEndpoint
        };

        handler.OnLog += msg => Console.WriteLine(msg);
        handler.OnSyncCompleted += () => Console.WriteLine("[node] sync finished successfully!");

        Console.WriteLine("\ncommands available:");
        Console.WriteLine("  sync                       - trigger synchronisation with peer");
        Console.WriteLine("  create <relPath> <text>    - create a test file");
        Console.WriteLine("  list                       - list files in folder");
        Console.WriteLine("  exit                       - quit\n");

        while (true)
        {
            Console.Write("> ");
            string? line = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            string[] parts = line.Trim().Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            string cmd = parts[0].ToLowerInvariant();

            if (cmd == "exit")
            {
                break;
            }
            else if (cmd == "sync")
            {
                handler.StartSync();
            }
            else if (cmd == "list")
            {
                string dir = handler.RootDirectory;
                string[] files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
                Console.WriteLine($"files in {dir} ({files.Length}):");
                foreach (string f in files)
                {
                    var fi = new FileInfo(f);
                    Console.WriteLine($"  - {Path.GetRelativePath(dir, f)} ({fi.Length} bytes, {fi.LastWriteTimeUtc})");
                }
            }
            else if (cmd == "create" && parts.Length >= 3)
            {
                string rel = parts[1];
                string content = parts[2];
                string full = Path.Combine(handler.RootDirectory, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(full)!);
                File.WriteAllText(full, content);
                Console.WriteLine($"created file at {rel}");
            }
            else
            {
                Console.WriteLine("unknown command. use: sync, list, create <path> <text>, exit");
            }
        }
    }
}
