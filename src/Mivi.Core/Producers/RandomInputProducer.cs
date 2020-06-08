using System;
using System.Threading.Tasks;
using System.Timers;

namespace Mivi.Core.Producers
{
    // Produces random input for testing
    // without a MIDI device present
    public class RandomInputProducer
    {
        private const double IntervalMillis = 200.0;
        private readonly Random _random = new Random();

        private readonly IEventBus _bus;

        public RandomInputProducer(IEventBus bus)
        {
            _bus = bus;

            var timer = new Timer(IntervalMillis)
            {
                Enabled = true
            };

            timer.Elapsed += ElapsedHandler;
        }

        private void ElapsedHandler(object source, ElapsedEventArgs e)
        {
            var key = _random.Next(MidiNote.LowestPianoIndex, MidiNote.HighestPianoIndex + 1);
            var velocity = _random.Next(1, 128);
            var length = _random.Next(50, 1000);

            _bus.Publish(new KeyPressed(key, velocity));

            Task.Run(async () =>
            {
                await Task.Delay(length);
                _bus.Publish(new KeyReleased(key));
            });
        }
    }
}
