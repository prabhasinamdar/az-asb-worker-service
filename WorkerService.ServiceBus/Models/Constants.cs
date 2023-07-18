using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService.ServiceBus
{
    public static class Constants
    {
        public const string FINALMESSAGELABEL = "-Final";
        public const string OUTBOUNDTASKNAME = "Sent Outbound Response message to queue";
        public const string MESSAGESENTSUCCESS = "MESSAGE SENT SUCCESSFULLY";
        public const string MESSAGESENTFAILED = "MESSAGE FAILED TO SEND";
        public const string BrokeredMessageListCannotBeNullOrEmpty = "The brokeredMessageEnumerable parameter cannot be null or empty.";        
        public const string SENDERLABEL = "FROMSENDER";
        public const string RECEIVERLABEL = "FROMRECEIVER";
    }
}
