using Jolly64Emu.Core;
using Jolly64Emu.Core.Bus;
using Jolly64Emu.Core.CIA6526;
using System.Xml.Serialization;

namespace JollyC64Emu.C64.CPU
{
    public class Cpu6510
    {
        private readonly Cpu6502 _cpu;
        private readonly C64Bus _bus;

        // 6510 I/O port
        private byte _ddr = 0x00;   // $0000
        private byte _data = 0xFF;   // $0001 (pulled high)

        public Instruction CurrentInstruction => _cpu.CurrentInstruction;

        public uint Instructions;
        private int _instructionCyclesLeft;

        public Cpu6510(C64Bus bus)
        {
            _cpu = new Cpu6502(bus);
            _bus = bus;
        }

        public void Reset()
        {
            _ddr = 0x2F;
            _data = 0x37;
            _bus.Write(0x0000, _ddr);

            // C64 power-on default ($37)
            Write(0x0001, 0x37);

            _cpu.Reset();
        }

        public int ClockCpu()
        {
            _bus._c64.Cia1.Count();
            _bus._c64.Cia2.Count();

            if (_instructionCyclesLeft > 0)
            {
                _instructionCyclesLeft--;
                return 0;
            }

            int cycles = ExecuteInstruction();
            _instructionCyclesLeft = Math.Max(0, cycles - 1);
            return cycles;
        }

        public int ExecuteInstruction()
        {
            Instructions = _cpu.Instructions;
            return _cpu.ExecuteInstruction();
        }

        public byte Read(ushort address)
        {
            return _bus.Read(address);
        }

        public void Write(ushort address, byte value)
        {
            if (address == 0x0000)
            {
                _ddr = value;
                _bus.Write(address, value);
                return;
            }

            if (address == 0x0001)
            {
                _data = value;
                _bus.Write(address, value);
                return;
            }

            _bus.Write(address, value);
        }

        public void TriggerIRQ(InterruptTypeEnum type)
        {
            _cpu.RequestIRQ(type);
        }

        public string FormatLastExecutedInstruction()
        {
            var instr = CurrentInstruction;
            if (instr == null)
                return "???";

            byte op1 = Read((ushort)(_cpu.lastPC + 1));
            byte op2 = Read((ushort)(_cpu.lastPC + 2));
            ushort addr = (ushort)(op1 | (op2 << 8));

            sbyte offset = (sbyte)op1;
            ushort target = (ushort)(_cpu.lastPC + 2 + offset);

            string str = " " + instr.Mode switch
            {
                AddressingMode.Implied => $"{_cpu.lastPC:X4} {instr.Opcode:X2}        {instr.Mnemonic}",
                AddressingMode.Accumulator => $"{_cpu.lastPC:X4} {instr.Opcode:X2}        {instr.Mnemonic} A",
                AddressingMode.Immediate => $"{_cpu.lastPC:X4} {instr.Opcode:X2} {op1:X2}     {instr.Mnemonic} #${op1:X2}",
                AddressingMode.ZeroPage => $"{_cpu.lastPC:X4} {instr.Opcode:X2} {op1:X2}     {instr.Mnemonic} ${op1:X2}",
                AddressingMode.ZeroPageX => $"{_cpu.lastPC:X4} {instr.Opcode:X2} {op1:X2}     {instr.Mnemonic} ${op1:X2},X",
                AddressingMode.ZeroPageY => $"{_cpu.lastPC:X4} {instr.Opcode:X2} {op1:X2}     {instr.Mnemonic} ${op1:X2},Y",
                AddressingMode.Absolute => $"{_cpu.lastPC:X4} {instr.Opcode:X2} {op1:X2} {op2:X2}  {instr.Mnemonic} ${addr:X4}",
                AddressingMode.AbsoluteX => $"{_cpu.lastPC:X4} {instr.Opcode:X2} {op1:X2} {op2:X2}  {instr.Mnemonic} ${addr:X4},X",
                AddressingMode.AbsoluteY => $"{_cpu.lastPC:X4} {instr.Opcode:X2} {op1:X2} {op2:X2}  {instr.Mnemonic} ${addr:X4},Y",
                AddressingMode.Indirect => $"{_cpu.lastPC:X4} {instr.Opcode:X2}        {instr.Mnemonic} (${addr:X4})",
                AddressingMode.IndexedIndirect => $"{_cpu.lastPC:X4} {instr.Opcode:X2}        {instr.Mnemonic} (${op1:X2},X)",
                AddressingMode.IndirectIndexed => $"{_cpu.lastPC:X4} {instr.Opcode:X2} {op1:X2}     {instr.Mnemonic} (${op1:X2}),Y",
                AddressingMode.Relative => $"{_cpu.lastPC:X4} {instr.Opcode:X2} {op1:X2}     {instr.Mnemonic} ${target:X4}",
                _ => $"{_cpu.lastPC:X4} {instr.Opcode:X2}       {instr.Opcode:X2}        {instr.Mnemonic} ???"
            };

            str = str.PadRight(29);
            str += $"A:{_cpu.lastA:X2} X:{_cpu.lastX:X2} Y:{_cpu.lastY:X2} P:{_cpu.lastP:X2} SP:{_cpu.lastSP:X2}";

            return str;
        }
    }
}


