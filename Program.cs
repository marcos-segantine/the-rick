using System.Diagnostics;

class Program
{
    static void Main()
    {
        Console.Clear();

        string audioUid = GetRandomUid() + ".wav";

        GetAudio(audioUid);
    }

    public static void GetAudio(string fileName)
    {
        using (Process process = new Process())
        {
            process.StartInfo = new ProcessStartInfo()
            {
                FileName = "/usr/bin/arecord",
                Arguments = "-D plughw:0,0 -f cd -t wav -d 10 /home/marcos/projects/the-rick/audios/" + fileName,
                RedirectStandardOutput = false,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            process.Start();

            Console.WriteLine("\n\nRecording...\n\n");

            process.WaitForExit();
        }
    }

    public static string GetRandomUid()
    {
        Guid guid = Guid.NewGuid();
        return guid.ToString();
    }
}
