using System.Diagnostics;
using Controller;

// ── Thin test harness — no business logic here ───────────────────────
Trace.Listeners.Add(new ConsoleTraceListener());

Console.WriteLine("=== Controller-Networking Test Harness ===");
Console.WriteLine("Choose role: [H]ost or [C]lient?");

string? role = Console.ReadLine()?.Trim().ToUpperInvariant();

if (role == "H")
{
    RunHost();
}
else if (role == "C")
{
    RunClient();
}
else
{
    Console.WriteLine("Invalid choice. Exiting.");
}

// ── Host flow ────────────────────────────────────────────────────────
void RunHost()
{
    Console.Write("Enter your host user ID: ");
    string hostUser = Console.ReadLine()?.Trim() ?? "host";

    using IControllerService host = ControllerFactory.CreateHost(hostUser);

    // Subscribe to events — this is what the UX team will do
    host.UserJoined += (_, e) => Console.WriteLine($"  + {e.UserId} joined (total: {e.NewCount})");
    host.UserLeft += (_, e) => Console.WriteLine($"  - {e.UserId} left (total: {e.NewCount})");
    host.CountChanged += (_, e) => Console.WriteLine($"  ~ {e.NewCount} user(s) in session");
    host.SessionStarted += (_, e) => Console.WriteLine($"  ● Meeting is live");
    host.SessionEnded += (_, e) => Console.WriteLine($"  ■ Meeting ended");

    Console.WriteLine($"Listening on port {host.ListenPort}");
    Console.WriteLine("Commands: start | end | count | users | quit");

    while (true)
    {
        Console.Write("> ");
        string? input = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(input)) continue;

        string command = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[0].ToLowerInvariant();

        switch (command)
        {
            case "start":
                bool started = host.StartMeeting();
                Console.WriteLine(started ? "Meeting started." : "Failed to start meeting.");
                break;

            case "end":
                bool ended = host.EndMeeting();
                Console.WriteLine(ended ? "Meeting ended." : "Failed to end meeting.");
                break;

            case "count":
                Console.WriteLine($"{host.GetActiveUserCount()} user(s) online");
                break;

            case "users":
                List<string> users = host.GetActiveUsers();
                Console.WriteLine($"{users.Count} user(s): {string.Join(", ", users)}");
                break;

            case "quit":
                return;

            default:
                Console.WriteLine("Unknown command. Try: start, end, count, users, quit");
                break;
        }
    }
}

// ── Client flow ──────────────────────────────────────────────────────
void RunClient()
{
    Console.Write("Enter your user ID: ");
    string userId = Console.ReadLine()?.Trim() ?? "user";

    using IControllerService client = ControllerFactory.CreateClient(userId);

    // Subscribe to events — UX team does this
    client.UserJoined += (_, e) => Console.WriteLine($"  + {e.UserId} joined (total: {e.NewCount})");
    client.UserLeft += (_, e) => Console.WriteLine($"  - {e.UserId} left (total: {e.NewCount})");
    client.SessionEnded += (_, e) => Console.WriteLine($"  ■ Meeting ended");

    Console.WriteLine($"Client listening on port {client.ListenPort}");
    Console.WriteLine("Commands: join <hostIp> <hostPort> | leave | users | quit");

    while (true)
    {
        Console.Write("> ");
        string? input = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(input)) continue;

        string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string command = parts[0].ToLowerInvariant();

        switch (command)
        {
            case "join" when parts.Length >= 3:
                client.JoinMeeting(parts[1], int.Parse(parts[2]), userId);
                Console.WriteLine($"Connecting to {parts[1]}:{parts[2]}...");
                break;

            case "leave":
                if (client is ClientController ccLeave && !ccLeave.IsConnected)
                {
                    Console.WriteLine("You're not in a meeting.");
                    break;
                }
                client.LeaveMeeting(userId);
                Console.WriteLine("Left the meeting.");
                break;

            case "users":
                if (client is ClientController ccUsersList && !ccUsersList.IsConnected)
                {
                    Console.WriteLine("You're not in a meeting.");
                }
                else
                {
                    List<string> users = client.GetActiveUsers();
                    Console.WriteLine($"{users.Count} user(s): {string.Join(", ", users)}");
                }
                break;

            case "count":
                if (client is ClientController ccCount && !ccCount.IsConnected)
                {
                    Console.WriteLine("You're not in a meeting.");
                }
                else
                {
                    Console.WriteLine($"{client.GetActiveUserCount()} user(s) online");
                }
                break;

            case "quit":
                return;

            default:
                Console.WriteLine("Usage: join <hostIp> <hostPort> | leave | users | quit");
                break;
        }
    }
}
