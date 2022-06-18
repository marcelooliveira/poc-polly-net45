using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using Polly.Wrap;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Console
{
    public class HttpClientExample
    {
        private const int _exceptionsAllowedBeforeBreaking = 5;
        private static int _maxRetryAttempts = 5;
        Func<int, TimeSpan> _pauseBetweenFailures = (i) => TimeSpan.FromSeconds(Math.Pow(2, i));
        TimeSpan durationOfBreak = TimeSpan.FromSeconds(10);

        public async Task ExecuteAsync()
        {
            var httpClient = new HttpClient();

            var policy = GetPolicy();

            var response = await policy.ExecuteAsync(ctx =>
                httpClient.GetAsync("http://localhost:5102/WeatherForecast"), new Dictionary<string, object>());

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var array = JsonConvert.DeserializeObject<WeatherForecast[]>(json);
                PrintWeatherForecast(json);
            }
        }

        private AsyncPolicyWrap GetPolicy()
        {
            var logFunction = new Action<string>((log) =>
            {
                System.Console.WriteLine(log);
            });

            var retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(_maxRetryAttempts, _pauseBetweenFailures, (exception, timeSpan, retryCount, context) =>
                {
                    logFunction($"The request failed. exception={exception}. Waiting {timeSpan} seconds before retry. Number attempt {retryCount}.");
                });

            var circuitBreakerPolicy = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(_exceptionsAllowedBeforeBreaking, durationOfBreak, onBreak: (exception, timespan, context) =>
                {
                    logFunction($"Circuit went into a fault state. exception: {exception}");
                },
                onReset: (context) =>
                {
                    logFunction($"Circuit left the fault state.");
                });

            return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
        }

        private static void PrintWeatherForecast(string json)
        {
            var array = JsonConvert.DeserializeObject<WeatherForecast[]>(json);

            foreach (var wf in array)
            {
                System.Console.WriteLine($"Date: {wf.Date}");
                System.Console.WriteLine($"TemperatureC: {wf.TemperatureC}");
                System.Console.WriteLine($"TemperatureF: {wf.TemperatureF}");
                System.Console.WriteLine($"Summary: {wf.Summary}");
            }
        }
    }
}
