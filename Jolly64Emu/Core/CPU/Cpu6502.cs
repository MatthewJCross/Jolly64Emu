using Jolly64Emu.Core;
using Jolly64Emu.Core.Bus;
using System.Diagnostics;

namespace JollyC64Emu.C64.CPU
{
    [Flags]
    public enum Flag : byte
    {
        C = 0x01,
        Z = 0x02,
        I = 0x04,
        D = 0x08,
        B = 0x10,
        U = 0x20,
        V = 0x40,
        N = 0x80
    }

    public sealed class Cpu6502
    {
        C64Bus _bus;
            
        public byte A, X, Y, SP, P;              // Registers
        public ushort PC;                        // Program Counter
        public byte[] Memory = new byte[65536];  // 64K CPU memory      
        public int Cycles;                       // Clock cycle counter
        public uint Instructions;

        public bool[] _interrupts = new bool[3];
        private bool _irqPending = false;
        
        public bool IsHalted { get; private set; }

        public ushort lastPC;
        public byte lastA, lastX, lastY, lastSP, lastP;              

        // Dictionary for fast opcode lookup
        public Dictionary<byte, Instruction> OpcodeMap { get; private set; } = new Dictionary<byte, Instruction>();
        public Instruction CurrentInstruction { get; private set; } = new Instruction();

        public Cpu6502(C64Bus bus)
        {
            _bus = bus;
            BuildOpcodeMap();
        }

        // Build dictionary from your InstructionSet
        private void BuildOpcodeMap()
        {
            OpcodeMap = InstructionSet.Instructions.ToDictionary(instr => instr.Opcode);
        }

        public void Reset()
        {
            // Initialize registers
            A = 0;
            X = 0;
            Y = 0;
            SP = 0xFD;             // NMOS default

            SetFlag(Flag.C, false);
            SetFlag(Flag.Z, false);
            SetFlag(Flag.I, true);   // IRQ disabled
            SetFlag(Flag.D, false);
            SetFlag(Flag.B, false);
            SetFlag(Flag.V, false);
            SetFlag(Flag.N, false);
            SetFlag(Flag.U, true);   // unused flag always set

            PC = ReadWord(0xFFFC); // reset vector
            Cycles = 0;
            Instructions = 0;

            for (var i = 0; i < _interrupts.Length; i++)
            {
                _interrupts[i] = false;
            }
        }

        // Read a byte from memory
        public byte Read(ushort addr)
        {
            return _bus.Read(addr);
        }

        // Write a byte to memory
        public void Write(ushort addr, byte value)
        {  
            _bus.Write(addr, value); 
        }

        public ushort ReadWord(ushort addr)
        {
            byte lo = _bus.Read(addr);
            byte hi = _bus.Read((ushort)(addr + 1));
            return (ushort)(lo | (hi << 8));
        }

        public void Push(byte value)
        {
            Write((ushort)(0x0100 | SP), value);
            SP--;
        }

        public void PushWord(ushort value)
        {
            // 6502 pushes high byte first
            Push((byte)(value >> 8));
            Push((byte)(value & 0xFF));
        }

        public byte Pop()
        {
            SP++;
            return Read((ushort)(0x0100 | SP));
        }

        public ushort PopWord()
        {
            byte lo = Pop();
            byte hi = Pop();
            return (ushort)(lo | (hi << 8));
        }

