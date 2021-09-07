using System.Collections.Generic;

namespace Doppler.EasyNetQ.HosepipeWorker
{
    public class HosepipeSettings
    {
        public List<Connections> Connections { get; set; }
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

    public class Connections
    {
        public string Name { get; set; }
        public string ConnectionString { get; set; }
    }
}
