using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService.ServiceBus.Contracts
{
    public interface IApiHelper
    {
        Task<string> InvokeApi(string action, string apiURL, string apiMethod, string JsonRequestContent);
    }
}
