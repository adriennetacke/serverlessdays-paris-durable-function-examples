using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace BasketOfFunctions
{
    public static class LumpiaFanOutFanIn
    {
        [FunctionName("LumpiaFanOutFanIn")]
        public static async Task ParallelLumpiaFunction([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var parallelTasks = new List<Task<int>>();
            
            List<string> helpers = await context.CallActivityAsync<List<string>>("FindAllTitasAndTitos", null);
            for (int i = 0; i < titas.Count; i++)
            {
                Task<int> task = context.CallActivityAsync<int>("MakeLotsOfLumpia", helpers[i]);
                parallelTasks.Add(task);
            }

            // Wait for all helpers to finish making Lumpia!
            await Task.WhenAll(parallelTasks);

            int allTheLumpias = parallelTasks.Sum(t => t.Result);
            await context.CallActivityAsync("FryThoseSuckers", allTheLumpias);
        }

        [FunctionName("MakeLotsOfLumpia")]
        public static int ScaleLumpia([ActivityTrigger] string name, ILogger log)
        {
            Random random = new Random();
            int totalLumpiaMade = random.Next(100);

            log.LogInformation($"{name} made a total of {totalLumpiaMade}!");
            return totalLumpiaMade;
        }

        [FunctionName("FindAllTitasAndTitos")]
        public static List<string> FindAllTitasAndTitos([ActivityTrigger] string name, ILogger log)
        {
            List<string> allHelpers = new List<string>() { "Tita Sheryl", "Tita Angelique", "Tito Joel", "Tita Jonathan" };
            return allHelpers;
        }

        [FunctionName("FryThoseSuckers")]
        public static string FryLumpia([ActivityTrigger] int totalLumpias, ILogger log)
        {
            log.LogInformation($"Frying {totalLumpias} delicious lumpia!");

            return $"{totalLumpias} deliciously fried and ready to eat!";
        }

        [FunctionName("MakeMoreLumpia")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("LumpiaFanOutFanIn", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}