        // Fetch operand according to addressing mode
        public ushort FetchAddress(Instruction instr)
        {
            ushort addr = 0;
            byte zp = 0;

            switch (instr.Mode)
            {
                case AddressingMode.Immediate:
                    return PC++; // immediate value is next byte

                case AddressingMode.ZeroPage:
                    return Read(PC++);

                case AddressingMode.ZeroPageX:
                    return (byte)(Read(PC++) + X);

                case AddressingMode.ZeroPageY:
                    return (byte)(Read(PC++) + Y);

                case AddressingMode.Absolute:
                    addr = (ushort)(Read(PC) | (Read((ushort)(PC + 1)) << 8));
                    PC += 2;
                    return addr;

                case AddressingMode.AbsoluteX:
                    addr = (ushort)((Read(PC) | (Read((ushort)(PC + 1)) << 8)) + X);
                    PC += 2;
                    return addr;

                case AddressingMode.AbsoluteY:
                    addr = (ushort)((Read(PC) | (Read((ushort)(PC + 1)) << 8)) + Y);
                    PC += 2;
                    return addr;

                case AddressingMode.IndexedIndirect:
                    zp = (byte)(Read(PC++) + X);
                    addr = (ushort)(Read(zp) | (Read((byte)(zp + 1)) << 8));
                    return addr;

                case AddressingMode.IndirectIndexed:
                    zp = Read(PC++);
                    addr = (ushort)((Read(zp) | (Read((byte)(zp + 1)) << 8)) + Y);
                    return addr;

                case AddressingMode.Relative:
                    sbyte offset = (sbyte)Read(PC++);
                    return (ushort)(PC + offset);

                case AddressingMode.Implied:
                case AddressingMode.Accumulator:
                    return 0; // No address needed

                case AddressingMode.Indirect:
                    ushort ptr = (ushort)(Read(PC) | (Read((ushort)(PC + 1)) << 8));
                    PC += 2;
                    addr = (ushort)(Read(ptr) | (Read((ushort)(ptr + 1)) << 8));
                    return addr;

                default:
                    throw new NotImplementedException($"AddressingMode {instr.Mode} not implemented");
            }
        }

        // Execute next instruction
        public int ExecuteInstruction()
        {
            if (IsHalted)
                return 0;

            for (var i = 0; i < _interrupts.Length; i++)
            {
                if (_interrupts[i])
                {
                    _interrupts[i] = false;
                    switch ((InterruptTypeEnum)i)
                    {
                        case InterruptTypeEnum.NMI:
                            return NonMaskableInterrupt();
                        case InterruptTypeEnum.IRQ:
                            return InterruptRequest();
                    }
                }
            }

            //if (_irqPending && !GetFlag(Flag.I))
            //{
            //    HandleIRQ();
            //    _irqPending = false;
            //}

            lastPC = PC;
            lastX = X;
            lastY = Y;
            lastA = A;
            lastP = P;
            lastSP = SP;

            byte opcode = Read(PC++);
            CurrentInstruction = InstructionSet.Instructions[opcode];

            CurrentInstruction.Execute(this);
            Cycles += CurrentInstruction.Cycles;
            Instructions++;

            return CurrentInstruction.Cycles;
        }

        public void SetFlag(Flag flag, bool state)
        {
            if (state)
                P |= (byte)flag;
            else
                P &= (byte)~flag;
        }

        public bool GetFlag(Flag flag)
        {
            return (P & (byte)flag) != 0;
        }

        public byte GetStatusFlags()
        {
            byte flags = 0;

            if (GetFlag(Flag.N)) flags |= 0x80;  // Negative
            if (GetFlag(Flag.V)) flags |= 0x40;  // Overflow
            flags |= 0x20;                       // Unused, always set in push
            if (GetFlag(Flag.B)) flags |= 0x10;  // Break (PHP/BRK)
            if (GetFlag(Flag.D)) flags |= 0x08;  // Decimal
            if (GetFlag(Flag.I)) flags |= 0x04;  // Interrupt Disable
            if (GetFlag(Flag.Z)) flags |= 0x02;  // Zero
            if (GetFlag(Flag.C)) flags |= 0x01;  // Carry

            return flags;
        }

        public void SetNZFlags(byte value)
        {
            // Zero
            if (value == 0) 
                P |= 0x02; 
            else 
                P &= 0xFD;

            // Negative
            if ((value & 0x80) != 0) 
                P |= 0x80; 
            else 
                P &= 0x7F;
        }

        public void RequestIRQ(InterruptTypeEnum type)
        {
            if (!GetFlag(Flag.I) || type == InterruptTypeEnum.NMI)
            {
                _interrupts[(int)type] = true;
            }

            _irqPending = true;
        }

        public int NonMaskableInterrupt()
        {
            PushWord(PC);
            SetFlag(Flag.B, false);
            SetFlag(Flag.U, true);
            Push(P);
            SetFlag(Flag.I, true);
            PC = (ushort)(Read(0xFFFA) + Read(0xFFFB) * 0x100); // Jump to NMI handler
            return 7;
        }

        public int InterruptRequest()
        {
            PushWord(PC);
            SetFlag(Flag.B, false);
            SetFlag(Flag.U, true);
            Push(P);
            SetFlag(Flag.I, true);
            PC = (ushort)(Read(0xFFFE) + Read(0xFFFF) * 0x100); // Jump to IRQ handler
            return 7;
        }

