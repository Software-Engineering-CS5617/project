namespace Whiteboard.Interfaces;

/// <summary>
/// Provides selection and transformation operations for whiteboard items.
/// </summary>
public interface ISelectionService
{
    /// <summary>
    /// Selects the topmost item at a point or clears the selection when no item is hit.
    /// </summary>
    /// <param name="point">Point at which to select an item.</param>
    /// <returns><see langword="true"/> when an item was selected; otherwise, <see langword="false"/>.</returns>
    bool SelectAtPoint(Point2D point);

    /// <summary>
    /// Moves the selected item or items by the specified offset.
    /// </summary>
    /// <param name="dx">Horizontal offset.</param>
    /// <param name="dy">Vertical offset.</param>
    /// <param name="modifiedBy">User performing the modification.</param>
    /// <returns><see langword="true"/> when the selection was moved; otherwise, <see langword="false"/>.</returns>
    bool MoveSelected(double dx, double dy, string modifiedBy = "system");

    /// <summary>
    /// Rotates the selected item or items by the specified angle.
    /// </summary>
    /// <param name="deltaAngle">Angle, in degrees, to add to the selected items.</param>
    /// <param name="modifiedBy">User performing the modification.</param>
    /// <returns><see langword="true"/> when the selection was rotated; otherwise, <see langword="false"/>.</returns>
    bool RotateSelected(double deltaAngle, string modifiedBy = "system");
}
