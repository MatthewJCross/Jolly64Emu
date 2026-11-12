namespace JollyC64Emu.Core.VIC
{
    public static class C64Palette
    {
        private static readonly uint[] Palette =
        {
            0xFF000000, // Black
            0xFFFFFFFF, // White
            0xFF68372B, // Red
            0xFF70A4B2, // Cyan
            0xFF6F3D86, // Purple
            0xFF588D43, // Green
            0xFF352879, // Blue
            0xFFB8C76F, // Yellow
            0xFF6F4F25, // Orange
            0xFF433900, // Brown
            0xFF9A6759, // Light red
            0xFF444444, // Dark gray
            0xFF6C6C6C, // Gray
            0xFF9AD284, // Light green
            0xFF6C5EB5, // Light blue
            0xFF959595  // Light gray
        };
        
        public static uint GetColor(int index) => Palette[index & 0x0F];
    }
}
