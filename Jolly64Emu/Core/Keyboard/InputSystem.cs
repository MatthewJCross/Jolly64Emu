using System.Windows.Input;
using System.Threading;

namespace Jolly64Emu.Core.Keyboard
{
    public struct RowCol
    {
        public RowCol(uint col, uint row)
        {
            Row = row;
            Col = col;
        }

        public uint Row;
        public uint Col;
    }

    public class InputSystem
    {
        private readonly KeyboardMatrix _keyboardMatrix = new KeyboardMatrix();
        private bool DeletePressed = false;

        // Joystick port 2: bits 0-4 are active-low (up, down, left, right, fire).
        // Volatile access allows WPF key events and the emulation thread to share the state.
        private int _joystick2 = 0xFF;
        public byte Joystick2 => (byte)Volatile.Read(ref _joystick2);

        private static int JoystickBit(Key key) => key switch
        {
            Key.Up => 0,
            Key.Down => 1,
            Key.Left => 2,
            Key.Right => 3,
            Key.Space or Key.RightCtrl => 4,
            _ => -1
        };

        private void UpdateJoystick(Key key, bool released)
        {
            int bit = JoystickBit(key);
            if (bit < 0) return;
            int mask = 1 << bit;
            if (released)
                Interlocked.Or(ref _joystick2, mask);
            else
                Interlocked.And(ref _joystick2, ~mask);
        }

        public uint key_col;
        private uint[] rowCols = new uint[8];

        byte[] _keyMatrix = new byte[8];	// C64 keyboard matrix, 1 bit/key (0: key down, 1: key up)
        public byte[] KeyMatrix
        {
            get => _keyMatrix;
            set => _keyMatrix = value;
        }

        byte[] _revMatrix = new byte[8];	// Reversed keyboard matrix     
        public byte[] RevMatrix
        {
            get => _revMatrix;
            set => _revMatrix = value;
        }

        public Key LastKeyPressed { get; private set; }

        private readonly bool[,] _keys = new bool[8, 8]; // [row, col]
        private readonly Dictionary<Key, (int row, int col)> _keyMap;

        public byte _keyCol;
        private byte[] _rowCols = new byte[8];

        public InputSystem()
        {
            _keyMap = BuildKeyMap();
            Array.Fill(_keyMatrix, (byte)0xFF);
            Array.Fill(_revMatrix, (byte)0xFF);
        }

        private Dictionary<Key, (int row, int col)> BuildKeyMap()
        {
            return new Dictionary<Key, (int, int)>
            {
                { Key.A, (1,2) }, { Key.B, (3,4) }, { Key.C, (2,4) }, { Key.D, (2,2) },
                { Key.E, (1,6) }, { Key.F, (2,5) }, { Key.G, (3,2) }, { Key.H, (3,5) },
                { Key.I, (4,1) }, { Key.J, (4,2) }, { Key.K, (4,5) }, { Key.L, (5,2) },
                { Key.M, (4,4) }, { Key.N, (4,7) }, { Key.O, (4,6) }, { Key.P, (5,1) },
                { Key.Q, (7,6) }, { Key.R, (2,1) }, { Key.S, (1,5) }, { Key.T, (2,6) },
                { Key.U, (3,6) }, { Key.V, (3,7) }, { Key.W, (1,1) }, { Key.X, (2,7) },
                { Key.Y, (3,1) }, { Key.Z, (1,4) },

                { Key.D1, (7,0) }, { Key.D2, (7,3) }, { Key.D3, (1,0) }, { Key.D4, (1,3) },
                { Key.D5, (2,0) }, { Key.D6, (2,3) }, { Key.D7, (3,0) }, { Key.D8, (3,3) },
                { Key.D9, (4,0) }, { Key.D0, (4,3) },

                { Key.Space, (6,4) },
                { Key.Enter, (0,1) },
                { Key.LeftShift, (1,7) },
                { Key.RightShift, (6,1) },

                { Key.Up, (7,7) },
                { Key.Down, (0,7) },
                { Key.Left, (6,7) },
                { Key.Right, (0,2) },
            };
        }

        public void KeyDown(Key key)
        {
            LastKeyPressed = key;
            UpdateJoystick(key, false);
            TranslateKey(key, false);
        }

        public void KeyUp(Key key)
        {
            UpdateJoystick(key, true);
            TranslateKey(key, true);
        }

        public void Clear()
        {
            LastKeyPressed = 0;
        }

        public byte PortBRead()
        {
            // Check if anything pressed
            if (key_col == 0xFF && ((rowCols[0] | rowCols[1] | rowCols[2] | rowCols[3] | rowCols[4] | rowCols[5] | rowCols[6] | rowCols[7]) > 0) | DeletePressed)
            {
                return 0x01;
            }

            if (DeletePressed & key_col == 1)
            {
                return 0xFE;
            }

            if (key_col != 0xFF)
            {
                var index = _keyboardMatrix.ColumnIndex[key_col];
                return (byte)(rowCols[index] ^ 0xFF);
            }

            return 0xFF;
        }

        public void PortAWrite(byte value)
        {
            key_col = (byte)(value ^ 0xFF);
        }

        public bool TryEncode(Key key, out RowCol rowCol)
        {
            rowCol = default(RowCol);
            return false;
        }




        // =====================================================================================================================================
        // Do we need this, save for now

        public byte ReadKeyboard(byte portA)
        {
            byte result = 0xFF;

            for (int col = 0; col < 8; col++)
            {
                //Column selected if bit is 0
                if (((portA >> col) & 1) == 0)
                {
                    for (int row = 0; row < 8; row++)
                    {
                        if (_keys[row, col])
                        {
                            result &= (byte)~(1 << row); // active LOW
                        }
                    }
                }
            }

            return result;
        }

        private void TranslateKey(Key key, bool key_up)
        {
            if (!_keyMap.TryGetValue(key, out var position))
                return;

            int row = position.row;
            int col = position.col;
            byte rowMask = (byte)(1 << col);
            byte colMask = (byte)(1 << row);

            if (key_up)
            {
                _keyMatrix[row] |= rowMask;
                _revMatrix[col] |= colMask;
            }
            else
            {
                _keyMatrix[row] &= (byte)~rowMask;
                _revMatrix[col] &= (byte)~colMask;
            }
        }

        private int MATRIX(int a, int b)
        {
            return (a << 3) | b;
        }

    }
}
