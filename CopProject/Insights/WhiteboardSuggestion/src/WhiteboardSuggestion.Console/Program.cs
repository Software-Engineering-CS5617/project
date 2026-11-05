using System.Text;
using WhiteboardSuggestion.Core;

Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine("===============================================");
Console.WriteLine("       WHITEBOARD AI SUGGESTION DEMO");
Console.WriteLine("===============================================");
Console.WriteLine("Suggest a clean shape, letter, or digit from a rough drawing.");
Console.WriteLine();

string? apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine("Setup required: GEMINI_API_KEY is not set.");
    Console.WriteLine("Set the environment variable and restart this console.");
    return;
}

IShapeSuggestionService suggestionService = new GeminiShapeSuggestionService(apiKey);

while (true)
{
    Console.WriteLine("1. Suggest from an image");
    Console.WriteLine("2. Exit");
    Console.Write("Choose an option: ");
    string? choice = Console.ReadLine();
    Console.WriteLine();

    if (string.Equals(choice, "2", StringComparison.OrdinalIgnoreCase)
        || string.Equals(choice, "exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    if (choice != "1")
    {
        Console.WriteLine("Please choose 1 or 2.");
        Console.WriteLine();
        continue;
    }

    Console.Write("Image path (you can drag an image file into this window): ");
    string imagePath = (Console.ReadLine() ?? string.Empty).Trim().Trim('"');

    if (string.IsNullOrWhiteSpace(imagePath))
    {
        Console.WriteLine("No image path was entered.");
        Console.WriteLine();
        continue;
    }

    try
    {
        if (!File.Exists(imagePath))
        {
            Console.WriteLine("That file could not be found. Check the path and try again.");
            Console.WriteLine();
            continue;
        }

        string mimeType = ImageMimeTypeResolver.GetMimeType(imagePath);
        byte[] imageBytes = await File.ReadAllBytesAsync(imagePath);

        Console.WriteLine();
        Console.WriteLine("Analysing drawing...");
        ShapeSuggestion suggestion = await suggestionService.SuggestAsync(imageBytes, mimeType);

        Console.WriteLine();
        Console.WriteLine("AI SUGGESTION");
        Console.WriteLine("-------------");
        Console.WriteLine(ShapeSuggestionFormatter.Format(suggestion));
        Console.WriteLine();
    }
    catch (NotSupportedException exception)
    {
        Console.WriteLine(exception.Message);
        Console.WriteLine();
    }
    catch (ArgumentException exception)
    {
        Console.WriteLine($"Input problem: {exception.Message}");
        Console.WriteLine();
    }
    catch (Exception exception)
    {
        Console.WriteLine("Suggestion could not be generated.");
        Console.WriteLine($"Details: {exception.Message}");
        Console.WriteLine("Check the internet connection, API key, and configured Gemini model.");
        Console.WriteLine();
    }
}

Console.WriteLine("Demo ended.");
