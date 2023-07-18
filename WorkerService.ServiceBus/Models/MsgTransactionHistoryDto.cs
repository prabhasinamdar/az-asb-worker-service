using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService.ServiceBus
{
    [DataContract]
    public partial class MsgTransactionHistoryDto
    {
        [DataMember]
        public long MsgTransactionHistoryId { get; set; }
        [DataMember]
        public string SessionID { get; set; }
        [DataMember]
        public string ClientId { get; set; }
        
        [DataMember]
        public string MessageId { get; set; }
        [DataMember]
        public string Message { get; set; }
        [DataMember]
        public string Label { get; set; }
        [DataMember]
        public Nullable<long> Size { get; set; }
        [DataMember]
        public bool IsLastMessage { get; set; }
        [DataMember]
        public System.DateTime ExpireAtUTC { get; set; }
        [DataMember]
        public Nullable<long> SequenceNo { get; set; }
        [DataMember]
        public Nullable<System.DateTime> DateAdded { get; set; }
        [DataMember]
        public string MessageTransactionID { get; set; }
    }
}
