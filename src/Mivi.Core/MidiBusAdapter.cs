using System;
using System.Linq;
using Commons.Music.Midi;
using Mivi.Core.MidiEvents;

namespace Mivi.Core
{
    /// <summary>
    /// Receives input from a MIDI bus
    /// and transforms it to internal
    /// events for EventBus consumption
    /// </summary>
    /// <remarks>
    /// Nothing outside of the composition
    /// root depends on this and it has no
    /// external API, so it has no interface
    /// </remarks>
    public class MidiBusAdapter : IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly IMidiInput _midiBus;

        public MidiBusAdapter(IEventBus eventBus, IMidiInput midiBus)
        {
            _eventBus = eventBus;
            _midiBus = midiBus;

            _midiBus.MessageReceived += ConsumeMidiEvent;
        }

        public void Dispose()
            => _midiBus.Dispose();

        private void ConsumeMidiEvent(object? sender, MidiReceivedEventArgs message)
        {
            var converted = ConvertToMidiEvent(message);
            if (converted != null)
            {
                Console.WriteLine($"Published {converted.GetType().Name}");
                _eventBus.Publish(converted);
            }
        }

        private object? ConvertToMidiEvent(MidiReceivedEventArgs message)
        {
            var data = message.Data;
            if (!data.Any())
            {
                return null;
            }

            switch (data[0])
            {
                case MidiEvent.NoteOn:
                    {
                        var keyIndex = data[1];
                        var velocity = data[2];

                        // For key up, sometimes NoteOff is not
                        // sent, but instead NoteOn with velocity = 0
                        if (velocity == 0)
                        {
                            return new KeyReleased(keyIndex);
                        }
                        return new KeyPressed(keyIndex, velocity);
                    }

                case MidiEvent.NoteOff:
                    {
                        var keyIndex = data[1];
                        return new KeyReleased(keyIndex);
                    }

                // sustain pedal
                case MidiEvent.CC:
                    {
                        var pedalId = data[1];
                        var position = data[2];

                        // https://nickfever.com/Music/midi-cc-list
                        var pedalString = pedalId switch
                        {
                            64 => "damper / sustain",
                            65 => "portamento",
                            66 => "sostenuto",
                            67 => "soft pedal",
                            _ => $"other [{pedalId}]"
                        };

                        Console.WriteLine($"Pedal: [{pedalString}] is at [{position}]");

                        break;
                    }
            }

            // No other events, return null
            return null;
        }
    }
}
