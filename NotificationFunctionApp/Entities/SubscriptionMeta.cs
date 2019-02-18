using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace NotificationFunctionApp.Entities
{
    [DataContract]
    [JsonObject(MemberSerialization.OptIn, ItemRequired = Required.Always)]
    public class SubscriptionMeta
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionMeta"/> class.
        /// </summary>
        public SubscriptionMeta()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionMeta"/> class.
        /// </summary>
        /// <param name="clone">
        /// The clone.
        /// </param>
        public SubscriptionMeta(SubscriptionMeta clone)
        {
            this.EventName = clone.EventName;
            this.CallbackUrl = clone.CallbackUrl;
            this.Secret = clone.Secret;
            this.ContactEmail = clone.ContactEmail;
            this.HealthCheckUrl = clone.HealthCheckUrl;
        }

        /// <summary>
        /// Gets or sets the callback url.
        /// </summary>
        [DataMember, Url]
        [JsonProperty("callbackUrl")]
        public string CallbackUrl { get; set; }

        /// <summary>
        /// Gets or sets the contact email.
        /// </summary>
        [DataMember, RegularExpression(@"^(([a-zA-Z0-9_\-\.]+)@([a-zA-Z0-9_\-\.]+)\.([a-zA-Z]{2,5}){1,25})+([;.](([a-zA-Z0-9_\-\.]+)@([a-zA-Z0-9_\-\.]+)\.([a-zA-Z]{2,5}){1,25})+)*$")]
        [JsonProperty("contactEmail")]
        public string ContactEmail { get; set; }

        /// <summary>
        /// Gets or sets the event name.
        /// </summary>
        [DataMember]
        [JsonProperty("eventName")]
        public string EventName { get; set; }

        /// <summary>
        /// Gets or sets the secret.
        /// </summary>
        [DataMember]
        [JsonProperty("secret")]
        public string Secret { get; set; }

        /// <summary>
        /// Gets or sets the health check url.
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false), Url]
        [JsonProperty("healthCheckUrl", Required = Required.Default)]
        public string HealthCheckUrl { get; set; }
    }
}
