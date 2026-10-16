using System;
namespace Whiteboard.Items;

public abstract class WhiteboardItem
{
    public Guid Id { get; } = Guid.NewGuid();

    public double X { get; set; }
    public double Y { get; set; }

    public double Width { get; set; }
    public double Height { get; set; }

    public double Rotation { get; set; }

    public double Scale { get; set; } = 1.0;
}
