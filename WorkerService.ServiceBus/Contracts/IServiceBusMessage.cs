using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkerService.ServiceBus.Models;

namespace WorkerService.ServiceBus.Contracts
{
    public interface IServiceBusMessage<TItem>
    {
        Task<int> WriteMessageToServiceBusEntity<T>(T serviceBusMessageDto, CancellationToken stoppingToken);

        Task CreateSessionQueueClient<T>(IEnumerable<T> queueConfiguration, CancellationToken stoppingToken);

        Task<bool> ProcessMessage<T>(T serviceBusReceivedMessageDto, CancellationToken token);

        Task<string> SendResponseToReceiverQueue(ServiceBusMessageDto serviceBusMessageDto                                                              
                                                              , CancellationToken token
                                                              );
    }
}