        private void HandleIRQ()
        {
            PushWord(PC);

            byte statusToPush = (byte)((P & ~(byte)Flag.B) | (byte)Flag.U);
            Push(statusToPush);

            SetFlag(Flag.I, true);

            ushort lo = Read(0xFFFE);
            ushort hi = Read(0xFFFF);

            PC = (ushort)((hi << 8) | lo);
        }

        public void ADC()
        {
            ushort addr = FetchAddress(CurrentInstruction);
            byte value = Read(addr);

            int carry = GetFlag(Flag.C) ? 1 : 0;
            int sum = A + value + carry;

            // Carry flag
            SetFlag(Flag.C, sum > 0xFF);

            byte result = (byte)sum;

            // Overflow flag
            SetFlag(Flag.V, (~(A ^ value) & (A ^ result) & 0x80) != 0);

            A = result;

            // Negative & Zero
            SetNZFlags(A);
        }

        public void AND()
        {
            ushort addr = FetchAddress(CurrentInstruction);
            byte value = Read(addr);

            A &= value;
            SetNZFlags(A);
        }

        public void ASL()
        {
            if (CurrentInstruction.Mode == AddressingMode.Accumulator)
            {
                // Shift A
                SetFlag(Flag.C, (A & 0x80) != 0);
                A <<= 1;
                SetNZFlags(A);
            }
            else
            {
                // Shift memory
                ushort addr = FetchAddress(CurrentInstruction);
                byte value = Read(addr);

                SetFlag(Flag.C, (value & 0x80) != 0);
                value <<= 1;

                Write(addr, value);
                SetNZFlags(value);
            }
        }

        public void AXS()
        {
            // Fetch operand
            ushort addr = FetchAddress(CurrentInstruction);
            byte value = Read(addr);

            byte temp = (byte)(A & X);
            X = (byte)(temp - value);

            // Set flags like SBC
            SetFlag(Flag.C, temp >= value);
            SetFlag(Flag.Z, X == 0);
            SetFlag(Flag.N, (X & 0x80) != 0);
        }

        public void BCC()
        {
            sbyte offset = (sbyte)Read(PC++); // fetch signed 8-bit offset

            if (!GetFlag(Flag.C))
            {
                ushort oldPC = PC;
                PC = (ushort)(PC + offset);

                // Branch taken adds 1 cycle
                Cycles++;

                // Additional cycle if page crossed
                if ((oldPC & 0xFF00) != (PC & 0xFF00))
                    Cycles++;
            }

            // Base cycles for BCC instruction
            Cycles += 2;
        }

        public void BCS()
        {
            sbyte offset = (sbyte)Read(PC++); // fetch signed 8-bit offset

            if (GetFlag(Flag.C))
            {
                ushort oldPC = PC;
                PC = (ushort)(PC + offset);

                // Branch taken adds 1 cycle
                Cycles++;

                // Extra cycle if page crossed
                if ((oldPC & 0xFF00) != (PC & 0xFF00))
                    Cycles++;
            }

            // Base cycles for BCS instruction
            Cycles += 2;
        }

        public void BEQ()
        {
            if (GetFlag(Flag.Z))
            {
                sbyte offset = (sbyte)Read(PC); // relative signed offset
                PC++;                           // advance past offset byte
                PC = (ushort)(PC + offset);
            }
            else
            {
                PC++; // skip offset byte if branch not taken
            }
        }

        public void BIT()
        {
            ushort addr = FetchAddress(CurrentInstruction);
            byte value = Read(addr);

            // Zero flag from A & M
            SetFlag(Flag.Z, (A & value) == 0);

            // Negative flag from bit 7 of memory
            SetFlag(Flag.N, (value & 0x80) != 0);

            // Overflow flag from bit 6 of memory
            SetFlag(Flag.V, (value & 0x40) != 0);
        }

        public void BMI()
        {
            sbyte offset = (sbyte)Read(PC++); // fetch signed relative offset

            if (GetFlag(Flag.N))
            {
                ushort oldPC = PC;
                PC = (ushort)(PC + offset);

                // Branch taken adds 1 cycle
                Cycles++;

                // Extra cycle if page crossed
                if ((oldPC & 0xFF00) != (PC & 0xFF00))
                    Cycles++;
            }

            // Base cycles for BMI instruction
            Cycles += 2;
        }

