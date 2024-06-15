using System.Net.Http.Headers;
using System.Diagnostics;
using System.Text;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;
using OpenAI_API;
using OpenAI_API.Models;
using System.Text.Json;

class Program
{
    private static readonly HttpClient client = new HttpClient();
    static async Task Main()
    {
        Console.Clear();

        string audioUid = GetRandomUid();

        GetAudio(audioUid);
        string question = await SendFile("./audios/" + audioUid);

        string responseFromAI = await AskToAI(question);

        await TextToSpeech(responseFromAI, audioUid);

        while (!File.Exists("./responses/" + audioUid + ".mp3"))
        {
            Thread.Sleep(500);
        }

        RunResponse(audioUid);
    }

    public static void GetAudio(string fileName)
    {
        using (Process process = new Process())
        {
            process.StartInfo = new ProcessStartInfo()
            {
                FileName = "/usr/bin/arecord",
                Arguments = "-D plughw:0,0 -f cd -t wav -d 3 /home/marcos/projects/the-rick/audios/" + fileName + ".wav",
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

    private static void RunResponse(string fileName)
    {
        using (Process process = new Process())
        {
            process.StartInfo = new ProcessStartInfo()
            {
                FileName = "/usr/bin/play",
                Arguments = "/home/marcos/projects/the-rick/responses/" + fileName + ".mp3",
                RedirectStandardOutput = false,
                CreateNoWindow = true,
                UseShellExecute = false,
            };

            process.Start();

            process.WaitForExit();
        }
    }

    private static async Task TextToSpeech(string inputText, string fileName)
    {
        var apiKey = GetEnvironmentKeys("OPEN_AI_KEY");
        var model = "tts-1";
        var voice = "shimmer";
        var speechFilePath = Path.Combine(Directory.GetCurrentDirectory(), "responses", fileName + ".mp3");

        var requestBody = new
        {
            model = model,
            voice = voice,
            input = inputText
        };

        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var response = await client.PostAsync(
            "https://api.openai.com/v1/audio/speech",
            new StringContent(JObject.FromObject(requestBody).ToString(), Encoding.UTF8, "application/json")
        );

        if (response.IsSuccessStatusCode)
        {
            var responseBytes = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(speechFilePath, responseBytes);
            Console.WriteLine($"Audio file saved to: {speechFilePath}");
        }
        else
        {
            Console.WriteLine($"Error: {response.StatusCode}");
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine(errorContent);
        }
    }

    private static async Task<string> AskToAI(string question)
    {
        string apiKey = GetEnvironmentKeys("OPEN_AI_KEY");

        OpenAIAPI api = new OpenAIAPI(apiKey);

        var chat = api.Chat.CreateConversation();
        chat.Model = Model.GPT4_Turbo;
        chat.RequestParameters.Temperature = 0;

        chat.AppendSystemMessage("You are a robot whose mission is just to talk to people, pretend you were created by Marcos Segantine. If anyone asks where you are, say you are in the city of Nova Ponte in Minas Gerais, Brazil");
        chat.AppendSystemMessage("pretend your name is Rick");

        chat.AppendUserInput(question);
        string response = await chat.GetResponseFromChatbotAsync();

        return response;
    }

    private static string GetEnvironmentKeys(string EnvironmentKey)
    {
        var builder = new ConfigurationBuilder()
                            .SetBasePath(AppContext.BaseDirectory)
                            .AddJsonFile("appsettings.json")
                            .AddEnvironmentVariables("");

        IConfiguration configuration = builder.Build();


        string key = configuration[EnvironmentKey];

        return key;
    }

    private static async Task<string> SendFile(string audioPath)
    {
        string apiKey = GetEnvironmentKeys("OPEN_AI_KEY");
        string model = "whisper-1";

        using (var client = new HttpClient())
        {
            client.BaseAddress = new Uri("https://api.openai.com/v1/audio/transcriptions");

            using (var request = new HttpRequestMessage(HttpMethod.Post, client.BaseAddress))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                var content = new MultipartFormDataContent();

                var fileContent = new ByteArrayContent(File.ReadAllBytes(audioPath + ".wav"));
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav");
                content.Add(fileContent, "file", Path.GetFileName(audioPath + ".wav"));

                content.Add(new StringContent(model), "model");

                request.Content = content;

                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();

                    var responseContent = await response.Content.ReadAsStringAsync();
                    TextFromAI json = JsonSerializer.Deserialize<TextFromAI>(responseContent);

                    return json.text;
                }
            }
        }
    }

    class TextFromAI
    {
        public string text { get; set; }
    }
}
