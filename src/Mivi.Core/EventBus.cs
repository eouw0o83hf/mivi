namespace Mivi.Core
{
    public interface IEventBus
    {
        void Publish(object _event);
        void RegisterConsumer(IEventConsumer consumer);
    }

    public class EventBus : IEventBus
    {
        private delegate void EventConsumerHandler(object _event);

        private event EventConsumerHandler? _published;

        public void Publish(object _event)
            => _published?.Invoke(_event);

        public void RegisterConsumer(IEventConsumer consumer)
            => _published += consumer.Consume;
    }
}
