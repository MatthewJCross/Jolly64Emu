using Jolly64Emu.Core.Bus;
using JollyC64Emu.C64.CPU;
using JollyC64Emu.Core.VIC;

namespace Jolly64Emu.Core.VIC
{
    // VIC-II register summary
    // Addr     Name     Purpose
    // $D000	SPR0_X   Sprite 0 X
    // $D001	SPR0_Y   Sprite 0 Y
    // $D002	SPR1_X   Sprite 1 X
    // $D003	SPR1_Y   Sprite 1 Y
    // $D004	SPR2_X   Sprite 2 X
    // $D005	SPR2_Y   Sprite 2 Y
    // $D006	SPR3_X   Sprite 3 X
    // $D007	SPR3_Y   Sprite 3 Y
    // $D008	SPR4_X   Sprite 4 X
    // $D009	SPR4_Y   Sprite 4 Y
    // $D00A	SPR5_X   Sprite 5 X
    // $D00B	SPR5_Y   Sprite 5 Y
    // $D00C	SPR6_X   Sprite 6 X
    // $D00D	SPR6_Y   Sprite 6 Y
    // $D00E	SPR7_X   Sprite 7 X
    // $D00F	SPR7_Y   Sprite 7 Y

    // Sprite control
    // Addr    Name Bits
    // $D010 SPR_X_MSB   Bit n = MSB of sprite n X
    // $D015 SPR_ENABLE  Enable sprites
    // $D017 SPR_Y_EXPAND    Y expand
    // $D01B SPR_PRIORITY    Behind background
    // $D01C SPR_MULTICOLOR  Multicolor enable
    // $D01D SPR_X_EXPAND    X expand

    // Raster & screen control(🔥 important)
    // Addr Name    Description
    // $D011 CTRL1   Y scroll, 25/24 rows, raster MSB
    // $D012 RASTER  Raster line(low 8 bits)
    // $D016 CTRL2   X scroll, 40/38 cols, multicolor
    // $D018 MEM_PTRS    Screen & character base
    // $D020 BORDER  Border color
    // $D021 BG0 Background color 0

    // Sprite & background colors
    // Addr         Name
    // $D022        BG1
    // $D023        BG2
    // $D024        BG3
    // $D025        SPR_MC0
    // $D026        SPR_MC1
    // $D027–$D02E  SPR0–SPR7 color

    // Interrupts & collisions
    // Addr    Name          Notes
    // $D019   IRQ_STATUS    Raster/sprite IRQ flags
    // $D01A   IRQ_ENABLE    Enable IRQ sources
    // $D01E   SPR_SPR_COLL  Sprite–sprite collision
    // $D01F   SPR_BG_COLL   Sprite–background collision

    public enum StateEnum
    {
        IdleState,
        DisplayState
    }

    public class VicII
    {
        // PAL timing
        private const int CyclesPerScanline = 63;
        private const int TotalScanlines = 312;
        private const int HBlank = 24;
        private const int StartRaster = 16;

        public int Width = 403 - HBlank;
        public int Height = 284 - StartRaster;

        public const int VisibleScanlines = 284;
        private const int FrameWidth = 418;
        private const int ScreenWidth = 320;
        private const int ScreenHeight = 200;
        const int FirstVisibleLine = 16;
        const int VisibleLines = 284;
        const int LastVisibleLine = FirstVisibleLine + VisibleLines;

        private int _borderLeft = 31;
        private int _borderRight = 335;
        private int _borderTop = 55;
        private int _borderBottom = 247;

        private bool _mainBorder;
        private bool _verticalBorder;

        public uint _cycles;

        private int _cycleCounter = 0;
        private uint _cycle = 1;
        private bool _badLineCondition;
        private int _rasterLine = 0;
        private int _rasterComparator;
        private byte _interruptRegister;
        private byte _interruptEnabled;

        private byte _borderColor;
        private byte _backgroundColor0;
        private byte _backgroundColor1;
        private byte _backgroundColor2;
        private byte _backgroundColor3;
        private byte _colorSprite0;
        private byte _colorSprite1;
        private byte _colorSprite2;
        private byte _colorSprite3;
        private byte _colorSprite4;
        private byte _colorSprite5;
        private byte _colorSprite6;
        private byte _colorSprite7;
        private byte _spriteMulticolor0;
        private byte _spriteMulticolor1;

