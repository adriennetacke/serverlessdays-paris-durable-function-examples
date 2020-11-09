using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace BasketOfFunctions
{
    public static class NevadaBallotCount
    {
        [FunctionName("NevadaBallotCount")]
        public static async Task<List<string>> Run(
            [OrchestrationTrigger]IDurableOrchestrationContext context,
            ILogger log)
        {
            try
            {
                var answers = new List<string>();

                answers.Add(await context.CallActivityAsync<string>("CheckIfFinished", null));
                context.SetCustomStatus("It's still too early. Check back tomorrow at 9am.");

                answers.Add(await context.CallActivityAsync<string>("CheckIfFinished", "all of America"));
                context.SetCustomStatus("Still counting. We have 67% of counties reporting so far.");

                answers.Add(await context.CallActivityAsync<string>("CheckIfFinished", "the entire world"));
                context.SetCustomStatus("We'll be done soon, promise!");

                return answers;
            }
            catch (Exception)
            {
                throw;
            }
        }

        [FunctionName("CheckIfFinished")]
        public static string Check([ActivityTrigger] string interestedParty, ILogger log)
        {
            log.LogInformation($"Checking if all ballots have been counted... No pressure, just {interestedParty} waiting and watching...");
            Thread.Sleep(15000); // simulate longer processing delay

            return "No";
        }

        [FunctionName("BeginNevadaCount")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")]HttpRequestMessage req,
            [DurableClient]IDurableOrchestrationClient starter,
            ILogger log)
        {
            string instanceId = await starter.StartNewAsync("NevadaBallotCount", null);

            log.LogInformation($"*GULP* Here we go, let's start counting Nevada's ballots! ID = {instanceId}");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}