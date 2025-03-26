using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TicTacToe
{
    class Program
    {
        static async Task Main()
        {
            try
            {
                var client = new HttpClient();
                var response = await client.GetAsync("http://localhost:8080/jugadors");
                var data = await response.Content.ReadAsStringAsync();

                var matches = Regex.Matches(data, @"participant\s+""(.+?)""\s+""(.+?)""");
                Console.WriteLine("Participantes:");
                foreach (Match m in matches)
                    Console.WriteLine($"- {m.Groups[1].Value} ({m.Groups[2].Value})");
            }
            catch
            {
                Console.WriteLine("Error");
            }
        }
    }
}