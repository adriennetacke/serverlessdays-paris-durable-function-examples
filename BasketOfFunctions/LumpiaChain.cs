using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace BasketOfFunctions
{
    public static class LumpiaChain
    {
        [FunctionName("LumpiaChain")]
        public static async Task<string> Run(
        [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            try
            {
                var potentialLumpia = await context.CallActivityAsync<string>("ScoopFillingOntoWrapper", null);
                var rolledLumpia = await context.CallActivityAsync<string>("FoldAndRoll", potentialLumpia);
                var sealedLumpia = await context.CallActivityAsync<string>("AddEggWhiteSeal", rolledLumpia);

                return await context.CallActivityAsync<string>("FryThatSucker", sealedLumpia);
            }
            catch (FormatException imperfectRoll)
            {
                var disapprovalFromMom =
                    new FormatException($"What is this?! Don't you know how to roll these by now? {imperfectRoll.InnerException}");

                throw disapprovalFromMom;
            }
            catch (ArgumentOutOfRangeException tooMuchFilling)
            {
                var structuralIntegrityWarning =
                    new ArgumentOutOfRangeException($"Warning: This much filling will prevent proper rolling and closure of lumpia. {tooMuchFilling.InnerException}");

                throw structuralIntegrityWarning;
            }
        }

        [FunctionName("ScoopFillingOntoWrapper")]
        public static string ScoopFilling([ActivityTrigger] string ingredients, ILogger log)
        {
            log.LogInformation($"Scooping filling onto empty wrapper.");
            return $"Potential lumpia";
        }

        [FunctionName("FoldAndRoll")]
        public static string RollLumpia([ActivityTrigger] string potentialLumpia, ILogger log)
        {
            log.LogInformation($"Folding and rolling {potentialLumpia}");

            var rolledLumpia = potentialLumpia.Replace("Potential", "Rolled");
            return rolledLumpia;
        }

        [FunctionName("AddEggWhiteSeal")]
        public static string AddSeal([ActivityTrigger] string rolledLumpia, ILogger log)
        {
            log.LogInformation($"Adding egg white seal to {rolledLumpia}");

            var sealedLumpia = rolledLumpia.Replace("Rolled", "Sealed");
            return sealedLumpia;
        }

        [FunctionName("FryThatSucker")]
        public static string FryLumpia([ActivityTrigger] string sealedLumpia, ILogger log)
        {
            log.LogInformation($"Frying delicious {sealedLumpia}!");

            var friedLumpia = sealedLumpia.Replace("Sealed", "Deliciously fried and ready to eat");
            return friedLumpia;
        }

        [FunctionName("MakeLumpia")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("LumpiaChain", null);

            log.LogInformation($"Started making lumpia with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}