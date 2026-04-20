using System.Diagnostics;
using System.Text.RegularExpressions;
using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using AiStructuredDataFromImageExtractionDemo.Models;

namespace AiStructuredDataFromImageExtractionDemo.Services;

/// <summary>
/// Extracts structured financial data (UcetniZaverka) from a PDF file
/// using Azure Document Intelligence + GPT-5.1 structured output.
/// </summary>
public partial class ExtractionService
{
	private readonly IChatClient _chatClient;
	private readonly DocumentIntelligenceClient _docClient;

	// Per-extraction metrics — reset at the start of each ExtractAsync call.
	private ExtractionMetrics _metrics = new();

	public ExtractionService(string endpoint, string apiKey)
	{
		var credential = new AzureKeyCredential(apiKey);
		_docClient = new DocumentIntelligenceClient(new Uri(endpoint), credential);
		_chatClient = new AzureOpenAIClient(new Uri(endpoint), credential)
			.GetChatClient("gpt-5.1-2")
			.AsIChatClient();
	}

	/// <summary>
	/// Full extraction pipeline: PDF → Markdown → GPT-5.1 structured extraction with validation retry.
	/// </summary>
	public async Task<ExtractionResult> ExtractAsync(string pdfPath, string? outputDir = null, CancellationToken cancellationToken = default)
	{
		_metrics = new ExtractionMetrics();
		var pipelineSw = Stopwatch.StartNew();

		var fileName = Path.GetFileNameWithoutExtension(pdfPath);
		Console.WriteLine($"\n{'=',-60}");
		Console.WriteLine($"  Processing: {Path.GetFileName(pdfPath)}");
		Console.WriteLine($"{'=',-60}");

		// Step 1: PDF → Markdown (with cache — skip Doc Intelligence if .md already exists)
		string? cachedMdPath = outputDir != null ? Path.Combine(outputDir, $"{fileName}.md") : null;
		string markdown;

		if (cachedMdPath != null && File.Exists(cachedMdPath))
		{
			markdown = await File.ReadAllTextAsync(cachedMdPath, cancellationToken);
			_metrics.MarkdownFromCache = true;
			Console.WriteLine($"  Step 1: Using cached Markdown ({markdown.Length:N0} chars)");
		}
		else
		{
			Console.WriteLine("  Step 1: Extracting Markdown via Document Intelligence...");
			var diSw = Stopwatch.StartNew();
			var pdfBytes = await File.ReadAllBytesAsync(pdfPath, cancellationToken);
			var analyzeRequest = new AnalyzeDocumentOptions("prebuilt-layout", BinaryData.FromBytes(pdfBytes))
			{
				OutputContentFormat = DocumentContentFormat.Markdown
			};
			var operation = await _docClient.AnalyzeDocumentAsync(WaitUntil.Completed, analyzeRequest, cancellationToken);
			markdown = operation.Value.Content;
			diSw.Stop();

			_metrics.DocumentIntelligenceDuration = diSw.Elapsed;
			_metrics.DocumentIntelligenceCalls = 1;
			Console.WriteLine($"  Markdown: {markdown.Length:N0} chars ({diSw.Elapsed.TotalSeconds:F1}s)");

			// Save Markdown for future cache
			if (cachedMdPath != null)
				await File.WriteAllTextAsync(cachedMdPath, markdown, cancellationToken);
		}

		// Step 2: Section extraction
		var rozvahaSection = ExtractSections(markdown, "ROZVAHA");
		var vzzSection = ExtractSections(markdown, "VÝKAZ ZISKU");
		Console.WriteLine($"  Sections: Rozvaha={rozvahaSection.Length:N0}, VZZ={vzzSection.Length:N0} chars");

		// Step 3: Auto-detect period from markdown
		var (obdobiOd, obdobiDo, nazev, ico) = DetectMetadata(markdown);
		Console.WriteLine($"  Detected: {nazev}, IČO {ico}, {obdobiOd?.ToString("dd.MM.yyyy") ?? "?"} — {obdobiDo?.ToString("dd.MM.yyyy") ?? "?"}");

		// Step 4: GPT-5.1 structured extraction with retry
		Console.WriteLine("  Step 2: GPT-5.1 extraction (2 parallel calls: Rozvaha + VZZ)...");
		var (aktiva, pasiva, vzz) = await ExtractWithRetryAsync(rozvahaSection, vzzSection, cancellationToken);

		var ucetniZaverka = new UcetniZaverka
		{
			NazevSpolecnosti = nazev,
			Ico = ico,
			ObdobiOd = obdobiOd,
			ObdobiDo = obdobiDo,
			MernaJednotka = "tis. Kč",
			Aktiva = aktiva,
			Pasiva = pasiva,
			VykazZiskuAZtraty = vzz
		};

		var result = new ExtractionResult
		{
			SourceFile = pdfPath,
			UcetniZaverka = ucetniZaverka,
			JeRozvahaVRovnovaze = ucetniZaverka.JeRozvahaVRovnovaze,
			JeVHKonzistentni = ucetniZaverka.JeVysledekHospodareniKonzistentni,
			Metrics = _metrics
		};

		pipelineSw.Stop();
		_metrics.TotalDuration = pipelineSw.Elapsed;
		Console.WriteLine($"  Result: Balance={Fmt(result.JeRozvahaVRovnovaze)}, VH={Fmt(result.JeVHKonzistentni)}");
		Console.WriteLine($"  Metrics: {_metrics.GptCalls} GPT calls, {_metrics.GptInputTokens:N0}+{_metrics.GptOutputTokens:N0}={_metrics.GptTotalTokens:N0} tokens, GPT {_metrics.GptTotalDuration.TotalSeconds:F1}s, total {_metrics.TotalDuration.TotalSeconds:F1}s");
		return result;
	}

	// Temperature=0 for deterministic extraction. Separate token budgets per response size.
	private static readonly ChatOptions RozvahaOptions = new() { Temperature = 0f, MaxOutputTokens = 16384 };
	private static readonly ChatOptions DefaultOptions = new() { Temperature = 0f, MaxOutputTokens = 16384 };

