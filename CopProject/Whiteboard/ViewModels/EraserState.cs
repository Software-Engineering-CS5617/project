// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Whiteboard.Client;
using Whiteboard.Interfaces;

namespace Whiteboard.ViewModels;

public class EraserState : IViewModelState
{
    private readonly IWhiteboardClient _whiteboardClient;

    public EraserState(IWhiteboardClient whiteboardClient)
    {
        ArgumentNullException.ThrowIfNull(whiteboardClient);

        _whiteboardClient = whiteboardClient;
    }

    public void PointerPressed(Point2D point)
    {
        IItem? item = _whiteboardClient.Canvas.HitTestTopmost(new Point2D(point.X, point.Y));
        if (item is null)
        {
            return;
        }

        _whiteboardClient.DeleteItem(item.Id);
    }

    public void PointerMoved(Point2D point)
    {
    }

    public void PointerReleased(Point2D point)
    {
    }

    public void PointerCaptureLost(Point2D point)
    {
    }
}
