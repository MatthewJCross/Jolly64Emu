using Jolly64Emu.Core.Keyboard;
using System.Diagnostics;
using System.Windows.Input;

namespace Jolly64Emu.Core.CIA6526
{
    public class CIA
    {
        private readonly Dictionary<Key, (int row, int col)> _map = new()
        {
            // Letters
            { Key.A, (1,2) }, { Key.B, (3,4) }, { Key.C, (2,4) }, { Key.D, (2,2) },
            { Key.E, (1,6) }, { Key.F, (2,5) }, { Key.G, (3,2) }, { Key.H, (3,5) },
            { Key.I, (4,1) }, { Key.J, (4,2) }, { Key.K, (4,5) }, { Key.L, (5,2) },
            { Key.M, (4,4) }, { Key.N, (4,7) }, { Key.O, (4,6) }, { Key.P, (5,1) },
            { Key.Q, (7,5) }, { Key.R, (2,1) }, { Key.S, (1,5) }, { Key.T, (2,6) },
            { Key.U, (3,6) }, { Key.V, (3,7) }, { Key.W, (1,1) }, { Key.X, (2,7) },
            { Key.Y, (3,1) }, { Key.Z, (1,4) },

            // Numbers
            { Key.D1, (7,0) }, { Key.D2, (7,3) }, { Key.D3, (1,0) }, { Key.D4, (1,3) },
            { Key.D5, (2,0) }, { Key.D6, (2,3) }, { Key.D7, (3,0) }, { Key.D8, (3,3) },
            { Key.D9, (4,0) }, { Key.D0, (4,3) },

            // Core keys
            { Key.Space, (6,4) },
            { Key.Enter, (0,1) },

            // Shift
            { Key.LeftShift, (1,7) },
            { Key.RightShift, (6,1) },

            // Cursor
            { Key.Up, (7,7) },
            { Key.Down, (0,7) },
            { Key.Left, (6,7) },
            { Key.Right, (0,2) },
        };

        private readonly bool[,] _keys = new bool[8, 8]; // [row, col]                                                         

        public byte PortA { get; private set; }
        public byte PortB { get; private set; }

        // CIA output pins: input bits are pulled high unless driven externally.
        public byte EffectivePortA => (byte)((_portA & _readwriteMaskA) | ~_readwriteMaskA);

        // Bit X: 0=Input (read only), 1=Output (read and write)
        private byte _portA;
        private byte _portB;
        private byte _readwriteMaskA;
        private byte _readwriteMaskB;

        public Func<byte> PortARead { get; set; }
        public Action<byte> PortAWrite { get; set; }

        public Func<byte> PortBRead { get; set; }
        public Action<byte> PortBWrite { get; set; }

        public Action RequestInterrupt { get; set; }

        private uint _intMask;
        private byte _intData;

        #region TimerVariables
        private int _timerA;
        private int _timerB;
        private int _timerALatch;
        private int _timerBLatch;
        private byte _rtcTenths;
        private byte _rtcSeconds;
        private byte _rtcTenSeconds;
        private byte _rtcMinutes;
        private byte _rtcTenMinutes;
        private byte _rtcHours;
        private byte _rtcTenHours;
        private byte _rtcAmPm;

        private uint alarm_tenths;
        private uint alarm_seconds;
        private uint alarm_tenseconds;
        private uint alarm_minutes;
        private uint alarm_tenminutes;
        private uint alarm_hours;
        private uint alarm_tenhours;
        private uint alarm_am_pm;

        private uint _todSetMode; // 0 = clock, 1 = alarm
        private bool _todStopped;

        private uint _shiftRegister_bit;
        private uint _shiftRegister_direction; // 0 = input, 1 = output
        private byte _shiftRegister;

        private byte int_underflow_A;
        private byte int_underflow_B;
        private byte int_alarm;


        private byte _timerAStart;
        private byte _timerA_underflow_portB6;
        private byte _timerA_underflow_portB6_type;
        private byte _timerAUnderflowStop;
        private byte _timerALoad;
        private byte _timerACountMode;

        private byte _timerBStart;
        private byte _timerB_underflow_portB6;
        private byte _timerB_underflow_portB6_type;
        private byte _timerBUnderflowStop;
        private byte _timerBLoad;
        private byte _timerBCountMode;

        private uint _todFrameDivider; // 5 = 50Hz, 6 = 60hz;
        private uint _todFrameCounter;
        #endregion


        private InputSystem _keyboard;
        public CIA(InputSystem keyboard = null)
        {
            _keyboard = keyboard;
        }

        public void Reset()
        {
            if (_keyboard is not null)
            {
                for (int i = 0; i < 8; i++)
                    _keyboard.KeyMatrix[i] = _keyboard.RevMatrix[i] = 0xff;
            }
        }

