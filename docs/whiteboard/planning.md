# Planning

## UML Diagrams

```mermaid
classDiagram

class IItem {
  <<interface>>
  + Guid Id
  + double ZIndex
  + string CreatedBy
  + DateTime CreatedDateTime
  + string LastModifiedBy
  + DateTime LastModifiedDateTime

  + double X
  + double Y
  + double Orientation
  + Rect2D BoundingBox
  + bool HitText(Point2D point)

%% Memento as well
}

class ITextbox {
  <<interface>>
  + string Content
  + string FontFace
  + int FontSize
  + Color Color
}

class IShape {
  <<interface>>
  + Color FillColor
  + Color BorderColor
  + double BorderWidth
}

class ICircle {
  <<interface>>
}

class ISquare {
  <<interface>>
}

class IRectangle {
  <<interface>>
}

class ITriangle {
  <<interface>>
}

class IEllipse {
  <<interface>>
}

class IPolygon {
  <<interface>>
}

class ILine {
  <<interface>>
  + Point2D Endpoint1
  + Point2D Endpoint2
}

class IFreehand {
  <<interface>>
  + List<Point2D> Points
  + Color StrokeColor
  + double StrokeWidth
}

class Textbox { }
class Circle {}
class Rectangle { }
class Square { }
class Triangle { }
class Ellipse { }
class Polygon { }
class Line { }
class Freehand { }

IItem <|-- ITextbox
IItem <|-- IShape
IItem <|-- ILine
IItem <|-- IFreehand

ITextbox <|.. Textbox

IShape <|-- ICircle
IShape <|-- ISquare
IShape <|-- IRectangle
IShape <|-- ITriangle
IShape <|-- IEllipse
IShape <|-- IPolygon

ICircle <|.. Circle
ISquare <|.. Square
IRectangle <|.. Rectangle
ITriangle <|.. Triangle
IEllipse <|.. Ellipse
IPolygon <|.. Polygon

ILine <|.. Line

IFreehand <|.. Freehand
```

```mermaid
classDiagram
class IWhiteboard {
  <<interface>>
  + SomeSortOfCollection<IItem> Items
  + void AddItem(IItem item)
  + void DeleteItem(Guid guid)
%% and more
}

class IWhiteboardAction {
  <<interface>>
  + Guid ActionId
  + string UserId
  + DataTime ActionDateTime

  + void Perform(IWhiteboard whiteboard)
  + IWhiteboardAction CreateInverse()
}
```

## Details

### Models

1. x and y coordinates of every shape are stored in `IItem`.
   Shapes will store information relative to these coordinates.
   When moving a shape, only its x and y coordinates will change.
   This is import, for example, in case of polyline, where we need
   to store the corner coordiantes, which will be relative to these
   x and y coordinates and will not change.
2. Whiteboard is a dictionary from `Guid` to `IItem`

### Taking Freehand input

1. Using [`InkCanvas`](https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.inkcanvas?view=winrt-28000) to take user input
2. Using [`InkPresenter`](https://learn.microsoft.com/en-us/dotnet/api/system.windows.controls.inkpresenter?view=windowsdesktop-10.0) to
   statically render strokes
3. Proposed chain of events for strokes and its interaction with whiteboard

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant InkCanvas
    participant InkPresenter
    participant Subscriber as Event Subscriber
    participant Whiteboard as Whiteboard State

    User->>InkCanvas: Draws stroke with mouse
    InkCanvas->>InkPresenter: Forwards input for live rendering
    InkPresenter->>Subscriber: Fires StrokesCollected event
    Subscriber->>Whiteboard: Adds collected stroke
    Subscriber->>User: Displays shape recognition and fix options
```

### Views

1. Using `Microsoft.UI.XAML.Shapes` [namespace](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.shapes?view=windows-app-sdk-2.0)
2. The following views should be there:
   1. All class implementing `IItem`
   2. Toolboxes and its various commands with icons
   3. Hover-tooltips
   4. Restore-buffer

## Further References

1. [How Figma’s multiplayer technology works](https://www.figma.com/blog/how-figmas-multiplayer-technology-works/)
2. [Creating chromeless, non-rectangular Windows from UWP](https://stefanwick.com/tag/desktop-extensions/)
