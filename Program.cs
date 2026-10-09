using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;

class Program
{
    static async Task Main()
    {
        //Console.ReadLine();
        while (string.IsNullOrEmpty(ConsoleIO.ApiKey))
        {
            ConsoleIO.RetrieveAPIKey();
        }
        Console.Clear();
        while (true)
        {
            await ConsoleIO.AwaitInput();
        }
    }
}

static class ConsoleIO
{
    public static string ApiKey;
    public static void RetrieveAPIKey()
    {
        if (File.Exists(Path.Combine(AppContext.BaseDirectory, "apikey.txt")) && (ApiKey = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "apikey.txt"))) != string.Empty)
        {
            Console.WriteLine("Api key successfully loaded from apikey.txt");
            Thread.Sleep(700);
            return;
        }
        Console.Write("Please enter your private api key\n\nInput: ");

        string? input = Console.ReadLine();
        if (input != null) ApiKey = input;
        ApiKey = ApiKey.Trim();

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            Console.WriteLine("Invalid API key!\n\n");
        }
        else if (ApiKey.ToLower() == "quit")
        {
            Environment.Exit(0);
        }
    }

    public static async Task AwaitInput()
    {
        //Console.Clear();
        Console.Write("Type 'help' to see list of commands\n\nInput: ");
        string[]? inputs = Console.ReadLine()?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (inputs == null || inputs?.Length == 0)
        {
            Console.WriteLine("Invalid Input\n\n");
            return;
        }
        string? input = inputs[0];
        string? keyValue = null;

        if (inputs.Length > 1)
        {
            keyValue = inputs[1];
        }

        //if (keyInfo.Key == ConsoleKey.Q)
        //{
        //    Environment.Exit(0);
        //    return;
        //}

        input = input.ToLower();
        input = input.Trim();
        if (input == "key" && !string.IsNullOrEmpty(keyValue))
        {
            ApiKey = keyValue;
            Console.WriteLine("Api key changed\n");
        }

        else if (input == "help")
        {
            Console.WriteLine("'key' + value = change api key\nstart = start server\nstop = stop server\ninfo = fetch stream info\nclear = clear console\nquit = quit\n\n");
        }
        else if (input == "quit") Environment.Exit(0);
        else if (input == "info")
        {
            await APIRequests.FetchStreamInfo(ApiKey);
            Console.WriteLine();
        }
        else if (input == "start")
        {
            await APIRequests.StartStop(true, ApiKey);
            Console.WriteLine();
        }
        else if (input == "stop")
        {
            await APIRequests.StartStop(false, ApiKey);
            Console.WriteLine();
        }
        else if (input == "clear")
        {
            Console.Clear();
        }
        else
        {
            Console.WriteLine("Invalid Input\n\n");
        }

    }
}

static class APIRequests
{
    public delegate Task ApiRequestExec();
    static HttpClient http = new HttpClient();
    public static async Task ApiRequestGeneric(ApiRequestExec exec)
    {
        try
        {
            await exec();
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"\nRequest error: {e.Message}");
        }
    }

    public static async Task FetchStreamInfo(string apiKey)
    {
        string url = $"https://hub.cloud.caster.fm/private/accountInfo?token={Uri.EscapeDataString(apiKey)}";
        async Task Exec()
        {
            using var resp = await http.GetAsync(url);
            if (resp.IsSuccessStatusCode && resp.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string content = await resp.Content.ReadAsStringAsync();
                content = content.Replace(",", ",\n\n");
                string outputPath = Path.Combine(AppContext.BaseDirectory, "streaminfo.txt");
                File.WriteAllText(outputPath, content);
                Console.WriteLine("Stream info written to streaminfo.txt");
            }
            else
            {
                Console.WriteLine($"Request failed: {(int)resp.StatusCode} {resp.ReasonPhrase}");
            }
        }

        ApiRequestExec exec = Exec;
        await ApiRequestGeneric(exec);
    }

    public static async Task StartStop(bool start, string? apiKey)
    {
        string url = $"https://hub.cloud.caster.fm/private/server/{Uri.EscapeDataString(start ? "start" : "stop")}?token={Uri.EscapeDataString(apiKey)}";
        async Task Exec()
        {
            using var resp = await http.PostAsync(url, null);
            string started = start ? "started!" : "stopped!";
            if (resp.IsSuccessStatusCode && resp.StatusCode == System.Net.HttpStatusCode.OK)
            {
                Console.WriteLine("Server " + started);
            }
            else if (resp.StatusCode == System.Net.HttpStatusCode.NotAcceptable)
            {
                Console.WriteLine("Server already " + started);
            }
            else
            {
                Console.WriteLine("An error occurred... Status Code: " + resp.StatusCode.ToString());
            }
        }

        ApiRequestExec exec = Exec;
        await ApiRequestGeneric(exec);
    }
}