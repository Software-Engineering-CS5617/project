/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using Whiteboard.Interfaces;

namespace Whiteboard.ViewModels;

/// <summary>
/// Represents the current state of the view model, which is dependent on the currently chosen tool
/// </summary>
public interface IViewModelState
{
    /// <summary>
    /// Whether a pointer press that lands on a rendered textbox should be handled by this state.
    /// When false, the press is left to the textbox (e.g. so the user can type into it).
    /// </summary>
    bool HandlesPointerOnTextbox { get; }

    /// <summary>
    /// Called every time the pointer is pressed on the canvas
    /// </summary>
    void PointerPressed(Point2D point);

    /// <summary>
    /// Called every time the captured pointer is moved on the canvas
    /// </summary>
    void PointerMoved(Point2D point);

    /// <summary>
    /// Called every time the captured pointer is released on the canvas
    /// </summary>
    void PointerReleased(Point2D point);

    /// <summary>
    /// Called every time the captured pointer is lost
    /// </summary>
    void PointerCaptureLost(Point2D point);
}
