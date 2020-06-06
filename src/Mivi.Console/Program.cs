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

                state = new MidiState();
                var input = await manager.OpenInputAsync(singleInput.Id);
                input.MessageReceived += (object? sender, MidiReceivedEventArgs args) => state.Consume(args);
            }
            else
            {
                SConsole.WriteLine("No inputs found, using constant state");
                state = new AlwaysOnMidiState();
            }

            TriangleProgram.EntryPoint(state);
        }

        public static async Task Main_Old(string[] args)
        {
            var manager = MidiAccessManager.Default;
            var singleInput = manager.Inputs.Single();

            SConsole.WriteLine($"Opening input {singleInput.Id}");

            var stateManager = new StateManager();
            var input = await manager.OpenInputAsync(singleInput.Id);
            input.MessageReceived += stateManager.Consume;

            var windowMillis = 250;

            for (var i = 0; i < 10_000 / windowMillis; ++i)
            {
                await Task.Delay(windowMillis);
                var stateOutput = stateManager.Tick();
                SConsole.WriteLine(stateOutput);
            }

            await input.CloseAsync();
        }

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
