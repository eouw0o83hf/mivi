using System;
using System.Linq;
using System.Threading.Tasks;
using Commons.Music.Midi;
using SConsole = System.Console;

namespace Mivi.Console
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            SConsole.WriteLine("Scanning inputs");

            var manager = MidiAccessManager.Default;

            // foreach (var inputOption in manager.Inputs)
            // {
            //     SConsole.WriteLine($"Id           : {inputOption.Id}");
            //     SConsole.WriteLine($"Manufacturer : {inputOption.Manufacturer}");
            //     SConsole.WriteLine($"Name         : {inputOption.Name}");
            //     SConsole.WriteLine($"Version      : {inputOption.Version}");
            // }

            var singleInput = manager.Inputs.Single();

            SConsole.WriteLine($"Opening input {singleInput.Id}");

            var input = await manager.OpenInputAsync(singleInput.Id);
            input.MessageReceived += HandleMessage;

            SConsole.WriteLine("Waiting 10 second");
            for (var i = 0; i < 10; ++i)
            {
                await Task.Delay(1000);
                SConsole.WriteLine(i + 1);
            }

            await input.CloseAsync();

            // SConsole.WriteLine("Scanning outputs");
            // foreach (var input in manager.Outputs)
            // {
            //     SConsole.WriteLine($"Id           : {input.Id}");
            //     SConsole.WriteLine($"Manufacturer : {input.Manufacturer}");
            //     SConsole.WriteLine($"Name         : {input.Name}");
            //     SConsole.WriteLine($"Version      : {input.Version}");
            // }
        }

        // TODO look up the standard, I'm pretty sure it's:
        //  - Clock signal occurs
        //    - Any number of midi events occur
        //      which describe all notes which were
        //      active during the window
        //  - Next clock signal occurs, closing the frame
        // So I just need to setup a


        private static void HandleMessage(object sender, MidiReceivedEventArgs args)
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
                    // Eliminate noise
                    // SConsole.WriteLine("ActiveSense");
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
                    SConsole.WriteLine("CC");
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
