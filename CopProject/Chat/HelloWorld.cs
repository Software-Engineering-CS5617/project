namespace Chat;

public class HelloWorld
{
    public string Greeting(bool uppercase)
    {
        if (uppercase == true)
        {
            return "HELLO, WORLD!";
        }

        return "Hello, World!";
    }
}
