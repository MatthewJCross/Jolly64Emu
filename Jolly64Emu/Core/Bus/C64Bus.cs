using Jolly64Emu.Core.CIA6526;
using Jolly64Emu.Core.SID6581;

namespace Jolly64Emu.Core.Bus
{
    public class C64Bus : IBus
    {
        public C64 _c64;
        public byte[] _ram = new byte[65536];
        public byte[] _colorRam = new byte[1024];

        private byte[] _vicRegisters = new byte[0x40];

        public byte[] CharRom { get; set; } = new byte[4096];
        public byte[] BasicRom { get; set; } = new byte[8192];
        public byte[] KernalRom { get; set; } = new byte[8192];

        private byte _ioPort = 0x37;
        private byte _ioPortDDR = 0x2F;
        private byte _ioPortDR = 0xF7;

        public bool BusAvailable = true;

        public C64Bus(C64 c64)
        {
            _c64 = c64;
            InitializeRamAndZeroPage();
        }

        public void LoadRoms(byte[] kernalRom, byte[] basicRom, byte[] characterRom)
        {
            KernalRom = kernalRom;
            BasicRom = basicRom;
            CharRom = characterRom;
        }

        private void InitializeRamAndZeroPage()
        {
            // Clear full RAM
            for (int i = 0; i < 0x10000; i++)
                _ram[i] = 0x00;

            // Zero page defaults
            _ram[0x00] = 0x2F; // CPU port 0 (memory config)
            _ram[0x01] = 0x37; // CPU port 1

            // KERNAL scratch pointers
            _ram[0xC1] = 0x00; // low byte of test pointer
            _ram[0xC2] = 0x20; // high byte → points to $2000 free RAM

            // BASIC pointers (minimal)
            _ram[0x2D] = 0x00; // start of BASIC program
            _ram[0x2E] = 0x08;
            _ram[0x2F] = 0x00; // end of BASIC program
            _ram[0x30] = 0x08;

            // More zero-page scratch can remain zeroed
            // Some KERNAL routines use $11-$12, $22-$23, etc.
            for (int i = 0x10; i < 0x40; i++)
                _ram[i] = 0x00;

            // I/O ports for VIC
            _ram[0xD020] = 0x06; // border color
            _ram[0xD021] = 0x00; // background color

            UpdateIOPort();
        }

        private void UpdateIOPort()
        {
            var output = _ioPort;
            output &= _ioPortDDR;
            _ioPortDR &= (byte)(_ioPortDDR ^ 0xFF);
            _ioPortDR |= output;
        }

        private byte MemoryConfiguration => (byte)((_ioPort & _ioPortDDR) | (~_ioPortDDR & 0xFF));

        public byte Read(ushort address)
        {
            if (address == 0x0000)
                return _ioPortDDR;
            if (address == 0x0001)
                return MemoryConfiguration;

            byte config = MemoryConfiguration;
            bool loram = (config & 1) != 0;
            bool hiram = (config & 2) != 0;
            bool charen = (config & 4) != 0;

            if (address >= 0xE000 && hiram)
                return KernalRom[address - 0xE000];
            if (address >= 0xA000 && address <= 0xBFFF && loram && hiram)
                return BasicRom[address - 0xA000];
            if (address >= 0xD000 && address <= 0xDFFF && (loram || hiram))
                return charen ? ReadIO(address) : CharRom[address - 0xD000];
            return _ram[address];
        }

        public byte CharROMRead(ushort address) => Read(address);

        private byte ReadIO(ushort address) => address switch
        {
            >= 0xD000 and <= 0xD3FF => _c64.Vic.Read(address),
            >= 0xD400 and <= 0xD7FF => _c64.Sid.Read(address),
            >= 0xD800 and <= 0xDBFF => (byte)(_colorRam[address - 0xD800] & 0x0F),
            >= 0xDC00 and <= 0xDCFF => _c64.Cia1.Read(address),
            >= 0xDD00 and <= 0xDDFF => _c64.Cia2.Read(address),
            _ => 0xFF
        };

        public void Write(ushort address, byte value)
        {
            if (address == 0x0000)
            {
                _ioPortDDR = value;
                _ram[address] = value;
                UpdateIOPort();
                return;
            }
            if (address == 0x0001)
            {
                _ioPort = value;
                _ram[address] = value;
                UpdateIOPort();
                return;
            }

            _ram[address] = value; // RAM remains writable underneath ROM and I/O.
            byte config = MemoryConfiguration;
            if (address >= 0xD000 && address <= 0xDFFF && (config & 3) != 0 && (config & 4) != 0)
                CharROMWrite(address, value);
        }

        public void CharROMWrite(ushort address, byte value)
        {
            if (address >= 0xD000 && address <= 0xD3FF)
                _c64.Vic.Write(address, value);
            else if (address >= 0xD400 && address <= 0xD7FF)
                _c64.Sid.Write(address, value);
            else if (address >= 0xD800 && address <= 0xDBFF)
                _colorRam[address - 0xD800] = (byte)(value & 0x0F);
            else if (address >= 0xDC00 && address <= 0xDCFF)
                _c64.Cia1.Write(address, value);
            else if (address >= 0xDD00 && address <= 0xDDFF)
                _c64.Cia2.Write(address, value);
        }

        public void WriteRAM(ushort address, byte value)
        {
            _ram[address] = value;
        }

        public byte ReadVicRegister(byte reg)
        {  
            return _vicRegisters[reg]; 
        }

        public void WriteVicRegister(byte reg, byte value)
        {
            _vicRegisters[reg] = value;
        }

        // helper for testing
        public void Poke(ushort addr, byte val) => _ram[addr] = val;
    }
}
