using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static int _failsBeforeSuccess = 10;

        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpPost()]
        [Route("Reset/{failsBeforeSuccess}")]
        public ActionResult Reset(int failsBeforeSuccess)
        {
            _failsBeforeSuccess = failsBeforeSuccess;
            return Ok();
        }

        [HttpGet(Name = "GetWeatherForecast")]
        [Route("{statusCode?}/{delaySecs?}")]
        public async Task<ActionResult<IEnumerable<WeatherForecast>>> GetAsync(int? statusCode = 200, int? delaySecs = 0)
        {
            if (delaySecs.HasValue)
            {
                await Task.Delay(delaySecs.Value * 1000);
            }

            if (statusCode != 200)
            {
                if (_failsBeforeSuccess == 0)
                {
                    statusCode = 200;
                }
                else if (_failsBeforeSuccess > 0)
                {
                    _failsBeforeSuccess--;
                }
            }

            if (statusCode == 200)
            {
                return Enumerable.Range(1, 5).Select(index => new WeatherForecast
                {
                    Date = DateTime.Now.AddDays(index),
                    TemperatureC = Random.Shared.Next(-20, 55),
                    Summary = Summaries[Random.Shared.Next(Summaries.Length)]
                })
                .ToArray();
            }

            return StatusCode(statusCode.Value);
        }
    }
}