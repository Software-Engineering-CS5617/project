using System.Net;
using ScreenShare;

namespace Screenshare.Tests;

public class HelloTests
{
    [Fact]
   public void Hello_StoreViewer()
    {
        Participant viewer = new("Sumit", new IPEndPoint(IPAddress.Loopback, 5000));
        Hello hello = new Hello(viewer);

        Assert.Same(viewer, hello.Viewer);
    }

    [Fact]
    public void Hello_RejectsNullViewer()
    {
        Assert.Throws<ArgumentNullException>(() => new Hello(null!));
    }

    [Fact]
    public void Hello_HasKind()
    {
        Participant viewer = new("Sumit", new IPEndPoint(IPAddress.Loopback, 5000));
        Hello hello = new(viewer);

        Assert.Equal(ScreenShareMessageKind.Hello, hello.Kind);
    }



}
