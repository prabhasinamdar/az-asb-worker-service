using System;
using System.ComponentModel.DataAnnotations;

namespace WorkerService.ServiceBus
{
    public partial class ApiModel
    {
        public Header Header { get; set; }

        //[Required(AllowEmptyStrings = false, ErrorMessage = "Content data is not valid.")]
        public object ContentData { get; set; }
    }

    public static class ApiHttpStatus
    {
        public const string Success = "Success";
        public const string Error = "Error";
    }

    public class Header
    {       
        public string ConsumerName { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Mandatory field Session Id not populated in Header.")]
        public string SessionID { get; set; }

               
        public Status Status { get; set; }
        public string ClientID { get; set; }
        public string MessageTransactionID { get; set; }
    }

    public class Status
    {
        public string StatusCode { get; set; }
        public string StatusDescription { get; set; }
    }
}
