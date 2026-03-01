using System;
using System.IO;
using System.IO.Compression;
using System.Text;

internal static class TarGzipUtility
{
    private const int TarBlockSize = 512;

    public static void ExtractTgz(string tgzPath, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);

        using (var fileStream = File.OpenRead(tgzPath))
        using (var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress, leaveOpen: false))
        {
            ExtractTar(gzipStream, destinationDirectory);
        }
    }

    private static void ExtractTar(Stream tarStream, string destinationDirectory)
    {
        byte[] header = new byte[TarBlockSize];
        string destinationRoot = Path.GetFullPath(destinationDirectory);
        string destinationRootWithSeparator = destinationRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
            ? destinationRoot
            : destinationRoot + Path.DirectorySeparatorChar;
        var pathComparison = IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        while (true)
        {
            int read = ReadBlockOrEof(tarStream, header, TarBlockSize);
            if (read == 0)
            {
                break;
            }

            if (read != TarBlockSize)
            {
                throw new InvalidDataException("Unexpected end of tar stream while reading entry header.");
            }

            if (IsZeroBlock(header))
            {
                break;
            }

            string name = ReadNullTerminatedAscii(header, 0, 100);
            string prefix = ReadNullTerminatedAscii(header, 345, 155);
            if (!string.IsNullOrEmpty(prefix))
            {
                name = prefix + "/" + name;
            }

            long size = ReadOctal(header, 124, 12);
            char typeFlag = (char)header[156];

            string relativePath = name.Replace('/', Path.DirectorySeparatorChar);
            string fullPath = Path.GetFullPath(Path.Combine(destinationRoot, relativePath));

            // Guard against path traversal entries (../, absolute paths, mixed separators).
            bool withinDestination = string.Equals(fullPath, destinationRoot, pathComparison) ||
                                     fullPath.StartsWith(destinationRootWithSeparator, pathComparison);
            if (!withinDestination)
            {
                throw new InvalidDataException($"Tar entry path escapes destination folder: {name}");
            }

            if (typeFlag == '5')
            {
                Directory.CreateDirectory(fullPath);
                SkipExact(tarStream, size);
                SkipPadding(tarStream, size);
            }
            else if (typeFlag == '0' || typeFlag == '\0')
            {
                string parentDirectory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(parentDirectory))
                {
                    Directory.CreateDirectory(parentDirectory);
                }

                using (var outputStream = File.Create(fullPath))
                {
                    CopyExact(tarStream, outputStream, size);
                }

                SkipPadding(tarStream, size);
            }
            else
            {
                SkipExact(tarStream, size);
                SkipPadding(tarStream, size);
            }
        }
    }

    private static void SkipPadding(Stream stream, long contentSize)
    {
        long remainder = contentSize % TarBlockSize;
        if (remainder == 0)
        {
            return;
        }

        long bytesToSkip = TarBlockSize - remainder;
        SkipExact(stream, bytesToSkip);
    }

    private static void CopyExact(Stream source, Stream destination, long byteCount)
    {
        byte[] buffer = new byte[81920];
        long remaining = byteCount;

        while (remaining > 0)
        {
            int toRead = (int)Math.Min(buffer.Length, remaining);
            int read = source.Read(buffer, 0, toRead);
            if (read <= 0)
            {
                throw new EndOfStreamException("Unexpected end of stream while reading tar entry content.");
            }

            destination.Write(buffer, 0, read);
            remaining -= read;
        }
    }

    private static void SkipExact(Stream source, long byteCount)
    {
        byte[] buffer = new byte[81920];
        long remaining = byteCount;

        while (remaining > 0)
        {
            int toRead = (int)Math.Min(buffer.Length, remaining);
            int read = source.Read(buffer, 0, toRead);
            if (read <= 0)
            {
                throw new EndOfStreamException("Unexpected end of stream while skipping tar content.");
            }

            remaining -= read;
        }
    }

    private static int ReadBlockOrEof(Stream stream, byte[] buffer, int count)
    {
        int totalRead = 0;

        while (totalRead < count)
        {
            int read = stream.Read(buffer, totalRead, count - totalRead);
            if (read == 0)
            {
                return totalRead;
            }

            totalRead += read;
        }

        return totalRead;
    }

    private static bool IsZeroBlock(byte[] block)
    {
        for (int i = 0; i < block.Length; i++)
        {
            if (block[i] != 0)
            {
                return false;
            }
        }

        return true;
    }

    private static string ReadNullTerminatedAscii(byte[] buffer, int offset, int length)
    {
        int end = offset;
        int max = offset + length;

        while (end < max && buffer[end] != 0)
        {
            end++;
        }

        return Encoding.ASCII.GetString(buffer, offset, end - offset).TrimEnd('\0');
    }

    private static long ReadOctal(byte[] buffer, int offset, int length)
    {
        long value = 0;
        int max = offset + length;
        int index = offset;

        while (index < max && (buffer[index] == 0 || buffer[index] == (byte)' '))
        {
            index++;
        }

        for (; index < max; index++)
        {
            byte character = buffer[index];
            if (character < (byte)'0' || character > (byte)'7')
            {
                break;
            }

            value = (value * 8) + (character - (byte)'0');
        }

        return value;
    }

    private static bool IsWindows()
    {
        return Environment.OSVersion.Platform == PlatformID.Win32NT;
    }
}
