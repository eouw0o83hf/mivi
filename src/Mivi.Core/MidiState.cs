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
        string Render();
        int[] GetKeyStates();
        IMidiState Clone();
    }

    public class MidiState : IMidiState
    {
        // Automatically initialized to zeroes
        private readonly int[] _keyVelocities;

        public MidiState()
            => _keyVelocities = new int[128];

        public MidiState(int[] keyVelocities)
            => _keyVelocities = keyVelocities;

        public IMidiState Clone()
        {
            var copy = new int[128];
            Array.Copy(_keyVelocities, copy, 128);
            return new MidiState(copy);
        }

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
                        _keyVelocities[noteIndex] = velocity;

                        break;
                    }

                case MidiEvent.NoteOff:
                    {
                        var noteIndex = data[1];
                        _keyVelocities[noteIndex] = 0;
                        break;
                    }
            }
        }

        public int[] GetKeyStates()
            => _keyVelocities.ToArray();

        public string Render()
            => new string(_keyVelocities
                // Convert to 0-9 scale
                .Select(a => Math.Min(a * 10 / 128, 128))
                .Select(a => a == 0 ? ' ' : (char)(a + 48))
                .ToArray()
            );
    }

    public class AlwaysOnMidiState : IMidiState
    {
        private static readonly int[] State = Enumerable
            .Range(0, 128)
            .Select(a => 128)
            .ToArray();

        public IMidiState Clone()
            => this;

        public void Consume(MidiReceivedEventArgs message) { }

        public int[] GetKeyStates()
            => State;

        public string Render()
            => "[fake input]";
    }
}
