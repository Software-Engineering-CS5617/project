/// Owners  : Raagam Hitesh Parmar 
/// Roll No : 112301024
/// LICENSE : MIT

using System;
using System.Diagnostics.CodeAnalysis;

namespace Whiteboard.ViewModels;

/// <summary>
/// Information to show when the user hovers over a whiteboard item
/// </summary>
/// <param name="CreatedBy">Name of the user who created the object</param>
/// <param name="CreatedDateTime">Time of creation</param>
[ExcludeFromCodeCoverage]
public record ToolTip(
    string CreatedBy,
    DateTime CreatedDateTime
);
