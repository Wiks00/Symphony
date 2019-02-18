using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Management.ServiceBus;
using Microsoft.Azure.Management.ServiceBus.Models;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using Microsoft.Rest.Azure;
using Newtonsoft.Json;

namespace NotificationFunctionApp
{
    public static class SubscriptionUpdaterFunction
    {
        [FunctionName(nameof(SubscriptionUpdaterFunction))]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "put", "delete", Route = "subscription/{applicationId}")] HttpRequest req,
            string applicationId,
            ExecutionContext context,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var azureServicesTokenProvider = new AzureServiceTokenProvider();
            var configuration = context.GetConfiguration();

            string filter = $"applicationId = '{applicationId}'";

            var sbClient = await azureServicesTokenProvider.AuthenticateAndCreateServiceBusClientAsync(configuration);

            log.LogInformation("Management client successfully initialized");

            if (HttpMethods.IsPut(req.Method))
            {
                log.LogInformation($"Application recognized request as a upsert statement for {applicationId}");

                await UpsertSubscriptionAsync(sbClient, configuration, applicationId, filter);

                log.LogInformation($"Subscription {applicationId} successfully upserted");
            }

            if (HttpMethods.IsDelete(req.Method))
            {
                log.LogInformation($"Application recognized request as a delete statement for {applicationId}");

                await DeleteSubscriptionAsync(sbClient, configuration, applicationId);

                log.LogInformation($"Subscription {applicationId} successfully deleted");
            }

            return new OkResult();
        }

        private static async Task<ServiceBusManagementClient> AuthenticateAndCreateServiceBusClientAsync(this AzureServiceTokenProvider tokenProvider, IConfiguration configuration)
        {
            
            string accessToken = await tokenProvider.GetAccessTokenAsync(configuration.GetValue<string>("managementConfig:audience"));

            return new ServiceBusManagementClient(new TokenCredentials(accessToken))
            {
                SubscriptionId = configuration.GetValue<string>("managementConfig:subscriptionId")
            };
        }

        private static async Task<IList<SBSubscription>> ListAndAggregateAsync(this ISubscriptionsOperations operations, string resourceGroup, string namespaceName, string topicName)
        {
            List<SBSubscription> subscriptions = new List<SBSubscription>();

            AzureOperationResponse<IPage<SBSubscription>> response =
                await operations.ListByTopicWithHttpMessagesAsync(resourceGroup, namespaceName, topicName);


            subscriptions.AddRange(response.Body);

            while (!string.IsNullOrEmpty(response.Body.NextPageLink)) 
            {
                response =
                    await operations.ListByTopicNextWithHttpMessagesAsync(response.Body.NextPageLink);

                subscriptions.AddRange(response.Body);
            } 

            return subscriptions;
        }

        private static async Task<SBSubscription> GetAsync(this ISubscriptionsOperations operations,
            string resourceGroup, string namespaceName, string topicName, string subscriptionName)
        {
            var subscriptions =
                await operations.ListAndAggregateAsync(resourceGroup, namespaceName, topicName);

            return subscriptions.FirstOrDefault(s =>
                s.Name.Equals(subscriptionName, StringComparison.OrdinalIgnoreCase));
        }

        private static async Task<IList<Rule>> ListAndAggregateAsync(this IRulesOperations operations, string resourceGroup, string namespaceName, string topicName, string subscriptionName)
        {
            List<Rule> subscriptions = new List<Rule>();

            AzureOperationResponse<IPage<Rule>> response =
                await operations.ListBySubscriptionsWithHttpMessagesAsync(resourceGroup, namespaceName, topicName, subscriptionName);

            subscriptions.AddRange(response.Body);

            while (!string.IsNullOrEmpty(response.Body.NextPageLink))
            {
                response =
                    await operations.ListBySubscriptionsNextWithHttpMessagesAsync(response.Body.NextPageLink);

                subscriptions.AddRange(response.Body);
            }

            return subscriptions;
        }

        private static async Task DeleteSubscriptionAsync(ServiceBusManagementClient sbClient, IConfiguration configuration, string subscriptionName)
        {
            string resourceGroup = configuration.GetValue<string>("managementConfig:resourceGroup");
            string nsName = configuration.GetValue<string>("notificationsServiceBusConfig:namespace");
            string topicName = configuration.GetValue<string>("notificationsServiceBusConfig:topicName");

            SBSubscription subscription =
                await sbClient.Subscriptions.GetAsync(resourceGroup, nsName, topicName, subscriptionName);

            if (subscription != null)
            {
                await sbClient.Subscriptions.DeleteAsync(resourceGroup, nsName, topicName, subscriptionName);
            }
        }

        private static async Task UpsertSubscriptionAsync(ServiceBusManagementClient sbClient, IConfiguration configuration, string subscriptionName, string filter)
        {
            string resourceGroup = configuration.GetValue<string>("managementConfig:resourceGroup");
            string nsName = configuration.GetValue<string>("notificationsServiceBusConfig:namespace");
            string topicName = configuration.GetValue<string>("notificationsServiceBusConfig:topicName");
            string notificationsDeliveryTopicName = configuration.GetValue<string>("notificationsServiceBusConfig:notificationsDeliveryTopicName");

            SBSubscription subscription =
                await sbClient.Subscriptions.GetAsync(resourceGroup, nsName, topicName, subscriptionName);

            if (subscription == null)
            {
                subscription = new SBSubscription
                {
                    DefaultMessageTimeToLive = new TimeSpan(0, 0, 1)
                };

                //The newly created subscription will have a '$Default' rule '1=1', making possible 
                //some unexpected messages to be catched.
                //Workaround: 1) Create a subscription with very short TTL and w/o ForwardTo. 
                //2) Replace default rule with the actual one.
                //3) Set default TTL and actual ForwardTo for the subscription.

                //Workaround step 1.
                subscription = await sbClient.Subscriptions.CreateOrUpdateAsync(resourceGroup, nsName, topicName,
                    subscriptionName, subscription);
            }

            //Workaround step 2.
            var rules = await sbClient.Rules.ListAndAggregateAsync(resourceGroup, nsName, topicName, subscriptionName);

            foreach (var rule in rules)
            {
                if (rule.SqlFilter == null || rule.SqlFilter.SqlExpression != filter)
                {
                    await sbClient.Rules.DeleteAsync(resourceGroup, nsName, topicName,
                        subscriptionName, rule.Name);
                }
            }

            if (!rules.Any(r => r.SqlFilter != null && r.SqlFilter.SqlExpression == filter))
            {
                var appIdRule = new Rule { SqlFilter = new SqlFilter(filter) };
                await sbClient.Rules.CreateOrUpdateAsync(resourceGroup, nsName, topicName,
                    subscriptionName, "rule", appIdRule);
            }

            if (subscription.ForwardTo != notificationsDeliveryTopicName)
            {
                //Workaround step 3.
                subscription.ForwardTo = notificationsDeliveryTopicName;
                subscription.DefaultMessageTimeToLive = null;
                await sbClient.Subscriptions.CreateOrUpdateAsync(resourceGroup, nsName, topicName,
                    subscriptionName, subscription);
            }
        }
    }
}