	private async Task<(RozvahaAktiva aktiva, RozvahaPasiva pasiva, VykazZiskuAZtraty vzz)>
		ExtractWithRetryAsync(string rozvahaSection, string vzzSection, CancellationToken cancellationToken = default)
	{
		var vzzMessages = BuildVzzMessages(vzzSection);

		// Try to extract reference totals from source markdown for comparison
		var (refAktivaNetto, refPasivaBezne) = ExtractReferenceTotals(rozvahaSection);
		if (refAktivaNetto != null)
			Console.WriteLine($"    Reference totals from PDF: Aktiva={refAktivaNetto:N0}, Pasiva={refPasivaBezne:N0}");

		// Initial extraction: combined Rozvaha (Aktiva+Pasiva) + VZZ in parallel (2 calls instead of 3)
		var rozvahaMessages = BuildRozvahaMessages(rozvahaSection, refAktivaNetto, refPasivaBezne);
		var rozvahaTask = SafeExtractAsync<RozvahaComplete>("Rozvaha", rozvahaMessages, options: RozvahaOptions);
		var vzzTask = SafeExtractAsync<VykazZiskuAZtraty>("VZZ", vzzMessages);
		await Task.WhenAll(rozvahaTask, vzzTask);

		var rozvahaResult = await rozvahaTask;
		var aktivaResult = rozvahaResult.Aktiva;
		var pasivaResult = rozvahaResult.Pasiva;
		var vzzResult = await vzzTask;

		// Validation loop: retry up to 3 times with error feedback
		const int maxRetries = 3;
		for (int retry = 1; retry <= maxRetries; retry++)
		{
			var aktivaNetto = aktivaResult.AktivaCelkem.Netto;
			var pasivaBezne = pasivaResult.PasivaCelkem.Bezne;

			if (aktivaNetto == pasivaBezne)
			{
				Console.WriteLine($"    ✓ Rozvaha balancuje: {aktivaNetto:N0}");
				break;
			}

			var diff = aktivaNetto - pasivaBezne;
			Console.WriteLine($"    ⚠ Nebalancuje ({retry}/{maxRetries}): A={aktivaNetto:N0} ≠ P={pasivaBezne:N0} (diff={diff:N0})");
			Console.WriteLine($"      Aktiva: B={aktivaResult.B_StalaAktiva.Netto:N0}, C={aktivaResult.C_ObeznaAktiva.Netto:N0}, D={aktivaResult.D_CasoveRozliseniAktiv.Netto:N0}");
			Console.WriteLine($"      Pasiva: A={pasivaResult.A_VlastniKapital.Bezne:N0}, B={pasivaResult.B_Rezervy.Bezne:N0}, C={pasivaResult.C_Zavazky.Bezne:N0}, D={pasivaResult.D_CasoveRozliseniPasiv.Bezne:N0}");

			if (retry == maxRetries)
			{
				Console.WriteLine($"    ✗ Nebalancuje ani po {maxRetries} pokusech.");
				break;
			}

			_metrics.BalanceRetries++;

			// Determine which side to retry based on reference totals
			bool retryAktiva = true, retryPasiva = true;
			if (refAktivaNetto != null && refPasivaBezne != null)
			{
				var aktivaMatchesRef = aktivaNetto == refAktivaNetto;
				var pasivaMatchesRef = pasivaBezne == refPasivaBezne;
				if (aktivaMatchesRef && !pasivaMatchesRef)
				{
					retryAktiva = false;
					Console.WriteLine($"    → Aktiva matches PDF reference, retrying only Pasiva...");
				}
				else if (pasivaMatchesRef && !aktivaMatchesRef)
				{
					retryPasiva = false;
					Console.WriteLine($"    → Pasiva matches PDF reference, retrying only Aktiva...");
				}
				else
				{
					Console.WriteLine($"    → Retrying both Aktiva and Pasiva with error feedback...");
				}
			}
			else
			{
				Console.WriteLine($"    → Retrying both with error feedback...");
			}

			// Feedback-based retry: use per-side calls for targeted correction
			Task<RozvahaAktiva>? retryAktivaTask = null;
			Task<RozvahaPasiva>? retryPasivaTask = null;

			if (retryAktiva)
			{
				var feedbackAktiva = BuildAktivaCorrectionMessages(rozvahaSection, aktivaResult, diff, refAktivaNetto);
				retryAktivaTask = SafeExtractAsync<RozvahaAktiva>("Aktiva-retry", feedbackAktiva);
			}
			if (retryPasiva)
			{
				var feedbackPasiva = BuildPasivaCorrectionMessages(rozvahaSection, pasivaResult, -diff, refPasivaBezne);
				retryPasivaTask = SafeExtractAsync<RozvahaPasiva>("Pasiva-retry", feedbackPasiva);
			}

			if (retryAktivaTask != null) aktivaResult = await retryAktivaTask;
			if (retryPasivaTask != null) pasivaResult = await retryPasivaTask;
		}

		// VH consistency validation (informational only - VH lines are now extracted directly)
		var pasAV = pasivaResult.AV_VysledekHospodareniBeznehoObdobi.Bezne;
		var vzzVH = vzzResult.VysledekHospodareniZaObdobi.Bezne;
		if (vzzVH == pasAV)
			Console.WriteLine($"    ✓ VH konzistentní: {vzzVH:N0}");
		else
			Console.WriteLine($"    ⚠ VH nesouhlasí: VZZ={vzzVH:N0} ≠ A.V.={pasAV:N0} (diff={vzzVH - pasAV:N0})");

		// Sub-item validation (detect OCR sign errors)
		var provozniDiff = vzzResult.ProvozniVysledekHospodareni.Bezne - vzzResult.ProvozniVHVypoctem.Bezne;
		var financniDiff = vzzResult.FinancniVysledekHospodareni.Bezne - vzzResult.FinancniVHVypoctem.Bezne;
		if (provozniDiff != 0)
			Console.WriteLine($"    ⚠ Provozní VH: tabulka={vzzResult.ProvozniVysledekHospodareni.Bezne:N0} vs. výpočet={vzzResult.ProvozniVHVypoctem.Bezne:N0} (diff={provozniDiff:N0}, pravděpodobně chyba OCR v znaménku)");
		if (financniDiff != 0)
			Console.WriteLine($"    ⚠ Finanční VH: tabulka={vzzResult.FinancniVysledekHospodareni.Bezne:N0} vs. výpočet={vzzResult.FinancniVHVypoctem.Bezne:N0} (diff={financniDiff:N0})");

		// Brutto+Korekce=Netto cross-validation for Aktiva (OCR digit error detection)
		ValidateAktivaBruttoKorekceNetto(aktivaResult);

		return (aktivaResult, pasivaResult, vzzResult);
	}