        private byte _sprite0X;
        private byte _sprite1X;
        private byte _sprite2X;
        private byte _sprite3X;
        private byte _sprite4X;
        private byte _sprite5X;
        private byte _sprite6X;
        private byte _sprite7X;
        private byte _msbSpriteXs;
        private byte _sprite0Y;
        private byte _sprite1Y;
        private byte _sprite2Y;
        private byte _sprite3Y;
        private byte _sprite4Y;
        private byte _sprite5Y;
        private byte _sprite6Y;
        private byte _sprite7Y;

        private ushort _videoCounter;
        private ushort _videoCounterBase;
        private uint _rowCounter;
        private uint _videoMatrixLineIndex;

        private byte _videoMode;
        private byte _videoMatrix;
        private byte _characterGenerator;
        private byte _spriteEnabled;
        private byte _spriteXExpansion;
        private byte _spriteYExpansion;
        private byte _spriteDataPriority;
        private byte _spriteMulticolor;
        private byte _spriteSpriteCollision;
        private byte _spriteDataCollision;

        //public uint[] _palette =
        //{
        //    0x000000,
        //    0xFFFFFF,
        //    0x313A8C,
        //    0xBDB563,
        //    0x943A8C,
        //    0x4AA552,
        //    0x8C3142,
        //    0x73CEBD,
        //    0x29528C,
        //    0x004252,
        //    0x636BBD,
        //    0x525252,
        //    0x7B7B7B,
        //    0x8CE694,
        //    0xC56B7B,
        //    0x9C9C9C
        //};

        private bool _denFrameSet;
        private byte _den;
        private byte _controlRegister1;
        private byte _xScroll;
        private byte _yScroll;

        private StateEnum _state;
        private ushort _gData;

        private byte[] _colorLine = new byte[40];
        private byte[] _videoMatrixLine = new byte[40];

        private byte _rSel;
        private byte _cSel;

        private readonly C64Bus _bus;
        private readonly Cpu6510 _cpu;

        uint[] _frameBuffer;

        public VicII(C64Bus bus, Cpu6510 cpu)
        {
            _bus = bus;
            _cpu = cpu;
            _frameBuffer = new uint[Width * Height];
        }

        public void InitVIC()
        {
            // $D011 - Control Register 1
            // Bit 4 = screen ON
            _bus.Write(0xD011, 0x1B); // 00011011

            // $D016 - Control Register 2
            _bus.Write(0xD016, 0x08); // 00001000

            // $D018 - Memory setup
            // Screen = $0400 (1)
            // Char   = $1000 (4)
            _bus.Write(0xD018, 0x14); // 00010100

            // Colors
            _bus.Write(0xD020, 14); // Border = light blue
            _bus.Write(0xD021, 6);  // Background = blue

            // Clear unused but important ones
            _bus.Write(0xD017, 0); // Sprite Y expand
            _bus.Write(0xD01B, 0); // Sprite priority
            _bus.Write(0xD01C, 0); // Multicolor sprites
            _bus.Write(0xD01D, 0); // Sprite X expand

            // Interrupts (off)
            _bus.Write(0xD019, 0);
            _bus.Write(0xD01A, 0);

            // Fill screen + color RAM
            for (int i = 0; i < 1000; i++)
            {
                _bus.Write((ushort)(0x0400 + i), 0);
                _bus.Write((ushort)(0xD800 + i), 14); // light blue text
            }

            _rasterLine = 0;
        }

