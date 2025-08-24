using System.Text;
// --- HELPER CLASS DEFINITION ---
// You can still define helper classes and other types in the same file,
// typically after the main executable code. The SrtProcessor logic is unchanged.
public static class SrtProcessor
{
    /// <summary>
    /// Finds all SRT files in a directory and its subdirectories,
    /// and combines them into a single output file.
    /// </summary>
    public static void CombineSrtFiles(string sourceFolderPath, string outputFilePath)
    {
        // Find all .srt files recursively.
        string[] srtFiles = Directory.GetFiles(sourceFolderPath, "*.srt", SearchOption.AllDirectories);

        if (srtFiles.Length == 0)
        {
            Console.WriteLine("Warning: No .srt files found.");
            return;
        }

        Console.WriteLine($"Found {srtFiles.Length} .srt files. Starting the process...");

        // Use a StreamWriter for efficient writing.
        using (StreamWriter writer = new StreamWriter(outputFilePath, false, new UTF8Encoding(true)))
        {
            foreach (string filePath in srtFiles)
            {
                string fileName = Path.GetFileName(filePath);
                Console.WriteLine($"Processing: {fileName}");

                writer.Write($"--- START OF: {fileName} ---\n\n");
                writer.Write(File.ReadAllText(filePath));
                writer.Write("\n\n");
            }
        }
    }
}