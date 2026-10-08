using Xunit;
namespace Chat.Test;

public class HelloWorldTests
{
    [Fact]
    public void HelloWorld()
    {
        Assert.Equal("Hello, World!", new HelloWorld().Greeting(false));
        Assert.Equal("HELLO, WORLD!", new HelloWorld().Greeting(true));
    }
}
