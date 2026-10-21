using System;

namespace Whiteboard.Interfaces;

/// <summary>
/// Represents a user-performed action, for example, creation or deletion of items
/// </summary>
public interface IWhiteboardAction
{
    /// <summary>
    /// Unique identifier of the action
    /// </summary>
    Guid ActionId { get; set; }

    /// <summary>
    /// User who performed the action
    /// </summary>
    string UserId { get; set; }

    /// <summary>
    /// Date and time at which the action was performed
    /// </summary>
    DateTime ActionDateTime { get; set; }

    /// <summary>
    /// Performs the action on the given whiteboard
    /// </summary>
    /// <param name="whiteboard">Whiteboard to perform action upon</param>
    void Perform(IWhiteboard whiteboard);

    /// <summary>
    /// Creates an inverse action, used for undo and redo operations
    /// </summary>
    /// <returns>The inverse action</returns>
    IWhiteboardAction CreateInverse();
}
