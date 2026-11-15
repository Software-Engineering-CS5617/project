//owner : U Sai Vignesh
//roll number : 112301034
// License : MIT
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
    /// <returns><see langword="true"/> when the selection was moved; otherwise, <see langword="false"/>.</returns>
    bool MoveSelected(double dx, double dy);

    /// <summary>
    /// Resizes the selected item from its top-left anchor to the supplied pointer position.
    /// </summary>
    /// <param name="originalItem">The item state captured when resizing started.</param>
    /// <param name="pointer">The current bottom-right handle position.</param>
    /// <param name="modifiedBy">User performing the modification.</param>
    /// <returns><see langword="true"/> when the selection was resized; otherwise, <see langword="false"/>.</returns>
    bool ResizeSelected(IItem originalItem, Point2D pointer, string modifiedBy = "system");

    /// <summary>
    /// Rotates the selected item or items by the specified angle.
    /// </summary>
    /// <param name="deltaAngle">Angle, in degrees, to add to the selected items.</param>
    /// <returns><see langword="true"/> when the selection was rotated; otherwise, <see langword="false"/>.</returns>
    bool RotateSelected(double deltaAngle);
}
