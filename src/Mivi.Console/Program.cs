using System.Linq;
using System.Threading.Tasks;
using Commons.Music.Midi;
using Mivi.Core;
using SConsole = System.Console;

namespace Mivi.Console
{
    public class Program
    {
        public static async Task Main(string[] _)
        {
            var manager = MidiAccessManager.Default;

            var singleInput = manager.Inputs.SingleOrDefault();

            IMidiState state;
            if (singleInput != null)
            {
                SConsole.WriteLine($"Opening input {singleInput.Id}");
                var input = await manager.OpenInputAsync(singleInput.Id);

                var eventBus = new EventBus();
                var adapter = new MidiBusAdapter(eventBus, input);
                while (true)
                {
                    await Task.Delay(1000);
                }
                // state = new MidiState();
                // input.MessageReceived += (object? sender, MidiReceivedEventArgs args) => state.Consume(args);
            }
            else
            {
                SConsole.WriteLine("No inputs found, using constant state");
                state = new CrescendoMidiState();
                new Task(async () =>
                {
                    while (true)
                    {
                        await Task.Delay(10);
                        state.Consume(new MidiReceivedEventArgs
                        {
                            Data = new byte[] { MidiEvent.MidiClock }
                        });
                    }
                });
            }

            TriangleProgram.EntryPoint(state);
        }
    }
}
