using System;
using System.Runtime.Serialization;

namespace WorkerService.ServiceBus
{
    [DataContract]
    public partial class ResponseAuditLogDto
    {
        [DataMember]
        public string TaskName { get; set; }
        [DataMember]
        public Nullable<System.DateTime> TaskStartTime { get; set; }
        [DataMember]
        public Nullable<System.DateTime> TaskEndTime { get; set; }
        [DataMember]
        public string SessionId { get; set; }
        [DataMember]
        public string TaskResponse { get; set; }
        [DataMember]
        public string TaskStatus { get; set; }
        [DataMember]
        public string TaskException { get; set; }
        [DataMember]
        public string MessageTransactionID { get; set; }
        
    }
}
