
namespace Mivi.Core.Consumers
{
    public class NoteLengthConsumer : IEventConsumer
    {
        private readonly SharedState _state;

        private int _pastNoteBufferIndex = 0;
        public const int PastNoteMaxAgeTicks = 10000;

        // ugly but we need it until the bus has
        // more events published for metadata
        private int[] _velocities = new int[128];

        public NoteLengthConsumer(SharedState state)
            => _state = state;

        public void Consume(object _event)
        {
            switch (_event)
            {
                case KeyPressed pressed:
                    _state.NoteLengths[pressed.KeyIndex] = 1;
                    _velocities[pressed.KeyIndex] = pressed.Velocity;
                    break;

                case KeyReleased released:
                    // cache values before updating lengths
                    var length = _state.NoteLengths[released.KeyIndex];
                    var velocity = _velocities[released.KeyIndex];

                    _state.NoteLengths[released.KeyIndex] = 0;
                    _velocities[released.KeyIndex] = 0;

                    // capture and increment past note pointer
                    var pastNoteIndex = _pastNoteBufferIndex;
                    _pastNoteBufferIndex = (_pastNoteBufferIndex + 1) % _state.PastNotes.Length;

                    // initialize historical record
                    _state.PastNotes[pastNoteIndex] = new PastNote
                    {
                        Index = released.KeyIndex,
                        Velocity = velocity,
                        Length = length,
                        TicksSinceKeyUp = 1
                    };

                    break;

                case ClockTicked _:
                    // Increment current note lengths
                    for (var i = 0; i < _state.NoteLengths.Length; ++i)
                    {
                        if (_state.NoteLengths[i] != 0)
                        {
                            _state.NoteLengths[i]++;
                        }
                    }

                    // Increment time since past note lengths where present
                    for (var i = 0; i < _state.PastNotes.Length; ++i)
                    {
                        var pastNote = _state.PastNotes[i];
                        if (pastNote != null)
                        {
                            pastNote.TicksSinceKeyUp++;

                            if (pastNote.TicksSinceKeyUp > PastNoteMaxAgeTicks)
                            {
                                _state.PastNotes[i] = null;
                            }
                        }
                    }
                    break;
            }
        }
    }
}
