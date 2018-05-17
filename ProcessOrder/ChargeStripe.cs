using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Stripe;
using ProcessOrder.Models;
using System;
using System.Threading.Tasks;

namespace ProcessOrder
{
    public static class ChargeStripe
    {
        private static StripeChargeService chargeService = new StripeChargeService();
        

        [FunctionName("ChargeStripe")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] ChargeRequest req, 
            [CosmosDB("store", "orders", ConnectionStringSetting = "CosmosDbConnectionString")] IAsyncCollector<StripeCharge> cosmosDbCollection,
            TraceWriter log)
        {
            log.Info("Order trigger function processed a request.");
            StripeConfiguration.SetApiKey("sk_test_BQokikJOvBiI2HlWgH4olfQ2");

            // Creating the charge for the credit card
            var chargeOptions = new StripeChargeCreateOptions()
            {
                Amount = Convert.ToInt32(req.stripeAmt * 100),
                Currency = "usd",
                Description = $"Charge for {req.stripeEmail}",
                SourceTokenOrExistingSourceId = req.stripeToken
            };

            // Charging the credit card
            StripeCharge charge = await chargeService.CreateAsync(chargeOptions);

            // Add to CosmosDb
            await cosmosDbCollection.AddAsync(charge);

            return new OkResult();
        }
    }
}
