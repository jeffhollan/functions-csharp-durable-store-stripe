using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using ProcessOrder.Models;
using Stripe;
using System.Net;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using System;

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
            var transactions = await context.CallActivityAsync<IEnumerable<StripeCharge>>("Durable_GetTransactions", 100);
            
            foreach(var transaction in transactions)
            {
                tasks.Add(context.CallActivityAsync<OrderDetails>("Durable_GetOrderProcess", transaction));
            }

            await Task.WhenAll(tasks);
            return from details in tasks
                   group details by details.Result.status into s
                   select new { status = s.Key.ToString(), count = s.Count() };
                   
        }

        [FunctionName("Durable_GetTransactions")]
        public static IEnumerable<StripeCharge> GetTransactions(
            [ActivityTrigger] double maxRecords, 
            TraceWriter log)
        {
            log.Info($"Fetching documents from CosmosDb");
            var docs = client.CreateDocumentQuery<StripeCharge>(UriFactory.CreateDocumentCollectionUri("store", "orders"), "SELECT top 100  * FROM c");
            foreach(var doc in docs)
            {
                log.Info($"amount: {doc.Amount}");
            }
            return docs;
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

        private static string EndpointUrl = Environment.GetEnvironmentVariable("EndpointUrl");
        private static string PrimaryKey = Environment.GetEnvironmentVariable("PrimaryKey");
        private static DocumentClient client = new DocumentClient(new Uri(EndpointUrl), PrimaryKey);
    }
}