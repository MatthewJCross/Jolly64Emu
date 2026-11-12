using NAudio.Wave;

namespace Jolly64Emu.Core.Audio
{
    public sealed class SidAudioOutput : IDisposable
    {
        private readonly BufferedWaveProvider _buffer;
        private readonly WaveOutEvent _output;
        private readonly byte[] _sample = new byte[2];

        public SidAudioOutput()
        {
            _buffer = new BufferedWaveProvider(new WaveFormat(44100, 16, 1))
            {
                BufferDuration = TimeSpan.FromMilliseconds(250),
                DiscardOnBufferOverflow = true,
                ReadFully = true
            };
            _output = new WaveOutEvent { DesiredLatency = 100 };
            _output.Init(_buffer);
            _output.Play();
        }

        public void Submit(short sample)
        {
            _sample[0] = (byte)sample;
            _sample[1] = (byte)(sample >> 8);
            _buffer.AddSamples(_sample, 0, 2);
        }

        public void Dispose()
        {
            _output.Stop();
            _output.Dispose();
        }
    }
}
