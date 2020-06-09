using System;

namespace Mivi.Core.Producers
{
    public class KeyboardInputProducer
    {
        private readonly IEventBus _bus;

        public KeyboardInputProducer(IEventBus bus, KeyboardEvents keyboard)
        {
            _bus = bus;
            keyboard.OnKeyChange += KeyPressed;
        }

        private int _velocity = 64;

        private void KeyPressed(int keyCode, bool pressed)
        {
            // Space bar
            if (keyCode == 32)
            {
                if (pressed)
                {
                    _bus.Publish(new SustainPedalPressed());
                }
                else
                {
                    _bus.Publish(new SustainPedalReleased());
                }
            }
            if (keyCode == 48)
            {
                // Alpha 0
                _velocity = 95;
            }
            else if (keyCode >= 49 && keyCode <= 57)
            {
                // Alpha #
                _velocity = (keyCode - 48) * 10 - 5;
            }
            // Other single-character input keys
            else if (keyCode >= 33 && keyCode <= 96)
            {
                if (pressed)
                {
                    _bus.Publish(new KeyPressed(keyCode - 15, _velocity));
                }
                else
                {
                    _bus.Publish(new KeyReleased(keyCode - 15));
                }
            }
        }
    }
}
