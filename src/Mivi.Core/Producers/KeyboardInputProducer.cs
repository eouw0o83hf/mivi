using System;
using System.Collections.Generic;
using System.Linq;

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

        private static readonly int[] _shiftKeyCodes = new[] { 340, 344 };
        private static readonly int[] _ctrlKeyCodes = new[] { 341, 345 };
        private static readonly int[] _altKeyCodes = new[] { 342, 346 };
        private static readonly int[] _superKeyCodes = new[] { 343, 347 };

        // OpenGL Key ID mappings to QWERT row indeces
        // Interleaved later to map a linear position
        // onto each key
        private static readonly int[][] qwertyRowIndecees = new[]
        {
            new[]
            {
                // tilde is handled specially since it's in its own character column
                49, 50, 51, 52, 53, 54, 55, 56, 57, 48, // numbers
                45, // dash
                61, // equals
            },
            new[]
            {
                81, 87, 69, 82, 84, 89, 85, 73, 79, 80, // letters
                91, 93, // brackets
                92 // backslash
            },
            new[]
            {
                65, 83, 68, 70, 71, 72, 74, 75, 76, // letters
                59, // semicolon
                39 // apostrophe
            },
            new[]
            {
                90, 88, 67, 86, 66, 78, 77, // letters
                44, // comma
                46 // period
            }
        };

        // Maps Key IDs to their index in a qwerty version of a piano keyboard
        private static readonly int[] _qwertyIndeces;
        private static readonly int[] _qwertyIndexToKeyIndex;

        static KeyboardInputProducer()
        {
            // initialize with tilde
            var qwertyIndeces = new List<int> { 96 };
            // scan down each column and add its keys to the index list
            for (var column = 0; column < 13; ++column)
            {
                for (var row = 0; row < qwertyRowIndecees.Length; ++row)
                {
                    if (qwertyRowIndecees[row].Length > column)
                    {
                        qwertyIndeces.Add(qwertyRowIndecees[row][column]);
                    }
                }
            }

            _qwertyIndeces = qwertyIndeces.ToArray();

            _qwertyIndexToKeyIndex = new int[_qwertyIndeces.Length];
            var keyIndexInterval = 88f / _qwertyIndeces.Length;
            for (var i = 0; i < _qwertyIndeces.Length; ++i)
            {
                _qwertyIndexToKeyIndex[i] = (int)Math.Round(
                    (i * keyIndexInterval) + MidiNote.LowestPianoIndex
                );
            }
        }


        private void KeyPressed(int keyCode, KeyboardEventTypes type)
        {
            // Check the repeat-sensitive keys first so that we can
            // bail early on repeat events and not check them later

            // Shift -> increase volume
            if (_shiftKeyCodes.Contains(keyCode))
            {
                if (type == KeyboardEventTypes.Pressed
                    || type == KeyboardEventTypes.Repeated)
                {
                    _velocity = Math.Min(_velocity + 32, 100);
                }
                return;
            }

            // Ctrl -> decrease volume
            if (_ctrlKeyCodes.Contains(keyCode))
            {
                if (type == KeyboardEventTypes.Pressed
                    || type == KeyboardEventTypes.Repeated)
                {
                    _velocity = Math.Max(_velocity - 32, 1);
                }
                return;
            }

            if (type == KeyboardEventTypes.Repeated)
            {
                return;
            }

            var qwertyIndex = Array.IndexOf(_qwertyIndeces, keyCode);

            // character key -> piano note
            if (qwertyIndex >= 0)
            {
                var keyIndex = _qwertyIndexToKeyIndex[qwertyIndex];
                if (type == KeyboardEventTypes.Pressed)
                {
                    _bus.Publish(new KeyPressed(keyIndex, _velocity));
                    return;
                }
                _bus.Publish(new KeyReleased(keyIndex));
                return;
            }

            // Space bar -> sustain pedal
            if (keyCode == 32)
            {
                if (type == KeyboardEventTypes.Pressed)
                {
                    _bus.Publish(new SustainPedalPressed());
                    return;
                }
                _bus.Publish(new SustainPedalReleased());
                return;
            }

            // Alt -> soft pedal
            if (_altKeyCodes.Contains(keyCode))
            {
                if (type == KeyboardEventTypes.Pressed)
                {
                    _bus.Publish(new SoftPedalPressed());
                    return;
                }
                _bus.Publish(new SoftPedalPressed());
                return;
            }

            // Ctrl -> sostenuto pedal
            if (_superKeyCodes.Contains(keyCode))
            {
                if (type == KeyboardEventTypes.Pressed)
                {
                    _bus.Publish(new SostenutoPedalPressed());
                    return;
                }
                _bus.Publish(new SostenutoPedalReleased());
                return;
            }
        }
    }
}
