// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Windows.UI;

namespace Whiteboard.Interfaces;

/// <summary>
/// Represents a textbox whiteboard item
/// </summary>
public interface ITextbox : IItem
{
    /// <summary>
    /// Content of the textbox
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// FontFace of the content
    /// </summary>
    public string FontFace { get; set; }

    /// <summary>
    /// Font size of the content
    /// </summary>
    public int FontSize { get; set; }

    /// <summary>
    /// Font color of the content
    /// </summary>
    public Color FontColor { get; set; }
}
