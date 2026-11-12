using Jolly64Emu.Core.Bus;
using System.IO;

namespace Jolly64Emu.Core
{
    public static class PrgLoader
    {
        public static (ushort Start, ushort End) Load(C64Bus bus, byte[] prg)
        {
            if (prg.Length < 3)
                throw new InvalidDataException("PRG must contain a two-byte load address and data.");

            int start = prg[0] | (prg[1] << 8);
            int length = prg.Length - 2;
            if (start + length > 65536)
                throw new InvalidDataException("PRG extends beyond C64 address space.");

            Array.Copy(prg, 2, bus._ram, start, length);
            int end = start + length;
            if (start == 0x0801)
            {
                // BASIC TXTTAB, VARTAB, ARYTAB, STREND; pointers to the end of program.
                foreach (int pointer in new[] { 0x2D, 0x2F, 0x31, 0x33 })
                {
                    bus._ram[pointer] = (byte)end;
                    bus._ram[pointer + 1] = (byte)(end >> 8);
                }
            }
            return ((ushort)start, (ushort)(end & 0xFFFF));
        }
    }
}
