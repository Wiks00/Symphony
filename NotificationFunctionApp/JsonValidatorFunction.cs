using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using NotificationFunctionApp.Entities;
using ErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs;

namespace NotificationFunctionApp
{
    public static class JsonValidatorFunction
    {
        private static IList<string> errors;

        [FunctionName("JsonValidatorFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ExecutionContext context,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            errors = new List<string>();
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            JSchema schema;

            switch (req.Query["use"])
            {
                case "object":
                    //For some reasons ignores DataAnnotation validation attributes
                    JsonConvert.DeserializeObject<SubscriptionMeta[]>(requestBody, new JsonSerializerSettings { Error = Error });
                    break;
                case "schema":
                    using (var file = new FileStream(Path.Combine(context.FunctionAppDirectory, "event-schema.json"), FileMode.Open))
                    using (var stream = new StreamReader(file))
                    {
                        schema = JSchema.Parse(stream.ReadToEnd());
                        Validate(requestBody, schema);
                    }
                    break;
                case "object-schema":
                    JSchemaGenerator generator = new JSchemaGenerator();
                    schema = generator.Generate(typeof(SubscriptionMeta[]));
                    Validate(requestBody, schema);
                    break;
            }

            return errors.Count < 1
                ? (ActionResult)new OkResult()
                : new BadRequestObjectResult(errors);
        }

        private static bool Validate(string request, JSchema schema)
            => JArray.Parse(request).IsValid(schema, out errors);

        private static void Error(object sender, ErrorEventArgs e)
        {
            errors.Add(e.ErrorContext.Error.Message);
            e.ErrorContext.Handled = true;
        }
    }
}
