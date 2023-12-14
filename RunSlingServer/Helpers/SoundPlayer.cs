using System.Runtime.InteropServices;

namespace RunSlingServer.Helpers
{
    public class SoundPlayer
    {
        private static readonly Dictionary<string, double> Notes = new()
        {
            { "DO", 261.63 },
            { "DO#", 277.18 },
            { "RE", 293.66 },
            { "RE#", 311.13 },
            { "MI", 329.63 },
            { "FA", 349.23 },
            { "FA#", 369.99 },
            { "SOL", 392.00 },
            { "SOL#", 415.30 },
            { "LA", 440.00 },
            { "LA#", 466.16 },
            { "SI", 493.88 },
            { "DO+", 523.25 }, // (C5) = First DO in the next octave
        };


        public static void PlayArpeggio()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            var solfeggioNotes = new List<string>
                {
                "DO",
                "MI",
                "SOL",
                "DO+",
                "DO+",
                "SOL",
                "MI",
                "DO"
            };

            const int duration = 50;
            foreach (var note in solfeggioNotes)
            {
                PlayNote(note, duration);
            }
        }

        public static void PlayNote(string note, int duration)
        {

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            var frequency = Notes[note];

            PlayNote(frequency, duration);
        }

        private static void PlayNote(double frequency, int duration)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.Beep((int)frequency, duration);
            }
            Thread.Sleep(100); // Add a small pause between notes for clarity
        }

    }
}
