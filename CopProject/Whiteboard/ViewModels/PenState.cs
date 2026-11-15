/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Whiteboard.Client;
using Whiteboard.Interfaces;
using Whiteboard.Items;
using Whiteboard.Services;

namespace Whiteboard.ViewModels;

/// <summary>
/// Represents the state when the Pen tool is selected
/// </summary>
/// <inheritdoc/>
public class PenState : ViewModelStateBase
{
    private readonly IShapeRecognitionService? _shapeRecognitionService;

    /// <summary>
    /// Active polyline
    /// </summary>
    private IFreehand? _freehand;

    public PenState(
        IWhiteboardClient whiteboardClient,
        ICanvasPreview preview,
        ToolbarInfo toolbarInfo,
        IShapeRecognitionService? shapeRecognitionService = null) : base(whiteboardClient, preview, toolbarInfo)
    {
        _shapeRecognitionService = shapeRecognitionService;
    }

    public override void PointerPressed(Point2D point)
    {
        // Set flag to begin drawing
        _isDrawing = true;

        // Create a new polyline with brush and fill properties taken from the toolbar
        _freehand = new FreehandItem() {
            CreatedBy = WhiteboardClient.UserId,

            StrokeColor = ToolbarInfo.BorderColor,
            StrokeWidth = ToolbarInfo.BorderWidth,

            // Begin the polyline with the first point
            Points = [point],

            // Add the Z index information
            Zindex = GetNextZIndex()
        };

        // Preview the drawing
        Preview.ShowPreview(_freehand);
    }

    public override void PointerMoved(Point2D point)
    {
        // Return early if the user is not drawing
        if (!_isDrawing || _freehand is null)
        {
            return;
        }

        // Add the current cursor point to the polyline
        _freehand.Points.Add(point);

        // Preview the drawing
        Preview.ShowPreview(_freehand);
    }

    public override void PointerReleased(Point2D point)
    {
        // Return early if the user is not drawing
        if (!_isDrawing || _freehand is null)
        {
            return;
        }

        // Unset flag to stop drawing
        _isDrawing = false;

        // Stop preview
        Preview.ClearPreview();

        // Send to whiteboard client state manager off the UI thread
        if (_freehand.Points.Count >= 2)
        {
            IFreehand item = _freehand;
            Task.Run(async () => {
                WhiteboardClient.AddItem(item);

                if (_shapeRecognitionService?.IsEnabled == true)
                {
                    try
                    {
                        IItem? recognizedShape = await _shapeRecognitionService.TryRecognizeShapeAsync(item).ConfigureAwait(false);
                        if (recognizedShape is not null)
                        {
                            WhiteboardClient.DeleteItem(item.Id);
                            WhiteboardClient.AddItem(recognizedShape);
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceWarning($"[Whiteboard] Auto shape recognition failed: {ex.Message}");
                    }
                }
            });
        }

        // Reset polyline state back to null
        _freehand = null;
    }

    public override void PointerCaptureLost(Point2D point)
    {
        // Return early if the user is not drawing
        if (!_isDrawing || _freehand is null)
        {
            return;
        }

        // Unset flag to stop drawing
        _isDrawing = false;

        // Stop preview
        Preview.ClearPreview();

        // Reset polyline state back to null
        _freehand = null;
    }
}
