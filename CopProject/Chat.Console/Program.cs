using System.Net;
using System.Net.Sockets;
using Controller;
using Networking;
using Utils;

namespace Chat;

internal static class Program
{
    private static void Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "--help" or "-h")
        {
            PrintUsage();
            return;
        }

        try
        {
            switch (args[0])
            {
                case "host" when args.Length == 4:
                    RunHost(args[1], args[2], ParsePort(args[3]));
                    break;

                case "client" when args.Length == 4:
                    RunClient(
                        args[1],
                        args[2],
                        ParsePort(args[3]));
                    break;

                default:
                    PrintUsage();
                    Environment.ExitCode = 2;
                    break;
            }
        }
        catch (ArgumentException exception)
        {
            Console.Error.WriteLine(exception.Message);
            Environment.ExitCode = 2;
        }
        catch (SocketException exception)
        {
            Console.Error.WriteLine($"Network error: {exception.Message}");
            Environment.ExitCode = 1;
        }
        catch (InvalidOperationException exception)
        {
            Console.Error.WriteLine(exception.Message);
            Environment.ExitCode = 1;
        }
    }

    private static void RunHost(string username, string hostIp, int serverPort)
    {
        IPEndPoint hostEndpoint = new(IPAddress.Parse(hostIp), serverPort);
        ICommunicator communicator = CommunicatorFactory.CreateServerCommunicator(hostEndpoint);
        ControllerUser hostUser = new(username, string.Empty, null, hostIp, serverPort, isHost: true);
        using IControllerService controller = new HostController(hostUser, communicator);
        if (!controller.StartMeeting())
        {
            throw new InvalidOperationException("Could not start the Controller meeting.");
        }

        using ChatServer server = new(
            new Chatroom("console-chat", username),
            controller);
        Console.WriteLine(
            $"Controller meeting available at {controller.CurrentUser?.Ip}:{controller.ListenPort}.");

        server.OperationApplied += operation =>
        {
            if (operation is MessageAdded added)
            {
                Console.WriteLine($"{added.Message.SenderName}: {added.Message.Content}");
            }
        };

        RunChatInput(username, content =>
        {
            RoomResult result = server.SubmitHostRequest(new SendMessage(
                DateTimeOffset.UtcNow,
                new Mentions(),
                content,
                null));
            if (result != RoomResult.Ok)
            {
                Console.Error.WriteLine($"Could not send message: {result}");
            }
        });
    }

    private static void RunClient(
        string username,
        string hostIp,
        int serverPort)
    {
        using IControllerService controller = ControllerFactory.CreateClient(username);
        ControllerUser user = controller.CurrentUser
            ?? throw new InvalidOperationException("The Controller client has no current user.");

        if (!controller.JoinMeeting(hostIp, serverPort, user))
        {
            throw new SocketException((int)SocketError.ConnectionRefused);
        }

        IPEndPoint serverAddress = new(IPAddress.Parse(hostIp), serverPort);
        using ChatClient client = new(
            username,
            new Chatroom("console-chat", "host"),
            controller.Communicator
                ?? throw new InvalidOperationException("The Controller client has no communicator."),
            serverAddress);
        client.Sync();

        client.OperationApplied += operation =>
        {
            if (operation is MessageAdded added)
            {
                Console.WriteLine($"{added.Message.SenderName}: {added.Message.Content}");
            }
        };

        RunChatInput(client.Username, content => client.SendMessage(content, new Mentions()));
    }

    private static void RunChatInput(
        string username,
        Action<string> sendMessage)
    {
        Console.WriteLine($"Connected as {username}. Type a message; use /quit to leave.");
        while (Console.ReadLine() is string content)
        {
            if (string.Equals(content, "/quit", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            try
            {
                sendMessage(content);
            }
            catch (SocketException exception)
            {
                Console.Error.WriteLine($"Could not send message: {exception.Message}");
            }
            catch (InvalidOperationException exception)
            {
                Console.Error.WriteLine($"Could not send message: {exception.Message}");
            }
        }
    }

    private static int ParsePort(string value)
    {
        if (!int.TryParse(value, out int port) || port is < 1 or > 65535)
        {
            throw new ArgumentException($"'{value}' is not a valid TCP port.");
        }

        return port;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  Chat host <username> <bind-ip> <server-port>");
        Console.WriteLine("  Chat client <username> <host-ip> <server-port>");
        Console.WriteLine();
        Console.WriteLine("Use 127.0.0.1 for two terminals on one computer.");
        Console.WriteLine("For another computer, use the host's reachable IP address.");
    }
}
