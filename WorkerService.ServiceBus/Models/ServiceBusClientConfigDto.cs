using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService.ServiceBus.Models
{
    public class ServiceBusClientConfigDto
    {
        [DataMember]
        public string SenderQueueName { get; set; } = string.Empty;
        [DataMember]
        public string SenderQueueEndPointUrl { get; set; } = string.Empty;
        [DataMember]
        public string ReceiverQueueName { get; set; } = string.Empty;
        [DataMember]
        public string ReceiverQueueEndPointUrl { get; set; } = string.Empty;
        [DataMember]
        public int ReceiverPreFetchCount { get; set; } = 10;
        [DataMember]
        public int ReceiverMaxConcurrentSession { get; set; } = 2;
        [DataMember]
        public Nullable<int> ReceiverTimeToLive { get; set; } = 30;
        [DataMember]
        public Nullable<int> ReceiverRetryCount { get; set; } = 3;
        [DataMember]
        public string ConsumerName { get; set; } = string.Empty;
    }
}
