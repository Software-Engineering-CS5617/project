// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Whiteboard.Interfaces;

namespace Whiteboard.Items;

/// <inheritdoc/>
public abstract class Item : IItem
{
    public Guid Id { get; set; } = new();
    public double Zindex { get; set; } = 0;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedDateTime { get; set; } = DateTime.Now;
    public string LastModifiedBy { get; set; } = string.Empty;
    public DateTime LastModifiedDateTime { get; set; } = DateTime.Now;
    public double Left { get; set; } = 0;
    public double Top { get; set; } = 0;
    public double Orientation { get; set; } = 0;
    public abstract Rect2D BoundingBox { get; set; }
    public abstract bool HitTest(Point2D point);
}
