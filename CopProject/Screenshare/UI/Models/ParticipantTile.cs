using System.Runtime.InteropServices.WindowsRuntime;   // AsBuffer()
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;

namespace ScreenShare.Models;

public class ParticipantTile
{
    public string Id { get; }
    public string Name { get; }
    public BitmapImage Frame { get; } = new();   // must be created on the UI thread
    private bool _decoding;

    public ParticipantTile(string id, string name) { Id = id; Name = name; }

    // Call on the UI thread. Drops the frame if the previous one is still decoding.
    public async void UpdateFrame(byte[] encodedImage)
    {
        if (_decoding)
        {
            return;
        }

        _decoding = true;
        try
        {
            using var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(encodedImage.AsBuffer());
            stream.Seek(0);
            await Frame.SetSourceAsync(stream);
        }
        catch { /* corrupt frame: skip it */ }
        finally { _decoding = false; }
    }
}
