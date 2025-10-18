using System;
using System.IO;
using System.IO.Compression;

namespace CodeGenWebApp.Helpers
{
    public static class ZipUtility
    {
        public static void CreateZipFromDirectory(string sourceDirectoryPath, string destinationZipFilePath)
        {
            try
            {
                // Ensure the source directory exists
                if (!Directory.Exists(sourceDirectoryPath))
                {
                    Console.WriteLine($"Source directory not found: {sourceDirectoryPath}");
                    return;
                }

                // If the destination zip file already exists, delete it to avoid errors
                if (File.Exists(destinationZipFilePath))
                {
                    File.Delete(destinationZipFilePath);
                }

                // Create the ZIP archive from the specified directory
                ZipFile.CreateFromDirectory(sourceDirectoryPath, destinationZipFilePath, CompressionLevel.Optimal, true);

                Console.WriteLine($"Successfully created ZIP archive at: {destinationZipFilePath}");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"An I/O error occurred: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Access denied: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
            }
        }
    }
}
