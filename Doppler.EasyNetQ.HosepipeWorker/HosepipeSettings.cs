using System.Collections.Generic;

namespace Doppler.EasyNetQ.HosepipeWorker
{
    public class HosepipeSettings
    {
        public IDictionary<string, Connection> Connections { get; set; }
        public string ErrorQueueName { get; set; } = "EasyNetQ_Default_Error_Queue";
        public string UnsolvedErrorQueueName { get; set; } = "EasyNetQ_Unsolved_Error_Queue";
        public string RetryCountHeader { get; set; } = "RetryCount";
        public int NumberOfMessagesToRetrieve { get; set; } = 1000;
        public int DelayedTimeInMilliseconds { get; set; } = 300000;
        public int MaxRetryCount { get; set; } = 3;
        public QueueProcessingStrategy QueueProcessingStrategy { get; set; }
    }

    public enum QueueProcessingStrategy
    {
        Consume,
        Subscribe,
    }

    public class Connection
    {
        public string ConnectionString { get; set; }
        /// <summary>
        /// The secret password of the user indicate in the <see cref="ConnectionString"/>
        /// </summary>
        /// <remarks>If ConnectionString has defined password parameter, will be replaced with this value if it is not empty.</remarks>
        public string SecretPassword { get; set; }

        public bool EnableLegacyTypeNaming { get; set; }
    }
}