        public byte Read(uint address)
        {
            byte data = 0;
            address &= 0x0F; // Mirror every 16 address bytes
            data = address switch
            {
                0x00 => ReadPortA(),
                0x01 => ReadPortB(),
                0x02 => _readwriteMaskA,
                0x03 => _readwriteMaskB,
                0x04 => (byte)(_timerA & 0xFF),
                0x05 => (byte)((_timerA >> 8) & 0xFF),
                0x06 => (byte)(_timerB & 0xFF),
                0x07 => (byte)((_timerB >> 8) & 0xFF),
                0x08 => _rtcTenths,
                0x09 => (byte)(_rtcSeconds | (_rtcTenSeconds << 4)),
                0x0A => (byte)(_rtcMinutes | (_rtcTenMinutes << 4)),
                0x0B => (byte)(_rtcHours | (_rtcTenHours << 4) | (_rtcAmPm << 7)),
                0x0C => _shiftRegister,
                0x0D => ReadInterrupts(),
                _ => data
            };// //
            return data;
        }

        private byte ReadPortA()
        {
            byte result = EffectivePortA;
            // Joystick port 2 is connected to CIA1 port A (DC00).
            // The joystick pulls input pins low; output pins remain driven by the CIA.
            if (_keyboard != null)
                result &= (byte)(~_readwriteMaskA | _keyboard.Joystick2);
            if (_keyboard == null)
                return result;

            byte selectedRows = (byte)((_portB & _readwriteMaskB) | ~_readwriteMaskB);
            for (int row = 0; row < 8; row++)
            {
                if ((selectedRows & (1 << row)) == 0)
                    result &= _keyboard.RevMatrix[row];
            }
            return result;
        }

        private byte ReadPortB()
        {
            byte result = (byte)((_portB & _readwriteMaskB) | ~_readwriteMaskB);
            if (_keyboard == null)
                return result;

            byte selectedColumns = EffectivePortA;
            for (int col = 0; col < 8; col++)
            {
                if ((selectedColumns & (1 << col)) == 0)
                    result &= _keyboard.KeyMatrix[col];
            }
            return result;
        }

        private byte ReadInterrupts()
        {
            var temp = _intData;
            _intData = 0; //Flags will be cleared after reading the register!
            return temp;
        }

        private Action<byte> GetPortAWrite()
        {
            return PortAWrite;
        }

        private void UpdatePortA(Action<byte> portAWrite)
        {
            var output = _portA;
            output &= _readwriteMaskA;

            if (PortAWrite != null)
            {
                portAWrite(output);
            }
            else
            {
                PortA &= (byte)(_readwriteMaskA ^ 0xFF);
                PortA |= output;
            }
        }

        private void UpdatePortB()
        {
            var output = _portB;
            _portB &= _readwriteMaskB;

            if (PortBWrite != null)
            {
                //PortBWrite(output);
            }
            else
            {
                PortB &= (byte)(_readwriteMaskB ^ 0xFF);
                PortB |= output;
            }
        }

        public void Write(ushort address, byte data)
        {
            address &= 0x0F; // Mirror every 16 address bytes

            switch (address)
            {
                case 0x0:
                    _portA = data;
                    UpdatePortA(GetPortAWrite());
                    break;

                case 0x1:
                    _portB = data;
                    UpdatePortB();
                    break;

                case 0x2:
                    _readwriteMaskA = data;
                    UpdatePortA(GetPortAWrite());
                    break;

                case 0x3:
                    _readwriteMaskB = data;
                    UpdatePortB();
                    break;

                case 0x4:
                    _timerALatch |= (int)data;
                    break;

                case 0x5:
                    _timerALatch |= (int)(data << 8);
                    break;

                case 0x6:
                    _timerBLatch |= (int)data;
                    break;

                case 0x7:
                    _timerBLatch |= (int)(data << 8);
                    break;

                case 0x8:
                    if (_todSetMode == 1)
                    {
                        alarm_tenths = (byte)(data & 0x0F);
                    }
                    else
                    {
                        _rtcTenths = (byte)(data & 0x0F);
                    }
                    _todStopped = false;
                    break;

                case 0x9:
                    if (_todSetMode == 1)
                    {
                        alarm_seconds = (byte)(data & 0x0F);
                        alarm_tenseconds = (byte)((data >> 4) & 0x07);
                    }
                    else
                    {
                        _rtcSeconds = (byte)(data & 0x0F);
                        _rtcTenSeconds = (byte)((data >> 4) & 0x07);
                    }
                    break;

                case 0xA:
                    if (_todSetMode == 1)
                    {
                        alarm_minutes = (byte)(data & 0x0F);
                        alarm_tenminutes = (byte)((data >> 4) & 0x07);
                    }
                    else
                    {
                        _rtcMinutes = (byte)(data & 0x0F);
                        _rtcTenMinutes = (byte)((data >> 4) & 0x07);
                    }
                    break;

                case 0xB:
                    if (_todSetMode == 1)
                    {
                        alarm_hours = (byte)(data & 0x0F);
                        alarm_tenhours = (byte)((data >> 4) & 0x07);
                        alarm_am_pm = (byte)((data >> 7) & 1);
                    }
                    else
                    {
                        _rtcHours = (byte)(data & 0x0F);
                        _rtcTenHours = (byte)((data >> 8) & 0x07);
                        _rtcAmPm = (byte)((data >> 7) & 1);
                    }
                    // TODO: Writing into this register stops TOD, until register 8(TOD 10THS) will be read.
                    _todStopped = true;
                    break;

                case 0xC:
                    _shiftRegister = data;
                    break;

                case 0xD:
                    // Bit 7: Source bit.
                    // 0 = set bits 0..4 are clearing the according mask bit.
                    // 1 = set bits 0..4 are setting the according mask bit.
                    // If all bits 0..4 are cleared, there will be no change to the mask.
                    if (((data >> 7) & 1) == 1)
                    {
                        // set bits
                        _intMask |= (byte)(data & 0x01F);
                    }
                    else
                    {
                        _intMask &= (byte)((data & 0x1F) ^ 0xFF);
                    }
                    break;

                case 0xE:
                    _timerAStart = (byte)(data & 1);
                    _timerAUnderflowStop = (byte)((data >> 3) & 1);
                    _timerALoad = (byte)((data >> 4) & 1);
                    // should this be done here?
                    if (_timerALoad == 1)
                    {
                        _timerA = _timerALatch;
                    }
                    _timerACountMode = (byte)((data >> 5) & 1);
                    _shiftRegister_direction = (byte)((data >> 6) & 1);
                    _todFrameDivider = (byte)(5 + ((data >> 7) & 1)); // value will be 5 or 6
                    break;

                case 0xF:
                    _timerBStart = (byte)(data & 1);
                    _timerBUnderflowStop = (byte)((data >> 3) & 1);
                    _timerBLoad = (byte)((data >> 4) & 1);
                    // should this be done here?
                    if (_timerBLoad == 1)
                    {
                        _timerB = _timerBLatch;
                    }
                    _timerBCountMode = (byte)((data >> 5) & 3);
                    _todSetMode = (byte)((data >> 7) & 1);
                    break;
            }
            return;
        }

