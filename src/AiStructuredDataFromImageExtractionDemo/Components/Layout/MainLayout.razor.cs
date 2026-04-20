using Microsoft.AspNetCore.Components;
using AiStructuredDataFromImageExtractionDemo.Services;

namespace AiStructuredDataFromImageExtractionDemo.Components.Layout;

public partial class MainLayout : LayoutComponentBase
{
	private int pdfCount;

	protected override void OnInitialized()
	{
		try { pdfCount = Runner.ListPdfs().Count; }
		catch { pdfCount = 0; }
	}
}
