namespace Mivi.Core
{
    /// <summary>
    /// Singleton state object for EventBus
    /// Consumers to write to and graphics
    /// handlers to read from.
    ///
    /// Should only have arrays or scalars
    /// of primitives.
    /// </summary>
    public class SharedState
    {
        // Current input state
        public readonly float[] NoteVelocities = new float[128];
        public readonly int[] NoteLengths = new int[128];

        public bool SustainPedalOn = false;

        // Past input state

        // Unordered array of past notes.
        // Each index may or may not contain
        // an entry, as it is used as a
        // rotating buffer
        public readonly PastNote?[] PastNotes = new PastNote?[50];
    }

    public class PastNote
    {
        public bool Active { get; set; }
        public int Index { get; set; }

        public int Velocity { get; set; }
        public int Length { get; set; }

        public int TicksSinceKeyUp { get; set; }
    }
}