	/// <summary>
	/// Validates that Brutto + Korekce == Netto for all non-zero Aktiva leaf rows.
	/// A mismatch indicates a likely OCR digit error in the source PDF.
	/// </summary>
	private static void ValidateAktivaBruttoKorekceNetto(RozvahaAktiva a)
	{
		var rows = new (string label, RozvahaAktivaRadek row)[]
		{
			("A.", a.A_PohledavkyZaUpsanyZakladniKapital),
			("B.I.1.", a.BI1_NehmotneVysledkyVyvoje),
			("B.I.2.", a.BI2_OcenitelnaPrava),
			("  B.I.2.1.", a.BI2_1_Software),
			("  B.I.2.2.", a.BI2_2_OstatniOcenitelnaPrava),
			("B.I.3.", a.BI3_Goodwill),
			("B.I.4.", a.BI4_OstatniDlouhodobyNehmotnyMajetek),
			("B.I.5.", a.BI5_PoskytnuTeZalohyNaDNMaNedokoncenyDNM),
			("  B.I.5.1.", a.BI5_1_PoskytnuTeZalohyNaDNM),
			("  B.I.5.2.", a.BI5_2_NedokoncenyDNM),
			("B.II.1.", a.BII1_PozemkyAStavby),
			("  B.II.1.1.", a.BII1_1_Pozemky),
			("  B.II.1.2.", a.BII1_2_Stavby),
			("B.II.2.", a.BII2_HmotneMoviteVeci),
			("B.II.3.", a.BII3_OcenovaciRozdilKNabytemu),
			("B.II.4.", a.BII4_OstatniDlouhodobyHmotnyMajetek),
			("B.II.5.", a.BII5_PoskytnuTeZalohyNaDHMaNedokoncenyDHM),
			("  B.II.5.1.", a.BII5_1_PoskytnuTeZalohyNaDHM),
			("  B.II.5.2.", a.BII5_2_NedokoncenyDHM),
			("B.III.1.", a.BIII1_PodilOvladanaOvladajici),
			("B.III.2.", a.BIII2_ZapujckyUveryOvladanaOvladajici),
			("B.III.3.", a.BIII3_PodilPodstatnyVliv),
			("B.III.4.", a.BIII4_ZapujckyUveryPodstatnyVliv),
			("B.III.5.", a.BIII5_OstatniDlouhodobeCennePapiry),
			("B.III.6.", a.BIII6_ZapujckyUveryOstatni),
			("B.III.7.", a.BIII7_OstatniDlouhodobyFinancniMajetek),
			("C.I.1.", a.CI1_Material),
			("C.I.2.", a.CI2_NedokoncenaVyroba),
			("C.I.3.", a.CI3_VyrobkyAZbozi),
			("  C.I.3.1.", a.CI3_1_Vyrobky),
			("  C.I.3.2.", a.CI3_2_Zbozi),
			("C.I.4.", a.CI4_MladaZvirata),
			("C.I.5.", a.CI5_PoskytnuTeZalohyNaZasoby),
			("C.II.1.1.", a.CII1_1_PohledavkyZObchodVztahu),
			("C.II.1.2.", a.CII1_2_PohledavkyOvladanaOvladajici),
			("C.II.1.3.", a.CII1_3_PohledavkyPodstatnyVliv),
			("C.II.1.4.", a.CII1_4_OdlozenaDanovaPohledavka),
			("C.II.1.5.", a.CII1_5_PohledavkyOstatni),
			("C.II.2.1.", a.CII2_1_PohledavkyZObchodVztahuKr),
			("C.II.2.2.", a.CII2_2_PohledavkyOvladanaOvladajiciKr),
			("C.II.2.3.", a.CII2_3_PohledavkyPodstatnyVlivKr),
			("C.II.2.4.", a.CII2_4_PohledavkyOstatniKr),
			("C.III.1.", a.CIII1_PodilOvladanaOvladajiciKr),
			("C.III.2.", a.CIII2_OstatniKratkodobyFinancniMajetek),
			("C.IV.1.", a.CIV1_PenezniProstredkyVPokladne),
			("C.IV.2.", a.CIV2_PenezniProstredkyNaUctech),
			("D.1.", a.D1_NakladyPristichObdobi),
			("D.2.", a.D2_KomplexniNakladyPristichObdobi),
			("D.3.", a.D3_PrijmyPristichObdobi),
		};

		int issues = 0;
		foreach (var (label, row) in rows)
		{
			if (row.Brutto == 0 && row.Korekce == 0 && row.Netto == 0) continue;
			var expected = row.Brutto + row.Korekce;
			if (expected != row.Netto)
			{
				if (issues == 0)
					Console.WriteLine("    ⚠ OCR kontrola Brutto+Korekce≠Netto:");
				Console.WriteLine($"      \x1b[33m{label,-14} Brutto={row.Brutto:N0} + Korekce={row.Korekce:N0} = {expected:N0} ≠ Netto={row.Netto:N0} (diff={row.Netto - expected:N0})\x1b[0m");
				issues++;
			}
		}

		if (issues == 0)
			Console.WriteLine("    ✓ Brutto+Korekce=Netto: OK pro všechny řádky Aktiv");
	}
	/// <summary>
	/// Safely call GetResponseAsync with retry on deserialization failures (truncated JSON).
	/// Captures token usage and timing metrics.
	/// </summary>
	private async Task<T> SafeExtractAsync<T>(string label, List<ChatMessage> messages, int maxAttempts = 3, ChatOptions? options = null)
	{
		options ??= DefaultOptions;
		for (int attempt = 1; attempt <= maxAttempts; attempt++)
		{
			var callSw = Stopwatch.StartNew();
			try
			{
				var response = await _chatClient.GetResponseAsync<T>(messages, options);
				callSw.Stop();
				RecordGptCall(label, callSw.Elapsed, response.Usage, attempt, success: true);
				return response.Result;
			}
			catch (System.Text.Json.JsonException) when (attempt < maxAttempts)
			{
				callSw.Stop();
				RecordGptCall(label, callSw.Elapsed, usage: null, attempt, success: false);
				Console.WriteLine($"    ⚠ {label}: JSON parse error (attempt {attempt}/{maxAttempts}), retrying...");
				await Task.Delay(1000 * attempt);
			}
			catch (System.Text.Json.JsonException)
			{
				callSw.Stop();
				RecordGptCall(label, callSw.Elapsed, usage: null, attempt, success: false);
				// Last attempt failed — try non-generic call to inspect raw response
				Console.WriteLine($"    ⚠ {label}: JSON parse error (attempt {attempt}/{maxAttempts}), trying raw inspection...");
			}
		}

		// Fallback: get raw text response and try to parse manually
		var fallbackSw = Stopwatch.StartNew();
		var rawResponse = await _chatClient.GetResponseAsync(messages, options);
		fallbackSw.Stop();
		RecordGptCall($"{label}-raw", fallbackSw.Elapsed, rawResponse.Usage, maxAttempts + 1, success: true);

		var rawText = rawResponse.Text ?? "";
		Console.WriteLine($"    ✗ {label}: Raw response length={rawText.Length}, last 200 chars: ...{rawText[Math.Max(0, rawText.Length - 200)..]}");
		return System.Text.Json.JsonSerializer.Deserialize<T>(rawText)
			?? throw new InvalidOperationException($"{label}: GPT-5.1 returned invalid JSON after {maxAttempts + 1} attempts");
	}

