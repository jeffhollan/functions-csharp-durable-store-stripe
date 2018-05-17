using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using ProcessOrder.Models;
using Stripe;

namespace ProcessOrder
{
    public static class DurableGenerateReport
    {
        [FunctionName("DurableGenerateReport")]
        public static async Task<dynamic> RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContext context)
        {
            var tasks = new List<Task<OrderDetails>>();
            var orderTotals = new List<double>();
            var transactions = await context.CallActivityAsync<IEnumerable<StripeCharge>>("Durable_GetTransactions", null);
            
            foreach(var transaction in transactions)
            {
                tasks.Add(context.CallActivityAsync<OrderDetails>("Durable_GetOrderProcess", transaction));
            }

            await Task.WhenAll(tasks);
            return from details in tasks
                   group details by details.Result.status into s
                   select new { status = s.Key, count = s.Count() };
                   
        }

        [FunctionName("Durable_GetTransactions")]
        public static IEnumerable<StripeCharge> GetTransactions(
            [ActivityTrigger] object maxRecords, 
            [CosmosDB("store", "orders", ConnectionStringSetting = "CosmosDbConnectionString", SqlQuery = "SELECT top 100  * FROM c")] IEnumerable<dynamic> documents,
            TraceWriter log)
        {
            log.Info($"Fetched recent documents");
            return documents as IEnumerable<StripeCharge>;
        }

        [FunctionName("DurableGenerateReport_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClient starter,
            TraceWriter log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("DurableGenerateReport", null);

            log.Info($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("Durable_GetOrderProcess")]
        public static OrderDetails GetOrderProcess(
            [ActivityTrigger] StripeCharge transaction,
            TraceWriter log)
        {
            log.Info($"Getting order details for order id: ${transaction.Id}");
            return new OrderDetails { status = Status.Delivered, orderId = transaction.Id };
        }
    }
}