using Microsoft.AspNetCore.Components;
using AiStructuredDataFromImageExtractionDemo.Services;

namespace AiStructuredDataFromImageExtractionDemo.Components;

public partial class FileResultCard
{
	[Parameter, EditorRequired] public ExtractionResult Result { get; set; } = default!;

	private string markdown = "";

	protected override void OnParametersSet()
	{
		markdown = Runner.ReadMarkdownCache(Result.SourceFile);
	}

	private static string Fmt(decimal v) => v.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("cs-CZ"));
}
