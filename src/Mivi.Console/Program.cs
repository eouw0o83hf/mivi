using System;
using System.Linq;
using System.Threading.Tasks;
using Commons.Music.Midi;
using GLFW;
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

        [Obsolete]
        private static void HandleMessage(object? sender, MidiReceivedEventArgs args)
        {
            if (!args.Data.Any())
            {
                return;
            }

            switch (args.Data[0])
            {
                case MidiEvent.NoteOff:
                    SConsole.WriteLine("NoteOff");
                    break;
                case MidiEvent.Meta:
                    SConsole.WriteLine("Meta or Reset");
                    break;
                case MidiEvent.EndSysEx:
                    SConsole.WriteLine("EndSysEx");
                    break;
                case MidiEvent.ActiveSense:
                    // Device-level message, irrelevant
                    break;
                case MidiEvent.MidiStop:
                    SConsole.WriteLine("MidiStop");
                    break;
                case MidiEvent.MidiContinue:
                    SConsole.WriteLine("MidiContinue");
                    break;
                case MidiEvent.MidiStart:
                    SConsole.WriteLine("MidiStart");
                    break;
                case MidiEvent.MidiTick:
                    SConsole.WriteLine("MidiTick");
                    break;
                case MidiEvent.MidiClock:
                    // Eliminate noise
                    // SConsole.WriteLine("MidiClock");
                    break;
                case MidiEvent.TuneRequest:
                    SConsole.WriteLine("TuneRequest");
                    break;
                case MidiEvent.SongPositionPointer:
                    SConsole.WriteLine("SongPositionPointer");
                    break;
                case MidiEvent.MtcQuarterFrame:
                    SConsole.WriteLine("MtcQuarterFrame");
                    break;
                case MidiEvent.SysEx1:
                    SConsole.WriteLine("SysEx1 or SysEx2");
                    break;
                case MidiEvent.Pitch:
                    SConsole.WriteLine("Pitch");
                    break;
                case MidiEvent.CAf:
                    SConsole.WriteLine("CAf");
                    break;
                case MidiEvent.Program:
                    SConsole.WriteLine("Program");
                    break;
                case MidiEvent.CC:
                    SConsole.WriteLine("CC: Pedal");
                    break;
                case MidiEvent.PAf:
                    SConsole.WriteLine("PAf");
                    break;
                case MidiEvent.NoteOn:
                    var noteIndex = args.Data[1];
                    var velocity = args.Data[2];

                    var note = MidiNote.Notes[noteIndex];

                    if (velocity == 0)
                    {
                        SConsole.WriteLine($" / {note.Note}{note.Octave}");
                    }
                    else
                    {
                        SConsole.WriteLine($"   {note.Note}{note.Octave}  {velocity}");
                    }
                    break;
                case MidiEvent.SongSelect:
                    SConsole.WriteLine("SongSelect");
                    break;
                default:
                    SConsole.WriteLine("Unknown: " + args.Data[0]);
                    break;
            }
        }
    }
}