        public void BNE()
        {
            sbyte offset = (sbyte)Read(PC++); // fetch signed relative offset

            if (!GetFlag(Flag.Z))
            {
                ushort oldPC = PC;
                PC = (ushort)(PC + offset);

                // Branch taken adds 1 cycle
                Cycles++;

                // Additional cycle if page crossed
                if ((oldPC & 0xFF00) != (PC & 0xFF00))
                    Cycles++;
            }
        }

        public void BPL()
        {
            sbyte offset = (sbyte)Read(PC++);

            if (!GetFlag(Flag.N))
            {
                ushort oldPC = PC;
                PC = (ushort)(PC + offset);

                // Optional: cycle penalty for page crossing
                if ((oldPC & 0xFF00) != (PC & 0xFF00))
                    Cycles++;

                // Branch taken penalty
                Cycles++;
            }
        }

        public void BRK()
        {
            PC++;
            PushWord(PC);
            Push((byte)(P | 0x10));
            P |= 0x04;
            PC = ReadWord(0xFFFE);
        }

        public void BVC()
        {
            sbyte offset = (sbyte)Read(PC++); // fetch signed relative offset

            if (!GetFlag(Flag.V))
            {
                ushort oldPC = PC;
                PC = (ushort)(PC + offset);

                // Branch taken adds 1 cycle
                Cycles++;

                // Extra cycle if page crossed
                if ((oldPC & 0xFF00) != (PC & 0xFF00))
                    Cycles++;
            }

            // Base cycles for BVC instruction
            Cycles += 2;
        }

        public void BVS()
        {
            sbyte offset = (sbyte)Read(PC++); // fetch signed 8-bit offset

            if (GetFlag(Flag.V))
            {
                ushort oldPC = PC;
                PC = (ushort)(PC + offset);

                // Branch taken adds 1 cycle
                Cycles++;

                // Extra cycle if page crossed
                if ((oldPC & 0xFF00) != (PC & 0xFF00))
                    Cycles++;
            }

            // Base cycles for BVS instruction
            Cycles += 2;
        }

        public void CLD()
        {
            SetFlag(Flag.D, false);
        }

        public void CLI()
        {
            SetFlag(Flag.I, false);
        }

        public void CLV()
        {
            SetFlag(Flag.V, false);
        }

        public void CLC()
        {
            SetFlag(Flag.C, false);
        }

        public void CMP()
        {
            ushort addr = FetchAddress(CurrentInstruction);
            byte value = Read(addr);

            int result = A - value;

            // Carry: set if A >= value
            SetFlag(Flag.C, A >= value);

            // Zero: set if A == value
            SetFlag(Flag.Z, (result & 0xFF) == 0);

            // Negative: set if bit 7 of result is 1
            SetFlag(Flag.N, (result & 0x80) != 0);
        }

        public void CPX()
        {
            ushort addr = FetchAddress(CurrentInstruction);
            byte value = Read(addr);

            int temp = X - value;

            SetFlag(Flag.C, X >= value);
            SetFlag(Flag.Z, (temp & 0xFF) == 0);
            SetFlag(Flag.N, (temp & 0x80) != 0);
        }

        public void CPY()
        {
            ushort addr = FetchAddress(CurrentInstruction);
            byte value = Read(addr);

            int temp = Y - value;

            SetFlag(Flag.C, Y >= value);
            SetFlag(Flag.Z, (temp & 0xFF) == 0);
            SetFlag(Flag.N, (temp & 0x80) != 0);
        }

        public void DEX()
        {
            X--;
            SetNZFlags(X);
        }

        public void DEY()
        {
            Y--;           // decrement Y
            SetNZFlags(Y); // update Negative and Zero flags
        }

        public void LDA()
        {
            ushort addr = FetchAddress(CurrentInstruction);
            A = Read(addr);
            SetNZFlags(A);
        }

        public void LDX()
        {
            ushort addr = FetchAddress(CurrentInstruction);
            X = Read(addr);
            SetNZFlags(X);
        }

        public void LDY()
        {
            ushort addr = FetchAddress(CurrentInstruction);
            Y = Read(addr);
            SetNZFlags(Y);
        }

