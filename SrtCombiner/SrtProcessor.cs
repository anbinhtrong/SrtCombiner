using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using SrtCombiner;

// --- HELPER CLASS DEFINITION ---
// You can still define helper classes and other types in the same file,
// typically after the main executable code. The SrtProcessor logic is extended
// to support .vtt files and normalizes both formats into a common Cue model.
public static class SrtProcessor
{
    private class Cue
    {
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
        public string Text { get; set; } = string.Empty;
        public string SourceFile { get; set; } = string.Empty;
    }

    /// <summary>
    /// Finds all supported subtitle files in a directory and its subdirectories,
    /// and combines them into a single output file. Supports .srt and .vtt files.
    /// Writes a marker before each file and then the original file content. This is
    /// intended for producing a large text file for notebook/LM ingestion rather
    /// than producing a single valid subtitle file.
    /// </summary>
    public static void CombineSrtFiles(string sourceFolderPath, string outputFilePath)
    {
        var settings = new AppSettings();
        var searchOption = settings.IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        // Gather files with supported extensions.
        var allFiles = Directory.GetFiles(sourceFolderPath, "*.*", searchOption)
            .Where(f => settings.SupportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .ToArray();

        if (allFiles.Length == 0)
        {
            Console.WriteLine("Warning: No supported subtitle files found.");
            return;
        }

        Console.WriteLine($"Found {allFiles.Length} subtitle files. Starting the process...");

        // Open output writer and write raw file contents with markers for each source file.
        using (StreamWriter writer = new StreamWriter(outputFilePath, false, new UTF8Encoding(true)))
        {
            foreach (string filePath in allFiles)
            {
                string fileName = Path.GetFileName(filePath);
                Console.WriteLine($"Processing: {fileName}");

                try
                {
                    writer.WriteLine($"--- START OF: {fileName} ---");
                    writer.WriteLine();

                    // Write the raw contents of the subtitle file so the combined file contains
                    // the original cues and formatting (useful for notebook/LM ingestion).
                    var raw = File.ReadAllText(filePath);
                    writer.WriteLine(raw.TrimEnd());
                    writer.WriteLine();
                    writer.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Warning: Failed to read '{fileName}': {ex.Message}");
                    Console.ResetColor();
                }
            }
        }

        Console.WriteLine("Finished writing combined raw subtitle file.");
    }

    // The following parsing helpers are preserved for potential future use but are
    // not used by the raw-combine flow above. They remain available if you later
    // choose to switch back to a cue-based merge.

    private static List<Cue> ParseSubtitleFile(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        var text = File.ReadAllText(path);

        return ext switch
        {
            ".srt" => ParseSrt(text),
            ".vtt" => ParseVtt(text),
            _ => new List<Cue>()
        };
    }

    private static List<Cue> ParseSrt(string content)
    {
        var lines = Regex.Split(content, "\r?\n");
        var cues = new List<Cue>();
        var timestampRegex = new Regex(@"(?<start>\d{1,2}:\d{2}:\d{2}[,\.]\d{3})\s*--?>\s*(?<end>\d{1,2}:\d{2}:\d{2}[,\.]\d{3})");

        int i = 0;
        while (i < lines.Length)
        {
            // Skip empty lines
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                i++; continue;
            }

            // Optional sequence number
            if (int.TryParse(lines[i].Trim(), out _))
            {
                i++; // skip sequence number
            }

            if (i >= lines.Length) break;

            // Timestamp line
            var m = timestampRegex.Match(lines[i]);
            if (!m.Success)
            {
                i++; continue; // not a timestamp, skip
            }

            var startRaw = m.Groups["start"].Value;
            var endRaw = m.Groups["end"].Value;
            var start = ParseTimestamp(startRaw);
            var end = ParseTimestamp(endRaw);

            i++;
            var textBuilder = new StringBuilder();
            while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i]))
            {
                textBuilder.AppendLine(lines[i]);
                i++;
            }

            cues.Add(new Cue { Start = start, End = end, Text = textBuilder.ToString().TrimEnd() });
        }

        return cues;
    }

    private static List<Cue> ParseVtt(string content)
    {
        var lines = Regex.Split(content, "\r?\n");
        var cues = new List<Cue>();

        var timestampRegex = new Regex(@"(?<start>\d{1,2}:\d{2}:\d{2}[\.,]\d{3}|\d{1,2}:\d{2}[\.,]\d{3})\s*--?>\s*(?<end>\d{1,2}:\d{2}:\d{2}[\.,]\d{3}|\d{1,2}:\d{2}[\.,]\d{3})");

        int i = 0;

        // Skip optional WEBVTT header and any header metadata
        if (i < lines.Length && lines[i].TrimStart().StartsWith("WEBVTT", StringComparison.OrdinalIgnoreCase))
        {
            i++;
            // skip until empty line after header
            while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i])) i++;
        }

        while (i < lines.Length)
        {
            // Skip blank lines
            if (string.IsNullOrWhiteSpace(lines[i])) { i++; continue; }

            // There may be a cue identifier line (not containing -->)
            // If current line does not match timestamp, and next line does, treat current as identifier and skip it.
            if (!timestampRegex.IsMatch(lines[i]))
            {
                if (i + 1 < lines.Length && timestampRegex.IsMatch(lines[i + 1]))
                {
                    i++; // skip identifier
                }
                else
                {
                    i++; continue; // unrecognized line
                }
            }

            if (i >= lines.Length) break;

            var m = timestampRegex.Match(lines[i]);
            if (!m.Success)
            {
                i++; continue;
            }

            var startRaw = m.Groups["start"].Value;
            var endRaw = m.Groups["end"].Value;
            var start = ParseTimestamp(startRaw);
            var end = ParseTimestamp(endRaw);

            i++;
            var textBuilder = new StringBuilder();
            while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i]))
            {
                // Skip cue settings lines that sometimes appear after the timestamp (e.g. position:50%)
                textBuilder.AppendLine(lines[i]);
                i++;
            }

            cues.Add(new Cue { Start = start, End = end, Text = textBuilder.ToString().TrimEnd() });
        }

        return cues;
    }

    private static TimeSpan ParseTimestamp(string raw)
    {
        raw = raw.Trim();
        raw = raw.Replace(',', '.');

        // Try standard formats with and without hours.
        string[] formats = new[] { @"hh\:mm\:ss\.fff", @"h\:mm\:ss\.fff", @"mm\:ss\.fff", @"m\:ss\.fff" };
        if (TimeSpan.TryParseExact(raw, formats, CultureInfo.InvariantCulture, out var ts))
            return ts;

        // Fallback: try to parse components manually
        var parts = raw.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
        try
        {
            if (parts.Length == 3)
            {
                int h = int.Parse(parts[0]);
                int m = int.Parse(parts[1]);
                var secParts = parts[2].Split('.');
                int s = int.Parse(secParts[0]);
                int ms = secParts.Length > 1 ? int.Parse(secParts[1].PadRight(3, '0')) : 0;
                return new TimeSpan(0, h, m, s, ms);
            }
            else if (parts.Length == 2)
            {
                int m = int.Parse(parts[0]);
                var secParts = parts[1].Split('.');
                int s = int.Parse(secParts[0]);
                int ms = secParts.Length > 1 ? int.Parse(secParts[1].PadRight(3, '0')) : 0;
                return new TimeSpan(0, 0, m, s, ms);
            }
        }
        catch { }

        throw new FormatException($"Unrecognized timestamp format: '{raw}'");
    }

    private static string FormatSrtTimestamp(TimeSpan t) => t.ToString(@"hh\:mm\:ss\,fff", CultureInfo.InvariantCulture);
    private static string FormatVttTimestamp(TimeSpan t) => t.ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture);
}