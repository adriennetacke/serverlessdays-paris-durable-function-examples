using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace BasketOfFunctions
{
    public static class Preorder
    {
        [FunctionName("Preorder")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req,
            [DurableClient] IDurableEntityClient durableEntityClient,
            ILogger log)
        {
            log.LogInformation("Preorder requested");

            var amount = req.Query["amount"];
            var entityId = new EntityId(nameof(ConsoleEntity), amount);

            var consoleEntity = await durableEntityClient.ReadEntityStateAsync<ConsoleEntity>(entityId);
            if (consoleEntity.EntityExists && consoleEntity.EntityState.TotalConsoles == 0)
            {
                return new BadRequestObjectResult($"All consoles are sold out!");
            }

            await durableEntityClient.SignalEntityAsync(entityId, nameof(ConsoleEntity.PreorderAsync));

            return new OkObjectResult("Console pre-ordered!");
        }
    }
}
