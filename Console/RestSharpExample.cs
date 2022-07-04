using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Console
{
    public class RestSharpExample
    {
        public async Task ExecuteAsync()
        {
            List<ExemploResilience> exemplos = new List<ExemploResilience>
            {
                new ExemploResilience(
                    title: "Sem resiliência + execução rápida",
                    comment: "Execução normal, sem resiliência. A resposta é rápida, como num request HTTP normal.",
                    apiStatusCode: 200,
                    apiDelaySecs: 0,
                    exampleFunc: () =>
                                ILang.Util.Resilience.RestSharpResilience
                                    .Build()
                ),
                new ExemploResilience(
                    title: "Sem resiliência + HTTP 500",
                    comment: "Execução normal, sem resiliência. Mas o servidor retorna HTTP 500 Internal Server Error.",
                    apiStatusCode: 500,
                    apiDelaySecs: 0,
                    exampleFunc: () =>
                                ILang.Util.Resilience.RestSharpResilience
                                    .Build()
                ),
                new ExemploResilience(
                    title: "WithWaitAndRetry + Log + HTTP 500",
                    comment: "Espera e Retentativa, com 3 tentativas (default)",
                    apiStatusCode: 500,
                    apiDelaySecs: 0,
                    exampleFunc: () =>
                                ILang.Util.Resilience.RestSharpResilience
                                    .Build()
                                    .WithWaitAndRetry()
                                    .WithLog(log => System.Console.WriteLine(log))
                ),
                new ExemploResilience(
                    title: "WithWaitAndRetry + HTTP 500",
                    comment: "Espera e Retentativa, com 2 tentativas",
                    apiStatusCode: 500,
                    apiDelaySecs: 0,
                    exampleFunc: () =>
                                ILang.Util.Resilience.RestSharpResilience
                                    .Build()
                                    .WithWaitAndRetry(2)
                                    .WithLog(log => System.Console.WriteLine(log))
                ),
                new ExemploResilience(
                    title: "WithWaitAndRetry + HTTP 500",
                    comment: "Espera e Retentativa, com 3 tentativas, com sucesso na última",
                    apiStatusCode: 500,
                    apiDelaySecs: 0,
                    failsBeforeSuccess: 2,
                    exampleFunc: () =>
                                ILang.Util.Resilience.RestSharpResilience
                                    .Build()
                                    .WithWaitAndRetry(3)
                                    .WithLog(log => System.Console.WriteLine(log))
                ),
                new ExemploResilience(
                    title: "Fallback + HTTP 500",
                    comment: "Servidor gera HTTP 500, mas o Polly gera resposta fallback custom",
                    apiStatusCode: 500,
                    apiDelaySecs: 0,
                    exampleFunc: () =>
                                ILang.Util.Resilience.RestSharpResilience
                                    .Build()
                                    .WithFallback(new RestResponse { StatusCode = HttpStatusCode.OK, Content = "Esta é uma resposta de fallback!" })
                ),
                new ExemploResilience(
                    title: "WaitAndRetry + CircuitBreaker + Log + HTTP 500",
                    comment: "Servidor gera HTTP 500, com 10 tentativas, mas o circuit breaker \r\nimpõe uma penalidade de 30 segundos após 2 tentativas falhas",
                    apiStatusCode: 500,
                    apiDelaySecs: 0,
                    exampleFunc: () =>
                                ILang.Util.Resilience.RestSharpResilience
                                    .Build()
                                    .WithWaitAndRetry(10)
                                    .WithCircuitBreaker()
                                    .WithLog(log => System.Console.WriteLine(log))
                ),
                new ExemploResilience(
                    title: "StatusCodes + WaitAndRetry + Log + HTTP 500",
                    comment: "Servidor gera HTTP 500, com 10 tentativas, mas \r\nessa política só vale para o erro HTTP 404",
                    apiStatusCode: 500,
                    apiDelaySecs: 0,
                    exampleFunc: () =>
                                ILang.Util.Resilience.RestSharpResilience
                                    .Build()
                                    .WithStatusCodes(c => c == (int)HttpStatusCode.NotFound)
                                    .WithWaitAndRetry(10)
                                    .WithLog(log => System.Console.WriteLine(log))
                ),
                new ExemploResilience(
                    title: "StatusCodes + WaitAndRetry + Log + HTTP 400",
                    comment: "Customização do filtro de status codes",
                    apiStatusCode: 400,
                    apiDelaySecs: 0,
                    exampleFunc: () =>
                                ILang.Util.Resilience.RestSharpResilience
                                    .Build()
                                    .WithStatusCodes(c =>
                                        new List<int> {
                                          (int)HttpStatusCode.BadRequest
                                        , (int)HttpStatusCode.MethodNotAllowed
                                        , (int)HttpStatusCode.Forbidden
                                        }.Contains(c) || c > 500)
                                    .WithWaitAndRetry(1)
                                    .WithLog(log => System.Console.WriteLine(log))
                ),
                new ExemploResilience(
                    title: "Sem resiliência + execução lenta",
                    comment: "Execução normal, sem resiliência. Mas o servidor demora 10 segundos pra responder.",
                    apiStatusCode: 200,
                    apiDelaySecs: 10,
                    exampleFunc: () =>
                                ILang.Util.Resilience.RestSharpResilience
                                    .Build()
                ),
                new ExemploResilience(
                    title: "Timeout padrão + execução lenta",
                    comment: "Timeout de 30s (default). Mas o servidor demora 10 segundos pra responder.",
                    apiStatusCode: 200,
                    apiDelaySecs: 10,
                    exampleFunc: () =>
                                ILang.Util.Resilience.RestSharpResilience
                                    .Build()
                                    .WithTimeout()
                ),
                new ExemploResilience(
                    title: "Timeout 5s + execução lenta",
                    comment: "Timeout de 5s. Mas o servidor demora 10 segundos pra responder.",
                    apiStatusCode: 200,
                    apiDelaySecs: 10,
                    exampleFunc: () =>
                                ILang.Util.Resilience.RestSharpResilience
                                    .Build()
                                    .WithTimeout(5)
                )
            };

            foreach (var exemplo in exemplos)
            {
                await ExecutaExemplo(exemplo);
            }

        }

        private async Task ExecutaExemplo(ExemploResilience exemplo)
        {
            System.Console.WriteLine(new string('=', 100));
            System.Console.WriteLine(exemplo.Title);
            System.Console.WriteLine(new string('-', 100));
            System.Console.WriteLine(exemplo.Comments);
            System.Console.WriteLine(new string('=', 100));
            try
            {
                var resilience = exemplo.Func();
                string baseUrl = "http://localhost:5102";
                string resource = $"WeatherForecast/{exemplo.ApiStatusCode}/{exemplo.ApiDelaySecs}";

                var restClient = new RestClient(baseUrl);

                var resetResponse =
                    await restClient.ExecuteTaskAsync(new RestRequest($"WeatherForecast/Reset/{exemplo.FailsBeforeSuccess}", Method.POST));

                var request = new RestRequest(resource);

                IRestResponse response =
                    await resilience.ExecuteAsync(resource, async (requestKey) =>
                        await restClient.ExecuteTaskAsync(request));

                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    System.Console.WriteLine($"response.StatusCode = {response.StatusCode}");
                    return;
                }

                System.Console.WriteLine($"response.StatusCode: {response.StatusCode}");
                System.Console.WriteLine($"response.Content: {response.Content}");
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine(ex);
                Debug.WriteLine(ex);
            }
            finally
            {
                System.Console.WriteLine();
                System.Console.WriteLine();
                System.Console.WriteLine();
            }
        }
        class ExemploResilience
        {
            string title;
            string comments;
            int apiStatusCode;
            int apiDelaySecs;
            int failsBeforeSuccess;
            Func<ILang.Util.Resilience.IPolicyBuilder<IRestResponse>> func;

            public ExemploResilience(string title, string comment, int apiStatusCode, int apiDelaySecs, Func<ILang.Util.Resilience.IPolicyBuilder<IRestResponse>> exampleFunc, int failsBeforeSuccess = 10)
            {
                this.title = title;
                this.comments = comment;
                this.apiStatusCode = apiStatusCode;
                this.apiDelaySecs = apiDelaySecs;
                this.failsBeforeSuccess = failsBeforeSuccess;
                this.func = exampleFunc;
            }

            public string Title { get => title; set => title = value; }
            public string Comments { get => comments; set => comments = value; }
            public int ApiStatusCode { get => apiStatusCode; set => apiStatusCode = value; }
            public int ApiDelaySecs { get => apiDelaySecs; set => apiDelaySecs = value; }
            public int FailsBeforeSuccess { get => failsBeforeSuccess; set => failsBeforeSuccess = value; }
            public Func<ILang.Util.Resilience.IPolicyBuilder<IRestResponse>> Func { get => func; set => func = value; }
        }
    }
}
