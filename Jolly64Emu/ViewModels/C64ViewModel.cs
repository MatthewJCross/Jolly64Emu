using Jolly64Emu.Core.Audio;
using Jolly64Emu.Core;
using JollyC64Emu.ViewModels;
using System.IO;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Jolly64Emu.ViewModels
{
    public class C64ViewModel : ViewModelBase
    {
        private string _assembler;
        public string Assembler
        {
            get => _assembler;
            set
            {
                _assembler = value;
                OnPropertyChanged("Assembler");
            }
        }

        private string _mainTitle;
        public string MainTitle
        {
            get => _mainTitle;
            set
            {
                _mainTitle = value;
                OnPropertyChanged("MainTitle");
            }
        }

        private readonly SemaphoreSlim _renderSemaphore = new(1, 1);
        private CancellationTokenSource? _cts;
        private volatile bool _isShuttingDown;
        public bool IsShutdownComplete { get; private set; }

        private int _frames;
        private double _fps;
        private DateTime _lastFpsUpdate = DateTime.Now;

        private int Width;
        private int Height;

        const int CYCLES_PER_FRAME = 19656;
        const int BORDER_X = 32;
        const int BORDER_Y = 32;
        const int totalScanlines = 312;
        const int cyclesPerLine = 63;

        private readonly C64 _c64;
        private readonly SidAudioOutput _sidAudio;
        private readonly object _machineLock = new();

        public (ushort Start, ushort End) LoadPrg(string path)
        {
            byte[] data = File.ReadAllBytes(path);
            return LoadPrgData(data);
        }

        public (ushort Start, ushort End) LoadPrgData(byte[] data)
        {
            lock (_machineLock)
            {
                var result = PrgLoader.Load(_c64.Bus, data);

                // BASIC PRGs (including games with a BASIC SYS launcher)
                // can be started by feeding RUN + RETURN to the KERNAL keyboard buffer.
                if (result.Start == 0x0801 && data.Length >= 6 &&
                    data[2] != 0 && (data[3] != 0 || data[4] != 0))
                {
                    byte[] command = [(byte)'R', (byte)'U', (byte)'N', 13];
                    Array.Copy(command, 0, _c64.Bus._ram, 0x0277, command.Length);
                    _c64.Bus._ram[0x00C6] = (byte)command.Length;
                }

                return result;
            }
        }

        public WriteableBitmap Screen { get; }
        private uint[] Frame { get; }

        bool _doTestRun = false;

        public C64ViewModel()
        {
            _c64 = new C64(_doTestRun);
            _sidAudio = new SidAudioOutput();
            _c64.Sid.SampleReady = _sidAudio.Submit;

            Width = _c64.Vic.Width;
            Height = _c64.Vic.Height;
            Screen = new WriteableBitmap(Width, Height, 96, 96, PixelFormats.Bgra32, null);
            MainTitle = "Jolly C64 Emulator";
            Frame = new uint[Width * Height];

            _c64.Reset();

            if (_doTestRun)
            { // Just execute the Assembler/ROM
                TestRun();
            }
            else
            { // Start background rendering loop
                _cts = new CancellationTokenSource();
                Task.Run(() => Run(_cts.Token));
            }
        }

        public void OnKeyDown(Key key)
        {
            _c64.InputSystem.KeyDown(key);
            //_c64.Cia1.KeyDown(key);
        }

        public void OnKeyUp(Key key)
        {
            _c64.InputSystem.KeyUp(key);
            //_c64.Cia1.KeyUp(key);
        }

        List<string> lines = new List<string>();
        private void TestRun()
        {
            string ass = "";
            var running = true;
            while (running)
            {
                int cycles = _c64.Step();
                if (cycles > 0)
                {
                    ass = $"{_c64.Cpu.Instructions + 1:D8}:" + _c64.Cpu.FormatLastExecutedInstruction();// + $" CYC:{cyclesThisFrame}";
                    lines.Add(ass);
                }

                if (_c64.Cpu.Instructions > 1000000)
                {
                    running = false;
                }
            }

            File.WriteAllLines("c64New.log", lines);
        }

        private async void Run(CancellationToken token)
        {
            string ass = "";

            while (!token.IsCancellationRequested)
            {
                var frameStart = System.Diagnostics.Stopwatch.GetTimestamp();
                int cyclesThisFrame = 0;

                _frames++;
                var now = DateTime.Now;
                var delta = (now - _lastFpsUpdate).TotalSeconds;

                if (delta >= 1.0)
                {
                    _fps = _frames / delta;
                    _frames = 0;
                    _lastFpsUpdate = now;
                }

                while (cyclesThisFrame < CYCLES_PER_FRAME)
                {
                    int cpuCycles;
                    lock (_machineLock)
                        cpuCycles = _c64.Step();
                    //ass = $"{i++}: " + _c64.Cpu.FormatLastExecutedInstruction() + $" CYC:{cyclesThisFrame}";
                    //lines.Add(ass);
                    cyclesThisFrame += cpuCycles;
                }

                //File.WriteAllLines("my64.log", lines);

                Render(_c64.Vic.GetFrame());

                _c64.Cia1.TimeOfDay();
                _c64.Cia1.TimeOfDay();

                //Assembler += ass;
                ass = "";
                //DrawFps(Frame, _fps);

                MainTitle = $"Jolly C64 Emulator - FPS: {_fps:F1}";

                double elapsedMs = System.Diagnostics.Stopwatch.GetElapsedTime(frameStart).TotalMilliseconds;
                int delayMs = Math.Max(0, (int)(20.0 - elapsedMs));
                if (delayMs > 0)
                    await Task.Delay(delayMs, token);
            }
        }

        private int GetRasterCompare()
        {
            int low = _c64.Bus.ReadVicRegister(0x12);
            int high = (_c64.Bus.ReadVicRegister(0x11) & 0x80) >> 7;

            return (high << 8) | low;
        }

        private bool IsRasterIrqEnabled()
        {
            return (_c64.Bus.ReadVicRegister(0x1A) & 0x01) != 0;
        }

        private void TriggerRasterIRQ()
        {
            _c64.Bus.WriteVicRegister(0x19, (byte)(_c64.Bus.ReadVicRegister(0x19) | 0x01));
        }

        private unsafe void Render(uint[] frame)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                Screen.Lock();

                byte* ptr = (byte*)Screen.BackBuffer;
                int stride = Screen.BackBufferStride;

                fixed (uint* src = frame)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        Buffer.MemoryCopy(src + y * Width, ptr + y * stride, stride, Width * 4); // 4 bytes per pixel
                    }
                }

                Screen.AddDirtyRect(new Int32Rect(0, 0, Width, Height));
                Screen.Unlock();
            });
        }

        void DrawFps(uint[] frame, double fps)
        {
            string text = $"FPS:{(int)fps}";
            int startX = 5, startY = 5;

            for (int i = 0; i < text.Length; i++)
            {
                byte charCode = (byte)text[i];
                for (int py = 0; py < 8; py++)
                {
                    byte row = _c64.Bus.CharRom[charCode * 8 + py];
                    for (int px = 0; px < 8; px++)
                    {
                        bool set = (row & (1 << (7 - px))) != 0;
                        int index = (startY + py) * 418 + (startX + i * 8 + px);
                        frame[index] = set ? 0xFFFFFFFF : 0xFF000000; // white/black
                    }
                }
            }
        }

        public async Task RequestShutdownAsync()
        {
            if (_isShuttingDown)
                return;

            _isShuttingDown = true;

            await StopEmulationAsync();

            IsShutdownComplete = true;
        }

        public async Task StopEmulationAsync()
        {
            _cts?.Cancel();

            // Wait for render to finish
            await _renderSemaphore.WaitAsync();
            _renderSemaphore.Release();
        }
    }
}

