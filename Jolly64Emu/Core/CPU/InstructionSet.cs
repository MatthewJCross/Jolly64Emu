namespace JollyC64Emu.C64.CPU
{
    public enum AddressingMode
    {
        Implied,         // No operand (e.g., NOP, CLC)
        Accumulator,     // Operates on accumulator (e.g., ASL A, LSR A)
        Immediate,       // #$nn
        ZeroPage,        // $nn
        ZeroPageX,       // $nn,X
        ZeroPageY,       // $nn,Y
        Relative,        // Branch instructions (e.g., BEQ, BNE)
        Absolute,        // $nnnn
        AbsoluteX,       // $nnnn,X
        AbsoluteY,       // $nnnn,Y
        Indirect,        // ($nnnn)
        IndexedIndirect, // (zp,X)
        IndirectIndexed, // (zp),Y
    }

    public class Instruction
    {
        public byte Opcode { get; init; }
        public string Mnemonic { get; init; } = string.Empty;
        public AddressingMode Mode { get; init; }
        public int Size { get; init; }
        public int Cycles { get; init; }
        public bool PageCrossPenalty { get; init; }
        public Action<Cpu6502> Execute { get; set; } = cpu => { };
    }

    public static class InstructionSet
    {
        public static readonly Instruction[] Instructions = new Instruction[256]
        {
            // 0x00 – 0x0F
            /* 0x00 */ new Instruction{Opcode=0x00,Mnemonic="BRK",Mode=AddressingMode.Implied,Size=1,Cycles=7,Execute=cpu=>cpu.BRK()},
            /* 0x01 */ new Instruction{Opcode=0x01,Mnemonic="ORA",Mode=AddressingMode.IndexedIndirect,Size=2,Cycles=6,Execute=cpu=>cpu.ORA()},
            /* 0x02 */ new Instruction{Opcode=0x02,Mnemonic="KIL",Mode=AddressingMode.Implied,Size=1,Cycles=0,Execute=cpu=>cpu.KIL()},
            /* 0x03 */ new Instruction{Opcode=0x03,Mnemonic="SLO",Mode=AddressingMode.IndexedIndirect,Size=2,Cycles=8,Execute=cpu=>cpu.SLO()},
            /* 0x04 */ new Instruction{Opcode=0x04,Mnemonic="NOP",Mode=AddressingMode.ZeroPage,Size=2,Cycles=3,Execute=cpu=>cpu.NOP()},
            /* 0x05 */ new Instruction{Opcode=0x05,Mnemonic="ORA",Mode=AddressingMode.ZeroPage,Size=2,Cycles=3,Execute=cpu=>cpu.ORA()},
            /* 0x06 */ new Instruction{Opcode=0x06,Mnemonic="ASL",Mode=AddressingMode.ZeroPage,Size=2,Cycles=5,Execute=cpu=>cpu.ASL()},
            /* 0x07 */ new Instruction{Opcode=0x07,Mnemonic="SLO",Mode=AddressingMode.ZeroPage,Size=2,Cycles=5,Execute=cpu=>cpu.SLO()},
            /* 0x08 */ new Instruction{Opcode=0x08,Mnemonic="PHP",Mode=AddressingMode.Implied,Size=1,Cycles=3,Execute=cpu=>cpu.PHP()},
            /* 0x09 */ new Instruction{Opcode=0x09,Mnemonic="ORA",Mode=AddressingMode.Immediate,Size=2,Cycles=2,Execute=cpu=>cpu.ORA()},
            /* 0x0A */ new Instruction{Opcode=0x0A,Mnemonic="ASL",Mode=AddressingMode.Accumulator,Size=1,Cycles=2,Execute=cpu=>cpu.ASL()},
            /* 0x0B */ new Instruction{Opcode=0x0B,Mnemonic="ANC",Mode=AddressingMode.Immediate,Size=2,Cycles=2,Execute=cpu=>cpu.ANC()},
            /* 0x0C */ new Instruction{Opcode=0x0C,Mnemonic="NOP",Mode=AddressingMode.Absolute,Size=3,Cycles=4,Execute=cpu=>cpu.NOP()},
            /* 0x0D */ new Instruction{Opcode=0x0D,Mnemonic="ORA",Mode=AddressingMode.Absolute,Size=3,Cycles=4,Execute=cpu=>cpu.ORA()},
            /* 0x0E */ new Instruction{Opcode=0x0E,Mnemonic="ASL",Mode=AddressingMode.Absolute,Size=3,Cycles=6,Execute=cpu=>cpu.ASL()},
            /* 0x0F */ new Instruction{Opcode=0x0F,Mnemonic="SLO",Mode=AddressingMode.Absolute,Size=3,Cycles=6,Execute=cpu=>cpu.SLO()},

            // 0x10 – 0x1F
            /* 0x10 */ new Instruction{Opcode=0x10,Mnemonic="BPL",Mode=AddressingMode.Relative,Size=2,Cycles=2,Execute=cpu=>cpu.BPL()},
            /* 0x11 */ new Instruction{Opcode=0x11,Mnemonic="ORA",Mode=AddressingMode.IndirectIndexed,Size=2,Cycles=5,Execute=cpu=>cpu.ORA()},
            /* 0x12 */ new Instruction{Opcode=0x12,Mnemonic="KIL",Mode=AddressingMode.Implied,Size=1,Cycles=0,Execute=cpu=>cpu.KIL()},
            /* 0x13 */ new Instruction{Opcode=0x13,Mnemonic="SLO",Mode=AddressingMode.IndirectIndexed,Size=2,Cycles=8,Execute=cpu=>cpu.SLO()},
            /* 0x14 */ new Instruction{Opcode=0x14,Mnemonic="NOP",Mode=AddressingMode.ZeroPageX,Size=2,Cycles=4,Execute=cpu=>cpu.NOP()},
            /* 0x15 */ new Instruction{Opcode=0x15,Mnemonic="ORA",Mode=AddressingMode.ZeroPageX,Size=2,Cycles=4,Execute=cpu=>cpu.ORA()},
            /* 0x16 */ new Instruction{Opcode=0x16,Mnemonic="ASL",Mode=AddressingMode.ZeroPageX,Size=2,Cycles=6,Execute=cpu=>cpu.ASL()},
            /* 0x17 */ new Instruction{Opcode=0x17,Mnemonic="SLO",Mode=AddressingMode.ZeroPageX,Size=2,Cycles=6,Execute=cpu=>cpu.SLO()},
            /* 0x18 */ new Instruction{Opcode=0x18,Mnemonic="CLC",Mode=AddressingMode.Implied,Size=1,Cycles=2,Execute=cpu=>cpu.CLC()},
            /* 0x19 */ new Instruction{Opcode=0x19,Mnemonic="ORA",Mode=AddressingMode.AbsoluteY,Size=3,Cycles=4,Execute=cpu=>cpu.ORA()},
            /* 0x1A */ new Instruction{Opcode=0x1A,Mnemonic="NOP",Mode=AddressingMode.Implied,Size=1,Cycles=2,Execute=cpu=>cpu.NOP()},
            /* 0x1B */ new Instruction{Opcode=0x1B,Mnemonic="SLO",Mode=AddressingMode.AbsoluteY,Size=3,Cycles=7,Execute=cpu=>cpu.SLO()},
            /* 0x1C */ new Instruction{Opcode=0x1C,Mnemonic="NOP",Mode=AddressingMode.AbsoluteX,Size=3,Cycles=4,Execute=cpu=>cpu.NOP()},
            /* 0x1D */ new Instruction{Opcode=0x1D,Mnemonic="ORA",Mode=AddressingMode.AbsoluteX,Size=3,Cycles=4,Execute=cpu=>cpu.ORA()},
            /* 0x1E */ new Instruction{Opcode=0x1E,Mnemonic="ASL",Mode=AddressingMode.AbsoluteX,Size=3,Cycles=7,Execute=cpu=>cpu.ASL()},
            /* 0x1F */ new Instruction{Opcode=0x1F,Mnemonic="SLO",Mode=AddressingMode.AbsoluteX,Size=3,Cycles=7,Execute=cpu=>cpu.SLO()},

            // 0x20 – 0x2F
            /* 0x20 */ new Instruction{Opcode=0x20,Mnemonic="JSR",Mode=AddressingMode.Absolute,Size=3,Cycles=6,Execute=cpu=>cpu.JSR()},
            /* 0x21 */ new Instruction{Opcode=0x21,Mnemonic="AND",Mode=AddressingMode.IndexedIndirect,Size=2,Cycles=6,Execute=cpu=>cpu.AND()},
            /* 0x22 */ new Instruction{Opcode=0x22,Mnemonic="KIL",Mode=AddressingMode.Implied,Size=1,Cycles=0,Execute=cpu=>cpu.KIL()},
            /* 0x23 */ new Instruction{Opcode=0x23,Mnemonic="RLA",Mode=AddressingMode.IndexedIndirect,Size=2,Cycles=8,Execute=cpu=>cpu.RLA()},
            /* 0x24 */ new Instruction{Opcode=0x24,Mnemonic="BIT",Mode=AddressingMode.ZeroPage,Size=2,Cycles=3,Execute=cpu=>cpu.BIT()},
            /* 0x25 */ new Instruction{Opcode=0x25,Mnemonic="AND",Mode=AddressingMode.ZeroPage,Size=2,Cycles=3,Execute=cpu=>cpu.AND()},
            /* 0x26 */ new Instruction{Opcode=0x26,Mnemonic="ROL",Mode=AddressingMode.ZeroPage,Size=2,Cycles=5,Execute=cpu=>cpu.ROL()},
            /* 0x27 */ new Instruction{Opcode=0x27,Mnemonic="RLA",Mode=AddressingMode.ZeroPage,Size=2,Cycles=5,Execute=cpu=>cpu.RLA()},
            /* 0x28 */ new Instruction{Opcode=0x28,Mnemonic="PLP",Mode=AddressingMode.Implied,Size=1,Cycles=4,Execute=cpu=>cpu.PLP()},
            /* 0x29 */ new Instruction{Opcode=0x29,Mnemonic="AND",Mode=AddressingMode.Immediate,Size=2,Cycles=2,Execute=cpu=>cpu.AND()},
            /* 0x2A */ new Instruction{Opcode=0x2A,Mnemonic="ROL",Mode=AddressingMode.Accumulator,Size=1,Cycles=2,Execute=cpu=>cpu.ROL()},
            /* 0x2B */ new Instruction{Opcode=0x2B,Mnemonic="ANC",Mode=AddressingMode.Immediate,Size=2,Cycles=2,Execute=cpu=>cpu.ANC()},
            /* 0x2C */ new Instruction{Opcode=0x2C,Mnemonic="BIT",Mode=AddressingMode.Absolute,Size=3,Cycles=4,Execute=cpu=>cpu.BIT()},
            /* 0x2D */ new Instruction{Opcode=0x2D,Mnemonic="AND",Mode=AddressingMode.Absolute,Size=3,Cycles=4,Execute=cpu=>cpu.AND()},
            /* 0x2E */ new Instruction{Opcode=0x2E,Mnemonic="ROL",Mode=AddressingMode.Absolute,Size=3,Cycles=6,Execute=cpu=>cpu.ROL()},
            /* 0x2F */ new Instruction{Opcode=0x2F,Mnemonic="RLA",Mode=AddressingMode.Absolute,Size=3,Cycles=6,Execute=cpu=>cpu.RLA()},

            // 0x30 – 0x3F
            /* 0x30 */ new Instruction{Opcode=0x30,Mnemonic="BMI",Mode=AddressingMode.Relative,Size=2,Cycles=2,Execute=cpu=>cpu.BMI()},
            /* 0x31 */ new Instruction{Opcode=0x31,Mnemonic="AND",Mode=AddressingMode.IndirectIndexed,Size=2,Cycles=5,Execute=cpu=>cpu.AND()},
            /* 0x32 */ new Instruction{Opcode=0x32,Mnemonic="KIL",Mode=AddressingMode.Implied,Size=1,Cycles=0,Execute=cpu=>cpu.KIL()},
            /* 0x33 */ new Instruction{Opcode=0x33,Mnemonic="RLA",Mode=AddressingMode.IndirectIndexed,Size=2,Cycles=8,Execute=cpu=>cpu.RLA()},
            /* 0x34 */ new Instruction{Opcode=0x34,Mnemonic="NOP",Mode=AddressingMode.ZeroPageX,Size=2,Cycles=4,Execute=cpu=>cpu.NOP()},
            /* 0x35 */ new Instruction{Opcode=0x35,Mnemonic="AND",Mode=AddressingMode.ZeroPageX,Size=2,Cycles=4,Execute=cpu=>cpu.AND()},
            /* 0x36 */ new Instruction{Opcode=0x36,Mnemonic="ROL",Mode=AddressingMode.ZeroPageX,Size=2,Cycles=6,Execute=cpu=>cpu.ROL()},
            /* 0x37 */ new Instruction{Opcode=0x37,Mnemonic="RLA",Mode=AddressingMode.ZeroPageX,Size=2,Cycles=6,Execute=cpu=>cpu.RLA()},
            /* 0x38 */ new Instruction{Opcode=0x38,Mnemonic="SEC",Mode=AddressingMode.Implied,Size=1,Cycles=2,Execute=cpu=>cpu.SEC()},
            /* 0x39 */ new Instruction{Opcode=0x39,Mnemonic="AND",Mode=AddressingMode.AbsoluteY,Size=3,Cycles=4,Execute=cpu=>cpu.AND()},
            /* 0x3A */ new Instruction{Opcode=0x3A,Mnemonic="NOP",Mode=AddressingMode.Implied,Size=1,Cycles=2,Execute=cpu=>cpu.NOP()},
            /* 0x3B */ new Instruction{Opcode=0x3B,Mnemonic="RLA",Mode=AddressingMode.AbsoluteY,Size=3,Cycles=7,Execute=cpu=>cpu.RLA()},
            /* 0x3C */ new Instruction{Opcode=0x3C,Mnemonic="NOP",Mode=AddressingMode.AbsoluteX,Size=3,Cycles=4,Execute=cpu=>cpu.NOP()},
            /* 0x3D */ new Instruction{Opcode=0x3D,Mnemonic="AND",Mode=AddressingMode.AbsoluteX,Size=3,Cycles=4,Execute=cpu=>cpu.AND()},
            /* 0x3E */ new Instruction{Opcode=0x3E,Mnemonic="ROL",Mode=AddressingMode.AbsoluteX,Size=3,Cycles=7,Execute=cpu=>cpu.ROL()},
            /* 0x3F */ new Instruction{Opcode=0x3F,Mnemonic="RLA",Mode=AddressingMode.AbsoluteX,Size=3,Cycles=7,Execute=cpu=>cpu.RLA()},

            // 0x40 – 0x4F
            /* 0x40 */ new Instruction{Opcode=0x40,Mnemonic="RTI",Mode=AddressingMode.Implied,Size=1,Cycles=6,Execute=cpu=>cpu.RTI()},
            /* 0x41 */ new Instruction{Opcode=0x41,Mnemonic="EOR",Mode=AddressingMode.IndexedIndirect,Size=2,Cycles=6,Execute=cpu=>cpu.EOR()},
            /* 0x42 */ new Instruction{Opcode=0x42,Mnemonic="KIL",Mode=AddressingMode.Implied,Size=1,Cycles=0,Execute=cpu=>cpu.KIL()},
            /* 0x43 */ new Instruction{Opcode=0x43,Mnemonic="SRE",Mode=AddressingMode.IndexedIndirect,Size=2,Cycles=8,Execute=cpu=>cpu.SRE()},
            /* 0x44 */ new Instruction{Opcode=0x44,Mnemonic="NOP",Mode=AddressingMode.ZeroPage,Size=2,Cycles=3,Execute=cpu=>cpu.NOP()},
            /* 0x45 */ new Instruction{Opcode=0x45,Mnemonic="EOR",Mode=AddressingMode.ZeroPage,Size=2,Cycles=3,Execute=cpu=>cpu.EOR()},
            /* 0x46 */ new Instruction{Opcode=0x46,Mnemonic="LSR",Mode=AddressingMode.ZeroPage,Size=2,Cycles=5,Execute=cpu=>cpu.LSR()},
            /* 0x47 */ new Instruction{Opcode=0x47,Mnemonic="SRE",Mode=AddressingMode.ZeroPage,Size=2,Cycles=5,Execute=cpu=>cpu.SRE()},
            /* 0x48 */ new Instruction{Opcode=0x48,Mnemonic="PHA",Mode=AddressingMode.Implied,Size=1,Cycles=3,Execute=cpu=>cpu.PHA()},
            /* 0x49 */ new Instruction{Opcode=0x49,Mnemonic="EOR",Mode=AddressingMode.Immediate,Size=2,Cycles=2,Execute=cpu=>cpu.EOR()},
            /* 0x4A */ new Instruction{Opcode=0x4A,Mnemonic="LSR",Mode=AddressingMode.Accumulator,Size=1,Cycles=2,Execute=cpu=>cpu.LSR()},
            /* 0x4B */ new Instruction{Opcode=0x4B,Mnemonic="ALR",Mode=AddressingMode.Immediate,Size=2,Cycles=2,Execute=cpu=>cpu.ALR()},
            /* 0x4C */ new Instruction{Opcode=0x4C,Mnemonic="JMP",Mode=AddressingMode.Absolute,Size=3,Cycles=3,Execute=cpu=>cpu.JMP()},
            /* 0x4D */ new Instruction{Opcode=0x4D,Mnemonic="EOR",Mode=AddressingMode.Absolute,Size=3,Cycles=4,Execute=cpu=>cpu.EOR()},
            /* 0x4E */ new Instruction{Opcode=0x4E,Mnemonic="LSR",Mode=AddressingMode.Absolute,Size=3,Cycles=6,Execute=cpu=>cpu.LSR()},
            /* 0x4F */ new Instruction{Opcode=0x4F,Mnemonic="SRE",Mode=AddressingMode.Absolute,Size=3,Cycles=6,Execute=cpu=>cpu.SRE()},

            // 0x50 – 0x5F
            /* 0x50 */ new Instruction{Opcode=0x50,Mnemonic="BVC",Mode=AddressingMode.Relative,Size=2,Cycles=2,Execute=cpu=>cpu.BVC()},
            /* 0x51 */ new Instruction{Opcode=0x51,Mnemonic="EOR",Mode=AddressingMode.IndirectIndexed,Size=2,Cycles=5,Execute=cpu=>cpu.EOR()},
            /* 0x52 */ new Instruction{Opcode=0x52,Mnemonic="KIL",Mode=AddressingMode.Implied,Size=1,Cycles=0,Execute=cpu=>cpu.KIL()},
            /* 0x53 */ new Instruction{Opcode=0x53,Mnemonic="SRE",Mode=AddressingMode.IndirectIndexed,Size=2,Cycles=8,Execute=cpu=>cpu.SRE()},
            /* 0x54 */ new Instruction{Opcode=0x54,Mnemonic="NOP",Mode=AddressingMode.ZeroPageX,Size=2,Cycles=4,Execute=cpu=>cpu.NOP()},
            /* 0x55 */ new Instruction{Opcode=0x55,Mnemonic="EOR",Mode=AddressingMode.ZeroPageX,Size=2,Cycles=4,Execute=cpu=>cpu.EOR()},
            /* 0x56 */ new Instruction{Opcode=0x56,Mnemonic="LSR",Mode=AddressingMode.ZeroPageX,Size=2,Cycles=6,Execute=cpu=>cpu.LSR()},
            /* 0x57 */ new Instruction{Opcode=0x57,Mnemonic="SRE",Mode=AddressingMode.ZeroPageX,Size=2,Cycles=6,Execute=cpu=>cpu.SRE()},
            /* 0x58 */ new Instruction{Opcode=0x58,Mnemonic="CLI",Mode=AddressingMode.Implied,Size=1,Cycles=2,Execute=cpu=>cpu.CLI()},
            /* 0x59 */ new Instruction{Opcode=0x59,Mnemonic="EOR",Mode=AddressingMode.AbsoluteY,Size=3,Cycles=4,Execute=cpu=>cpu.EOR()},
            /* 0x5A */ new Instruction{Opcode=0x5A,Mnemonic="NOP",Mode=AddressingMode.Implied,Size=1,Cycles=2,Execute=cpu=>cpu.NOP()},
            /* 0x5B */ new Instruction{Opcode=0x5B,Mnemonic="SRE",Mode=AddressingMode.AbsoluteY,Size=3,Cycles=7,Execute=cpu=>cpu.SRE()},
            /* 0x5C */ new Instruction{Opcode=0x5C,Mnemonic="NOP",Mode=AddressingMode.AbsoluteX,Size=3,Cycles=4,Execute=cpu=>cpu.NOP()},
            /* 0x5D */ new Instruction{Opcode=0x5D,Mnemonic="EOR",Mode=AddressingMode.AbsoluteX,Size=3,Cycles=4,Execute=cpu=>cpu.EOR()},
            /* 0x5E */ new Instruction{Opcode=0x5E,Mnemonic="LSR",Mode=AddressingMode.AbsoluteX,Size=3,Cycles=7,Execute=cpu=>cpu.LSR()},
            /* 0x5F */ new Instruction{Opcode=0x5F,Mnemonic="SRE",Mode=AddressingMode.AbsoluteX,Size=3,Cycles=7,Execute=cpu=>cpu.SRE()},

            // 0x60 – 0x6F
            /* 0x60 */ new Instruction{Opcode=0x60,Mnemonic="RTS",Mode=AddressingMode.Implied,Size=1,Cycles=6,Execute=cpu=>cpu.RTS()},
            /* 0x61 */ new Instruction{Opcode=0x61,Mnemonic="ADC",Mode=AddressingMode.IndexedIndirect,Size=2,Cycles=6,Execute=cpu=>cpu.ADC()},
            /* 0x62 */ new Instruction{Opcode=0x62,Mnemonic="KIL",Mode=AddressingMode.Implied,Size=1,Cycles=0,Execute=cpu=>cpu.KIL()},
            /* 0x63 */ new Instruction{Opcode=0x63,Mnemonic="RRA",Mode=AddressingMode.IndexedIndirect,Size=2,Cycles=8,Execute=cpu=>cpu.RRA()},
            /* 0x64 */ new Instruction{Opcode=0x64,Mnemonic="NOP",Mode=AddressingMode.ZeroPage,Size=2,Cycles=3,Execute=cpu=>cpu.NOP()},
            /* 0x65 */ new Instruction{Opcode=0x65,Mnemonic="ADC",Mode=AddressingMode.ZeroPage,Size=2,Cycles=3,Execute=cpu=>cpu.ADC()},
            /* 0x66 */ new Instruction{Opcode=0x66,Mnemonic="ROR",Mode=AddressingMode.ZeroPage,Size=2,Cycles=5,Execute=cpu=>cpu.ROR()},
            /* 0x67 */ new Instruction{Opcode=0x67,Mnemonic="RRA",Mode=AddressingMode.ZeroPage,Size=2,Cycles=5,Execute=cpu=>cpu.RRA()},
            /* 0x68 */ new Instruction{Opcode=0x68,Mnemonic="PLA",Mode=AddressingMode.Implied,Size=1,Cycles=4,Execute=cpu=>cpu.PLA()},
            /* 0x69 */ new Instruction{Opcode=0x69,Mnemonic="ADC",Mode=AddressingMode.Immediate,Size=2,Cycles=2,Execute=cpu=>cpu.ADC()},
            /* 0x6A */ new Instruction{Opcode=0x6A,Mnemonic="ROR",Mode=AddressingMode.Accumulator,Size=1,Cycles=2,Execute=cpu=>cpu.ROR()},
            /* 0x6B */ new Instruction{Opcode=0x6B,Mnemonic="ARR",Mode=AddressingMode.Immediate,Size=2,Cycles=2,Execute=cpu=>cpu.ARR()},
            /* 0x6C */ new Instruction{Opcode=0x6C,Mnemonic="JMP",Mode=AddressingMode.Indirect,Size=3,Cycles=5,Execute=cpu=>cpu.JMP()},
            /* 0x6D */ new Instruction{Opcode=0x6D,Mnemonic="ADC",Mode=AddressingMode.Absolute,Size=3,Cycles=4,Execute=cpu=>cpu.ADC()},
            /* 0x6E */ new Instruction{Opcode=0x6E,Mnemonic="ROR",Mode=AddressingMode.Absolute,Size=3,Cycles=6,Execute=cpu=>cpu.ROR()},
            /* 0x6F */ new Instruction{Opcode=0x6F,Mnemonic="RRA",Mode=AddressingMode.Absolute,Size=3,Cycles=6,Execute=cpu=>cpu.RRA()},

            // 0x70 – 0x7F
            /* 0x70 */ new Instruction{Opcode=0x70,Mnemonic="BVS",Mode=AddressingMode.Relative,Size=2,Cycles=2,Execute=cpu=>cpu.BVS()},
            /* 0x71 */ new Instruction{Opcode=0x71,Mnemonic="ADC",Mode=AddressingMode.IndirectIndexed,Size=2,Cycles=5,Execute=cpu=>cpu.ADC()},
            /* 0x72 */ new Instruction{Opcode=0x72,Mnemonic="KIL",Mode=AddressingMode.Implied,Size=1,Cycles=0,Execute=cpu=>cpu.KIL()},
            /* 0x73 */ new Instruction{Opcode=0x73,Mnemonic="RRA",Mode=AddressingMode.IndirectIndexed,Size=2,Cycles=8,Execute=cpu=>cpu.RRA()},
            /* 0x74 */ new Instruction{Opcode=0x74,Mnemonic="NOP",Mode=AddressingMode.ZeroPageX,Size=2,Cycles=4,Execute=cpu=>cpu.NOP()},
            /* 0x75 */ new Instruction{Opcode=0x75,Mnemonic="ADC",Mode=AddressingMode.ZeroPageX,Size=2,Cycles=4,Execute=cpu=>cpu.ADC()},
            /* 0x76 */ new Instruction{Opcode=0x76,Mnemonic="ROR",Mode=AddressingMode.ZeroPageX,Size=2,Cycles=6,Execute=cpu=>cpu.ROR()},
            /* 0x77 */ new Instruction{Opcode=0x77,Mnemonic="RRA",Mode=AddressingMode.ZeroPageX,Size=2,Cycles=6,Execute=cpu=>cpu.RRA()},
            /* 0x78 */ new Instruction{Opcode=0x78,Mnemonic="SEI",Mode=AddressingMode.Implied,Size=1,Cycles=2,Execute=cpu=>cpu.SEI()},
            /* 0x79 */ new Instruction{Opcode=0x79,Mnemonic="ADC",Mode=AddressingMode.AbsoluteY,Size=3,Cycles=4,Execute=cpu=>cpu.ADC()},
            /* 0x7A */ new Instruction{Opcode=0x7A,Mnemonic="NOP",Mode=AddressingMode.Implied,Size=1,Cycles=2,Execute=cpu=>cpu.NOP()},
            /* 0x7B */ new Instruction{Opcode=0x7B,Mnemonic="RRA",Mode=AddressingMode.AbsoluteY,Size=3,Cycles=7,Execute=cpu=>cpu.RRA()},
            /* 0x7C */ new Instruction{Opcode=0x7C,Mnemonic="NOP",Mode=AddressingMode.AbsoluteX,Size=3,Cycles=4,Execute=cpu=>cpu.NOP()},
            /* 0x7D */ new Instruction{Opcode=0x7D,Mnemonic="ADC",Mode=AddressingMode.AbsoluteX,Size=3,Cycles=4,Execute=cpu=>cpu.ADC()},
            /* 0x7E */ new Instruction{Opcode=0x7E,Mnemonic="ROR",Mode=AddressingMode.AbsoluteX,Size=3,Cycles=7,Execute=cpu=>cpu.ROR()},
            /* 0x7F */ new Instruction{Opcode=0x7F,Mnemonic="RRA",Mode=AddressingMode.AbsoluteX,Size=3,Cycles=7,Execute=cpu=>cpu.RRA()},

            // 0x80 – 0x8F
            /* 0x80 */ new Instruction{Opcode=0x80,Mnemonic="NOP",Mode=AddressingMode.Immediate,Size=2,Cycles=2,Execute=cpu=>cpu.NOP()},
            /* 0x81 */ new Instruction{Opcode=0x81,Mnemonic="STA",Mode=AddressingMode.IndexedIndirect,Size=2,Cycles=6,Execute=cpu=>cpu.STA()},
            /* 0x82 */ new Instruction{Opcode=0x82,Mnemonic="NOP",Mode=AddressingMode.Immediate,Size=2,Cycles=2,Execute=cpu=>cpu.NOP()},
            /* 0x83 */ new Instruction{Opcode=0x83,Mnemonic="SAX",Mode=AddressingMode.IndexedIndirect,Size=2,Cycles=6,Execute=cpu=>cpu.SAX()},
            /* 0x84 */ new Instruction{Opcode=0x84,Mnemonic="STY",Mode=AddressingMode.ZeroPage,Size=2,Cycles=3,Execute=cpu=>cpu.STY()},
            /* 0x85 */ new Instruction{Opcode=0x85,Mnemonic="STA",Mode=AddressingMode.ZeroPage,Size=2,Cycles=3,Execute=cpu=>cpu.STA()},
            /* 0x86 */ new Instruction{Opcode=0x86,Mnemonic="STX",Mode=AddressingMode.ZeroPage,Size=2,Cycles=3,Execute=cpu=>cpu.STX()},
            /* 0x87 */ new Instruction{Opcode=0x87,Mnemonic="SAX",Mode=AddressingMode.ZeroPage,Size=2,Cycles=3,Execute=cpu=>cpu.SAX()},
            /* 0x88 */ new Instruction{Opcode=0x88,Mnemonic="DEY",Mode=AddressingMode.Implied,Size=1,Cycles=2,Execute=cpu=>cpu.DEY()},
            /* 0x89 */ new Instruction{Opcode=0x89,Mnemonic="NOP",Mode=AddressingMode.Immediate,Size=2,Cycles=2,Execute=cpu=>cpu.NOP()},
            /* 0x8A */ new Instruction{Opcode=0x8A,Mnemonic="TXA",Mode=AddressingMode.Implied,Size=1,Cycles=2,Execute=cpu=>cpu.TXA()},
            /* 0x8B */ new Instruction{Opcode=0x8B,Mnemonic="XAA",Mode=AddressingMode.Immediate,Size=2,Cycles=2,Execute=cpu=>cpu.XAA()},
            /* 0x8C */ new Instruction{Opcode=0x8C,Mnemonic="STY",Mode=AddressingMode.Absolute,Size=3,Cycles=4,Execute=cpu=>cpu.STY()},
            /* 0x8D */ new Instruction{Opcode=0x8D,Mnemonic="STA",Mode=AddressingMode.Absolute,Size=3,Cycles=4,Execute=cpu=>cpu.STA()},
            /* 0x8E */ new Instruction{Opcode=0x8E,Mnemonic="STX",Mode=AddressingMode.Absolute,Size=3,Cycles=4,Execute=cpu=>cpu.STX()},
            /* 0x8F */ new Instruction{Opcode=0x8F,Mnemonic="SAX",Mode=AddressingMode.Absolute,Size=3,Cycles=4,Execute=cpu=>cpu.SAX()},

            // 0x90 – 0x9F
            /* 0x90 */ new Instruction{Opcode=0x90,Mnemonic="BCC",Mode=AddressingMode.Relative,Size=2,Cycles=2,Execute=cpu=>cpu.BCC()},
            /* 0x91 */ new Instruction{Opcode=0x91,Mnemonic="STA",Mode=AddressingMode.IndirectIndexed,Size=2,Cycles=6,Execute=cpu=>cpu.STA()},
            /* 0x92 */ new Instruction{Opcode=0x92,Mnemonic="KIL",Mode=AddressingMode.Implied,Size=1,Cycles=0,Execute=cpu=>cpu.KIL()},
            /* 0x93 */ new Instruction{Opcode=0x93,Mnemonic="AHX",Mode=AddressingMode.IndirectIndexed,Size=2,Cycles=6,Execute=cpu=>cpu.AHX()},
            /* 0x94 */ new Instruction{Opcode=0x94,Mnemonic="STY",Mode=AddressingMode.ZeroPageX,Size=2,Cycles=4,Execute=cpu=>cpu.STY()},
            /* 0x95 */ new Instruction{Opcode=0x95,Mnemonic="STA",Mode=AddressingMode.ZeroPageX,Size=2,Cycles=4,Execute=cpu=>cpu.STA()},
            /* 0x96 */ new Instruction{Opcode=0x96,Mnemonic="STX",Mode=AddressingMode.ZeroPageY,Size=2,Cycles=4,Execute=cpu=>cpu.STX()},
            /* 0x97 */ new Instruction{Opcode=0x97,Mnemonic="SAX",Mode=AddressingMode.ZeroPageY,Size=2,Cycles=4,Execute=cpu=>cpu.SAX()},
            /* 0x98 */ new Instruction{Opcode=0x98,Mnemonic="TYA",Mode=AddressingMode.Implied,Size=1,Cycles=2,Execute=cpu=>cpu.TYA()},
            /* 0x99 */ new Instruction{Opcode=0x99,Mnemonic="STA",Mode=AddressingMode.AbsoluteY,Size=3,Cycles=5,Execute=cpu=>cpu.STA()},
            /* 0x9A */ new Instruction{Opcode=0x9A,Mnemonic="TXS",Mode=AddressingMode.Implied,Size=1,Cycles=2,Execute=cpu=>cpu.TXS()},
            /* 0x9B */ new Instruction{Opcode=0x9B,Mnemonic="TAS",Mode=AddressingMode.AbsoluteY,Size=3,Cycles=5,Execute=cpu=>cpu.TAS()},
            /* 0x9C */ new Instruction{Opcode=0x9C,Mnemonic="SHY",Mode=AddressingMode.AbsoluteX,Size=3,Cycles=5,Execute=cpu=>cpu.SHY()},
            /* 0x9D */ new Instruction{Opcode=0x9D,Mnemonic="STA",Mode=AddressingMode.AbsoluteX,Size=3,Cycles=5,Execute=cpu=>cpu.STA()},
            /* 0x9E */ new Instruction{Opcode=0x9E,Mnemonic="SHX",Mode=AddressingMode.AbsoluteY,Size=3,Cycles=5,Execute=cpu=>cpu.SHX()},
            /* 0x9F */ new Instruction{Opcode=0x9F,Mnemonic="AHX",Mode=AddressingMode.AbsoluteY,Size=3,Cycles=5,Execute=cpu=>cpu.AHX()},

            // 0xA0 – 0xAF
            /* 0xA0 */ new Instruction{Opcode=0xA0,Mnemonic="LDY",Mode=AddressingMode.Immediate,Size=2,Cycles=2,Execute=cpu=>cpu.LDY()},
            /* 0xA1 */ new Instruction{Opcode=0xA1,Mnemonic="LDA",Mode=AddressingMode.IndexedIndirect,Size=2,Cycles=6,Execute=cpu=>cpu.LDA()},
            /* 0xA2 */ new Instruction{Opcode=0xA2,Mnemonic="LDX",Mode=AddressingMode.Immediate,Size=2,Cycles=2,Execute=cpu=>cpu.LDX()},
            /* 0xA3 */ new Instruction{Opcode=0xA3,Mnemonic="LAX",Mode=AddressingMode.IndexedIndirect,Size=2,Cycles=6,Execute=cpu=>cpu.LAX()},
            /* 0xA4 */ new Instruction{Opcode=0xA4,Mnemonic="LDY",Mode=AddressingMode.ZeroPage,Size=2,Cycles=3,Execute=cpu=>cpu.LDY()},
            /* 0xA5 */ new Instruction{Opcode=0xA5,Mnemonic="LDA",Mode=AddressingMode.ZeroPage,Size=2,Cycles=3,Execute=cpu=>cpu.LDA()},
            /* 0xA6 */ new Instruction{Opcode=0xA6,Mnemonic="LDX",Mode=AddressingMode.ZeroPage,Size=2,Cycles=3,Execute=cpu=>cpu.LDX()},
            /* 0xA7 */ new Instruction{Opcode=0xA7,Mnemonic="LAX",Mode=AddressingMode.ZeroPage,Size=2,Cycles=3,Execute=cpu=>cpu.LAX()},
            /* 0xA8 */ new Instruction{Opcode=0xA8,Mnemonic="TAY",Mode=AddressingMode.Implied,Size=1,Cycles=2,Execute=cpu=>cpu.TAY()},
            /* 0xA9 */ new Instruction{Opcode=0xA9,Mnemonic="LDA",Mode=AddressingMode.Immediate,Size=2,Cycles=2,Execute=cpu=>cpu.LDA()},
            /* 0xAA */ new Instruction{Opcode=0xAA,Mnemonic="TAX",Mode=AddressingMode.Implied,Size=1,Cycles=2,Execute=cpu=>cpu.TAX()},
            /* 0xAB */ new Instruction{Opcode=0xAB,Mnemonic="LAX",Mode=AddressingMode.Immediate,Size=2,Cycles=2,Execute=cpu=>cpu.LAX()},
            /* 0xAC */ new Instruction{Opcode=0xAC,Mnemonic="LDY",Mode=AddressingMode.Absolute,Size=3,Cycles=4,Execute=cpu=>cpu.LDY()},
            /* 0xAD */ new Instruction{Opcode=0xAD,Mnemonic="LDA",Mode=AddressingMode.Absolute,Size=3,Cycles=4,Execute=cpu=>cpu.LDA()},
            /* 0xAE */ new Instruction{Opcode=0xAE,Mnemonic="LDX",Mode=AddressingMode.Absolute,Size=3,Cycles=4,Execute=cpu=>cpu.LDX()},
            /* 0xAF */ new Instruction{Opcode=0xAF,Mnemonic="LAX",Mode=AddressingMode.Absolute,Size=3,Cycles=4,Execute=cpu=>cpu.LAX()},

                // 0xB0 – 0xBF
            /* 0xB0 */ new Instruction{Opcode=0xB0,Mnemonic="BCS",Mode=AddressingMode.Relative,Size=2,Cycles=2,Execute=cpu=>cpu.BCS()},
            /* 0xB1 */ new Instruction{Opcode=0xB1,Mnemonic="LDA",Mode=AddressingMode.IndirectIndexed,Size=2,Cycles=5,Execute=cpu=>cpu.LDA()},
            /* 0xB2 */ new Instruction{Opcode=0xB2,Mnemonic="KIL",Mode=AddressingMode.Implied,Size=1,Cycles=0,Execute=cpu=>cpu.KIL()},
            /* 0xB3 */ new Instruction{Opcode=0xB3,Mnemonic="LAX",Mode=AddressingMode.IndirectIndexed,Size=2,Cycles=5,Execute=cpu=>cpu.LAX()},
            /* 0xB4 */ new Instruction{Opcode=0xB4,Mnemonic="LDY",Mode=AddressingMode.ZeroPageX,Size=2,Cycles=4,Execute=cpu=>cpu.LDY()},
            /* 0xB5 */ new Instruction{Opcode=0xB5,Mnemonic="LDA",Mode=AddressingMode.ZeroPageX,Size=2,Cycles=4,Execute=cpu=>cpu.LDA()},
            /* 0xB6 */ new Instruction{Opcode=0xB6,Mnemonic="LDX",Mode=AddressingMode.ZeroPageY,Size=2,Cycles=4,Execute=cpu=>cpu.LDX()},
            /* 0xB7 */ new Instruction{Opcode=0xB7,Mnemonic="LAX",Mode=AddressingMode.ZeroPageY,Size=2,Cycles=4,Execute=cpu=>cpu.LAX()},
            /* 0xB8 */ new Instruction{Opcode=0xB8,Mnemonic="CLV",Mode=AddressingMode.Implied,Size=1,Cycles=2,Execute=cpu=>cpu.CLV()},
            /* 0xB9 */ new Instruction{Opcode=0xB9,Mnemonic="LDA",Mode=AddressingMode.AbsoluteY,Size=3,Cycles=4,Execute=cpu=>cpu.LDA()},
            /* 0xBA */ new Instruction{Opcode=0xBA,Mnemonic="TSX",Mode=AddressingMode.Implied,Size=1,Cycles=2,Execute=cpu=>cpu.TSX()},
            /* 0xBB */ new Instruction{Opcode=0xBB,Mnemonic="LAS",Mode=AddressingMode.AbsoluteY,Size=3,Cycles=4,Execute=cpu=>cpu.LAS()},
            /* 0xBC */ new Instruction{Opcode=0xBC,Mnemonic="LDY",Mode=AddressingMode.AbsoluteX,Size=3,Cycles=4,Execute=cpu=>cpu.LDY()},
            /* 0xBD */ new Instruction{Opcode=0xBD,Mnemonic="LDA",Mode=AddressingMode.AbsoluteX,Size=3,Cycles=4,Execute=cpu=>cpu.LDA()},
            /* 0xBE */ new Instruction{Opcode=0xBE,Mnemonic="LDX",Mode=AddressingMode.AbsoluteY,Size=3,Cycles=4,Execute=cpu=>cpu.LDX()},
            /* 0xBF */ new Instruction{Opcode=0xBF,Mnemonic="LAX",Mode=AddressingMode.AbsoluteY,Size=3,Cycles=4,Execute=cpu=>cpu.LAX()},

            // 0xC0 – 0xCF
            /* 0xC0 */ new Instruction{Opcode=0xC0,Mnemonic="CPY",Mode=AddressingMode.Immediate,Size=2,Cycles=2,Execute=cpu=>cpu.CPY()},
            /* 0xC1 */ new Instruction{Opcode=0xC1,Mnemonic="CMP",Mode=AddressingMode.IndexedIndirect,Size=2,Cycles=6,Execute=cpu=>cpu.CMP()},
            /* 0xC2 */ new Instruction{Opcode=0xC2,Mnemonic="NOP",Mode=AddressingMode.Immediate,Size=2,Cycles=2,Execute=cpu=>cpu.NOP()},
            /* 0xC3 */ new Instruction{Opcode=0xC3,Mnemonic="DCP",Mode=AddressingMode.IndexedIndirect,Size=2,Cycles=8,Execute=cpu=>cpu.DCP()},
            /* 0xC4 */ new Instruction{Opcode=0xC4,Mnemonic="CPY",Mode=AddressingMode.ZeroPage,Size=2,Cycles=3,Execute=cpu=>cpu.CPY()},
            /* 0xC5 */ new Instruction{Opcode=0xC5,Mnemonic="CMP",Mode=AddressingMode.ZeroPage,Size=2,Cycles=3,Execute=cpu=>cpu.CMP()},
            /* 0xC6 */ new Instruction{Opcode=0xC6,Mnemonic="DEC",Mode=AddressingMode.ZeroPage,Size=2,Cycles=5,Execute=cpu=>cpu.DEC()},
            /* 0xC7 */ new Instruction{Opcode=0xC7,Mnemonic="DCP",Mode=AddressingMode.ZeroPage,Size=2,Cycles=5,Execute=cpu=>cpu.DCP()},
            /* 0xC8 */ new Instruction{Opcode=0xC8,Mnemonic="INY",Mode=AddressingMode.Implied,Size=1,Cycles=2,Execute=cpu=>cpu.INY()},
            /* 0xC9 */ new Instruction{Opcode=0xC9,Mnemonic="CMP",Mode=AddressingMode.Immediate,Size=2,Cycles=2,Execute=cpu=>cpu.CMP()},
            /* 0xCA */ new Instruction{Opcode=0xCA,Mnemonic="DEX",Mode=AddressingMode.Implied,Size=1,Cycles=2,Execute=cpu=>cpu.DEX()},
            /* 0xCB */ new Instruction{Opcode=0xCB,Mnemonic="AXS",Mode=AddressingMode.Immediate,Size=2,Cycles=2,Execute=cpu=>cpu.AXS()},
            /* 0xCC */ new Instruction{Opcode=0xCC,Mnemonic="CPY",Mode=AddressingMode.Absolute,Size=3,Cycles=4,Execute=cpu=>cpu.CPY()},
            /* 0xCD */ new Instruction{Opcode=0xCD,Mnemonic="CMP",Mode=AddressingMode.Absolute,Size=3,Cycles=4,Execute=cpu=>cpu.CMP()},
            /* 0xCE */ new Instruction{Opcode=0xCE,Mnemonic="DEC",Mode=AddressingMode.Absolute,Size=3,Cycles=6,Execute=cpu=>cpu.DEC()},
            /* 0xCF */ new Instruction{Opcode=0xCF,Mnemonic="DCP",Mode=AddressingMode.Absolute,Size=3,Cycles=6,Execute=cpu=>cpu.DCP()},

            // 0xD0 – 0xDF
            /* 0xD0 */ new Instruction{Opcode=0xD0,Mnemonic="BNE",Mode=AddressingMode.Relative,Size=2,Cycles=2,Execute=cpu=>cpu.BNE()},
            /* 0xD1 */ new Instruction{Opcode=0xD1,Mnemonic="CMP",Mode=AddressingMode.IndirectIndexed,Size=2,Cycles=5,Execute=cpu=>cpu.CMP()},
            /* 0xD2 */ new Instruction{Opcode=0xD2,Mnemonic="KIL",Mode=AddressingMode.Implied,Size=1,Cycles=0,Execute=cpu=>cpu.KIL()},
            /* 0xD3 */ new Instruction{Opcode=0xD3,Mnemonic="DCP",Mode=AddressingMode.IndirectIndexed,Size=2,Cycles=8,Execute=cpu=>cpu.DCP()},
            /* 0xD4 */ new Instruction{Opcode=0xD4,Mnemonic="NOP",Mode=AddressingMode.ZeroPageX,Size=2,Cycles=4,Execute=cpu=>cpu.NOP()},
            /* 0xD5 */ new Instruction{Opcode=0xD5,Mnemonic="CMP",Mode=AddressingMode.ZeroPageX,Size=2,Cycles=4,Execute=cpu=>cpu.CMP()},
            /* 0xD6 */ new Instruction{Opcode=0xD6,Mnemonic="DEC",Mode=AddressingMode.ZeroPageX,Size=2,Cycles=6,Execute=cpu=>cpu.DEC()},
            /* 0xD7 */ new Instruction{Opcode=0xD7,Mnemonic="DCP",Mode=AddressingMode.ZeroPageX,Size=2,Cycles=6,Execute=cpu=>cpu.DCP()},
            /* 0xD8 */ new Instruction{Opcode=0xD8,Mnemonic="CLD",Mode=AddressingMode.Implied,Size=1,Cycles=2,Execute=cpu=>cpu.CLD()},
            /* 0xD9 */ new Instruction{Opcode=0xD9,Mnemonic="CMP",Mode=AddressingMode.AbsoluteY,Size=3,Cycles=4,Execute=cpu=>cpu.CMP()},
            /* 0xDA */ new Instruction{Opcode=0xDA,Mnemonic="NOP",Mode=AddressingMode.Implied,Size=1,Cycles=2,Execute=cpu=>cpu.NOP()},
            /* 0xDB */ new Instruction{Opcode=0xDB,Mnemonic="DCP",Mode=AddressingMode.AbsoluteY,Size=3,Cycles=7,Execute=cpu=>cpu.DCP()},
            /* 0xDC */ new Instruction{Opcode=0xDC,Mnemonic="NOP",Mode=AddressingMode.AbsoluteX,Size=3,Cycles=4,Execute=cpu=>cpu.NOP()},
            /* 0xDD */ new Instruction{Opcode=0xDD,Mnemonic="CMP",Mode=AddressingMode.AbsoluteX,Size=3,Cycles=4,Execute=cpu=>cpu.CMP()},
            /* 0xDE */ new Instruction{Opcode=0xDE,Mnemonic="DEC",Mode=AddressingMode.AbsoluteX,Size=3,Cycles=7,Execute=cpu=>cpu.DEC()},
            /* 0xDF */ new Instruction{Opcode=0xDF,Mnemonic="DCP",Mode=AddressingMode.AbsoluteX,Size=3,Cycles=7,Execute=cpu=>cpu.DCP()},

            // 0xE0 – 0xEF
            /* 0xE0 */ new Instruction{Opcode=0xE0,Mnemonic="CPX",Mode=AddressingMode.Immediate,Size=2,Cycles=2,Execute=cpu=>cpu.CPX()},
            /* 0xE1 */ new Instruction{Opcode=0xE1,Mnemonic="SBC",Mode=AddressingMode.IndexedIndirect,Size=2,Cycles=6,Execute=cpu=>cpu.SBC()},
            /* 0xE2 */ new Instruction{Opcode=0xE2,Mnemonic="NOP",Mode=AddressingMode.Immediate,Size=2,Cycles=2,Execute=cpu=>cpu.NOP()},
            /* 0xE3 */ new Instruction{Opcode=0xE3,Mnemonic="ISB",Mode=AddressingMode.IndexedIndirect,Size=2,Cycles=8,Execute=cpu=>cpu.ISB()},
            /* 0xE4 */ new Instruction{Opcode=0xE4,Mnemonic="CPX",Mode=AddressingMode.ZeroPage,Size=2,Cycles=3,Execute=cpu=>cpu.CPX()},
            /* 0xE5 */ new Instruction{Opcode=0xE5,Mnemonic="SBC",Mode=AddressingMode.ZeroPage,Size=2,Cycles=3,Execute=cpu=>cpu.SBC()},
            /* 0xE6 */ new Instruction{Opcode=0xE6,Mnemonic="INC",Mode=AddressingMode.ZeroPage,Size=2,Cycles=5,Execute=cpu=>cpu.INC()},
            /* 0xE7 */ new Instruction{Opcode=0xE7,Mnemonic="ISB",Mode=AddressingMode.ZeroPage,Size=2,Cycles=5,Execute=cpu=>cpu.ISB()},
            /* 0xE8 */ new Instruction{Opcode=0xE8,Mnemonic="INX",Mode=AddressingMode.Implied,Size=1,Cycles=2,Execute=cpu=>cpu.INX()},
            /* 0xE9 */ new Instruction{Opcode=0xE9,Mnemonic="SBC",Mode=AddressingMode.Immediate,Size=2,Cycles=2,Execute=cpu=>cpu.SBC()},
            /* 0xEA */ new Instruction{Opcode=0xEA,Mnemonic="NOP",Mode=AddressingMode.Implied,Size=1,Cycles=2,Execute=cpu=>cpu.NOP()},
            /* 0xEB */ new Instruction{Opcode=0xEB,Mnemonic="SBC",Mode=AddressingMode.Immediate,Size=2,Cycles=2,Execute=cpu=>cpu.SBC()},
            /* 0xEC */ new Instruction{Opcode=0xEC,Mnemonic="CPX",Mode=AddressingMode.Absolute,Size=3,Cycles=4,Execute=cpu=>cpu.CPX()},
            /* 0xED */ new Instruction{Opcode=0xED,Mnemonic="SBC",Mode=AddressingMode.Absolute,Size=3,Cycles=4,Execute=cpu=>cpu.SBC()},
            /* 0xEE */ new Instruction{Opcode=0xEE,Mnemonic="INC",Mode=AddressingMode.Absolute,Size=3,Cycles=6,Execute=cpu=>cpu.INC()},
            /* 0xEF */ new Instruction{Opcode=0xEF,Mnemonic="ISB",Mode=AddressingMode.Absolute,Size=3,Cycles=6,Execute=cpu=>cpu.ISB()},

            // 0xF0 – 0xFF
            /* 0xF0 */ new Instruction{Opcode=0xF0,Mnemonic="BEQ",Mode=AddressingMode.Relative,Size=2,Cycles=2,Execute=cpu=>cpu.BEQ()},
            /* 0xF1 */ new Instruction{Opcode=0xF1,Mnemonic="SBC",Mode=AddressingMode.IndirectIndexed,Size=2,Cycles=5,Execute=cpu=>cpu.SBC()},
            /* 0xF2 */ new Instruction{Opcode=0xF2,Mnemonic="KIL",Mode=AddressingMode.Implied,Size=1,Cycles=0,Execute=cpu=>cpu.KIL()},
            /* 0xF3 */ new Instruction{Opcode=0xF3,Mnemonic="ISB",Mode=AddressingMode.IndirectIndexed,Size=2,Cycles=8,Execute=cpu=>cpu.ISB()},
            /* 0xF4 */ new Instruction{Opcode=0xF4,Mnemonic="NOP",Mode=AddressingMode.ZeroPageX,Size=2,Cycles=4,Execute=cpu=>cpu.NOP()},
            /* 0xF5 */ new Instruction{Opcode=0xF5,Mnemonic="SBC",Mode=AddressingMode.ZeroPageX,Size=2,Cycles=4,Execute=cpu=>cpu.SBC()},
            /* 0xF6 */ new Instruction{Opcode=0xF6,Mnemonic="INC",Mode=AddressingMode.ZeroPageX,Size=2,Cycles=6,Execute=cpu=>cpu.INC()},
            /* 0xF7 */ new Instruction{Opcode=0xF7,Mnemonic="ISB",Mode=AddressingMode.ZeroPageX,Size=2,Cycles=6,Execute=cpu=>cpu.ISB()},
            /* 0xF8 */ new Instruction{Opcode=0xF8,Mnemonic="SED",Mode=AddressingMode.Implied,Size=1,Cycles=2,Execute=cpu=>cpu.SED()},
            /* 0xF9 */ new Instruction{Opcode=0xF9,Mnemonic="SBC",Mode=AddressingMode.AbsoluteY,Size=3,Cycles=4,Execute=cpu=>cpu.SBC()},
            /* 0xFA */ new Instruction{Opcode=0xFA,Mnemonic="NOP",Mode=AddressingMode.Implied,Size=1,Cycles=2,Execute=cpu=>cpu.NOP()},
            /* 0xFB */ new Instruction{Opcode=0xFB,Mnemonic="ISB",Mode=AddressingMode.AbsoluteY,Size=3,Cycles=7,Execute=cpu=>cpu.ISB()},
            /* 0xFC */ new Instruction{Opcode=0xFC,Mnemonic="NOP",Mode=AddressingMode.AbsoluteX,Size=3,Cycles=4,Execute=cpu=>cpu.NOP()},
            /* 0xFD */ new Instruction{Opcode=0xFD,Mnemonic="SBC",Mode=AddressingMode.AbsoluteX,Size=3,Cycles=4,Execute=cpu=>cpu.SBC()},
            /* 0xFE */ new Instruction{Opcode=0xFE,Mnemonic="INC",Mode=AddressingMode.AbsoluteX,Size=3,Cycles=7,Execute=cpu=>cpu.INC()},
            /* 0xFF */ new Instruction{Opcode=0xFF,Mnemonic="ISB",Mode=AddressingMode.AbsoluteX,Size=3,Cycles=7,Execute=cpu=>cpu.ISB()},
        };

        public static Instruction Find(string mnemonic, AddressingMode mode)
        {
            var instr = Instructions.FirstOrDefault(i => i.Mnemonic == mnemonic && i.Mode == mode);
            if (instr == null)
                throw new InvalidOperationException($"Instruction {mnemonic} with mode {mode} not found.");

            return instr;
        }
    }
}
