using System.IO;
using System.Text;

namespace Jolly64Emu.Core
{
    public sealed record T64Entry(string Name, ushort Start, ushort End, int DataOffset, int Length);

    public static class T64Loader
    {
        public static IReadOnlyList<T64Entry> ReadDirectory(byte[] image)
        {
            if (image.Length < 0x40)
                throw new InvalidDataException("T64 image is too short.");

            string signature = Encoding.ASCII.GetString(image, 0, 32).TrimEnd('\0', ' ');
            if (!signature.StartsWith("C64", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Not a recognised C64 T64 tape image.");

            int maximumEntries = image[0x22] | (image[0x23] << 8);
            if (maximumEntries == 0 || maximumEntries > (image.Length - 0x40) / 32)
                throw new InvalidDataException("Invalid T64 directory size.");

            var entries = new List<T64Entry>();
            for (int index = 0; index < maximumEntries; index++)
            {
                int offset = 0x40 + index * 32;
                if (image[offset] != 1)
                    continue;

                int start = image[offset + 2] | (image[offset + 3] << 8);
                int end = image[offset + 4] | (image[offset + 5] << 8);
                uint dataOffset = (uint)(image[offset + 8] | (image[offset + 9] << 8) | (image[offset + 10] << 16) | (image[offset + 11] << 24));
                int length = end - start;

                if (length <= 0 || dataOffset > image.Length || length > image.Length - (long)dataOffset)
                    throw new InvalidDataException($"Invalid T64 data for directory entry {index + 1}.");

                string name = Encoding.ASCII.GetString(image, offset + 16, 16).TrimEnd('\0', ' ');
                entries.Add(new T64Entry(string.IsNullOrWhiteSpace(name) ? $"Program {index + 1}" : name, (ushort)start, (ushort)end, (int)dataOffset, length));
            }

            if (entries.Count == 0)
                throw new InvalidDataException("No loadable programs were found in the T64 image.");

            return entries;
        }

        public static byte[] ExtractPrg(byte[] image, T64Entry entry)
        {
            byte[] prg = new byte[entry.Length + 2];
            prg[0] = (byte)entry.Start;
            prg[1] = (byte)(entry.Start >> 8);
            Array.Copy(image, entry.DataOffset, prg, 2, entry.Length);
            return prg;
        }
    }
}

