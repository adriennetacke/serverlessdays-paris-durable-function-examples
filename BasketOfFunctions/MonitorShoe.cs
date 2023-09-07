using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

using Microsoft.Extensions.Logging;
using BasketOfFunctions.Classes;
using Newtonsoft.Json.Linq;


namespace BasketOfFunctions
{
    public static class Monitor
    {
        [FunctionName("Monitor")]
        public static async Task Run([OrchestrationTrigger] IDurableOrchestrationContext monitorContext, ILogger log)
        {
            MonitorRequest input = monitorContext.GetInput<MonitorRequest>();
            if (!monitorContext.IsReplaying) { log.LogInformation($"Received monitor request. Size: {input?.Size}. Email: {input?.Email}."); }

            VerifyRequest(input);

            DateTime endTime = monitorContext.CurrentUtcDateTime.AddDays(30);
            if (!monitorContext.IsReplaying) { log.LogInformation($"Instantiating monitor for {input.Size}. Expires: {endTime}."); }

            while (monitorContext.CurrentUtcDateTime < endTime)
            {
                // Check the inventory
                if (!monitorContext.IsReplaying) { log.LogInformation($"Checking current inventory for {input.Size} at {monitorContext.CurrentUtcDateTime}."); }

                bool isBackInStock = await monitorContext.CallActivityAsync<bool>("CheckInventoryForRestock", input.Size);

                if (isBackInStock)
                {
                    // It's back in stock. Send customer an email alert.
                    if (!monitorContext.IsReplaying) { log.LogInformation($"Detected availability for {input.Size}. Notifying {input.Email}"); }

                    await monitorContext.CallActivityAsync("SendInStockAlert", input.Email);
                    break;
                }
            }

            log.LogInformation($"Monitor expiring.");
        }




















        [Deterministic]
        private static void VerifyRequest(MonitorRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), "An input object is required.");
            }

            if (request.Location == null)
            {
                throw new ArgumentNullException(nameof(request.Location), "A location input is required.");
            }

            if (string.IsNullOrEmpty(request.Phone))
            {
                throw new ArgumentNullException(nameof(request.Phone), "A phone number input is required.");
            }
        }

        [FunctionName("CheckInventoryForRestock")]
        public static async Task<bool> CheckInventoryForRestock([ActivityTrigger] Location location)
        {
            var currentConditions = await WeatherUnderground.GetCurrentConditionsAsync(location);
            return currentConditions.Equals(WeatherCondition.Clear);
        }

        [FunctionName("SendGoodWeatherAlert")]
        public static string SendGoodWeatherAlert(
        [ActivityTrigger] string phoneNumber,
        ILogger log)
        {
            return $"The weather's clear outside! Go take a walk!";
        }

    }
    internal class WeatherUnderground
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static IReadOnlyDictionary<string, WeatherCondition> weatherMapping = new Dictionary<string, WeatherCondition>()
                {
                    { "Clear", WeatherCondition.Clear },
                    { "Overcast", WeatherCondition.Clear },
                    { "Cloudy", WeatherCondition.Clear },
                    { "Clouds", WeatherCondition.Clear },
                    { "Drizzle", WeatherCondition.Precipitation },
                    { "Hail", WeatherCondition.Precipitation },
                    { "Ice", WeatherCondition.Precipitation },
                    { "Mist", WeatherCondition.Precipitation },
                    { "Precipitation", WeatherCondition.Precipitation },
                    { "Rain", WeatherCondition.Precipitation },
                    { "Showers", WeatherCondition.Precipitation },
                    { "Snow", WeatherCondition.Precipitation },
                    { "Spray", WeatherCondition.Precipitation },
                    { "Squall", WeatherCondition.Precipitation },
                    { "Thunderstorm", WeatherCondition.Precipitation },
                };

        internal static async Task<WeatherCondition> GetCurrentConditionsAsync(Location location)
        {
            var apiKey = Environment.GetEnvironmentVariable("WeatherUndergroundApiKey");
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("The WeatherUndergroundApiKey environment variable was not set.");
            }

            var callString = string.Format("http://api.wunderground.com/api/{0}/conditions/q/{1}/{2}.json", apiKey, location.State, location.City);
            var response = await httpClient.GetAsync(callString);
            var conditions = await response.Content.ReadAsAsync<JObject>();

            JToken currentObservation;
            if (!conditions.TryGetValue("current_observation", out currentObservation))
            {
                JToken error = conditions.SelectToken("response.error");

                if (error != null)
                {
                    throw new InvalidOperationException($"API returned an error: {error}.");
                }
                else
                {
                    throw new ArgumentException("Could not find weather for this location. Try being more specific.");
                }
            }

            return MapToWeatherCondition((string)(currentObservation as JObject).GetValue("weather"));
        }

        private static WeatherCondition MapToWeatherCondition(string weather)
        {
            foreach (var pair in weatherMapping)
            {
                if (weather.Contains(pair.Key))
                {
                    return pair.Value;
                }
            }

            return WeatherCondition.Other;
        }
    }

    public enum WeatherCondition
    {
        Other,
        Clear,
        Precipitation,
    }
}