# Whiteboard Specification

Inside an active meeting/session, any one of the connected users can begin
sharing a whiteboard. At any time, at most one whiteboard would be active.
Only the user who begins sharing the whiteboard will be able to close the
whiteboard. In tiled view, the whiteboard will appear alongside any other
screen-sharing tiles. Users can click on the whiteboard tile to interact
with the whiteboard.

We will be using Windows Ink framework. Details for the same can be found
[here](https://learn.microsoft.com/en-us/dotnet/api/system.windows.ink).

## Features

### Pen

*Allows users to draw on the whiteboard*

The pen uses vector-based strokes, as specified by Windows Ink
documentation. Users will be able to vary the pen stroke width.

### Colors

*Allows users to change color of whiteboard items*

> [!NOTE]
> Stretch Goal - Support for full RGB color palette

### Eraser

*Allows users to erase items from the whiteboard*

Users will be able to erase part of the pen strokes (similar to pixel
erasers), or complete stroke objects (object erasers).

### Text

*Allows users to write text on the screen*

> [!NOTE]
> Stretch Goal - Support for multiple font faces

### Shapes

*Allows users to draw shapes like circles and polygons on the screen*

If a shape is drawn in a single stroke, and the pen is held down for more
than a few seconds, the system will attempt to recognise the shape and replace
it with a standard shape. Two modes will be available for achieving the same:

- Deterministic Algorithm for shape recognition (default)
- LLM-based shape recognition

### Selection

*Allows users to select items from the whiteboard and perform _actions_*

Actions supported after selection:

- Changing orientation
- Changing scale
- Changing position

### Modification Stack

*Allows users to delete whiteboard items, as well as undo and redo operations*

Each connected user will have his/her local modification stack. Hence, history
of operations is distinct for each user.

### Export

*Allows users to export/save the whiteboard for later use*

The whiteboard will be exported as a `.png` or a `.jpeg` file, to be saved
locally or on the cloud.

> [!NOTE]
> Stretch Goal - Custom format for exporting whiteboard to allow modifications
> at a later time.

## Other Stretch Goals

### Zoom

*Allows users to zoom in and out of the whiteboard*

### Pan

*Allows users to move the whiteboard and all of its contents*

### Snapping

*Allows user cursor to snap to nearby objects like corner, edge, centroid, etc.*

### Collaboration Tracking

*Allows users to see who created a stroke/shape by showing a small bubble next*
*to it for a short time, similar to how it works on Google Office Suites*

### Importing Images

*Allows users to import images into the whiteboard*

Users will be able to import images from their local machine, or directly from an
existing screen/camera share.
