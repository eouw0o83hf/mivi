using System;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
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
            // Composition root singletons
            var state = new SharedState();
            var eventBus = new EventBus();

            // Event bus producers
            var clockTickProducer = new ClockTickProducer(eventBus);

            // Event bus consumers
            var consumer = new NoteVolumeConsumer(state);
            eventBus.RegisterConsumer(consumer);

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
                throw new Exception("Yo you need to implement the FIDI keyboard input dawg");
                // SConsole.WriteLine("No inputs found, using constant state");
                // state = new CrescendoMidiState();
                // new Task(async () =>
                // {
                //     while (true)
                //     {
                //         await Task.Delay(10);
                //         state.Consume(new MidiReceivedEventArgs
                //         {
                //             Data = new byte[] { MidiEvent.MidiClock }
                //         });
                //     }
                // });
            }

            TriangleProgram.EntryPoint(state);
        }
    }
}
