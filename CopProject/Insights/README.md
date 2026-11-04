# Whiteboard AI Suggestion Demo

This package includes both the original console proof-of-concept and a clickable Windows UI demo for the whiteboard AI suggestion flow.

## What the interactive overlay does

1. Draw one rough shape, letter, or digit directly on the whiteboard canvas with the mouse.
2. Click **Suggest correction**. Only the current rough strokes are cropped to a clean PNG and sent to Gemini.
3. A floating suggestion overlay appears on the board with the best match, alternatives, confidence estimate, and a short explanation.
4. Click a candidate such as **Circle**, **Oval**, **Letter M**, or **Digit 8**. The rough strokes are replaced by a clean vector shape or text glyph in the same area.
5. Continue drawing or click **Clear board** to start over.

The overlay is a real clickable control—not just AI text printed on screen. The UI calls the reusable `IShapeSuggestionService` and `ShapeSuggestionCandidateFactory`; the WPF-specific panel does not contain Gemini request/parsing logic.

## Projects

- `WhiteboardSuggestion.Core`: reusable class library with `IShapeSuggestionService`, Gemini integration, JSON response parsing, formatting, MIME type handling, and framework-independent candidate creation.
- `WhiteboardSuggestion.Console`: the original image-file console demo.
- `WhiteboardSuggestion.WpfDemo`: interactive Windows desktop whiteboard demo with mouse drawing, floating suggestion panel, and click-to-apply behavior.
- `WhiteboardSuggestion.Tests`: offline xUnit tests for parsing, formatting, image types, and candidate building. They do not call Gemini or consume API quota.

## Requirements

- .NET 10 SDK
- Windows for the WPF desktop demo
- A Gemini API key with access to a vision-capable model
- Internet connection for live Gemini requests

## Configure the API key

Do not hard-code the key or commit it to Git.

**PowerShell** (current terminal window):

```powershell
$env:GEMINI_API_KEY = "YOUR_API_KEY"
# Optional; the default is gemini-flash-latest.
$env:GEMINI_MODEL = "gemini-3.5-flash-lite"
```

**Command Prompt**:

```bat
set GEMINI_API_KEY=YOUR_API_KEY
set GEMINI_MODEL=gemini-3.5-flash-lite
```

Use a model name enabled for your API key and capable of image input.

## Run the interactive overlay

Open PowerShell in this folder, configure the API key, and run:

```powershell
dotnet restore .\WhiteboardSuggestion.sln
dotnet run --project .\src\WhiteboardSuggestion.WpfDemo\WhiteboardSuggestion.WpfDemo.csproj
```

In the app, draw with left-click and drag, click **Suggest correction**, then click one candidate button in the floating panel. The selected candidate replaces the rough strokes in place. Use **Clear board** to reset.

The sample currently handles common shapes (circle, oval/ellipse, triangle, rectangle/square, diamond, pentagon, hexagon, and star) plus letters and digits. For an unfamiliar shape, the demo falls back to rendering its name as text; extend `CreateCleanElement` if your team's whiteboard has a richer shape model.

## Run the console demo

```powershell
dotnet run --project .\src\WhiteboardSuggestion.Console\WhiteboardSuggestion.Console.csproj
```

Choose option `1`, then enter the path to a `.png`, `.jpg`, `.jpeg`, or `.webp` image. Three sample images are included in `sample-images/`.

## Run unit tests

```powershell
dotnet test .\WhiteboardSuggestion.sln
```

The xUnit tests use local sample JSON and values. They do not require `GEMINI_API_KEY`; a separate live API run is needed to verify credentials/network/model setup. The WPF demo itself should be run on Windows.

## Architecture / integration boundary

The reusable pieces are in `WhiteboardSuggestion.Core`:

```csharp
IShapeSuggestionService service = new GeminiShapeSuggestionService(apiKey);
ShapeSuggestion suggestion = await service.SuggestAsync(drawingPngBytes, "image/png");
IReadOnlyList<SuggestionCandidate> options = ShapeSuggestionCandidateFactory.Create(suggestion);
```

A production whiteboard UI can bind `options` to its own overlay buttons. On button click, it receives a `SuggestionCandidate` with `Label`, `Category`, `CanonicalValue`, and `IsRecommended`. The UI then inserts that canonical item into its own drawing model. The WPF control is only a Windows demo host; its XAML should not be directly copied into WinUI 3. A WinUI 3 equivalent panel is included under `integration/WinUI3/` as a starting point for the actual app shell. See `REPOSITORY_INTEGRATION.md` for the repository boundary and integration notes.

## Design notes / limitations

- Gemini requests schema-constrained JSON; local code validates and normalizes it.
- Confidence is the model's self-estimate, not a calibrated probability.
- Letters/numbers that look alike (for example, M/N or 3/8) can be ambiguous, which is why alternatives are exposed.
- The interactive host sends only the currently active rough strokes, not the entire board, to reduce ambiguity and unnecessary image content.
- This is a standalone prototype. It has not been pushed to the course repository, and should be merged only after the Whiteboard team agrees on the interface and target UI framework.
