using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService.ServiceBus.Contracts
{
    public interface IScopedService
    {
        Task DoWork(CancellationToken cancellationToken);
    }
}
