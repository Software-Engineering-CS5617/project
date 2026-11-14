using System.Drawing;

namespace ScreenShare;

public interface IImageDiffer
{
    ImageDiffResult Compare(Bitmap previousFrame, Bitmap currentFrame);
}