        public void LSR()
        {
            if (CurrentInstruction.Mode == AddressingMode.Accumulator)
            {
                // Accumulator variant
                SetFlag(Flag.C, (A & 0x01) != 0);
                A >>= 1;
                SetFlag(Flag.N, false); // bit 7 always cleared
                SetFlag(Flag.Z, A == 0);
            }
            else
            {
                // Memory variant
                ushort addr = FetchAddress(CurrentInstruction);
                byte value = Read(addr);

                SetFlag(Flag.C, (value & 0x01) != 0);
                value >>= 1;
                Write(addr, value);

                SetFlag(Flag.N, false); // bit 7 always cleared
                SetFlag(Flag.Z, value == 0);
            }
        }

        public void ORA()
        {
            ushort addr = FetchAddress(CurrentInstruction);
            byte value = Read(addr);

            A |= value;
            SetNZFlags(A);
        }

        public void PHA()
        {
            Push(A);
        }

        public void PLA()
        {
            A = Pop();
            SetNZFlags(A); // update Negative and Zero flags
        }

        public void PLP()
        {
            byte value = Pop();

            // Break flag (B) is ignored when pulling from stack
            SetFlag(Flag.N, (value & 0x80) != 0);
            SetFlag(Flag.V, (value & 0x40) != 0);
            SetFlag(Flag.D, (value & 0x08) != 0);
            SetFlag(Flag.I, (value & 0x04) != 0);
            SetFlag(Flag.Z, (value & 0x02) != 0);
            SetFlag(Flag.C, (value & 0x01) != 0);
        }

        public void ROR()
        {
            bool oldCarry = GetFlag(Flag.C);

            if (CurrentInstruction.Mode == AddressingMode.Accumulator)
            {
                // Accumulator form
                SetFlag(Flag.C, (A & 0x01) != 0);
                A = (byte)((A >> 1) | (oldCarry ? 0x80 : 0x00));
                SetNZFlags(A);
            }
            else
            {
                // Memory form
                ushort addr = FetchAddress(CurrentInstruction);
                byte value = Read(addr);

                SetFlag(Flag.C, (value & 0x01) != 0);
                value = (byte)((value >> 1) | (oldCarry ? 0x80 : 0x00));
                Write(addr, value);

                SetNZFlags(value);
            }
        }

        public void STA()
        {
            ushort addr = FetchAddress(CurrentInstruction);
            Write(addr, A);
        }

        public void STX()
        {
            ushort addr = FetchAddress(CurrentInstruction);
            Write(addr, X);
        }

        public void STY()
        {
            ushort addr = FetchAddress(CurrentInstruction);
            Write(addr, Y);
        }

        public void SBC()
        {
            ushort addr = FetchAddress(CurrentInstruction);
            byte value = Read(addr);

            // Invert value for 2's complement subtraction
            int temp = A - value - (1 - (P & (byte)Flag.C));

            // Set flags
            SetFlag(Flag.C, temp >= 0);
            SetFlag(Flag.Z, (temp & 0xFF) == 0);
            SetFlag(Flag.N, (temp & 0x80) != 0);

            // Overflow occurs if sign of A != sign of result and sign of A != sign of M
            SetFlag(Flag.V, ((A ^ temp) & 0x80) != 0 && ((A ^ value) & 0x80) != 0);

            A = (byte)(temp & 0xFF);
        }

        public void NOP()
        {
            // Does nothing
        }

        public void ROL()
        {
            if (CurrentInstruction.Mode == AddressingMode.Accumulator)
            {
                // Rotate A left through carry
                bool oldCarry = (P & (byte)Flag.C) != 0;
                SetFlag(Flag.C, (A & 0x80) != 0);

                A = (byte)((A << 1) | (oldCarry ? 1 : 0));
                SetNZFlags(A);
            }
            else
            {
                // Rotate memory left through carry
                ushort addr = FetchAddress(CurrentInstruction);
                byte value = Read(addr);
                bool oldCarry = (P & (byte)Flag.C) != 0;

                SetFlag(Flag.C, (value & 0x80) != 0);
                value = (byte)((value << 1) | (oldCarry ? 1 : 0));
                Write(addr, value);
                SetNZFlags(value);
            }
        }

        public void DEC()
        {
            ushort addr = FetchAddress(CurrentInstruction);
            byte value = Read(addr);

            value--;           // decrement memory
            Write(addr, value);
            SetNZFlags(value); // update N and Z flags
        }

