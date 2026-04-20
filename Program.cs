using System.Text.Json;
using AiStructuredDataFromImageExtractionDemo.Services;
using AiStructuredDataFromImageExtractionDemo.Rendering;

// ============================================================
// Configuration
// ============================================================
var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
	?? "https://crha-mm394332-switzerlandnorth.services.ai.azure.com/";
var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")
	?? throw new InvalidOperationException(
		"Set the AZURE_OPENAI_API_KEY environment variable. " +
		"E.g.: export AZURE_OPENAI_API_KEY=your-key-here");

// PDF files: from command-line args, or default to ~/Documents/extraction/*.pdf
var extractionDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents", "extraction");
var pdfFiles = args.Length > 0
	? args.SelectMany(a => Directory.GetFiles(Path.GetDirectoryName(a) ?? ".", Path.GetFileName(a))).Distinct().ToArray()
	: Directory.Exists(extractionDir)
		? Directory.GetFiles(extractionDir, "*.pdf")
		: [];

if (pdfFiles.Length == 0)
{
	Console.WriteLine("No PDF files found. Pass PDF paths as arguments or place them in ~/Documents/.");
	return;
}

Console.WriteLine($"Found {pdfFiles.Length} PDF file(s) to process:");
foreach (var f in pdfFiles) Console.WriteLine($"  • {Path.GetFileName(f)}");

// Output directory for Markdown + JSON
var outputDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));

// ============================================================
// Batch extraction (parallel across files)
// ============================================================
var sw = System.Diagnostics.Stopwatch.StartNew();
var service = new ExtractionService(endpoint, apiKey);
var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

// Process all files in parallel (limited by Azure API concurrency)
var tasks = pdfFiles.Select(async pdfPath =>
{
	try
	{
		var result = await service.ExtractAsync(pdfPath, outputDir);

		// Save JSON per file
		var json = JsonSerializer.Serialize(result.UcetniZaverka, jsonOptions);
		var jsonPath = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(pdfPath) + ".json");
		await File.WriteAllTextAsync(jsonPath, json);

		return (ExtractionResult?)result;
	}
	catch (Exception ex)
	{
		Console.WriteLine($"\n  ✗ Error processing {Path.GetFileName(pdfPath)}: {ex.Message}");
		return null;
	}
}).ToArray();

var completed = await Task.WhenAll(tasks);
var results = completed.Where(r => r != null).Select(r => r!).ToList();

sw.Stop();
Console.WriteLine($"\nExtraction completed in {sw.Elapsed.TotalSeconds:F1}s");

// Render per-file details (sequentially for clean output)
foreach (var result in results.OrderBy(r => r.Rok))
	ConsoleRenderer.RenderDetail(result);

// ============================================================
// Cross-year summary
// ============================================================
if (results.Count > 1)
{
	ConsoleRenderer.RenderSummary(results);
}

Console.WriteLine($"\nDone. Processed {results.Count}/{pdfFiles.Length} file(s) successfully.");