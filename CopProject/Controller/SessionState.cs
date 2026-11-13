namespace Controller;

// Represents the lifecycle state of a meeting session.
// Transitions: Created → Active → Ended.
public enum SessionState
{
    Created,
    Active,
    Ended
}
