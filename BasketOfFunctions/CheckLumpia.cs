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
    public static class CheckLumpia
    {
        [FunctionName("CheckLumpia")]
        public static async Task<List<string>> Run(
            [OrchestrationTrigger]IDurableOrchestrationContext context,
            ILogger log)
        {
            try
            {
                var answers = new List<string>();

                answers.Add(await context.CallActivityAsync<string>("CheckIfFinished", null));
                context.SetCustomStatus("It's still too early. Check back in an hour.");

                answers.Add(await context.CallActivityAsync<string>("CheckIfFinished", "all of the family"));
                context.SetCustomStatus("Still frying.");

                answers.Add(await context.CallActivityAsync<string>("CheckIfFinished", "the entire neighborhood"));
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
            log.LogInformation($"Checking if all lumpia have been fried...");
            Thread.Sleep(15000); // simulate longer processing delay

            return "No";
        }

        [FunctionName("BeginLumpiaCheck")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")]HttpRequestMessage req,
            [DurableClient]IDurableOrchestrationClient starter,
            ILogger log)
        {
            string instanceId = await starter.StartNewAsync("CheckLumpia", null);

            log.LogInformation($"*sniffs* So let's see if those lumpias are done! ID = {instanceId}");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}