	private void RecordGptCall(string label, TimeSpan duration, UsageDetails? usage, int attempt, bool success)
	{
		long inputTokens = usage?.InputTokenCount ?? 0;
		long outputTokens = usage?.OutputTokenCount ?? 0;

		_metrics.GptCalls++;
		_metrics.GptTotalDuration += duration;
		_metrics.GptInputTokens += inputTokens;
		_metrics.GptOutputTokens += outputTokens;
		_metrics.GptCallDetails.Add(new GptCallDetail
		{
			Label = label,
			Duration = duration,
			InputTokens = inputTokens,
			OutputTokens = outputTokens,
			Attempt = attempt,
			Success = success
		});
	}

	/// <summary>
	/// Try to extract the AKTIVA CELKEM and PASIVA CELKEM totals directly from the markdown
	/// using regex, to serve as reference values for identifying which side is wrong.
	/// </summary>
	private static (decimal? aktivaNetto, decimal? pasivaBezne) ExtractReferenceTotals(string rozvahaSection)
	{
		decimal? aktiva = null, pasiva = null;

		// Look for AKTIVA CELKEM row — typically has Netto value as 3rd numeric column
		var aktivaMatch = AktivaCelkemRegex().Match(rozvahaSection);
		if (aktivaMatch.Success)
			aktiva = ParseCzechNumber(aktivaMatch.Groups[3].Value);

		// Look for PASIVA CELKEM row — 1st numeric column is Běžné
		var pasivaMatch = PasivaCelkemRegex().Match(rozvahaSection);
		if (pasivaMatch.Success)
			pasiva = ParseCzechNumber(pasivaMatch.Groups[1].Value);

		return (aktiva, pasiva);
	}

	private static decimal? ParseCzechNumber(string s)
	{
		s = s.Trim().Replace(" ", "");
		if (string.IsNullOrEmpty(s)) return null;
		if (decimal.TryParse(s, System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture, out var v))
			return v;
		return null;
	}

	/// <summary>
	/// Build Aktiva messages with correction feedback from previous failed attempt.
	/// </summary>
	private static List<ChatMessage> BuildAktivaCorrectionMessages(string rozvahaSection, RozvahaAktiva prev, decimal diff, decimal? refTotal)
	{
		var messages = BuildAktivaMessages(rozvahaSection);
		var refHint = refTotal != null ? $"Správná hodnota AKTIVA CELKEM Netto z PDF je {refTotal:N0}.\n" : "";
		var correction = $"Tvoje předchozí extrakce dala AKTIVA CELKEM Netto = {prev.AktivaCelkem.Netto:N0}.\n"
			+ $"Rozvaha ale nebalancuje — rozdíl Aktiva-Pasiva je {diff:N0}.\n"
			+ $"Tvoje extrahované subtotály:\n"
			+ $"- A. = {prev.A_PohledavkyZaUpsanyZakladniKapital.Netto:N0}\n"
			+ $"- B. Stálá aktiva = {prev.B_StalaAktiva.Netto:N0} (B.I.={prev.BI_DlouhodobyNehmotnyMajetek.Netto:N0}, B.II.={prev.BII_DlouhodobyHmotnyMajetek.Netto:N0}, B.III.={prev.BIII_DlouhodobyFinancniMajetek.Netto:N0})\n"
			+ $"- C. Oběžná aktiva = {prev.C_ObeznaAktiva.Netto:N0} (C.I.={prev.CI_Zasoby.Netto:N0}, C.II.={prev.CII_Pohledavky.Netto:N0} [dl={prev.CII1_DlouhodobePohledavky.Netto:N0}/kr={prev.CII2_KratkodobePohledavky.Netto:N0}], C.III.={prev.CIII_KratkodobyFinancniMajetek.Netto:N0}, C.IV.={prev.CIV_PenezniProstredky.Netto:N0})\n"
			+ $"- D. Časové rozlišení = {prev.D_CasoveRozliseniAktiv.Netto:N0}\n"
			+ refHint
			+ "Prosím extrahuj znovu PEČLIVĚ, zkontroluj každý řádek proti zdrojové tabulce.";
		messages.Add(new ChatMessage(ChatRole.User, correction));
		return messages;
	}

