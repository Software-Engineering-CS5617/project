using System.Diagnostics;
using System.Text;
using Networking;

namespace Controller;

// Receives messages from the Networking module and updates the SessionManager accordingly.
// Single Responsibility: Only translates network messages into SessionManager calls.
// Open-Closed: Add new message types by adding a case — no existing code changes.
public class SessionMessageListener : IMessageListener
{
    private readonly SessionManager _sessionManager;

    public SessionMessageListener(SessionManager sessionManager)
    {
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
    }

    public void OnMessageReceived(byte[] message)
    {
        if (message == null || message.Length == 0)
        {
            Trace.TraceWarning($"Controller received empty message.");
            return;
        }

        string rawMessage = Encoding.UTF8.GetString(message);
        if (string.IsNullOrEmpty(rawMessage))
        {
            Trace.TraceWarning($"Controller received empty message.");
            return;
        }

        string[] parts = rawMessage.Split(':');
        if (parts.Length < 2)
        {
            Trace.TraceWarning($"Controller received malformed message: {rawMessage}");
            return;
        }

        string type = parts[0];

        switch (type)
        {
            case Protocol.Join:
                HandleJoin(parts);
                break;

            case Protocol.Leave:
                HandleLeave(parts);
                break;

            case Protocol.Start:
                HandleStart(parts);
                break;

            case Protocol.End:
                HandleEnd(parts);
                break;

            default:
                break;
        }
    }

    /// <summary>
    /// Handles JOIN messages.
    /// Format: "JOIN:userId:ip:port"
    /// </summary>
    private void HandleJoin(string[] parts)
    {
        string userId;
        string ip;
        int port;

        if (parts.Length >= 4)
        {
            // Format: JOIN:userId:ip:port
            userId = parts[1];
            ip = parts[2];
            port = int.TryParse(parts[3], out int p) ? p : 0;
        }
        else
        {
            Trace.TraceWarning($"JOIN message has too few fields: {string.Join(':', parts)}");
            return;
        }

        if (!_sessionManager.HasActiveSession())
        {
            Trace.TraceWarning($"Join failed for {userId}: no active session on this host.");
            return;
        }

        var peer = new PeerInfo(userId, ip, port);
        bool joined = _sessionManager.JoinSession(peer);

        if (joined)
            Trace.TraceInformation($"{userId} joined session from {ip}:{port}");
        else
            Trace.TraceWarning($"Join failed for {userId} (session not found or ended)");
    }

    // Handles LEAVE messages.Format: "LEAVE:userId"
    private void HandleLeave(string[] parts)
    {
        string userId;

        if (parts.Length >= 2)
        {
            // Format: LEAVE:userId
            userId = parts[1];
        }
        else
        {
            Trace.TraceWarning($"LEAVE message has too few fields: {string.Join(':', parts)}");
            return;
        }

        if (!_sessionManager.HasActiveSession())
        {
            Trace.TraceWarning($"Leave failed for {userId}: no active session on this host.");
            return;
        }

        bool left = _sessionManager.LeaveSession(userId);

        if (left)
            Trace.TraceInformation($"{userId} left session");
        else
            Trace.TraceWarning($"Leave failed for {userId}");
    }

    private void HandleStart(string[] parts)
    {
        ParsedMessage? parsed = Protocol.Parse(string.Join(':', parts));
        if (parsed == null) return;

        bool started = _sessionManager.StartSession(parsed.UserId);

        if (started)
            Trace.TraceInformation($"Session started by {parsed.UserId}");
        else
            Trace.TraceWarning($"Start failed by {parsed.UserId}");
    }

    private void HandleEnd(string[] parts)
    {
        ParsedMessage? parsed = Protocol.Parse(string.Join(':', parts));
        if (parsed == null) return;

        bool ended = _sessionManager.EndSession(parsed.UserId);

        if (ended)
            Trace.TraceInformation($"Session ended by {parsed.UserId}");
        else
            Trace.TraceWarning($"End failed by {parsed.UserId}");
    }
}