        // All VIC-II registers are mirrored every 64 bytes.
        public byte Read(ushort address)
        {
            int register = address & 0x3F;
            if (register >= 0x2F)
                return 0xFF;

            if (register <= 0x0F)
                return GetSpriteCoordinate(register);

            return register switch
            {
                0x10 => _msbSpriteXs,
                0x11 => (byte)((_controlRegister1 & 0x7F) | ((_rasterLine & 0x100) >> 1)),
                0x12 => (byte)_rasterLine,
                0x13 => _lightPenX,
                0x14 => _lightPenY,
                0x15 => _spriteEnabled,
                0x16 => (byte)(0xC0 | _controlRegister2),
                0x17 => _spriteYExpansion,
                0x18 => (byte)(0x01 | (_videoMatrix << 4) | (_characterGenerator << 1)),
                0x19 => (byte)(0x70 | _interruptRegister | ((_interruptRegister & _interruptEnabled & 0x0F) != 0 ? 0x80 : 0)),
                0x1A => (byte)(0xF0 | (_interruptEnabled & 0x0F)),
                0x1B => _spriteDataPriority,
                0x1C => _spriteMulticolor,
                0x1D => _spriteXExpansion,
                0x1E => ReadCollision(ref _spriteSpriteCollision),
                0x1F => ReadCollision(ref _spriteDataCollision),
                0x20 => (byte)(0xF0 | _borderColor),
                0x21 => (byte)(0xF0 | _backgroundColor0),
                0x22 => (byte)(0xF0 | _backgroundColor1),
                0x23 => (byte)(0xF0 | _backgroundColor2),
                0x24 => (byte)(0xF0 | _backgroundColor3),
                0x25 => (byte)(0xF0 | _spriteMulticolor0),
                0x26 => (byte)(0xF0 | _spriteMulticolor1),
                >= 0x27 and <= 0x2E => (byte)(0xF0 | GetSpriteColor(register - 0x27)),
                _ => 0xFF
            };
        }

        private byte _controlRegister2;
        private byte _lightPenX;
        private byte _lightPenY;

        private byte GetSpriteCoordinate(int register)
        {
            return register switch
            {
                0x00 => _sprite0X,
                0x01 => _sprite0Y,
                0x02 => _sprite1X,
                0x03 => _sprite1Y,
                0x04 => _sprite2X,
                0x05 => _sprite2Y,
                0x06 => _sprite3X,
                0x07 => _sprite3Y,
                0x08 => _sprite4X,
                0x09 => _sprite4Y,
                0x0A => _sprite5X,
                0x0B => _sprite5Y,
                0x0C => _sprite6X,
                0x0D => _sprite6Y,
                0x0E => _sprite7X,
                0x0F => _sprite7Y,
                _ => 0
            };
        }

        private byte GetSpriteColor(int sprite)
        {
            return sprite switch
            {
                0 => _colorSprite0,
                1 => _colorSprite1,
                2 => _colorSprite2,
                3 => _colorSprite3,
                4 => _colorSprite4,
                5 => _colorSprite5,
                6 => _colorSprite6,
                _ => _colorSprite7
            };
        }

        private static byte ReadCollision(ref byte collision)
        {
            byte result = collision;
            collision = 0;
            return result;
        }

        private void UpdateInterruptOutput()
        {
            if ((_interruptRegister & _interruptEnabled & 0x0F) != 0)
                _cpu.TriggerIRQ(InterruptTypeEnum.IRQ);
        }

