using WorkerService.ServiceBus.Contracts;
using WorkerService.ServiceBus.Models;

namespace WorkerService.ServiceBus
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        
        private readonly IServiceProvider Services;
        
        private readonly AppSettingsModel _setting;

        public Worker(ILogger<Worker> logger, IServiceProvider services, AppSettingsModel settings)
        {
            _setting = settings;
            _logger = logger;
            Services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await DoWork(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogCritical("Error", ex);
            }
        }

        private async Task DoWork(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Consume Scoped Service Hosted Service is working.");

            using (var scope = Services.CreateScope())
            {
                var scopedServiceBusService =
                            scope.ServiceProvider
                                .GetRequiredService<IServiceBusMessage<ServiceBusClientConfigDto>>();

                var queueConfigurationList = await GetServiceBusQueueConfiguration();

                if (queueConfigurationList != null && queueConfigurationList.Count > 0)
                {
                    Parallel.ForEach(queueConfigurationList, queueData => scopedServiceBusService.CreateSessionQueueClient<ServiceBusClientConfigDto>(new List<ServiceBusClientConfigDto> { queueData }, stoppingToken));
                    _logger.LogInformation("CreateServiceBusQueueClient Method: Initialization Completed.");
                }
            }
        }

        public async Task<List<ServiceBusClientConfigDto>> GetServiceBusQueueConfiguration()
        {
            ServiceBusClientConfigDto _qconfigsDTO = new ServiceBusClientConfigDto();
            var serviceBusClientConfigList = new List<ServiceBusClientConfigDto>();

            _qconfigsDTO.ReceiverQueueName = _setting.ReceiverQueueName;
            _qconfigsDTO.ReceiverQueueEndPointUrl = _setting.ReceiverQueueEndPointUrl;

            serviceBusClientConfigList.Add(_qconfigsDTO);
            return await Task.Run(() => { return serviceBusClientConfigList; });
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Consume Scoped Service Hosted Service is started.");

            await base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Consume Scoped Service Hosted Service is stopping.");

            await base.StopAsync(cancellationToken);
        }
    }
}