using JollyC64Emu.C64.CPU;

namespace Jolly64Emu.Core.SID6581
{
    // First-pass, three-voice SID synthesizer. All chip state is owned by the emulation thread.
    public sealed class SID
    {
        public const int SampleRate = 44100;
        private const double ClockRate = 985248.0; // PAL
        private static readonly double[] AttackSeconds = [0.002, 0.008, 0.016, 0.024, 0.038, 0.056, 0.068, 0.080, 0.100, 0.250, 0.500, 0.800, 1.0, 3.0, 5.0, 8.0];
        private static readonly double[] DecaySeconds = [0.006, 0.024, 0.048, 0.072, 0.114, 0.168, 0.204, 0.240, 0.300, 0.750, 1.5, 2.4, 3.0, 9.0, 15.0, 24.0];
        private readonly byte[] _registers = new byte[32];
        private readonly Voice[] _voices = [new(), new(), new()];
        private double _samplePhase;
        private uint _noise = 0x7FFFF8;
        public uint _sampleclock;
        public Action<short>? SampleReady { get; set; }

        public SID(Cpu6510 cpu) { }

        public void Reset()
        {
            Array.Clear(_registers);
            foreach (Voice voice in _voices) voice.Reset();
            _samplePhase = 0;
            _noise = 0x7FFFF8;
            _sampleclock = 0;
        }

        public byte Read(ushort address)
        {
            int register = address & 0x1F;
            return register switch
            {
                0x19 or 0x1A => 0xFF, // Unconnected paddle inputs
                0x1B => (byte)(_voices[2].Phase * 256),
                0x1C => (byte)(_voices[2].Envelope * 255),
                _ => 0 // Write-only registers: approximate open bus
            };
        }

        public void Write(ushort address, byte value)
        {
            _registers[address & 0x1F] = value;
        }

        // Call once for every emulated PAL clock tick, not once per CPU instruction.
        public void Clock()
        {
            for (int i = 0; i < 3; i++)
            {
                int offset = i * 7;
                Voice voice = _voices[i];
                int frequency = _registers[offset] | (_registers[offset + 1] << 8);
                int pulse = _registers[offset + 2] | ((_registers[offset + 3] & 0x0F) << 8);
                byte control = _registers[offset + 4];
                bool gate = (control & 1) != 0;
                if (gate && !voice.Gate) voice.Stage = EnvelopeStage.Attack;
                if (!gate && voice.Gate) voice.Stage = EnvelopeStage.Release;
                voice.Gate = gate;
                if ((control & 0x08) != 0) voice.Phase = 0;
                else
                {
                    double previous = voice.Phase;
                    voice.Phase = (voice.Phase + frequency / 16777216.0) % 1.0;
                    if (voice.Phase < previous)
                    {
                        uint feedback = ((_noise >> 22) ^ (_noise >> 17)) & 1;
                        _noise = ((_noise << 1) | feedback) & 0x7FFFFF;
                    }
                }
                double sustain = ((_registers[offset + 6] >> 4) & 15) / 15.0;
                double attack = AttackSeconds[_registers[offset + 5] >> 4];
                double decay = DecaySeconds[_registers[offset + 5] & 15];
                double release = DecaySeconds[_registers[offset + 6] & 15];
                switch (voice.Stage)
                {
                    case EnvelopeStage.Attack:
                        voice.Envelope += 1.0 / (attack * ClockRate);
                        if (voice.Envelope >= 1) { voice.Envelope = 1; voice.Stage = EnvelopeStage.Decay; }
                        break;
                    case EnvelopeStage.Decay:
                        voice.Envelope = Math.Max(sustain, voice.Envelope - 1.0 / (decay * ClockRate));
                        if (voice.Envelope <= sustain) voice.Stage = EnvelopeStage.Sustain;
                        break;
                    case EnvelopeStage.Sustain:
                        voice.Envelope = sustain;
                        break;
                    case EnvelopeStage.Release:
                        voice.Envelope = Math.Max(0, voice.Envelope - 1.0 / (release * ClockRate));
                        break;
                }
                voice.PulseWidth = Math.Clamp(pulse / 4096.0, 0.01, 0.99);
                voice.Control = control;
            }

            _samplePhase += SampleRate;
            if (_samplePhase < ClockRate) return;
            _samplePhase -= ClockRate;
            double mixed = 0;
            for (int i = 0; i < 3; i++)
            {
                Voice voice = _voices[i];
                byte wave = (byte)(voice.Control & 0xF0);
                if (wave == 0 || (voice.Control & 0x08) != 0) continue;
                double sample = 0;
                int selected = 0;
                if ((wave & 0x10) != 0) { sample += 1 - 4 * Math.Abs(voice.Phase - 0.5); selected++; }
                if ((wave & 0x20) != 0) { sample += 2 * voice.Phase - 1; selected++; }
                if ((wave & 0x40) != 0) { sample += voice.Phase < voice.PulseWidth ? 1 : -1; selected++; }
                if ((wave & 0x80) != 0) { sample += ((_noise & 0xFFFF) / 32767.5) - 1; selected++; }
                if (selected > 0) mixed += sample / selected * voice.Envelope;
            }
            double volume = (_registers[0x18] & 15) / 15.0;
            mixed = Math.Clamp(mixed / 3.0 * volume * 0.7, -1.0, 1.0);
            SampleReady?.Invoke((short)(mixed * short.MaxValue));
            _sampleclock++;
        }

        public int EmitSample() => 0; // Legacy compatibility; Clock() now produces samples.
        public void UpdateSampleClock() => _sampleclock++;

        private enum EnvelopeStage { Attack, Decay, Sustain, Release }
        private sealed class Voice
        {
            public double Phase, Envelope, PulseWidth;
            public bool Gate;
            public byte Control;
            public EnvelopeStage Stage;
            public void Reset() { Phase = 0; Envelope = 0; PulseWidth = 0.5; Gate = false; Control = 0; Stage = EnvelopeStage.Release; }
        }
    }
}
