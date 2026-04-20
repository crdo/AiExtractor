namespace AiStructuredDataFromImageExtractionDemo.Models;

/// <summary>
/// Combined Rozvaha extraction result containing both Aktiva and Pasiva
/// in a single GPT-5.1 call to reduce API call count.
/// </summary>
public record RozvahaComplete
{
	public RozvahaAktiva Aktiva { get; init; } = new();
	public RozvahaPasiva Pasiva { get; init; } = new();
}
