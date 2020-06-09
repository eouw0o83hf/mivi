using System.Linq;
using System.Threading.Tasks;
using Commons.Music.Midi;
using Mivi.Core;
using Mivi.Core.Consumers;
using Mivi.Core.Producers;
using SConsole = System.Console;

namespace Mivi.Console
{
    public class Program
    {
        public static async Task Main(string[] _)
        {
            // Core components
            var state = new SharedState();
            var eventBus = new EventBus();

            // dotnet core does not provide a cross-platform keyboard hook interface,
            // but GLFW does. So, we force GLFW to pull double duty and send us back
            // keyboard events.
            var keyboardEvents = new KeyboardEvents();

            // Event bus producers
            var clockTickProducer = new ClockTickProducer(eventBus);

            // Event bus consumers
            eventBus.RegisterConsumer(new NoteVolumeConsumer(state));
            eventBus.RegisterConsumer(new NoteLengthConsumer(state));

            // Low-level MIDI wire-up
            var manager = MidiAccessManager.Default;
            var midiInput = manager.Inputs.SingleOrDefault();

            // Determine MIDI producer based on physical device presence
            if (midiInput != null)
            {
                SConsole.WriteLine($"Opening input {midiInput.Id}");
                var input = await manager.OpenInputAsync(midiInput.Id);

                var adapter = new MidiBusAdapter(eventBus, input);
            }
            else
            {
                SConsole.WriteLine("No MIDI devices found, using keyboard input");
                var producer = new KeyboardInputProducer(eventBus, keyboardEvents);
            }

            var graphicsApplication = new GraphicsApplication(keyboardEvents, state);
            // Blocks until finished
            graphicsApplication.Launch();
        }
    }
}
