using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using OpenAI_API;
using OpenAI_API.Models;

class Program
{
    static async Task Main()
    {
        Console.Clear();

        string audioUid = GetRandomUid() + ".wav";

        // GetAudio(audioUid);
        // SendFile("./audios/" + audioUid).Wait();

        string responseFromAI = await AskToAI("");

        Console.WriteLine(responseFromAI);
    }

    public static void GetAudio(string fileName)
    {
        using (Process process = new Process())
        {
            process.StartInfo = new ProcessStartInfo()
            {
                FileName = "/usr/bin/arecord",
                Arguments = "-D plughw:0,0 -f cd -t wav -d 5 /home/marcos/projects/the-rick/audios/" + fileName,
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

    private static async Task<string> AskToAI(string question)
    {
        string apiKey = GetEnvironmentKeys("OPEN_AI_KEY");

        OpenAIAPI api = new OpenAIAPI(apiKey);

        var chat = api.Chat.CreateConversation();
        chat.Model = Model.GPT4_Turbo;
        chat.RequestParameters.Temperature = 0;

        chat.AppendSystemMessage("You are a robot whose mission is just to talk to people, pretend you were created by Marcos Segantine. If anyone asks where you are, say you are in the city of Nova Ponte in Minas Gerais, Brazil");

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

    private static async Task SendFile(string audioPath)
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

                var fileContent = new ByteArrayContent(File.ReadAllBytes(audioPath));
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav");
                content.Add(fileContent, "file", Path.GetFileName(audioPath));

                content.Add(new StringContent(model), "model");

                request.Content = content;

                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();

                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(responseContent);
                }
            }
        }
    }
}
