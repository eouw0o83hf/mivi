namespace Mivi.Core
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

    public class SustainPedalPressed { }
    public class SustainPedalReleased { }

    public class SostenutoPedalPressed { }
    public class SostenutoPedalReleased { }

    public class SoftPedalPressed { }
    public class SoftPedalReleased { }

    public class ClockTicked { }
}
