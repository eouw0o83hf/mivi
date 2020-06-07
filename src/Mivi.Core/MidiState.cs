using System;
using System.Collections.Generic;
using System.Linq;
using Commons.Music.Midi;

namespace Mivi.Core
{
    /// <summary>
    /// Encapsulates a consumer for MIDI
    /// events, meant to be bracketed to
    /// a short timeframe. At the end of
    /// that timeframe, should be Cloned
    /// into a new one so that history
    /// can be kept
    /// </summary>
    public interface IMidiState
    {
        void Consume(MidiReceivedEventArgs message);
        float[] GetKeyStates();
    }

    // This is essentially a projection or redux
    // reducer. It should work perfectly for the
    // actual MIDI source, and should also be
    // relatively easy to wire up for testing
    public class MidiState : IMidiState
    {
        // Automatically initialized to zeroes/false
        private readonly int[] _actualKeyVelocities = new int[128];
        private readonly bool[] _actualKeyFreshBits = new bool[128];

        private bool _sustainOn = false;

        private readonly float[] _outputVelocities = new float[128];

        public void Consume(MidiReceivedEventArgs message)
        {
            var data = message.Data;
            if (!data.Any())
            {
                return;
            }

            switch (data[0])
            {
                case MidiEvent.NoteOn:
                    {
                        var noteIndex = data[1];
                        var velocity = data[2];
                        // var note = MidiNote.Notes[noteIndex];
                        // For key up, sometimes NoteOff is not
                        // sent, but instead NoteOn with velocity = 0
                        _actualKeyVelocities[noteIndex] = velocity;
                        _actualKeyFreshBits[noteIndex] = true;

                        // 0->something means a key was pressed,
                        // which means an immediate sound
                        if (velocity > 0)
                        {
                            _outputVelocities[noteIndex] = velocity;
                        }
                        // something-> means a key was released,
                        // which means attenuation needs to occur
                        // via clock events
                        break;
                    }

                case MidiEvent.NoteOff:
                    {
                        // This is equivalent to `NoteOn` with
                        // velocity of 0
                        var noteIndex = data[1];
                        _actualKeyVelocities[noteIndex] = 0;
                        _actualKeyFreshBits[noteIndex] = true;
                        break;
                    }

                // sustain pedal
                case MidiEvent.CC:
                    {
                        // TODO figure out what data CC sends
                        _sustainOn = !_sustainOn;
                        break;
                    }

                case MidiEvent.MidiClock:
                    {
                        for (var i = 0; i < 128; ++i)
                        {
                            if (_actualKeyFreshBits[i])
                            {
                                // Velocity just arrived this cycle. Nothing
                                // to do until attentuation starts next cycle
                                _actualKeyFreshBits[i] = false;
                                continue;
                            }

                            float attenuationFactor;
                            if (_actualKeyVelocities[i] > 0.01f)
                            {
                                // The key is still being held down
                                if (_sustainOn)
                                {
                                    // All of the resonance
                                    attenuationFactor = 0.998f;
                                }
                                else
                                {
                                    // Just pretty good resonance
                                    attenuationFactor = 0.9979f;
                                }
                            }
                            else if (_sustainOn)
                            {
                                // Key was lifted but sustain pedal is on,
                                // so slow attenuation
                                attenuationFactor = 0.9975f;
                            }
                            else
                            {
                                // Key was lifted without sustain pedal.
                                // Rapid attenuation
                                attenuationFactor = 0.94f;
                            }

                            _outputVelocities[i] *= attenuationFactor;
                        }
                        break;
                    }
            }
        }

        public float[] GetKeyStates()
            => _outputVelocities.ToArray();
    }

    public class AlwaysOnMidiState : StaticMidiStateBase
    {
        protected override IEnumerable<float> stateGenerator => Enumerable
            .Range(0, 128)
            .Select(a => 128f);
    }

    public class CrescendoMidiState : StaticMidiStateBase
    {
        protected override IEnumerable<float> stateGenerator => Enumerable.Range(0, 128).Select(a => (float)a);
    }

    public abstract class StaticMidiStateBase : IMidiState
    {
        protected abstract IEnumerable<float> stateGenerator { get; }

        public IMidiState Clone()
            => this;

        public void Consume(MidiReceivedEventArgs message) { }

        public float[] GetKeyStates()
            => stateGenerator.ToArray();
    }
}