        public void Write(ushort address, byte value)
        {
            address &= 0x3F;

            switch (address)
            {
                case 0x00:
                    _sprite0X = value;
                    break;

                case 0x01:
                    _sprite0Y = value;
                    break;

                case 0x02:
                    _sprite1X = value;
                    break;

                case 0x03:
                    _sprite1Y = value;
                    break;

                case 0x04:
                    _sprite2X = value;
                    break;

                case 0x05:
                    _sprite2Y = value;
                    break;

                case 0x06:
                    _sprite3X = value;
                    break;

                case 0x07:
                    _sprite3Y = value;
                    break;

                case 0x08:
                    _sprite4X = value;
                    break;

                case 0x09:
                    _sprite4Y = value;
                    break;

                case 0x0A:
                    _sprite5X = value;
                    break;

                case 0x0B:
                    _sprite5Y = value;
                    break;

                case 0x0C:
                    _sprite6X = value;
                    break;

                case 0x0D:
                    _sprite6Y = value;
                    break;

                case 0x0E:
                    _sprite7X = value;
                    break;

                case 0x0F:
                    _sprite7Y = value;
                    break;

                case 0x10:
                    _msbSpriteXs = value;
                    break;

                case 0x11:
                    _controlRegister1 = value;
                    _rSel = (byte)((value & 0x8) >> 3);
                    if (_rSel == 0)
                    {
                        _borderTop = 55;
                        _borderBottom = 246;
                    }
                    else
                    {
                        //text_lines = 25;
                        _borderTop = 51;
                        _borderBottom = 250;
                    }

                    // save lower bit
                    _videoMode &= 0x01;
                    _videoMode |= (byte)((value >> 4) & 0x6); // filter out lower bit
                    _den = (byte)((value >> 4) & 0x1);
                    _yScroll = (byte)(value & 7);
                    _rasterComparator = (_rasterComparator & 0xFF) | ((value & 0x80) << 1);
                    break;

                case 0x12:
                    _rasterComparator = (_rasterComparator & 0x100) | value;
                    break;

                case 0x13:
                case 0x14:
                    break; // Light pen position latches are read-only.

                case 0x15:
                    _spriteEnabled = value;
                    break;

                case 0x16:
                    _controlRegister2 = (byte)(value & 0x3F);
                    _cSel = (byte)((value & 0x8) >> 3);

                    if (_cSel == 0)
                    {
                        _borderLeft = 31;
                        _borderRight = 334;
                    }
                    else
                    {
                        _borderLeft = 24;
                        _borderRight = 343;
                    }
                    _videoMode = (byte)((_videoMode & 0x06) | ((value >> 4) & 1));

                    _xScroll = (byte)(value & 7);
                    break;
                case 0x17:
                    _spriteYExpansion = value;
                    break;

                case 0x18:
                    _videoMatrix = (byte)((value >> 4) & 0xF);
                    _characterGenerator = (byte)((value >> 1) & 0x7);
                    break;

                case 0x19:
                    // Writing a 1 acknowledges the corresponding interrupt source.
                    _interruptRegister &= (byte)~(value & 0x0F);
                    break;

                case 0x1A:
                    _interruptEnabled = (byte)(value & 0x0F);
                    UpdateInterruptOutput();
                    break;

                case 0x1B:
                    _spriteDataPriority = value;
                    break;

                case 0x1C:
                    _spriteMulticolor = value;
                    break;

                case 0x1D:
                    _spriteXExpansion = value;
                    break;

                case 0x1E:
                case 0x1F:
                    break; // Collision registers are read-only.


                case 0x20:
                    _borderColor = (byte)(value & 0xF);
                    break;

                case 0x21:
                    _backgroundColor0 = (byte)(value & 0xF);
                    break;

                case 0x22:
                    _backgroundColor1 = (byte)(value & 0xF);
                    break;

                case 0x23:
                    _backgroundColor2 = (byte)(value & 0xF);
                    break;

                case 0x24:
                    _backgroundColor3 = (byte)(value & 0xF);
                    break;

                case 0x25:
                    _spriteMulticolor0 = (byte)(value & 0xF);
                    break;

                case 0x26:
                    _spriteMulticolor1 = (byte)(value & 0xF);
                    break;

                case 0x27:
                    _colorSprite0 = (byte)(value & 0xF);
                    break;

                case 0x28:
                    _colorSprite1 = (byte)(value & 0xF);
                    break;

                case 0x29:
                    _colorSprite2 = (byte)(value & 0xF);
                    break;

                case 0x2A:
                    _colorSprite3 = (byte)(value & 0xF);
                    break;

                case 0x2B:
                    _colorSprite4 = (byte)(value & 0xF);
                    break;

                case 0x2C:
                    _colorSprite5 = (byte)(value & 0xF);
                    break;

                case 0x2D:
                    _colorSprite6 = (byte)(value & 0xF);
                    break;

                case 0x2E:
                    _colorSprite7 = (byte)(value & 0xF);
                    break;

                default:
                    break;
            }
        }

        public byte VicRead(ushort address)
        {
            // Bank
            // 0x00 3 0xC000 - 0xFFFF
            // 0x01 2 0x8000 - 0xBFFF
            // 0x02 1 0x4000 - 0x7FFF
            // 0x03 0 0x0000 - 0x3FFF
            var bank = 3 - (_bus._c64.Cia2.EffectivePortA & 3);

            address = (ushort)((bank * 0x4000) | address);

            byte data = address switch
            {
                >= 0x1000 and < 0x1FFF => _bus.CharRom[address - 0x1000],
                >= 0x9000 and < 0x9FFF => _bus.CharRom[address - 0x9000],
                _ => _bus._ram[address]
            };
            return data;
        }

