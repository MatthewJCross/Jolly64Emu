using JollyC64Emu.C64.CPU;

namespace Jolly64Emu.Core.VIC
{
    public class MOS6560
    {
        private Cpu6510 _cpu;

        public MOS6560(Cpu6510 cpu)
        {
            _cpu = cpu;
        }

        public byte Read(ushort address)
        {
            return 0;
        }

        public void Write(ushort address, byte value)
        {
        }

        public void Clock()
        {
        }
    }
}
