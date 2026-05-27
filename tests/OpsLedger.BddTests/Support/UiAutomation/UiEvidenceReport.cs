#if WINDOWS
using System.Net;

namespace OpsLedger.BddTests.Support.UiAutomation;

public sealed class UiEvidenceReport
{
    private const string EvidenceDirectoryVariableName = "OPSLEDGER_UI_EVIDENCE_DIR";
    private readonly List<UiEvidenceEntry> entries = [];
    private readonly string evidenceDirectory;
    private readonly string screenshotDirectory;
    private readonly string reportPath;

    public UiEvidenceReport(string scenarioName)
    {
        string? configuredDirectory = Environment.GetEnvironmentVariable(EvidenceDirectoryVariableName);
        evidenceDirectory = string.IsNullOrWhiteSpace(configuredDirectory)
            ? Path.Combine("artifacts", "test-results", "bdd", "ui-evidence")
            : configuredDirectory;
        screenshotDirectory = Path.Combine(evidenceDirectory, "screenshots");
        reportPath = Path.Combine(evidenceDirectory, "index.html");

        Directory.CreateDirectory(screenshotDirectory);
        ScenarioName = scenarioName;
        WriteReport();
    }

    public string ScenarioName { get; }

    public void Capture(string label, Action<string> capture)
    {
        string fileName = $"{entries.Count + 1:00}-{ToSlug(label)}.png";
        string screenshotPath = Path.Combine(screenshotDirectory, fileName);
        capture(screenshotPath);

        entries.Add(new UiEvidenceEntry(
            label,
            Path.Combine("screenshots", fileName).Replace('\\', '/'),
            DateTimeOffset.UtcNow));

        WriteReport();
    }

    private void WriteReport()
    {
        using StreamWriter writer = new(reportPath, false);
        writer.WriteLine("<!doctype html>");
        writer.WriteLine("<html lang=\"en\">");
        writer.WriteLine("<head>");
        writer.WriteLine("  <meta charset=\"utf-8\">");
        writer.WriteLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        writer.WriteLine($"  <title>{Encode(ScenarioName)} UI evidence</title>");
        writer.WriteLine("  <style>");
        writer.WriteLine("    body { font-family: Segoe UI, Arial, sans-serif; margin: 32px; color: #172026; background: #f7f9fb; }");
        writer.WriteLine("    h1 { margin-bottom: 4px; font-size: 28px; }");
        writer.WriteLine("    .meta { color: #5b6872; margin-bottom: 24px; }");
        writer.WriteLine("    .entry { background: #fff; border: 1px solid #d8e0e7; border-radius: 8px; padding: 16px; margin-bottom: 20px; }");
        writer.WriteLine("    .entry h2 { font-size: 18px; margin: 0 0 4px; }");
        writer.WriteLine("    .time { color: #687783; font-size: 13px; margin-bottom: 12px; }");
        writer.WriteLine("    img { width: 100%; max-width: 1200px; border: 1px solid #d8e0e7; border-radius: 6px; background: #fff; }");
        writer.WriteLine("  </style>");
        writer.WriteLine("</head>");
        writer.WriteLine("<body>");
        writer.WriteLine($"  <h1>{Encode(ScenarioName)}</h1>");
        writer.WriteLine($"  <div class=\"meta\">Generated {Encode(DateTimeOffset.UtcNow.ToString("O"))}</div>");

        if (entries.Count == 0)
        {
            writer.WriteLine("  <p>No screenshots captured yet.</p>");
        }

        foreach (UiEvidenceEntry entry in entries)
        {
            writer.WriteLine("  <section class=\"entry\">");
            writer.WriteLine($"    <h2>{Encode(entry.Label)}</h2>");
            writer.WriteLine($"    <div class=\"time\">{Encode(entry.CapturedAtUtc.ToString("O"))}</div>");
            writer.WriteLine($"    <img src=\"{Encode(entry.RelativePath)}\" alt=\"{Encode(entry.Label)}\">");
            writer.WriteLine("  </section>");
        }

        writer.WriteLine("</body>");
        writer.WriteLine("</html>");
    }

    private static string ToSlug(string value)
    {
        char[] characters = value.ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray();

        return string.Join('-', new string(characters)
            .Split('-', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string Encode(string value)
    {
        return WebUtility.HtmlEncode(value);
    }

    private sealed record UiEvidenceEntry(
        string Label,
        string RelativePath,
        DateTimeOffset CapturedAtUtc);
}
#endif
