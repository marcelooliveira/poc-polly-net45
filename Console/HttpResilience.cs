using Polly;
using Polly.CircuitBreaker;
using Polly.Fallback;
using Polly.Retry;
using Polly.Timeout;
using RestSharp;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ILang.Util.Resilience
{
    public interface IPolicyBuilder<T>
    {
        /// <summary>
        /// Executes the specified asynchronous action within the policy and returns the
        /// result.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <returns>The value returned by the action</returns>
        Task<T> ExecuteAsync(Func<Task<T>> action);
        /// <summary>
        /// Executes the specified asynchronous action within the policy and returns the
        /// result.
        /// </summary>
        /// <param name="requestKey">The request identifier.</param>
        /// <param name="action">The action to perform.</param>
        /// <returns></returns>
        Task<T> ExecuteAsync(string requestKey, Func<Context, Task<T>> action);
        /// <summary>
        /// Specifies the type of return result that this policy can handle with additional
        /// filters on the result status codes.
        /// </summary>
        /// <param name="statusCodeFilter">Predicate to filter the status codes</param>
        /// <returns></returns>
        IPolicyBuilder<T> WithStatusCodes(Func<int, bool> statusCodeFilter);
        /// <summary>
        /// Builds an Polly.AsyncPolicy that will wait asynchronously for a delegate to complete
        /// for a specified period of time. A Polly.Timeout.TimeoutRejectedException will
        /// be thrown if the delegate does not complete within the configured timeout.
        /// </summary>
        /// <param name="timeoutSeconds">The number of seconds after which to timeout.</param>
        /// <returns></returns>
        IPolicyBuilder<T> WithTimeout(int timeoutSeconds = 30);
        /// <summary>
        /// Builds an Polly.Retry.AsyncRetryPolicy`1 that will wait and retry retryCount
        /// times calling onRetry on each retry with the handled exception or result, the
        /// current sleep duration, retry count, and context data. On each retry, the duration
        /// to wait is calculated by: Math.Pow(2, attempt) seconds.
        /// </summary>
        /// <param name="maxRetryAttempts">The retry count.</param>
        /// <returns></returns>
        IPolicyBuilder<T> WithWaitAndRetry(int maxRetryAttempts = 3);
        /// <summary>
        /// Builds a Polly.AsyncPolicy`1 that will function like a Circuit Breaker.
        /// The circuit will break if handledEventsAllowedBeforeBreaking exceptions or results
        /// that are handled by this policy are encountered consecutively.
        /// The circuit will stay broken for the durationOfBreak. Any attempt to execute
        /// this policy while the circuit is broken, will immediately throw a Polly.CircuitBreaker.BrokenCircuitException
        /// containing the exception or result that broke the circuit.
        /// If the first action after the break duration period results in a handled exception
        /// or result, the circuit will break again for another durationOfBreak; if no exception
        /// or handled result is encountered, the circuit will reset.
        /// </summary>
        /// <param name="handledEventsAllowedBeforeBreaking">
        /// The number of exceptions or handled results that are allowed before opening the circuit.
        /// </param>
        /// <param name="durationOfBreakSeconds">The duration the circuit will stay open before resetting.</param>
        /// <returns></returns>
        IPolicyBuilder<T> WithCircuitBreaker(int handledEventsAllowedBeforeBreaking = 6, int durationOfBreakSeconds = 30);
        /// <summary>
        /// Builds an Polly.Fallback.AsyncFallbackPolicy`1 which provides a fallback value
        /// if the main execution fails.Executes the main delegate asynchronously, but if
        /// this throws a handled exception or raises a handled result, returns fallbackValue.
        /// </summary>
        /// <param name="fallbackResponse">The fallback IRestResponse value to provide.</param>
        /// <returns></returns>
        IPolicyBuilder<T> WithFallback(T fallbackResponse);
        IPolicyBuilder<T> WithLog(Action<string> logFunction);
    }

    public abstract class PolicyBuilder<T> : IPolicyBuilder<T>
    {
        private const int DefaultTimeout = 30;
        private const int DefaultMaxRetryAttempts = 3;
        private const int DefaultHandledEventsAllowedBeforeBreaking = 3;
        private const int DefaultDurationOfBreakSeconds = 30;
        private Func<int, TimeSpan> DefaultPauseBetweenFailures;

        private AsyncPolicy<T> policy;
        protected Action<string> logFunction;
        protected Func<int, bool> statusCodeFilter
            = statusCode => statusCode == 0 || statusCode >= 400;
        protected string requestKey;

        public PolicyBuilder()
        {
            DefaultPauseBetweenFailures = (i) => TimeSpan.FromSeconds(Math.Pow(2, i));

            logFunction = new Action<string>((errMessage) => { });
            requestKey = Guid.NewGuid().ToString();
            policy = Policy.NoOpAsync<T>();
        }

        public async Task<T> ExecuteAsync(Func<Task<T>> action)
        {
            return await this.policy.ExecuteAsync(action);
        }

        public async Task<T> ExecuteAsync(string requestKey, Func<Context, Task<T>> action)
        {
            this.requestKey = requestKey;
            return await this.policy.ExecuteAsync(action, new Context(requestKey));
        }

        public IPolicyBuilder<T> WithStatusCodes(Func<int, bool> statusCodeFilter)
        {
            this.statusCodeFilter = statusCodeFilter;
            return this;
        }

        public IPolicyBuilder<T> WithWaitAndRetry(int maxRetryAttempts = DefaultMaxRetryAttempts)
        {
            this.policy = this.policy
                .WrapAsync(GetWaitAndRetry(maxRetryAttempts));

            return this;
        }

        public IPolicyBuilder<T> WithCircuitBreaker(int handledEventsAllowedBeforeBreaking = DefaultHandledEventsAllowedBeforeBreaking, int durationOfBreakSeconds = DefaultDurationOfBreakSeconds)
        {
            var durationOfBreak = TimeSpan.FromSeconds(durationOfBreakSeconds);

            this.policy = this.policy
                .WrapAsync(GetCircuitBreaker(handledEventsAllowedBeforeBreaking
                , durationOfBreak));

            return this;
        }

        public IPolicyBuilder<T> WithTimeout(int timeoutSeconds = DefaultTimeout)
        {
            this.policy = this.policy
                .WrapAsync(GetTimeout(timeoutSeconds));

            return this;
        }

        public IPolicyBuilder<T> WithFallback(T fallbackResponse)
        {
            this.policy = this.policy
                .WrapAsync(GetFallback(fallbackResponse));

            return this;
        }

        public IPolicyBuilder<T> WithLog(Action<string> logFunction)
        {
            this.logFunction = logFunction;
            return this;
        }

        private AsyncRetryPolicy<T> GetWaitAndRetry(int maxRetryAttempts)
        {
            return Policy
                    .HandleResult<T>(GetResultPredicate())
                    .WaitAndRetryAsync(maxRetryAttempts, DefaultPauseBetweenFailures, GetOnRetry());
        }

        private AsyncCircuitBreakerPolicy<T> GetCircuitBreaker(int handledEventsAllowedBeforeBreaking, TimeSpan durationOfBreak)
        {
            return Policy
                .HandleResult<T>(GetResultPredicate())
                .CircuitBreakerAsync(handledEventsAllowedBeforeBreaking, durationOfBreak,
                onBreak: (iRestResponse, timespan, context) =>
                {
                    logFunction($"Circuit went into a fault state. RequestKey: {requestKey}");
                },
                onReset: (context) =>
                {
                    logFunction($"Circuit left the fault state. RequestKey: {requestKey}");
                });
        }

        private AsyncTimeoutPolicy GetTimeout(int timeoutSeconds)
        {
            return Policy.TimeoutAsync(timeoutSeconds, TimeoutStrategy.Pessimistic);
        }

        private AsyncFallbackPolicy<T> GetFallback(T fallbackResponse)
        {
            return Policy
                    .HandleResult<T>(GetResultPredicate())
                    .FallbackAsync(fallbackResponse);
        }

        protected abstract Func<T, bool> GetResultPredicate();

        protected abstract Action<DelegateResult<T>, TimeSpan, int, Context> GetOnRetry();
    }

    public interface IRestSharpResilience : IPolicyBuilder<IRestResponse>
    {

    }

    public class RestSharpResilience : PolicyBuilder<IRestResponse>, IRestSharpResilience
    {
        private RestSharpResilience()
        {

        }

        /// <summary>
        /// Build the container for the RestSharp resilience policy.
        /// </summary>
        /// <returns></returns>
        public static RestSharpResilience Build()
        {
            return new RestSharpResilience();
        }

        protected override Func<IRestResponse, bool> GetResultPredicate()
        {
            return r => statusCodeFilter((int)r.StatusCode);
        }

        protected override Action<DelegateResult<IRestResponse>, TimeSpan, int, Context> GetOnRetry()
        {
            return (response, timeSpan, retryCount, context) =>
                logFunction($"The request failed. HttpStatusCode={response.Result.StatusCode}. Waiting {timeSpan} seconds before retry. Number attempt {retryCount}.  RequestKey: {requestKey}");
        }
    }

    
    public interface IHttpClientResilience : IPolicyBuilder<HttpResponseMessage>
    {

    }

    public class HttpClientResilience : PolicyBuilder<HttpResponseMessage>, IHttpClientResilience
    {
        private HttpClientResilience()
        {

        }

        /// <summary>
        /// Build the container for the HttpClient resilience policy.
        /// </summary>
        /// <returns></returns>
        public static HttpClientResilience Build()
        {
            return new HttpClientResilience();
        }

        protected override Func<HttpResponseMessage, bool> GetResultPredicate()
        {
            return r => statusCodeFilter((int)r.StatusCode);
        }

        protected override Action<DelegateResult<HttpResponseMessage>, TimeSpan, int, Context> GetOnRetry()
        {
            return (response, timeSpan, retryCount, context) =>
                logFunction($"The request failed. HttpStatusCode={response.Result.StatusCode}. Waiting {timeSpan} seconds before retry. Number attempt {retryCount}.  RequestKey: {requestKey}");
        }
    }
}
