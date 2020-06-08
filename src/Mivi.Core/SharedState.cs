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
        public readonly float[] NoteVelocities = new float[128];
    }
}