	/// <summary>
	/// Build Pasiva messages with correction feedback from previous failed attempt.
	/// </summary>
	private static List<ChatMessage> BuildPasivaCorrectionMessages(string rozvahaSection, RozvahaPasiva prev, decimal diff, decimal? refTotal)
	{
		var messages = BuildPasivaMessages(rozvahaSection);
		var refHint = refTotal != null ? $"Správná hodnota PASIVA CELKEM Běžné z PDF je {refTotal:N0}.\n" : "";
		var correction = $"Tvoje předchozí extrakce dala PASIVA CELKEM Běžné = {prev.PasivaCelkem.Bezne:N0}.\n"
			+ $"Rozvaha ale nebalancuje — rozdíl Pasiva-Aktiva je {diff:N0}.\n"
			+ $"Tvoje extrahované subtotály:\n"
			+ $"- A. Vlastní kapitál = {prev.A_VlastniKapital.Bezne:N0} (A.I.={prev.AI_ZakladniKapital.Bezne:N0}, A.II.={prev.AII_AzioAKapitaloveFondy.Bezne:N0}, A.III.={prev.AIII_FondyZeZisku.Bezne:N0}, A.IV.={prev.AIV_VysledekHospodareniMinulychLet.Bezne:N0}, A.V.={prev.AV_VysledekHospodareniBeznehoObdobi?.Bezne:N0})\n"
			+ $"- B. Rezervy = {prev.B_Rezervy.Bezne:N0}\n"
			+ $"- C. Závazky = {prev.C_Zavazky.Bezne:N0} (C.I.={prev.CI_DlouhodobeZavazky.Bezne:N0}, C.II.={prev.CII_KratkodobeZavazky.Bezne:N0})\n"
			+ $"- D. Časové rozlišení = {prev.D_CasoveRozliseniPasiv.Bezne:N0}\n"
			+ refHint
			+ "Prosím extrahuj znovu PEČLIVĚ, zkontroluj každý řádek proti zdrojové tabulce.";
		messages.Add(new ChatMessage(ChatRole.User, correction));
		return messages;
	}

	// ============================================================
	// Metadata detection
	// ============================================================

	private static (DateOnly? od, DateOnly? @do, string nazev, string ico) DetectMetadata(string markdown)
	{
		DateOnly? od = null, do_ = null;
		string nazev = "", ico = "";

		// Try to find "k 31. prosinci 2022" or period mentions (word-based month)
		var dateMatch = WordDateRegex().Match(markdown);
		if (dateMatch.Success)
		{
			var day = int.Parse(dateMatch.Groups[1].Value);
			var month = MonthNameToNumber(dateMatch.Groups[2].Value);
			var year = int.Parse(dateMatch.Groups[3].Value);
			do_ = new DateOnly(year, month, day);
			od = new DateOnly(year, 1, 1); // default assumption: calendar year
		}

		// Try "ke dni DD.MM.YYYY" or "ke dni D.M.YYYY" (numeric date)
		if (do_ == null)
		{
			var keDniMatch = NumericDateRegex().Match(markdown);
			if (keDniMatch.Success)
			{
				var day = int.Parse(keDniMatch.Groups[1].Value);
				var month = int.Parse(keDniMatch.Groups[2].Value);
				var year = int.Parse(keDniMatch.Groups[3].Value);
				do_ = new DateOnly(year, month, day);
				od = new DateOnly(year, 1, 1);
			}
		}

		// Try "za rok 2022" or "za období od 1.1.2022 do 31.12.2022"
		if (do_ == null)
		{
			var yearMatch = YearOnlyRegex().Match(markdown);
			if (yearMatch.Success)
			{
				var year = int.Parse(yearMatch.Groups[1].Value);
				od = new DateOnly(year, 1, 1);
				do_ = new DateOnly(year, 12, 31);
			}
		}

		// Try explicit period "od DD.MM.YYYY do DD.MM.YYYY"
		if (do_ == null)
		{
			var periodMatch = PeriodRegex().Match(markdown);
			if (periodMatch.Success)
			{
				od = new DateOnly(int.Parse(periodMatch.Groups[3].Value), int.Parse(periodMatch.Groups[2].Value), int.Parse(periodMatch.Groups[1].Value));
				do_ = new DateOnly(int.Parse(periodMatch.Groups[6].Value), int.Parse(periodMatch.Groups[5].Value), int.Parse(periodMatch.Groups[4].Value));
			}
		}

		// Try to find IČO — also check HTML comments like <!-- PageHeader="60719257" -->
		var icoMatch = IcoRegex().Match(markdown);
		if (icoMatch.Success)
			ico = icoMatch.Groups[1].Value;

		// Fallback: IČO in PageHeader comments ("IČ:" on one line, digits on next)
		if (string.IsNullOrEmpty(ico))
		{
			var headerIcoMatch = IcoPageHeaderRegex().Match(markdown);
			if (headerIcoMatch.Success)
				ico = headerIcoMatch.Groups[1].Value;
		}

		// Try to find company name — look for "Výroční zpráva společnosti X"
		var nameMatch = CompanyNameRegex().Match(markdown);
		if (nameMatch.Success)
			nazev = nameMatch.Groups[1].Value.Trim();

		// Fallback: first H1 heading that is NOT a financial statement heading (Rozvaha/Výkaz)
		if (string.IsNullOrEmpty(nazev))
		{
			var h1Matches = H1HeadingRegex().Matches(markdown);
			foreach (Match h1 in h1Matches)
			{
				var val = h1.Groups[1].Value.Trim();
				// Skip headings that are financial statement titles
				if (val.Contains("Rozvaha", StringComparison.OrdinalIgnoreCase) ||
					val.Contains("Výkaz zisku", StringComparison.OrdinalIgnoreCase))
					continue;
				nazev = val;
				break;
			}
		}

		return (od, do_, nazev, ico);
	}

