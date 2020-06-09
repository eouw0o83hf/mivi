using System;

namespace Mivi.Console
{
    public class KeyColorProvider
    {
        private int tickOffset = 0;

        private const int periodTicks = 2000;

        public void Tick()
        {
            ++tickOffset;

            if (tickOffset >= periodTicks)
            {
                tickOffset = 0;
            }
        }

        public float[] GetColor(int keyIndex)
        {
            var adjustedKeyIndex = keyIndex * 5;

            var redPeriod = adjustedKeyIndex + tickOffset;
            var greenPeriod = adjustedKeyIndex + tickOffset + (periodTicks / 3);
            var bluePeriod = adjustedKeyIndex + tickOffset + (2 * periodTicks / 3);

            var reduction = 2f * Math.PI / (float)periodTicks;

            var redRadians = redPeriod * reduction;
            var greenRadians = greenPeriod * reduction;
            var blueRadians = bluePeriod * reduction;

            var red = (float)(Math.Cos(redRadians) + 1f) / 2f;
            var green = (float)(Math.Cos(greenRadians) + 1f) / 2f;
            var blue = (float)(Math.Cos(blueRadians) + 1f) / 2f;

            return new[] { red, green, blue };
        }
    }
}