        public void TimeOfDay()
        {
            if (_todStopped)
                return;

            _todFrameCounter++;
            if (_todFrameCounter > _todFrameDivider)
            {
                _todFrameCounter = 0;
                _rtcTenths++;
                if (_rtcTenths > 9)
                {
                    _rtcTenths = 0;
                    _rtcSeconds++;
                    if (_rtcSeconds > 9)
                    {
                        _rtcSeconds = 0;
                        _rtcTenSeconds++;
                        if (_rtcTenSeconds > 5)
                        {
                            _rtcTenSeconds = 0;
                            _rtcMinutes++;
                            if (_rtcMinutes > 9)
                            {
                                _rtcMinutes = 0;
                                _rtcTenMinutes++;
                                if (_rtcTenMinutes > 5)
                                {
                                    _rtcTenMinutes = 0;
                                    _rtcHours++;
                                    if (_rtcTenHours < 2 && _rtcHours > 9)
                                    {
                                        _rtcHours = 0;
                                        _rtcTenHours++;
                                    }
                                    else if (_rtcTenHours == 2 && _rtcHours > 3)
                                    {
                                        _rtcTenths = 0;
                                        _rtcSeconds = 0;
                                        _rtcTenSeconds = 0;
                                        _rtcMinutes = 0;
                                        _rtcTenMinutes = 0;
                                        _rtcHours = 0;
                                        _rtcTenHours = 0;
                                    }
                                }
                            }
                        }
                    }
                    //Console.WriteLine($"{_rtcTenHours}{_rtcHours}:{_rtcTenMinutes}{_rtcMinutes}:{_rtcTenSeconds}{_rtcSeconds}");
                }
            }

        }

        public void Count()
        {
            if (_timerACountMode == 1)
            {
                UpdateTimerA();
            }
            if (_timerBCountMode == 1)
            {
                UpdateTimerB();
            }
        }

        private void UpdateTimerA()
        {
            if (_timerAStart == 1)
            {
                _timerA--;
                if (_timerA < 0)
                {
                    if ((_intMask & 1) == 1)
                    {
                        _intData |= 1;
                        RequestInterrupt?.Invoke();
                    }
                    if (_timerAUnderflowStop == 0)
                    {
                        _timerA = _timerALatch;
                        if (_timerBCountMode == 2)
                        {
                            //0b10 = Timer counts underflow of timer A
                            //TODO: 0b11 = Timer counts underflow of timer A if the CNT - pin is high
                            UpdateTimerB();
                        }
                    }
                    else
                    {
                        _timerAStart = 0;
                    }
                }
            }
        }

        private void UpdateTimerB()
        {
            if (_timerBStart == 1)
            {
                _timerB--;
                if (_timerB < 0)
                {
                    if ((_intMask & 2) == 2)
                    {
                        _intData |= 2;
                        RequestInterrupt?.Invoke();
                    }
                    if (_timerBUnderflowStop == 0)
                    {
                        _timerB = _timerBLatch;
                    }
                }
            }
        }

        public void Clock()
        {
            if (_timerACountMode == 0)
            {
                UpdateTimerA();
            }
            if (_timerBCountMode == 0)
            {
                UpdateTimerB();
            }
        }
    }
}
