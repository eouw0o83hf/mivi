using System;

namespace Mivi.Core.Consumers
{
    public class NoteVolumeConsumer : IEventConsumer
    {
        // Automatically initialized to zeroes/false
        private readonly int[] _actualKeyVelocities = new int[128];
        private readonly bool[] _actualKeyFreshBits = new bool[128];

        private bool _softOn = false;
        private bool _sustainOn = false;

        private readonly SharedState _state;

        public NoteVolumeConsumer(SharedState state)
            => _state = state;

        public void Consume(object _event)
        {
            switch (_event)
            {
                case KeyPressed pressed:
                    var velocity = pressed.Velocity;
                    if (_softOn)
                    {
                        velocity = (int)Math.Ceiling(velocity * 2f / 3f);
                    }

                    _actualKeyVelocities[pressed.KeyIndex] = velocity;
                    _actualKeyFreshBits[pressed.KeyIndex] = true;
                    // key was pressed, so instigate an immediate sound
                    _state.NoteVelocities[pressed.KeyIndex] = velocity;
                    break;

                case KeyReleased released:
                    _actualKeyVelocities[released.KeyIndex] = 0;
                    _actualKeyFreshBits[released.KeyIndex] = true;
                    // key was released, which means attenuation
                    // needs to occur via clock events
                    break;

                case SustainPedalPressed _:
                    _sustainOn = true;
                    break;

                case SustainPedalReleased _:
                    _sustainOn = false;
                    break;

                case SoftPedalPressed _:
                    _softOn = true;
                    break;

                case SoftPedalReleased _:
                    _softOn = false;
                    break;

                case ClockTicked _:
                    // Attenuate all notes
                    for (var i = 0; i < 128; ++i)
                    {
                        if (_actualKeyFreshBits[i])
                        {
                            // Velocity just arrived this cycle. Nothing
                            // to do until attenuation starts next cycle
                            _actualKeyFreshBits[i] = false;
                            continue;
                        }

                        float attenuationFactor;
                        if (_actualKeyVelocities[i] > 0.01f)
                        {
                            // The key is still being held down
                            if (_sustainOn)
                            {
                                // All of the resonance
                                attenuationFactor = 0.998f;
                            }
                            else
                            {
                                // Just pretty good resonance
                                attenuationFactor = 0.9979f;
                            }
                        }
                        else if (_sustainOn)
                        {
                            // Key was lifted but sustain pedal is on,
                            // so slow attenuation
                            attenuationFactor = 0.9975f;
                        }
                        else
                        {
                            // Key was lifted without sustain pedal.
                            // Rapid attenuation
                            attenuationFactor = 0.94f;
                        }

                        _state.NoteVelocities[i] *= attenuationFactor;
                    }
                    break;
            }
        }
    }
}