        public void INC()
        {
            ushort addr = FetchAddress(CurrentInstruction);
            byte value = Read(addr);

            value++;
            Write(addr, value);

            SetNZFlags(value);
        }

        public void INY()
        {
            Y++;
            SetNZFlags(Y);
        }

        public void INX()
        {
            X++;
            SetNZFlags(X);
        }

        public void JMP()
        {
            if (CurrentInstruction.Mode == AddressingMode.Absolute)
            {
                PC = ReadWord(PC);
            }
            else if (CurrentInstruction.Mode == AddressingMode.Indirect)
            {
                ushort addr = ReadWord(PC);

                // 6502 page-boundary bug emulation
                ushort target;
                if ((addr & 0x00FF) == 0x00FF)
                {
                    byte low = Read(addr);
                    byte high = Read((ushort)(addr & 0xFF00)); // wraps around page
                    target = (ushort)((high << 8) | low);
                }
                else
                {
                    target = ReadWord(addr);
                }

                PC = target;
            }
        }

        public void JSR()
        {
            // Fetch absolute address operand
            ushort addr = ReadWord(PC);
            PC += 2; // increment past operand

            // Push return address (PC - 1) high then low
            Push((byte)((PC - 1) >> 8)); // high byte
            Push((byte)((PC - 1) & 0xFF)); // low byte

            // Jump to target
            PC = addr;
        }

        public void RTI()
        {
            // Pull processor status
            byte flags = Pop();
            SetFlag(Flag.N, (flags & 0x80) != 0);
            SetFlag(Flag.V, (flags & 0x40) != 0);
            SetFlag(Flag.D, (flags & 0x08) != 0);
            SetFlag(Flag.I, (flags & 0x04) != 0);
            SetFlag(Flag.Z, (flags & 0x02) != 0);
            SetFlag(Flag.C, (flags & 0x01) != 0);

            // Pull PC
            byte pcl = Pop();
            byte pch = Pop();
            PC = (ushort)((pch << 8) | pcl);
        }

        public void RTS()
        {
            byte pcl = Pop();
            byte pch = Pop();
            PC = (ushort)(((pch << 8) | pcl) + 1);
        }

        public void SEC()
        {
            SetFlag(Flag.C, true);
        }

        public void SED()
        {
            SetFlag(Flag.D, true);
        }

        public void SEI()
        {
            SetFlag(Flag.I, true);
        }

        public void TAX()
        {
            X = A;
            SetNZFlags(X); // Update N and Z flags
        }

        public void TAY()
        {
            Y = A;
            SetNZFlags(Y); // Update N and Z flags
        }

        public void TSX()
        {
            X = SP;
            SetNZFlags(X); // Update N and Z flags
        }

        public void TXA()
        {
            A = X;
            SetNZFlags(A); // Update N and Z flags
        }

        public void TXS()
        {
            SP = X;
        }

        public void TYA()
        {
            A = Y;
            SetNZFlags(A); // Update N and Z flags
        }

        public void EOR()
        {
            ushort addr = FetchAddress(CurrentInstruction);
            byte value = Read(addr);

            A ^= value;
            SetNZFlags(A);
        }

        public void PHP()
        {
            // Push status flags to stack
            Push((byte)(GetStatusFlags() | 0x10)); // set break flag in copy
        }

        // Illegal opcodes
        public void ANC()
        {
            // Immediate operand
            byte value = Read(PC++);

            A &= value;

            // Set flags
            SetNZFlags(A);
            SetFlag(Flag.C, (A & 0x80) != 0);
        }

        public void KIL()
        {
            // CPU is jammed; PC does not advance
            IsHalted = true;
        }

        public void LAS()
        {
            ushort addr = FetchAddress(CurrentInstruction);
            byte value = Read(addr);

            value &= X;
            A = X = value;
            SP = value;

            SetNZFlags(A);
        }

        public void RLA()
        {
            ushort addr = FetchAddress(CurrentInstruction);
            byte value = Read(addr);

            bool oldCarry = GetFlag(Flag.C);

            // ROL memory
            SetFlag(Flag.C, (value & 0x80) != 0);
            value = (byte)((value << 1) | (oldCarry ? 1 : 0));
            Write(addr, value);

            // AND with accumulator
            A &= value;
            SetNZFlags(A);
        }

