using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService.ServiceBus.Models
{
    [DataContract]
    public partial class ServiceBusMessageDto
    {
        [DataMember]
        public string SeriveBusEndpointUrl { get; set; }
        [DataMember]
        public string ServiceBusEntityName { get; set; }
        [DataMember]
        public string Message { get; set; }
        [DataMember]
        public string MessageLabel { get; set; }
        [DataMember]
        public string SessionID { get; set; }
        [DataMember]
        public string ConsumerName { get; set; }
        [DataMember]
        public string MessageTransactionID { get; set; }
    }
}
