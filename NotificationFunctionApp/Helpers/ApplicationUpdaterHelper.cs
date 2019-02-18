using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using NotificationFunctionApp.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace NotificationFunctionApp.Helpers
{
    public static class ApplicationUpdaterHelper
    {
        private const string ApplicationsSettingsTableName = "ApplicationsSettings";

        public static Dictionary<string, string> SaveSubscriptionInitialDataAsync(IEnumerable<SubscriptionMeta> subscriptions)
        {
            var applicationSettings = new Dictionary<string, string>();

            foreach (var subscription in subscriptions)
            {
                var urlKey = $"{subscription.EventName}_{nameof(subscription.CallbackUrl)}"; //must be the same in logic app
                var urlValue = subscription.CallbackUrl;

                var emailKey = $"{subscription.EventName}_{nameof(subscription.ContactEmail)}"; //must be the same in logic app
                var emailValue = subscription.ContactEmail;

                var healthCheckUrlKey = $"{subscription.EventName}_{nameof(subscription.HealthCheckUrl)}";  //must be the same in logic app
                var healthCheckUrlValue = subscription.HealthCheckUrl;

                if (!applicationSettings.ContainsKey(urlKey))
                {
                    applicationSettings.Add(urlKey, urlValue);
                }

                if (!applicationSettings.ContainsKey(emailKey))
                {
                    applicationSettings.Add(emailKey, emailValue);
                }

                if (!applicationSettings.ContainsKey(healthCheckUrlKey))
                {
                    applicationSettings.Add(healthCheckUrlKey, healthCheckUrlValue);
                }
            }

            return applicationSettings;
        }
        public static async Task SaveOrUpdateColumnsLastValuesAsync(string partitionKey, Dictionary<string, string> columnsAndValues)
        {
            var rowKey = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("d19");

            var tableRecord = new DynamicTableEntity(partitionKey, rowKey);
            foreach (var setting in columnsAndValues)
            {
                tableRecord.Properties[setting.Key] = new EntityProperty(setting.Value);
            }

            await InsertOrMergeAsync(ApplicationsSettingsTableName, tableRecord);
        }

        private static async Task<TEntity> InsertOrMergeAsync<TEntity>(string tableName, TEntity entity)
           where TEntity : class, ITableEntity
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            var table = await CreateCloudTableAsync(tableName);

            var insertOrMergeOperation = TableOperation.InsertOrMerge(entity);

            var result = table.ExecuteAsync(insertOrMergeOperation);
            var insertedEntity = result.Result as TEntity;

            return insertedEntity;
        }

        private static async Task<CloudTable> CreateCloudTableAsync(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new System.ArgumentException("TableName should be provided", nameof(tableName));
            }
                       
            var cloudTableClient = CreateStorageAccount().CreateCloudTableClient();

            var cloudTableInstance = cloudTableClient.GetTableReference(tableName);

            try
            {
                if (await cloudTableInstance.ExistsAsync())
                {
                    return cloudTableInstance;
                }

                await cloudTableInstance.CreateAsync();
            }
            catch (StorageException)
            {
                throw;
            }

            return cloudTableInstance;
        }

        private static CloudStorageAccount CreateStorageAccount()
        {
            //TO DO: get from settings
            var connectionString = "DefaultEndpointsProtocol=https;AccountName=symphonysa;AccountKey=zM+FIP81jnn4BAGq8iHxWriczRzX0xehu0OdsE42I9QsHDAqyPx2Hy6NqjPfcuwaGH3WdmMbnbWvE971lN1h0Q==;EndpointSuffix=core.windows.net";

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string to storage account should be provided.");
            }

            CloudStorageAccount storageAccount;
            try
            {
                storageAccount = CloudStorageAccount.Parse(connectionString);
            }
            catch (FormatException)
            {
                Debug.WriteLine(
                    "Invalid storage account information provided. Please confirm the AccountName and AccountKey are valid in the app.config file - then restart the application.");
                throw;
            }
            catch (ArgumentException)
            {
                Debug.WriteLine(
                    "Invalid storage account information provided. Please confirm the AccountName and AccountKey are valid in the app.config file - then restart the sample.");
                throw;
            }

            return storageAccount;
        }

    }
}
