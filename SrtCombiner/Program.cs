// --- CONFIGURATION ---
// With top-level statements, your code starts executing directly.
// Define the source directory containing the .srt files.
string sourceFolderPath = @"D:\Hoctap\Chung Khoan\Udemy - Fibonacci Technical Analysis Skill for Forex & Stock Trading 2022-5";

string outputFileName = Path.GetFileName(sourceFolderPath);

string outputDirectory = Directory.GetParent(sourceFolderPath).FullName;

string outputFilePath = Path.Combine(outputDirectory, $"{outputFileName}.srt");
// --- END OF CONFIGURATION ---

// Display header information.
Console.WriteLine("SRT File Combiner Initialized (Modern Syntax).");
Console.WriteLine($"Source Folder: {sourceFolderPath}");
Console.WriteLine($"Output File: {outputFilePath}");
Console.WriteLine("------------------------------------");

// The main execution logic with error handling.
try
{
    // Call the core logic function to perform the combination.
    SrtProcessor.CombineSrtFiles(sourceFolderPath, outputFilePath);

    // If the function completes without errors, show a success message.
    Console.WriteLine("------------------------------------");
    Console.WriteLine("Success! All files have been combined into:");
    Console.WriteLine(outputFilePath);
}
catch (DirectoryNotFoundException)
{
    // Handle cases where the source directory does not exist.
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Error: The source directory was not found: {sourceFolderPath}");
    Console.ResetColor();
}
catch (Exception ex)
{
    // Catch any other potential errors during file processing.
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"An unexpected error occurred: {ex.Message}");
    Console.ResetColor();
}

Console.WriteLine("\nPress any key to exit.");
Console.ReadKey();
