namespace WorkerService.ServiceBus
{
    public class AppSettingsModel
    {
        public double ApiConnectionTimeOut { get; set; } = 30;
        public string ApiBaseUrl { get; set; }= string.Empty;
        public string WinServiceHostName { get; set; } = string.Empty;
        public string CRONJobSchedule { get; set; } = string.Empty;
        public string ReceiverQueueName { get; set; } = string.Empty;
        public string ReceiverQueueEndPointUrl { get; set; } = string.Empty;
        public string SenderQueueName { get; set; } = string.Empty;
        public string SenderQueueEndPointUrl { get; set; } = string.Empty;
    }
}