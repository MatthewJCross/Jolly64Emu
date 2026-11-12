using System.IO;
using System.Text;

namespace Jolly64Emu.Core
{
    public sealed record D64Entry(string Name, byte FileType, int StartTrack, int StartSector, int Blocks)
    {
        public override string ToString() => $"{Name}  ({Blocks} blocks)";
    }

    public static class D64Loader
    {
        private static readonly int[] SectorsPerTrack = BuildSectors();

        private static int[] BuildSectors()
        {
            int[] sectors = new int[41];
            for (int track = 1; track <= 40; track++)
                sectors[track] = track <= 17 ? 21 : track <= 24 ? 19 : track <= 30 ? 18 : 17;
            return sectors;
        }

        private static int SectorOffset(byte[] image, int track, int sector)
        {
            int tracks = image.Length >= 196608 ? 40 : 35;
            if (track < 1 || track > tracks || sector < 0 || sector >= SectorsPerTrack[track])
                throw new InvalidDataException($"Invalid D64 track/sector {track}/{sector}.");

            int sectorsBefore = 0;
            for (int t = 1; t < track; t++)
                sectorsBefore += SectorsPerTrack[t];

            int offset = (sectorsBefore + sector) * 256;
            if (offset + 256 > image.Length)
                throw new InvalidDataException("D64 image is truncated.");
            return offset;
        }

        private static void ValidateImage(byte[] image)
        {
            if (image.Length != 174848 && image.Length != 175531 &&
                image.Length != 196608 && image.Length != 197376)
                throw new InvalidDataException("Expected a standard 35- or 40-track D64 image (with or without error bytes).");
        }

        private static string DecodeName(byte[] image, int offset, int length)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                byte value = image[offset + i];
                if (value == 0xA0 || value == 0)
                    break;
                builder.Append(value >= 0x20 && value <= 0x7E ? (char)value : '?');
            }
            return builder.ToString().Trim();
        }

        public static IReadOnlyList<D64Entry> ReadDirectory(byte[] image)
        {
            ValidateImage(image);
            var entries = new List<D64Entry>();
            var visited = new HashSet<(int Track, int Sector)>();
            int track = 18;
            int sector = 1;

            while (track != 0)
            {
                if (!visited.Add((track, sector)))
                    throw new InvalidDataException("D64 directory contains a circular sector chain.");

                int offset = SectorOffset(image, track, sector);
                int nextTrack = image[offset];
                int nextSector = image[offset + 1];

                for (int i = 0; i < 8; i++)
                {
                    int entry = offset + i * 32;
                    byte type = image[entry + 2];
                    if ((type & 0x07) != 2 || (type & 0x80) == 0)
                        continue;

                    int startTrack = image[entry + 3];
                    int startSector = image[entry + 4];
                    int blocks = image[entry + 30] | (image[entry + 31] << 8);
                    string name = DecodeName(image, entry + 5, 16);
                    entries.Add(new D64Entry(name, type, startTrack, startSector, blocks));
                }

                track = nextTrack;
                sector = nextSector;
            }

            if (entries.Count == 0)
                throw new InvalidDataException("No closed PRG files found in the D64 directory.");
            return entries;
        }

        public static byte[] ExtractPrg(byte[] image, D64Entry entry)
        {
            ValidateImage(image);
            using var output = new MemoryStream();
            var visited = new HashSet<(int Track, int Sector)>();
            int track = entry.StartTrack;
            int sector = entry.StartSector;

            if (track == 0)
                throw new InvalidDataException("The selected PRG has no data sectors.");

            while (track != 0)
            {
                if (!visited.Add((track, sector)))
                    throw new InvalidDataException("D64 file contains a circular sector chain.");

                int offset = SectorOffset(image, track, sector);
                int nextTrack = image[offset];
                int nextSector = image[offset + 1];
                int count = nextTrack == 0 ? nextSector - 1 : 254;
                if (count < 0 || count > 254)
                    throw new InvalidDataException("Invalid final D64 file sector length.");

                output.Write(image, offset + 2, count);
                if (output.Length > 65538)
                    throw new InvalidDataException("PRG is too large for the current direct memory loader.");

                track = nextTrack;
                sector = nextSector;
            }

            if (output.Length < 3)
                throw new InvalidDataException("The selected D64 PRG is empty or invalid.");
            return output.ToArray();
        }
    }
}
