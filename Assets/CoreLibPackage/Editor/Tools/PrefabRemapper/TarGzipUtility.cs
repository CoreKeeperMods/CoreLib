using System;
using System.IO;
using System.Text;
using Unity.SharpZipLib.GZip;
using Unity.SharpZipLib.Tar;

internal static class TarGzipUtility
{
    public static void ExtractTgz(string tgzPath, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);

        using (var fileStream = File.OpenRead(tgzPath))
        using (var gzipStream = new GZipInputStream(fileStream))
        using (var tarStream = new TarInputStream(gzipStream, Encoding.UTF8))
        {
            ExtractTar(tarStream, destinationDirectory);
        }
    }

    private static void ExtractTar(TarInputStream tarStream, string destinationDirectory)
    {
        string destinationRoot = Path.GetFullPath(destinationDirectory);
        string destinationRootWithSeparator = destinationRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
            ? destinationRoot
            : destinationRoot + Path.DirectorySeparatorChar;
        var pathComparison = IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        TarEntry entry;
        while ((entry = tarStream.GetNextEntry()) != null)
        {
            if (string.IsNullOrEmpty(entry.Name))
            {
                continue;
            }

            string relativePath = entry.Name
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);
            string fullPath = Path.GetFullPath(Path.Combine(destinationRoot, relativePath));

            // Guard against path traversal entries (../, absolute paths, mixed separators).
            bool withinDestination = string.Equals(fullPath, destinationRoot, pathComparison) ||
                                     fullPath.StartsWith(destinationRootWithSeparator, pathComparison);
            if (!withinDestination)
            {
                throw new InvalidDataException($"Tar entry path escapes destination folder: {entry.Name}");
            }

            if (entry.IsDirectory)
            {
                Directory.CreateDirectory(fullPath);
            }
            else if (entry.TarHeader.TypeFlag == TarHeader.LF_NORMAL || entry.TarHeader.TypeFlag == TarHeader.LF_OLDNORM)
            {
                string parentDirectory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(parentDirectory))
                {
                    Directory.CreateDirectory(parentDirectory);
                }

                using (var outputStream = File.Create(fullPath))
                {
                    tarStream.CopyEntryContents(outputStream);
                }
            }
        }
    }

    private static bool IsWindows()
    {
        return Environment.OSVersion.Platform == PlatformID.Win32NT;
    }
}
