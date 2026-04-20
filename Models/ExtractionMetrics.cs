namespace AiStructuredDataFromImageExtractionDemo.Models;

/// <summary>
/// Tracks API call metrics (timing, token usage, call counts) for a single PDF extraction.
/// </summary>
public sealed class ExtractionMetrics
{
    /// <summary>Total wall-clock time for the entire extraction pipeline.</summary>
    public TimeSpan TotalDuration { get; set; }

    /// <summary>Time spent in Azure Document Intelligence (PDF → Markdown).</summary>
    public TimeSpan DocumentIntelligenceDuration { get; set; }

    /// <summary>Number of Document Intelligence API calls.</summary>
    public int DocumentIntelligenceCalls { get; set; }

    /// <summary>Whether the Markdown result was served from cache.</summary>
    public bool MarkdownFromCache { get; set; }

    /// <summary>Time spent in GPT-5.1 calls (sum of all calls including retries).</summary>
    public TimeSpan GptTotalDuration { get; set; }

    /// <summary>Total number of GPT-5.1 API calls (initial + retries).</summary>
    public int GptCalls { get; set; }

    /// <summary>Total input (prompt) tokens across all GPT-5.1 calls.</summary>
    public long GptInputTokens { get; set; }

    /// <summary>Total output (completion) tokens across all GPT-5.1 calls.</summary>
    public long GptOutputTokens { get; set; }

    /// <summary>Total tokens (input + output).</summary>
    public long GptTotalTokens => GptInputTokens + GptOutputTokens;

    /// <summary>Individual GPT-5.1 call details.</summary>
    public List<GptCallDetail> GptCallDetails { get; } = [];

    /// <summary>Number of balance-retry iterations performed.</summary>
    public int BalanceRetries { get; set; }
}

/// <summary>
/// Details for a single GPT-5.1 API call.
/// </summary>
public sealed class GptCallDetail
{
    /// <summary>Descriptive label (e.g., "Aktiva", "Pasiva-retry").</summary>
    public required string Label { get; init; }

    /// <summary>Duration of this individual call.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Input tokens for this call.</summary>
    public long InputTokens { get; init; }

    /// <summary>Output tokens for this call.</summary>
    public long OutputTokens { get; init; }

    /// <summary>Whether this call succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>Attempt number (1-based).</summary>
    public int Attempt { get; init; }
}
