using System;

namespace SrtCombiner;

public class AppSettings
{
    // Supported subtitle file extensions (lowercase, include leading dot).
    public string[] SupportedExtensions { get; set; } = new[] { ".srt", ".vtt" };

    // Output format for combined file: "srt" or "vtt". If not specified, inferred from output file extension.
    public string OutputFormat { get; set; } = "srt";

    // Whether to include files in subdirectories when searching.
    public bool IncludeSubdirectories { get; set; } = true;
}