        public void Clock()
        {
            if (_rasterLine == 0x30 && _den == 1)
            {
                _denFrameSet = true;
            }

            if (_rasterLine >= 0x30 && _rasterLine <= 0xF7 && ((_rasterLine & 7) == _yScroll) && _denFrameSet)
            {
                _state = StateEnum.DisplayState;
                _badLineCondition = true;
            }

            if (_cycle >= 12 && _cycle <= 54 && _badLineCondition)
            {
                _bus.BusAvailable = false;
            }

            byte spr = 0;
            switch (_cycle)
            {
                // First few odd cycles of the raster line access sprite data 3 - 9
                case <= 9:
                    if (_cycle % 2 == 1)
                    {
                        spr = (byte)(3 + (_cycle - 1) / 2);
                        p_access(spr);
                    }
                    break;
                case 14:
                    _videoCounter = _videoCounterBase;
                    _videoMatrixLineIndex = 0;
                    if (_badLineCondition)
                    {
                        _rowCounter = 0;
                    }
                    break;

                case 15:
                    c_access();
                    break;

                case >= 16 and <= 54:
                    g_access();
                    c_access();
                    break;

                case 55:
                    g_access();
                    break;

                case 58:
                    _bus.BusAvailable = true;
                    if (_rowCounter == 7)
                    {
                        _videoCounterBase = _videoCounter;
                        _state = StateEnum.IdleState;
                    }

                    if (_state == StateEnum.DisplayState)
                    {
                        _rowCounter++;
                    }

                    spr = (byte)((_cycle - 58) / 2);
                    p_access(spr);
                    break;

                case 60:
                case 62:
                    spr = (byte)((_cycle - 58) / 2);
                    p_access(spr);
                    break;
            }

            uint y = (uint)(_rasterLine - StartRaster);
            var displayIndex = _videoMatrixLineIndex == 0 ? 0 : (int)_videoMatrixLineIndex - 1;
            if (displayIndex > 39)
                displayIndex = 39;
            var c_color = _colorLine[displayIndex];

            if (y <= (Height - 1))
            {
                for (var i = 0; i < 8; i++)
                {
                    // Some magic offset
                    var x = (_cycle - 13) * 8 + i;

                    if (x <= Width - 1)
                    {
                        //There are 2×2 comparators belonging to each of the two flip flops. There
                        //comparators compare the X/Y position of the raster beam with one of two
                        //hardwired values (depending on the state of the CSEL/RSEL bits) to control
                        //the flip flops. The comparisons only match if the values are reached
                        //precisely. There is no comparison with an interval.


                        //The flip flops are switched according to the following rules:

                        //1. If the X coordinate reaches the right comparison value, the main border
                        //   flip flop is set.
                        //2. If the Y coordinate reaches the bottom comparison value in cycle 63, the
                        //   vertical border flip flop is set.
                        //3. If the Y coordinate reaches the top comparison value in cycle 63 and the
                        //   DEN bit in register $d011 is set, the vertical border flip flop is
                        //   reset.
                        //4. If the X coordinate reaches the left comparison value and the Y
                        //   coordinate reaches the bottom one, the vertical border flip flop is set.
                        //5. If the X coordinate reaches the left comparison value and the Y
                        //   coordinate reaches the top one and the DEN bit in register $d011 is set,
                        //   the vertical border flip flop is reset.
                        //6. If the X coordinate reaches the left comparison value and the vertical
                        //   border flip flop is not set, the main flip flop is reset.

                        if (x == _borderRight)
                        {
                            _mainBorder = true;
                        }
                        if (y == _borderBottom - StartRaster && _cycle == CyclesPerScanline)
                        {
                            _verticalBorder = true;
                        }
                        if (y == _borderTop - StartRaster && _den == 1)
                        {
                            _verticalBorder = false;
                        }
                        if (x == _borderLeft && y == _borderBottom - StartRaster)
                        {
                            _verticalBorder = true;
                        }
                        if (x == _borderLeft && y == _borderTop - StartRaster && _den == 1)
                        {
                            _verticalBorder = false;
                        }
                        if (x == _borderLeft && !_verticalBorder)
                        {
                            _mainBorder = false;
                        }

                        //So the Y coordinate is checked once or twice within each raster line: In
                        //cycle 63 and if the X coordinate reaches the left comparison value.

                        if (_mainBorder || _verticalBorder)
                        {
                            _frameBuffer[x + y * Width] = C64Palette.GetColor(_borderColor);
                        }
                        else
                        {
                            if (_state == StateEnum.DisplayState)
                            {
                                // Decode standard text, multicolour text, hires bitmap and multicolour bitmap.
                                byte screenByte = _videoMatrixLine[displayIndex];
                                byte pixelColor;
                                if ((_videoMode & 2) != 0)
                                {
                                    if ((_videoMode & 1) != 0)
                                    {
                                        int pair = (_gData >> (6 - (i & ~1))) & 3;
                                        pixelColor = pair switch
                                        {
                                            0 => _backgroundColor0,
                                            1 => (byte)(screenByte >> 4),
                                            2 => (byte)(screenByte & 0x0F),
                                            _ => (byte)(c_color & 0x0F)
                                        };
                                    }
                                    else
                                    {
                                        pixelColor = ((_gData >> (7 - i)) & 1) == 0
                                            ? (byte)(screenByte & 0x0F) : (byte)(screenByte >> 4);
                                    }
                                }
                                else if ((_videoMode & 1) != 0 && (c_color & 8) != 0)
                                {
                                    int pair = (_gData >> (6 - (i & ~1))) & 3;
                                    pixelColor = pair switch
                                    {
                                        0 => _backgroundColor0,
                                        1 => _backgroundColor1,
                                        2 => _backgroundColor2,
                                        _ => (byte)(c_color & 7)
                                    };
                                }
                                else
                                {
                                    pixelColor = ((_gData >> (7 - i)) & 1) == 0
                                        ? _backgroundColor0 : (byte)(c_color & 0x0F);
                                }
                                bool foreground = false;
                                if ((_videoMode & 2) != 0)
                                    foreground = ((_gData >> (7 - i)) & 1) != 0;
                                else if ((_videoMode & 1) != 0 && (c_color & 8) != 0)
                                    foreground = ((_gData >> (6 - (i & ~1))) & 3) != 0;
                                else
                                    foreground = ((_gData >> (7 - i)) & 1) != 0;

                                _frameBuffer[x + y * Width] = RenderSpritePixel((int)x, foreground, C64Palette.GetColor(pixelColor));
                            }
                            else
                            {
                                _frameBuffer[x + y * Width] = C64Palette.GetColor(_borderColor);
                            }
                        }
                    }
                }
            }

            _cycle++;

            if (_cycle >= CyclesPerScanline)
            {
                // IRQ
                // The negative edge of IRQ on a raster interrupt has been used to define the
                // beginning of a line(this is also the moment in which the RASTER register
                // is incremented).Raster line 0 is, however, an exception: In this line, IRQ
                // and incrementing(resp.resetting) of RASTER are performed one cycle later
                // than in the other lines.But for simplicity we assume equal line lengths
                // and define the beginning of raster line 0 to be one cycle before the
                // occurrence of the IRQ.

                _cycle = 1;
                _rasterLine++;

                if (_rasterLine == _rasterComparator)
                {
                    _interruptRegister |= 0x01;
                    UpdateInterruptOutput();
                }

                _badLineCondition = false;

                if (_rasterLine >= TotalScanlines)
                {
                    _rasterLine = 0;

                    // Should this be here?
                    _denFrameSet = false;

                    _videoCounterBase = 0;
                }
            }

            _cycles++;
        }

