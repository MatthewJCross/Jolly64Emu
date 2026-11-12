using Jolly64Emu.Core.Bus;
using Jolly64Emu.Core.CIA6526;
using Jolly64Emu.Core.Keyboard;
using Jolly64Emu.Core.SID6581;
using Jolly64Emu.Core.VIC;
using JollyC64Emu.C64.CPU;
using System.IO;
using System.Reflection;

namespace Jolly64Emu.Core
{
    public enum InterruptTypeEnum
    {
        NMI,
        IRQ,
        BRK
    }

    public class C64
    {
        public C64Bus Bus { get; }
        public Cpu6510 Cpu { get; }
        public VicII Vic { get; }
        public SID Sid { get; }
        public CIA Cia1 { get; }
        public CIA Cia2 { get; }

        public InputSystem InputSystem { get; private set; }

        private int _divider = 0;

        public C64(bool debug = false)
        {
            InputSystem = new InputSystem();

            Bus = new C64Bus(this);
            Cpu = new Cpu6510(Bus);
            Sid = new SID(Cpu);
            Vic = new VicII(Bus, Cpu);
            Cia1 = new CIA(InputSystem);

            Cia1.RequestInterrupt = () => Cpu.TriggerIRQ(InterruptTypeEnum.IRQ);
            Cia1.PortAWrite = InputSystem.PortAWrite;
            Cia1.PortBRead = InputSystem.PortBRead;

            Cia2 = new CIA();
            LoadRoms();

            Vic.InitVIC();
        }

        public void Reset()
        {
            Cpu.Reset();
            Cia1.Reset();
            Cia2.Reset();
            Sid.Reset();
        }

        public int Step()
        {
            int cycles = 0;
            Vic.Clock();

            if (Bus.BusAvailable)
            {
                cycles = Cpu.ClockCpu();
            }

            if (++_divider == 8)
            {
                _divider = 0;
                Cia1.Clock();
                Cia2.Clock();
            }

            Sid.Clock();

            return cycles;
        }

        private void LoadRoms()
        {
            Bus.LoadRoms(LoadEmbeddedRom("Jolly64Emu.Core.ROMS.Kernal.ROM"), LoadEmbeddedRom("Jolly64Emu.Core.ROMS.Basic.ROM"), LoadEmbeddedRom("Jolly64Emu.Core.ROMS.Char.ROM"));
        }

        public byte[] LoadEmbeddedRom(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using Stream stream = assembly.GetManifestResourceStream(resourceName) ?? throw new FileNotFoundException("Embedded ROM not found", resourceName);

            byte[] data = new byte[stream.Length];
            int offset = 0;
            while (offset < data.Length)
            {
                int read = stream.Read(data, offset, data.Length - offset);
                if (read == 0)
                    throw new EndOfStreamException("Unexpected end of ROM stream.");
                offset += read;
            }

            return data;
        }
    }
}
