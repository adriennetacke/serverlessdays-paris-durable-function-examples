using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace BasketOfFunctions
{
    public class ConsoleEntity
    {
        [JsonProperty("value")]
        public int TotalConsoles { get; set; }

        public void PreorderAsync(int amount) => TotalConsoles += amount;

        public int Get() => TotalConsoles;

        [FunctionName(nameof(ConsoleEntity))]
        public static Task Run([EntityTrigger] IDurableEntityContext context)
            => context.DispatchAsync<ConsoleEntity>();
    }
}