        // Sprite row data is cached once per raster line rather than fetched for every pixel.
        private readonly uint[] _spriteRowBits = new uint[8];
        private int _spriteCacheRaster = -1;

        private void CacheSpriteRows()
        {
            if (_spriteCacheRaster == _rasterLine)
                return;

            _spriteCacheRaster = _rasterLine;
            for (int sprite = 0; sprite < 8; sprite++)
            {
                _spriteRowBits[sprite] = 0;
                if ((_spriteEnabled & (1 << sprite)) == 0)
                    continue;

                int sy = GetSpriteCoordinate(sprite * 2 + 1);
                int dy = _rasterLine - sy;
                bool expandY = (_spriteYExpansion & (1 << sprite)) != 0;
                int row = expandY ? dy / 2 : dy;
                if (dy < 0 || row >= 21)
                    continue;

                // Sprite pointers are the last eight bytes of the current screen page.
                ushort pointerAddress = (ushort)((_videoMatrix << 10) + 0x3F8 + sprite);
                int spriteBase = VicRead(pointerAddress) * 64;
                int offset = spriteBase + row * 3;
                _spriteRowBits[sprite] = (uint)((VicRead((ushort)offset) << 16) |
                    (VicRead((ushort)(offset + 1)) << 8) | VicRead((ushort)(offset + 2)));
            }
        }

