using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Configuration;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace NotificationFunctionApp
{
    public static class ApplicationSettings
    {
        private static readonly AutoResetEvent ConfigurationEvent = new AutoResetEvent(true);
        private static readonly Dictionary<string, IConfiguration> Configs = new Dictionary<string, IConfiguration>();

        public static T GetConfigurationSetting<T>(this ExecutionContext context, string setting)
            => GetConfiguration(context).GetValue<T>(setting);


        public static IConfiguration GetConfiguration(this ExecutionContext context)
        {
            string key = context.FunctionAppDirectory;

            if (!Configs.TryGetValue(key, out IConfiguration configuration))
            {
                ConfigurationEvent.WaitOne();

                try
                {
                    if (!Configs.TryGetValue(key, out configuration))
                    {
                        configuration = BuildConfigurationManager(context);

                        Configs.Add(key, configuration);
                    }
                }
                finally
                {
                    ConfigurationEvent.Set();
                }
            }

            return configuration;
        }

        private static IConfiguration BuildConfigurationManager(ExecutionContext context)
            => new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
    }
}
