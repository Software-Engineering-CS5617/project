/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using System;
using Whiteboard.Client;
using Whiteboard.Interfaces;

namespace Whiteboard.ViewModels;

/// <summary>
/// Abstract base class for view model states
/// </summary>
public abstract class ViewModelStateBase : IViewModelState
{
    /// <summary>
    /// The whiteboard client
    /// </summary>
    protected readonly IWhiteboardClient WhiteboardClient;

    /// <summary>
    /// Canvas preview
    /// </summary>
    protected readonly ICanvasPreview Preview;

    /// <summary>
    /// Toolbar information
    /// </summary>
    protected readonly ToolbarInfo ToolbarInfo;

    /// <summary>
    /// Check if the user is currently drawing on screen
    /// </summary>
    protected bool _isDrawing = false;

    protected ViewModelStateBase(IWhiteboardClient whiteboardClient, ICanvasPreview preview, ToolbarInfo toolbarInfo)
    {
        ArgumentNullException.ThrowIfNull(whiteboardClient);
        ArgumentNullException.ThrowIfNull(preview);
        ArgumentNullException.ThrowIfNull(toolbarInfo);

        WhiteboardClient = whiteboardClient;
        Preview = preview;
        ToolbarInfo = toolbarInfo;
    }

    /// <summary>
    /// Computes the next topmost Z-index above all current items on the whiteboard.
    /// </summary>
    protected double GetNextZIndex()
    {
        double maxZ = 0;

        // flag to return early from calculating the Z index
        bool hasItems = false;

        foreach (IItem item in WhiteboardClient.Canvas.Items.Values)
        {
            if (!hasItems || item.Zindex > maxZ)
            {
                maxZ = item.Zindex;
            }

            hasItems = true;
        }

        return hasItems ? maxZ + 1 : 0;
    }

    /// <inheritdoc/>
    public abstract void PointerPressed(Point2D point);

    /// <inheritdoc/>
    public abstract void PointerMoved(Point2D point);

    /// <inheritdoc/>
    public abstract void PointerReleased(Point2D point);

    /// <inheritdoc/>
    public abstract void PointerCaptureLost(Point2D point);
}