	// Source-generated regex patterns for metadata detection (compiled at build time)
	[GeneratedRegex(@"k\s+(\d{1,2})\.\s*(lednu|únoru|březnu|dubnu|květnu|červnu|červenci|srpnu|září|říjnu|listopadu|prosinci)\s+(\d{4})", RegexOptions.IgnoreCase)]
	private static partial Regex WordDateRegex();

	[GeneratedRegex(@"ke\s+dni\s+(\d{1,2})\.(\d{1,2})\.(\d{4})", RegexOptions.IgnoreCase)]
	private static partial Regex NumericDateRegex();

	[GeneratedRegex(@"za\s+rok\s+(\d{4})", RegexOptions.IgnoreCase)]
	private static partial Regex YearOnlyRegex();

	[GeneratedRegex(@"od\s+(\d{1,2})\.(\d{1,2})\.(\d{4})\s+do\s+(\d{1,2})\.(\d{1,2})\.(\d{4})", RegexOptions.IgnoreCase)]
	private static partial Regex PeriodRegex();

	[GeneratedRegex(@"I[Čč][Oo]?\s*:?\s*(\d{6,8})", RegexOptions.IgnoreCase)]
	private static partial Regex IcoRegex();

	[GeneratedRegex(@"PageHeader=""I[Čč]:?""\s*-->\s*<!--\s*PageHeader=""(\d{6,8})""", RegexOptions.IgnoreCase)]
	private static partial Regex IcoPageHeaderRegex();

