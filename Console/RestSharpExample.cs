using Newtonsoft.Json;
using Polly;
using RestSharp;
using System;
using System.Net;

namespace Console
{
    public class RestSharpExample
    {
        private static int _maxRetryAttempts = 5;
        private static TimeSpan _pauseBetweenFailures = TimeSpan.FromSeconds(10);

        public void Execute()
        {
            var client = new RestClient("http://localhost:5102");
            var request = new RestRequest("WeatherForecast");
            var response = RestSharpResponseWithPolicy(client, request, (log) => {
                System.Console.WriteLine(log);
            });

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var json = response.Content;

                PrintWeatherForecast(json);
            }
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

        private IRestResponse RestSharpResponseWithPolicy(RestClient restClient, RestRequest restRequest, Action<string> logFunction)
        {
            var retryPolicy = Policy
                .HandleResult<IRestResponse>(x => x.StatusCode != HttpStatusCode.OK)
                .WaitAndRetry(_maxRetryAttempts, x => _pauseBetweenFailures, (iRestResponse, timeSpan, retryCount, context) =>
                {
                    logFunction($"The request failed. HttpStatusCode={iRestResponse.Result.StatusCode}. Waiting {timeSpan} seconds before retry. Number attempt {retryCount}. Uri={iRestResponse.Result.ResponseUri}; RequestResponse={iRestResponse.Result.Content}");
                });

            var circuitBreakerPolicy = Policy
                .HandleResult<IRestResponse>(x => x.StatusCode == HttpStatusCode.ServiceUnavailable)
                .CircuitBreaker(1, TimeSpan.FromSeconds(60), onBreak: (iRestResponse, timespan, context) =>
                {
                    logFunction($"Circuit went into a fault state. Reason: {iRestResponse.Result.Content}");
                },
                onReset: (context) =>
                {
                    logFunction($"Circuit left the fault state.");
                });

            return retryPolicy.Wrap(circuitBreakerPolicy).Execute(() => restClient.Execute(restRequest));
        }
    }
}
