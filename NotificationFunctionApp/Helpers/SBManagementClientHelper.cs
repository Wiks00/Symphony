using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ServiceBus;
using Microsoft.Azure.Management.ServiceBus.Models;
using Microsoft.Rest.Azure;

namespace NotificationFunctionApp.Helpers
{
    public static class SBManagementClientHelper
    {
        public static async Task<IList<SBSubscription>> ListAndAggregateAsync(this ISubscriptionsOperations operations, string resourceGroup, string namespaceName, string topicName)
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

        public static async Task<SBSubscription> GetAsync(this ISubscriptionsOperations operations,
            string resourceGroup, string namespaceName, string topicName, string subscriptionName)
        {
            var subscriptions =
                await operations.ListAndAggregateAsync(resourceGroup, namespaceName, topicName);

            return subscriptions.FirstOrDefault(s =>
                s.Name.Equals(subscriptionName, StringComparison.OrdinalIgnoreCase));
        }

        public static async Task<IList<Rule>> ListAndAggregateAsync(this IRulesOperations operations, string resourceGroup, string namespaceName, string topicName, string subscriptionName)
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
    }
}
