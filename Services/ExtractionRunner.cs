using System.Collections.Concurrent;
using System.Text.Json;

namespace AiStructuredDataFromImageExtractionDemo.Services;

/// <summary>
/// Orchestrates multi-file extraction for the UI. Wraps <see cref="ExtractionService"/> and
/// caches the generated Markdown + JSON next to the PDF so repeated UI runs reuse them.
/// </summary>
public sealed class ExtractionRunner
{
	private readonly ExtractionService _service;

	public ExtractionRunner(ExtractionService service)
	{
		_service = service;
	}

	/// <summary>Default directory where the UI looks for PDFs.</summary>
	public static string DefaultExtractionDir =>
		Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents", "extraction");

	public IReadOnlyList<PdfCandidate> ListPdfs(string? directory = null)
	{
		directory ??= DefaultExtractionDir;
		if (!Directory.Exists(directory)) return Array.Empty<PdfCandidate>();

		return Directory.EnumerateFiles(directory, "*.pdf")
			.OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
			.Select(p =>
			{
				var info = new FileInfo(p);
				var mdPath = Path.Combine(directory, Path.GetFileNameWithoutExtension(p) + ".md");
				var jsonPath = Path.Combine(directory, Path.GetFileNameWithoutExtension(p) + ".json");
				return new PdfCandidate(p, info.Length, info.LastWriteTime, File.Exists(mdPath), File.Exists(jsonPath));
			})
			.ToList();
	}

	public string ReadMarkdownCache(string pdfPath)
	{
		var mdPath = Path.Combine(Path.GetDirectoryName(pdfPath)!, Path.GetFileNameWithoutExtension(pdfPath) + ".md");
		return File.Exists(mdPath) ? File.ReadAllText(mdPath) : "";
	}

	/// <summary>Run extraction for all selected files in parallel, reporting per-file progress.</summary>
	public async Task<IReadOnlyList<ExtractionResult>> RunAsync(
		IEnumerable<string> pdfPaths,
		IProgress<RunProgress>? progress = null,
		CancellationToken ct = default)
	{
		var outputDir = Path.GetDirectoryName(pdfPaths.First())!;
		var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
		var completed = new ConcurrentBag<ExtractionResult>();
		var total = pdfPaths.Count();
		var done = 0;

		var tasks = pdfPaths.Select(async path =>
		{
			var name = Path.GetFileName(path);
			progress?.Report(new RunProgress(name, RunProgressState.Started, Interlocked.CompareExchange(ref done, 0, 0), total, null));
			try
			{
				var result = await _service.ExtractAsync(path, outputDir, ct);

				// persist JSON for future offline inspection
				var json = JsonSerializer.Serialize(result.UcetniZaverka, jsonOptions);
				var jsonPath = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(path) + ".json");
				await File.WriteAllTextAsync(jsonPath, json, ct);

				completed.Add(result);
				var c = Interlocked.Increment(ref done);
				progress?.Report(new RunProgress(name, RunProgressState.Succeeded, c, total, null));
				return result;
			}
			catch (Exception ex)
			{
				var c = Interlocked.Increment(ref done);
				progress?.Report(new RunProgress(name, RunProgressState.Failed, c, total, ex.Message));
				return null;
			}
		}).ToArray();

		await Task.WhenAll(tasks);
		return completed.OrderBy(r => r.Rok ?? 0).ToList();
	}
}

public sealed record PdfCandidate(string FullPath, long SizeBytes, DateTime LastWrite, bool HasMarkdownCache, bool HasJsonCache)
{
	public string FileName => Path.GetFileName(FullPath);
	public string SizeHuman => SizeBytes < 1024 * 1024 ? $"{SizeBytes / 1024.0:F0} KB" : $"{SizeBytes / 1024.0 / 1024.0:F1} MB";
}

public enum RunProgressState { Started, Succeeded, Failed }

public sealed record RunProgress(string FileName, RunProgressState State, int Done, int Total, string? Error);
