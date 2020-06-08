using System.Timers;

namespace Mivi.Core.Producers
{
    public class ClockTickProducer
    {
        private const double IntervalMillis = 15.0;

        private readonly IEventBus _bus;

        public ClockTickProducer(IEventBus bus)
        {
            _bus = bus;

            var timer = new Timer(IntervalMillis)
            {
                Enabled = true
            };

            timer.Elapsed += ElapsedHandler;
        }

        private void ElapsedHandler(object source, ElapsedEventArgs e)
            => _bus.Publish(new ClockTicked());
    }
}
