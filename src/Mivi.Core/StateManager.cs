using System.Collections.Generic;
using Commons.Music.Midi;

namespace Mivi.Core
{
    /// <summary>
    /// Routes from the MIDI stream to
    /// the current MIDI state, and provides
    /// access to current and past MIDI
    /// state
    /// /// </summary>
    public interface IStateManager
    {
        // Clock input from temporal manager.
        // Increments state and returns the output
        // representation
        string Tick();
        // Routes the inbound message to the
        // current MIDI state
        void Consume(object? sender, MidiReceivedEventArgs args);
    }

    public class StateManager : IStateManager
    {
        private IMidiState _current;
        public Queue<IMidiState> _states = new Queue<IMidiState>(10);

        public StateManager()
            => _current = new MidiState();

        public void Consume(object? sender, MidiReceivedEventArgs args)
            => _current.Consume(args);

        public string Tick()
        {
            var previous = _current;
            _current = _current.Clone();
            _states.Enqueue(previous);

            return previous.Render();
        }
    }
}