        private uint RenderSpritePixel(int x, bool foreground, uint backgroundPixel)
        {
            CacheSpriteRows();
            int present = 0;
            int visibleSprite = -1;
            byte visibleColor = 0;

            for (int sprite = 0; sprite < 8; sprite++)
            {
                if ((_spriteEnabled & (1 << sprite)) == 0)
                    continue;

                int sx = GetSpriteCoordinate(sprite * 2);
                if ((_msbSpriteXs & (1 << sprite)) != 0)
                    sx += 256;

                int dx = x - sx;
                bool expandX = (_spriteXExpansion & (1 << sprite)) != 0;
                int pixel = expandX ? dx / 2 : dx;
                if (dx < 0 || pixel >= 24 || _spriteRowBits[sprite] == 0)
                    continue;

                byte color;
                if ((_spriteMulticolor & (1 << sprite)) != 0)
                {
                    int pair = (int)((_spriteRowBits[sprite] >> (22 - (pixel / 2) * 2)) & 3);
                    if (pair == 0)
                        continue;
                    color = pair switch
                    {
                        1 => _spriteMulticolor0,
                        2 => GetSpriteColor(sprite),
                        _ => _spriteMulticolor1
                    };
                }
                else
                {
                    if (((_spriteRowBits[sprite] >> (23 - pixel)) & 1) == 0)
                        continue;
                    color = GetSpriteColor(sprite);
                }

                int mask = 1 << sprite;
                if (present != 0)
                {
                    _spriteSpriteCollision |= (byte)(present | mask);
                    _interruptRegister |= 0x04;
                }
                present |= mask;
                if (foreground)
                {
                    _spriteDataCollision |= (byte)mask;
                    _interruptRegister |= 0x02;
                }

                if (visibleSprite < 0 && (!foreground || (_spriteDataPriority & mask) == 0))
                {
                    visibleSprite = sprite;
                    visibleColor = color;
                }
            }

            if ((_interruptRegister & 0x06) != 0)
                UpdateInterruptOutput();

            return visibleSprite >= 0 ? C64Palette.GetColor(visibleColor) : backgroundPixel;
        }

        void p_access(byte sprite)
        {
        }

        void s_access()
        {
        }

        void c_access()
        {
            if (_badLineCondition)
            {
                // d018 |VM13|VM12|VM11|VM10|CB13|CB12|CB11|  - | Memory pointers
                ushort address = (ushort)(_videoMatrix << 10);
                address |= _videoCounter;

                //The read data is stored
                //in the video matrix/color line at the position specified by VMLI. These
                //data is internally read from the position specified by VMLI as well on
                //each g-access in display state.

                _videoMatrixLine[_videoMatrixLineIndex] = VicRead(address);
                _colorLine[_videoMatrixLineIndex] = _bus._colorRam[_videoCounter & 0x3FF];
            }
        }

        void g_access()
        {
            var c_data = _videoMatrixLine[_videoMatrixLineIndex];

            // TEST: Display incrementing data to show character set
            //c_data = video_counter;

            // Bitmap mode uses the screen matrix index, not the character code.
            var address = (ushort)(((_videoMode & 2) != 0)
                ? ((_characterGenerator & 4) << 11) | ((_videoCounter & 0x3FF) << 3) | (_rowCounter & 7)
                : (_characterGenerator << 11) | (c_data << 3) | (_rowCounter & 7));
            _gData = VicRead(address);
            if (_state == StateEnum.DisplayState)
            {
                //  VC and VMLI are incremented after each g-access in display state.
                _videoCounter++;
                _videoMatrixLineIndex++;
            }
        }

        public uint[] GetFrame()
        {
            return _frameBuffer;
        }
    }
}
