// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Cloud.Networking;

public class Program
{
    // lock on shared console
    private static readonly object s_lock = new();

    // unsent text typed by user
    private static readonly StringBuilder s_input = new();

    private static async Task Main(string[] args)
    {
        // Do: dotnet run -- http://localhost:5038/hub [or https://localhost:7279/hub]
        string url = args.Length > 0 ? args[0] : "http://localhost:5038/hub";

        await using SignalRMessenger messenger = new(url);

        // register all the subscribers
        messenger.MessageReceived += m => Print($"[{m.Timestamp.ToLocalTime():HH:mm:ss}] {m.SenderId}: {m.Content}");
        messenger.Reconnecting += _ => Print("-- connection lost, reconnecting...");
        messenger.Reconnected += _ => Print("-- reconnected");
        messenger.Closed += e => Print(e is null ? "-- disconnected" : $"-- connection closed: {e.Message}");

        // continue even if connect failed
        try
        {
            await messenger.ConnectAsync();
            Print($"Connected to {url}.");
        }
        catch (Exception ex)
        {
            Print($"Could not connect to {url}: {ex.Message}");
        }

        Print("Type a non-blank message and press Enter. Type /c to connect (again), /q or ^C to exit.");

        while (true)
        {
            string line = ReadLine();

            // graceful cancel
            if (line == "/q")
            {
                break;
            }

            // continue even if connection failed
            if (line == "/c")
            {
                try
                {
                    await messenger.ConnectAsync();
                    Print("-- connected");
                }
                catch (Exception ex)
                {
                    Print($"-- connection failed: {ex.Message}");
                }

                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            // continue even if send failed
            try
            {
                await messenger.SendMessageAsync(line);
            }
            catch (Exception ex)
            {
                Print($"-- send failed: {ex.Message}");
            }
        }

        // just disconnect not dispose
        await messenger.DisconnectAsync();
    }

    // reads one line key by key
    // (to aloow typed text to come at bottom)
    private static string ReadLine()
    {
        lock (s_lock)
        {
            ClearLine();
            Console.Write("> " + s_input);
        }

        while (true)
        {
            // read key by key and keep on rewriting (clear, write)
            ConsoleKeyInfo key = Console.ReadKey(intercept: true);

            lock (s_lock)
            {
                if (key.Key == ConsoleKey.Enter)
                {
                    string line = s_input.ToString();
                    s_input.Clear();
                    Console.WriteLine();
                    return line;
                }

                if (key.Key == ConsoleKey.Backspace)
                {
                    if (s_input.Length > 0)
                    {
                        s_input.Length--;
                    }
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    s_input.Append(key.KeyChar);
                }

                ClearLine();
                Console.Write("> " + s_input);
            }
        }
    }

    private static void Print(string text)
    {
        lock (s_lock)
        {
            // first clear, write text, move to next line, clear, write unsent text
            ClearLine();
            Console.WriteLine(text);
            ClearLine();
            Console.Write("> " + s_input);
        }
    }

    private static void ClearLine()
    {
        // move to beginning , overwrite and move to beginning
        Console.Write('\r' + new string(' ', Math.Max(0, Console.WindowWidth - 1)) + '\r');
    }
}
