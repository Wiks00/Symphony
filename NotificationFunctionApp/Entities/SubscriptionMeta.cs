using System.Runtime.Serialization;

namespace NotificationFunctionApp.Entities
{
    [DataContract]
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
        [DataMember]
        public string CallbackUrl { get; set; }

        /// <summary>
        /// Gets or sets the contact email.
        /// </summary>
        [DataMember]
        public string ContactEmail { get; set; }

        /// <summary>
        /// Gets or sets the event name.
        /// </summary>
        [DataMember]
        public string EventName { get; set; }

        /// <summary>
        /// Gets or sets the secret.
        /// </summary>
        [DataMember]
        public string Secret { get; set; }

        /// <summary>
        /// Gets or sets the health check url.
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public string HealthCheckUrl { get; set; }
    }
}
