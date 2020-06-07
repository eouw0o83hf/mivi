namespace Mivi.Core.MidiEvents
{
    public class KeyPressed
    {
        public readonly int KeyIndex;
        public readonly int Velocity;

        public KeyPressed(int keyIndex, int velocity)
        {
            KeyIndex = keyIndex;
            Velocity = velocity;
        }
    }

    public class KeyReleased
    {
        public readonly int KeyIndex;

        public KeyReleased(int keyIndex)
        {
            KeyIndex = keyIndex;
        }
    }

    public class PedalPressed { }
    public class PedalReleased { }

    public class ClockTicked { }
}
