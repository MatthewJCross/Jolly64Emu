using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace Jolly64Emu.Core.Keyboard
{
    public class KeyboardMatrix
    {
        // The COLUMN is stored in the first dimension (vertically here) 
        // The ROW is stored in the second dimension (horizontally here)
        // COLUMN is the value strobed in CIA2 Port A
        // ROW is the value read in CIA2 Port B

        private static KeyboardEnums[,] _lookup = new KeyboardEnums[,]
        {
            {
                KeyboardEnums.STOP, KeyboardEnums.Q, KeyboardEnums.COMMODORE, KeyboardEnums.SPACE, KeyboardEnums.D2,
                KeyboardEnums.CTRL, KeyboardEnums.BACKSPACE, KeyboardEnums.D1
            },
            {
                KeyboardEnums.SLASH, KeyboardEnums.CARET, KeyboardEnums.EQUALS, KeyboardEnums.RSHIFT, KeyboardEnums.HOME,
                KeyboardEnums.SEMICOLON, KeyboardEnums.ASTERISK, KeyboardEnums.POUND
            },
            {
                KeyboardEnums.COMMA, KeyboardEnums.AT, KeyboardEnums.COLON, KeyboardEnums.PERIOD, KeyboardEnums.MINUS,
                KeyboardEnums.L, KeyboardEnums.P, KeyboardEnums.PLUS
            },
            {
                KeyboardEnums.N, KeyboardEnums.O, KeyboardEnums.K, KeyboardEnums.M, KeyboardEnums.D0, KeyboardEnums.J,
                KeyboardEnums.I, KeyboardEnums.D9    
            },
            {
                KeyboardEnums.V, KeyboardEnums.U, KeyboardEnums.H, KeyboardEnums.B, KeyboardEnums.D8,
                KeyboardEnums.G, KeyboardEnums.Y, KeyboardEnums.D7
            },
            {
                KeyboardEnums.X, KeyboardEnums.T, KeyboardEnums.F, KeyboardEnums.C, KeyboardEnums.D6,
                KeyboardEnums.D, KeyboardEnums.R, KeyboardEnums.D5
            },
            {
                KeyboardEnums.LSHIFT, KeyboardEnums.E, KeyboardEnums.S, KeyboardEnums.Z, KeyboardEnums.D4,
                KeyboardEnums.A, KeyboardEnums.W, KeyboardEnums.D3
            },
            {
                KeyboardEnums.CURSOR_DOWN, KeyboardEnums.F5, KeyboardEnums.F3, KeyboardEnums.F1, KeyboardEnums.F7,
                KeyboardEnums.CURSOR_RIGHT, KeyboardEnums.RETURN, KeyboardEnums.DELETE
            },
        };

        public Dictionary<KeyboardEnums, RowCol> _matrix = new Dictionary<KeyboardEnums, RowCol>();

        public uint[] ColumnIndex = new uint[129];

        public KeyboardMatrix()
        {
            for (int col = 0; col < 8; col++)
            {
                for (int row = 0; row < 8; row++)
                {
                    var key = _lookup[col, row];
                    _matrix.Add(key, new RowCol((uint)(1 << (7 - col)), (uint)(1 << (7 - row))));
                }
            }

            for (var i = 0; i < 8; i++)
            {
                ColumnIndex[1 << i] = (uint)i;
            }
        }

        public bool TryEncode(KeyboardEnums key, out RowCol rowCol)
        {
            return _matrix.TryGetValue(key, out rowCol);
        }

        internal void TryEncode(Key key, out RowCol rowCol)
        {
            throw new NotImplementedException();
        }
    }
}
