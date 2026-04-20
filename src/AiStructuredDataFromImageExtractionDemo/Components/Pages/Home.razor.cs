using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using AiStructuredDataFromImageExtractionDemo.Services;

namespace AiStructuredDataFromImageExtractionDemo.Components.Pages;

public partial class Home
{
	private List<PdfCandidate>? candidates;
	private HxGrid<PdfCandidate>? fileGrid;
	private HxInputFileDropZone? dropZone;
	private HashSet<PdfCandidate> selectedFiles = new();

	private bool running;
	private readonly List<RunProgress> progressItems = new();
	private List<ExtractionResult> results = new();

	private string runText => running ? "Zpracovává se…" : $"Spustit extrakci ({selectedFiles.Count})";

	private int progressPercent =>
		progressItems.Count == 0 ? 0
		: (int)Math.Round(100.0 * progressItems.Count(p => p.State != RunProgressState.Started) / progressItems.Count);

	private RunProgress? ProgressFor(PdfCandidate c)
		=> progressItems.FirstOrDefault(p => StringComparer.OrdinalIgnoreCase.Equals(p.FileName, c.FileName));

	protected override void OnInitialized() => LoadFiles();

	private void LoadFiles()
	{
		try
		{
			candidates = Runner.ListPdfs().ToList();
			// drop any selected files that are no longer present
			selectedFiles = selectedFiles.Intersect(candidates, new PathEqComparer()).ToHashSet(new PathEqComparer());
		}
		catch (Exception ex)
		{
			Messenger.AddError("Chyba při čtení složky", ex.Message);
			candidates = new();
			selectedFiles = new();
		}
	}

	private async Task HandleUploadCompleted(UploadCompletedEventArgs args)
	{
		var ok = args.FilesUploaded.Count(f => f.ResponseStatus == System.Net.HttpStatusCode.OK);
		if (ok > 0)
		{
			Messenger.AddInformation("Nahráno", $"{ok} PDF souborů");
			await RefreshGrid();
			if (dropZone is not null)
				await dropZone.ResetAsync();

			// Auto-select newly uploaded files.
			if (candidates is not null)
			{
				var uploadedNames = args.FilesUploaded
					.Where(f => f.ResponseStatus == System.Net.HttpStatusCode.OK)
					.Select(f => f.OriginalFileName)
					.ToHashSet(StringComparer.OrdinalIgnoreCase);
				foreach (var c in candidates)
					if (uploadedNames.Contains(c.FileName))
						selectedFiles.Add(c);
			}
		}
	}

	private async Task RefreshGrid()
	{
		LoadFiles();
		if (fileGrid is not null)
			await fileGrid.RefreshDataAsync();
	}

	private Task<GridDataProviderResult<PdfCandidate>> ProvidePdfs(GridDataProviderRequest<PdfCandidate> request)
		=> Task.FromResult(new GridDataProviderResult<PdfCandidate>
		{
			Data = candidates ?? new(),
			TotalCount = candidates?.Count ?? 0
		});

	private void SelectAll()
	{
		if (candidates is null) return;
		selectedFiles = new HashSet<PdfCandidate>(candidates, new PathEqComparer());
	}

	private void ClearSelection() => selectedFiles = new(new PathEqComparer());

	private async Task RunAsync()
	{
		if (selectedFiles.Count == 0) return;
		running = true;
		progressItems.Clear();
		results.Clear();

		var prog = new Progress<RunProgress>(p =>
		{
			var i = progressItems.FindIndex(x => x.FileName == p.FileName);
			if (i >= 0) progressItems[i] = p; else progressItems.Add(p);
			InvokeAsync(StateHasChanged);
		});

		try
		{
			var done = await Runner.RunAsync(selectedFiles.Select(s => s.FullPath).ToArray(), prog);
			results = done.ToList();
			Messenger.AddInformation("Extrakce dokončena", $"{results.Count} / {selectedFiles.Count} souborů");
			await RefreshGrid();
		}
		catch (Exception ex)
		{
			Messenger.AddError("Extrakce selhala", ex.Message);
		}
		finally
		{
			running = false;
		}
	}

	private sealed class PathEqComparer : IEqualityComparer<PdfCandidate>
	{
		public bool Equals(PdfCandidate? x, PdfCandidate? y)
			=> StringComparer.OrdinalIgnoreCase.Equals(x?.FullPath, y?.FullPath);
		public int GetHashCode(PdfCandidate obj)
			=> StringComparer.OrdinalIgnoreCase.GetHashCode(obj.FullPath);
	}
}
