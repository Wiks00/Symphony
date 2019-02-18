using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using NotificationFunctionApp.Entities;
using NotificationFunctionApp.Helpers;

namespace NotificationFunctionApp
{
    public static class SettingsUpdaterFunction
    {
        [FunctionName("SettingsUpdaterFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "settingsupdater/{applicationId}")] HttpRequest req,
            string applicationId,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            if (string.IsNullOrWhiteSpace(applicationId))
            {
                return new BadRequestObjectResult("X-Application-Id header should be provided");
            }

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            List<SubscriptionMeta> subscriptions = JsonConvert.DeserializeObject<List<SubscriptionMeta>>(requestBody);

            var applicationSettingsToSave = ApplicationUpdaterHelper.SaveSubscriptionInitialDataAsync(subscriptions);
            await ApplicationUpdaterHelper.SaveOrUpdateColumnsLastValuesAsync(applicationId, applicationSettingsToSave);

            return new OkResult();
        }
    }
}