        public void RRA()
        {
            // Fetch target memory
            ushort addr = FetchAddress(CurrentInstruction);
            byte value = Read(addr);

            // --- ROR part ---
            bool oldCarry = GetFlag(Flag.C);
            SetFlag(Flag.C, (value & 0x01) != 0);
            value = (byte)((value >> 1) | (oldCarry ? 0x80 : 0x00));

            // Write back to memory
            Write(addr, value);

            // --- ADC part ---
            int carry = GetFlag(Flag.C) ? 1 : 0;
            int sum = A + value + carry;

            SetFlag(Flag.C, sum > 0xFF);
            byte result = (byte)sum;
            SetFlag(Flag.V, (~(A ^ value) & (A ^ result) & 0x80) != 0);

            A = result;
            SetNZFlags(A);
        }

        public void SLO()
        {
            ushort addr = FetchAddress(CurrentInstruction);
            byte value = Read(addr);

            // ASL part
            SetFlag(Flag.C, (value & 0x80) != 0);
            value <<= 1;
            Write(addr, value);

            // ORA part
            A |= value;
            SetNZFlags(A);
        }

        // --- SRE: LSR M then EOR A ---
        public void SRE()
        {
            ushort addr = FetchAddress(CurrentInstruction);
            byte value = Read(addr);

            // LSR part
            SetFlag(Flag.C, (value & 0x01) != 0);
            value >>= 1;
            Write(addr, value);

            // EOR part
            A ^= value;
            SetNZFlags(A);
        }

        // --- DCP: DEC M then CMP A ---
        public void DCP()
        {
            ushort addr = FetchAddress(CurrentInstruction);
            byte value = Read(addr);

            value--;
            Write(addr, value);

            // CMP part
            int temp = A - value;
            SetFlag(Flag.C, A >= value);
            SetFlag(Flag.Z, (temp & 0xFF) == 0);
            SetFlag(Flag.N, (temp & 0x80) != 0);
        }

        // --- ISB/ISC: INC M then SBC A ---
        public void ISB()
        {
            ushort addr = FetchAddress(CurrentInstruction);
            byte value = Read(addr);

            value++;
            Write(addr, value);

            int carry = GetFlag(Flag.C) ? 1 : 0;
            int temp = A - value - (1 - carry);
            SetFlag(Flag.C, temp >= 0);
            SetFlag(Flag.Z, (temp & 0xFF) == 0);
            SetFlag(Flag.N, (temp & 0x80) != 0);
            SetFlag(Flag.V, ((A ^ temp) & (A ^ value) & 0x80) != 0);
            A = (byte)(temp & 0xFF);
        }

        // --- LAX: Load A & X with memory ---
        public void LAX()
        {
            ushort addr = FetchAddress(CurrentInstruction);
            byte value = Read(addr);

            A = value;
            X = value;
            SetNZFlags(A);
        }

        // --- SAX: Store A & X ANDed into memory ---
        public void SAX()
        {
            ushort addr = FetchAddress(CurrentInstruction);
            byte value = (byte)(A & X);
            Write(addr, value);
        }

        // --- ALR: AND then LSR A ---
        public void ALR()
        {
            byte value = Read(PC++);
            A &= value;
            SetNZFlags(A);

            SetFlag(Flag.C, (A & 0x01) != 0);
            A >>= 1;
            SetNZFlags(A);
        }

        // --- ARR: AND then ROR A with weird overflow rules ---
        public void ARR()
        {
            byte value = Read(PC++);
            A &= value;

            bool oldCarry = GetFlag(Flag.C);
            SetFlag(Flag.C, (A & 0x01) != 0);
            A = (byte)((A >> 1) | (oldCarry ? 0x80 : 0x00));

            SetNZFlags(A);
            SetFlag(Flag.V, (((A >> 6) & 1) != 0) ^ (((A >> 5) & 1) != 0));
        }

        // --- XAA / AHX / TAS / SHX / SHY ---
        public void XAA() { /* mostly unstable, rarely used */ }
        public void AHX() { /* store A&X&high byte in memory */ }
        public void TAS() { /* store A&X in SP-relative */ }
        public void SHX() { /* store X&high byte in memory */ }
        public void SHY() { /* store Y&high byte in memory */ }
    }
}
