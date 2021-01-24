using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace GpuFinderOnCurryPcWorld
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();
        private static Dictionary<string, DateTime> urlsOpened = new Dictionary<string, DateTime>();
        static void Main()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                .WriteTo.File("./logs.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            while (true)
            {
                ExecuteSearch();
                Thread.Sleep(1000);
            }
        }

        public static async Task ExecuteSearch()
        {
            var urls = File.ReadAllLines("./Urls.txt").Where(x => !String.IsNullOrWhiteSpace(x));
            var calls = new List<Task<(bool, string, string)>>();
            foreach (var url in urls)
            {
                calls.Add(IsInStock(url));
            }
            var completedChecks = await Task.WhenAll(calls);

            foreach (var check in completedChecks)
            {
                Log.Information("{0}", check.Item1 ? "Available " : "Unavailable ");
                Log.Information(check.Item2.Substring(check.Item2.IndexOf("<title>") + 7, 50));
                if (check.Item1)
                    Log.Information(check.Item3);
            }

            foreach (var successCase in completedChecks.Where(x => x.Item1))
            {
                using (var soundPlayer = new SoundPlayer(@"c:\Windows\Media\Alarm01.wav"))
                {
                    soundPlayer.Play();
                }
                if (urlsOpened.TryGetValue(successCase.Item3, out var lastTimeCalled))
                {
                    if (lastTimeCalled > DateTime.Now.AddMinutes(-5)) continue;
                }
                Log.Information("Opening link - {0}", successCase.Item3);
                var psi = new ProcessStartInfo
                {
                    FileName = successCase.Item3,
                    UseShellExecute = true
                };
                Process.Start(psi);
                urlsOpened[successCase.Item3] = DateTime.Now;
            }

            return;
        }

        public static async Task<(bool, string, string)> IsInStock(string url)
        {
            var responseString = await client.GetStringAsync(url);
            return (!responseString.Contains("Sorry this item is out of stock"), responseString, url);
        }
    }
}
