/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using System.Threading;
using System.Threading.Tasks;
using Whiteboard.Interfaces;

namespace Whiteboard.Services;

/// <summary>
/// Service that recognizes a freehand stroke drawing and converts it into a geometric item
/// </summary>
public interface IShapeRecognitionService
{
    /// <summary>
    /// Gets or sets whether automatic shape recognition is enabled
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the minimum confidence required to replace a freehand stroke (between 0.0 and 1.0)
    /// </summary>
    double ConfidenceThreshold { get; set; }

    /// <summary>
    /// Attempts to recognize the given freehand stroke and convert it into a geometric whiteboard item.
    /// Returns null if recognition fails, is disabled, or produces an unsupported/low-confidence shape.
    /// </summary>
    Task<IItem?> TryRecognizeShapeAsync(IFreehand freehand, CancellationToken cancellationToken = default);
}
