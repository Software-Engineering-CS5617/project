> [!NOTE]
> The interface is not finalized. Please expect changes and added details soon.
> Please communicate with the team representative for more information.

# Overview

The whiteboard is composed of a bunch of whiteboard items. A whiteboard item 
represents a stroke, a shape, line, point, etc. It has properties like
1. Position
2. Orientation
3. Stroke Width
4. Stroke Color
5. Fill Color (only for Shape)

```C#
interface IWhiteboardItem { ... }
interface IShape { ... }
interface IStroke { ... }
interface IWhiteboard { ... }
interface IColorable { ... }
```

Since multiple users may use the whiteboard at a time, each one of them is given a
username. Every whiteboard interaction has an associated username.

```C#
interface IUsername = { ... }
```

Almost all operations are tracked by the modification stack. Modifications done by
user X can be undone and redone only by user X.

# Whiteboard Item Operations

```C#
// Draw `item` on the whiteboard, and associate it with `user`
void drawItem(IUsername user, IWhiteboardItem item);

// Get the user who drew this item on the board
IUsername getDrawer();

// Get all items within the bounds of the given rectangular region
List<IWhiteboardItem> drawnInRectangularRegion(Point corner1, Point corner2);

// Get all items within a radius `r` of the given point
List<IWhiteboardItem> drawnInCircularRegion(Point center, int radius);

// Change fill color of a whiteboard item
// implemented only by IColorable
void changeFillColor(Color color);

// Change border properties of a whiteboard item
void changeBorderColor(Color color);
void changeBorderThickness(int thickness);

// Rotate an item
void rotate(int angle);

// Scale an item
void scale(double factor);

// Delete an item
void delete();

// Clear the canvas
void clearCanvas();

// Can one step of undoing be taken for user X?
bool canUndo(IUsername user);
// Undo one step, doesn't do anything if there is nothing to undo
void undo(IUsername user);

// Can one step of redoing be taken for user X?
bool canRedo(IUsername user);
// redo one step, doesn't do anything if there is nothing to redo
void redo(IUsername user);
```

# Interfacing with insights

There are two ways of determining the shape of a free-handed drawn stroke
1. Deterministic algorithm
2. Using ML or LLM

We will be providing both options, defaulting to (1).

```C#
enum shapeIdentifier = { ... }

// identify shape using the given identifier
IShape identify(IStroke stroke, shapeIdentifier identifier);
```