	[GeneratedRegex(@"výroční\s+zpráva\s+(?:společnosti\s+)?([^\n\r]+?)(?:\s+(?:za\s+rok|k\s+\d|$))", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
	private static partial Regex CompanyNameRegex();

	[GeneratedRegex(@"^#\s+(.+)$", RegexOptions.Multiline)]
	private static partial Regex H1HeadingRegex();

	[GeneratedRegex(@"AKTIVA\s+CELKEM[^\n]*?\|\s*([\d\s-]+?)\s*\|\s*([\d\s-]+?)\s*\|\s*([\d\s-]+?)\s*\|", RegexOptions.IgnoreCase)]
	private static partial Regex AktivaCelkemRegex();

	[GeneratedRegex(@"PASIVA\s+CELKEM[^\n]*?\|\s*([\d\s-]+?)\s*\|", RegexOptions.IgnoreCase)]
	private static partial Regex PasivaCelkemRegex();

	private static int MonthNameToNumber(string name) => name.ToLowerInvariant() switch
	{
		"lednu" or "ledna" => 1,
		"únoru" or "února" => 2,
		"březnu" or "března" => 3,
		"dubnu" or "dubna" => 4,
		"květnu" or "května" => 5,
		"červnu" or "června" => 6,
		"červenci" or "července" => 7,
		"srpnu" or "srpna" => 8,
		"září" => 9,
		"říjnu" or "října" => 10,
		"listopadu" => 11,
		"prosinci" or "prosince" => 12,
		_ => 1
	};

	// ============================================================
	// GPT-5.1 message builders
	// ============================================================

	private static readonly string SystemPromptBase = """
		Jsi odborný účetní asistent. Z poskytnutého textu výroční zprávy extrahuj požadovaná data.
		Pravidla:
		- Hodnoty jsou v tisících Kč (tis. Kč), extrahuj je jako celá čísla typu decimal.
		- Záporné hodnoty zapiš jako záporná čísla (např. -66523). Pozor: v markdown tabulkách může být
		  záporné číslo zobrazeno jako "- 66 523" (s mezerou za pomlčkou), to je záporná hodnota -66523.
		- Prázdné buňky nebo chybějící hodnoty zapiš jako 0.
		- Pečlivě přiřazuj hodnoty ke správným řádkům dle označení (A., B.I.1., C.II.2.4. apod.).
		- Vyplňuj POUZE koncové (leaf) vlastnosti s { get; init; }, NIKDY nevyplňuj computed properties.
		- Data mohou být rozdělena přes více HTML tabulek (kvůli stránkování PDF). Zpracuj VŠECHNY tabulky.
		- V prvním sloupci HTML tabulky bývají označení řádků sloučeny (rowspan). Identifikuj řádky VŽDY
		  podle NÁZVU ve druhém sloupci (např. "Zásoby", "Materiál", "Krátkodobé pohledávky" apod.),
		  NE podle označení v prvním sloupci (ta jsou kvůli rowspan zkomolená a nespolehlivá).
		- KRITICKÉ: Každý řádek HTML tabulky (<tr>) odpovídá jednomu řádku výkazu. Název řádku je vždy
		  ve druhém <td> sloupci. Hodnoty pro daný řádek jsou v dalších <td> sloupcích TÉHOŽ <tr>.
		  NEKOPÍRUJ hodnoty z jednoho řádku do jiného – pokud má řádek prázdné buňky, zapiš 0.

		UPOZORNĚNÍ NA OCR CHYBY:
		- Zdrojový text pochází z OCR (Azure Document Intelligence) a může obsahovat záměny číslic.
		  Typické záměny: 8↔0, 6↔0, 5↔8, 1↔7, 3↔8, 5↔6, rn↔m.
		- Pro řádky Aktiv VŽDY ověř, že Brutto + Korekce = Netto. Pokud ne, pravděpodobně došlo k OCR
		  chybě — zkus identifikovat, která ze tří hodnot je chybná, a opravu proveď.
		- Porovnej součty pod-položek s mezisoučty v tabulce. Pokud se neshodují, buď podezřívavý
		  k jednotlivým číslicím a hledej typické OCR záměny.
		- Pokud vidíš číslo, které vypadá nekonzistentně s okolními hodnotami (např. řádově odlišné),
		  zvaž, zda nejde o OCR chybu.
		""";

	/// <summary>
	/// Build messages for combined Rozvaha extraction (Aktiva + Pasiva in one call).
	/// Includes reference totals from regex so the model can self-validate.
	/// </summary>
	private static List<ChatMessage> BuildRozvahaMessages(string rozvahaSection, decimal? refAktiva = null, decimal? refPasiva = null)
	{
		var refHint = refAktiva != null && refPasiva != null
			? $"\nExpected totals from PDF: AKTIVA CELKEM Netto={refAktiva:N0}, PASIVA CELKEM Běžné={refPasiva:N0}. Verify your result matches."
			: "";

		return
		[
			new(ChatRole.System, SystemPromptBase + $$"""

			Extrahuj CELOU Rozvahu — sekci AKTIVA (řádky A. až D.) i sekci PASIVA (řádky A. až D.) NAJEDNOU.
			Výstup je JSON objekt se dvěma klíči: {"Aktiva": ..., "Pasiva": ...}.

			=== AKTIVA ===
			Každý řádek má 4 sloupce: Brutto, Korekce (záporná hodnota), Netto, MinuleNetto (Netto minulého období).
			Data aktiv jsou typicky ve třech tabulkách: B. Stálá aktiva, C. Oběžná aktiva, D. Časové rozlišení.
			MUSÍŠ vyplnit všechny sekce včetně C.I. Zásoby, C.II. Pohledávky, C.IV. Peněžní prostředky atd.

			KRITICKÉ UPOZORNĚNÍ pro C.II. Pohledávky:
			- C.II.1. = "Dlouhodobé pohledávky" a C.II.2. = "Krátkodobé pohledávky" jsou DVĚ RŮZNÉ sekce.
			- Rozlišuj je STRIKTNĚ podle názvu řádku v HTML: "Dlouhodobé pohledávky" vs. "Krátkodobé pohledávky".
			- NEKOPÍRUJ hodnoty z krátkodobých do dlouhodobých! Pokud má "Dlouhodobé pohledávky" hodnotu 0, zapiš 0.
			- Pod-řádky C.II.1.1-5 (dlouhodobé) a C.II.2.1-4 (krátkodobé) mají stejné názvy (např. "Pohledávky z obchodních vztahů")
			  ale RŮZNÉ hodnoty — poznej je podle pořadí v HTML tabulce.

			=== PASIVA ===
			Každý řádek má 2 sloupce: Bezne (běžné účetní období), Minule (minulé účetní období).

			KRITICKÉ UPOZORNĚNÍ:
			- Sekce A.IV. "VH minulých let" obsahuje A.IV.1. "Nerozdělený zisk minulých let / neuhrazená ztráta minulých let"
			  a A.IV.2. "Jiný výsledek hospodaření minulých let". Obě musí být vyplněny správně dle zdrojových dat.
			- Sekce D. "Časové rozlišení pasiv" obsahuje D.1. "Výdaje příštích období" a D.2. "Výnosy příštích období".
			- Tato sekce MUSÍ být vyplněna, pokud v tabulce existuje. Hledej řádky s textem "Časové rozlišení pasiv",
			  "Výdaje příštích období" a "Výnosy příštích období".
			- V HTML mohou být označení zkomolená (např. rowspan "D. D. 1.") — řiď se NÁZVEM řádku, ne označením.

			=== VALIDACE ===
			Po extrakci zkontroluj, že AKTIVA CELKEM Netto == PASIVA CELKEM Běžné. Pokud nebalancují, zkontroluj
			zvláště pod-součty a opravu proveď ještě před odevzdáním výsledku.{{refHint}}
			"""),
			new(ChatRole.User, $"Extrahuj CELOU Rozvahu (Aktiva i Pasiva) z těchto tabulek:\n\n{CompactMarkdown(rozvahaSection)}")
		];
	}

	/// <summary>
	/// Build Aktiva-only messages (used for targeted retries when balance validation fails).
	/// </summary>
	private static List<ChatMessage> BuildAktivaMessages(string rozvahaSection) =>
	[
		new(ChatRole.System, SystemPromptBase + """

		Extrahuj POUZE sekci Rozvaha - AKTIVA (řádky A. až D.).
		Každý řádek má 4 sloupce: Brutto, Korekce (záporná hodnota), Netto, MinuleNetto (Netto minulého období).
		Data aktiv jsou typicky ve třech tabulkách: B. Stálá aktiva, C. Oběžná aktiva, D. Časové rozlišení.
		MUSÍŠ vyplnit všechny sekce včetně C.I. Zásoby, C.II. Pohledávky, C.IV. Peněžní prostředky atd.

		KRITICKÉ UPOZORNĚNÍ pro C.II. Pohledávky:
		- C.II.1. = "Dlouhodobé pohledávky" a C.II.2. = "Krátkodobé pohledávky" jsou DVĚ RŮZNÉ sekce.
		- Rozlišuj je STRIKTNĚ podle názvu řádku v HTML: "Dlouhodobé pohledávky" vs. "Krátkodobé pohledávky".
		- NEKOPÍRUJ hodnoty z krátkodobých do dlouhodobých! Pokud má "Dlouhodobé pohledávky" hodnotu 0, zapiš 0.
		- Pod-řádky C.II.1.1-5 (dlouhodobé) a C.II.2.1-4 (krátkodobé) mají stejné názvy (např. "Pohledávky z obchodních vztahů")
		  ale RŮZNÉ hodnoty — poznej je podle pořadí v HTML tabulce.
		"""),
		new(ChatRole.User, $"Extrahuj Rozvahu Aktiva z těchto tabulek:\n\n{CompactMarkdown(rozvahaSection)}")
	];

	private static List<ChatMessage> BuildPasivaMessages(string rozvahaSection) =>
	[
		new(ChatRole.System, SystemPromptBase + """

		Extrahuj POUZE sekci Rozvaha - PASIVA (řádky A. až D.).
		Každý řádek má 2 sloupce: Bezne (běžné účetní období), Minule (minulé účetní období).

		KRITICKÉ UPOZORNĚNÍ:
		- Sekce A.IV. "VH minulých let" obsahuje A.IV.1. "Nerozdělený zisk minulých let / neuhrazená ztráta minulých let"
		  a A.IV.2. "Jiný výsledek hospodaření minulých let". Obě musí být vyplněny správně dle zdrojových dat.
		- Sekce D. "Časové rozlišení pasiv" obsahuje D.1. "Výdaje příštích období" a D.2. "Výnosy příštích období".
		- Tato sekce MUSÍ být vyplněna, pokud v tabulce existuje. Hledej řádky s textem "Časové rozlišení pasiv",
		  "Výdaje příštích období" a "Výnosy příštích období".
		- V HTML mohou být označení zkomolená (např. rowspan "D. D. 1.") — řiď se NÁZVEM řádku, ne označením.
		"""),
		new(ChatRole.User, $"Extrahuj Rozvahu Pasiva z těchto tabulek:\n\n{CompactMarkdown(rozvahaSection)}")
	];

	private static List<ChatMessage> BuildVzzMessages(string vzzSection) =>
	[
		new(ChatRole.System, SystemPromptBase + """

		Extrahuj POUZE Výkaz zisku a ztráty (druhové členění, Příloha č. 2 Vyhlášky 500/2002 Sb.).
		Každý řádek má 2 sloupce: Bezne (běžné účetní období), Minule (minulé účetní období).
		Výnosy (I., II., III. atd.) jsou kladné, náklady (A., B., C. atd.) jsou kladné.
		"""),
		new(ChatRole.User, $"Extrahuj Výkaz zisku a ztráty z těchto tabulek:\n\n{CompactMarkdown(vzzSection)}")
	];

	// ============================================================
	// Markdown compaction (strip noise to reduce input tokens)
	// ============================================================

	/// <summary>
	/// Strip page headers, HTML comments, excessive blank lines and whitespace
	/// from the markdown to reduce GPT input token count.
	/// </summary>
	private static string CompactMarkdown(string md)
	{
		// Remove HTML comments (<!-- PageHeader="..." --> etc.)
		md = HtmlCommentRegex().Replace(md, "");
		// Collapse runs of 3+ blank lines to 2
		md = ExcessiveBlankLinesRegex().Replace(md, "\n\n");
		// Trim trailing whitespace from each line
		md = TrailingWhitespaceRegex().Replace(md, "");
		return md.Trim();
	}

	[GeneratedRegex(@"<!--.*?-->", RegexOptions.Singleline)]
	private static partial Regex HtmlCommentRegex();

	[GeneratedRegex(@"\n{3,}")]
	private static partial Regex ExcessiveBlankLinesRegex();

	[GeneratedRegex(@"[ \t]+$", RegexOptions.Multiline)]
	private static partial Regex TrailingWhitespaceRegex();

	// ============================================================
	// Section extraction helper
	// ============================================================

	private static string ExtractSections(string text, string keyword)
	{
		// Match any markdown heading (# or ##) containing the keyword (case-insensitive).
		// Dynamic pattern — cannot use [GeneratedRegex] due to runtime keyword interpolation.
		var headingPattern = new Regex(
			$@"^(#{{1,3}})\s+.*{Regex.Escape(keyword)}",
			RegexOptions.IgnoreCase | RegexOptions.Multiline);

		var sections = new List<string>();
		var matches = headingPattern.Matches(text);

		for (int i = 0; i < matches.Count; i++)
		{
			int start = matches[i].Index;
			int headingLevel = matches[i].Groups[1].Value.Length; // number of # chars

			// Find the end: next heading of same or higher level that does NOT also match our keyword
			var nextHeadingPattern = new Regex(
				$@"^#{{{1},{headingLevel}}}\s+",
				RegexOptions.Multiline);

			int searchAfter = start + matches[i].Length;
			int end = text.Length;
			var nextMatch = nextHeadingPattern.Match(text, searchAfter);
			while (nextMatch.Success)
			{
				// If the next heading also matches our keyword, include it (merge split tables)
				var nextLine = text.Substring(nextMatch.Index,
					Math.Min(200, text.Length - nextMatch.Index));
				if (nextLine.Contains(keyword, StringComparison.OrdinalIgnoreCase))
				{
					searchAfter = nextMatch.Index + nextMatch.Length;
					nextMatch = nextHeadingPattern.Match(text, searchAfter);
					continue;
				}
				end = nextMatch.Index;
				break;
			}

			sections.Add(text[start..end].Trim());
			// Skip any subsequent matches already included in this section
			while (i + 1 < matches.Count && matches[i + 1].Index < end)
				i++;
		}

		return string.Join("\n\n", sections);
	}

	private static string Fmt(bool v) => v ? "✓" : "✗";
}

/// <summary>
/// Result of processing a single PDF file.
/// </summary>
public class ExtractionResult
{
	public required string SourceFile { get; init; }
	public required UcetniZaverka UcetniZaverka { get; init; }
	public bool JeRozvahaVRovnovaze { get; init; }
	public bool JeVHKonzistentni { get; init; }

	/// <summary>API call metrics (timing, tokens, call counts).</summary>
	public ExtractionMetrics? Metrics { get; init; }

	/// <summary>Rok (ObdobiDo.Year) for sorting and display.</summary>
	public int? Rok => UcetniZaverka.ObdobiDo?.Year;
}
