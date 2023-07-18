using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using WorkerService.ServiceBus.Contracts;
using WorkerService.ServiceBus.Models;

namespace WorkerService.ServiceBus.Helper
{
    public class ServiceBusMessage<TItem> : IServiceBusMessage<TItem>
    {
        private readonly int SubMessageBodySize = 192 * 1024;
        public readonly int MaxBatchSizeInBytes = 262144;

        private readonly ILogger<ServiceBusMessage<TItem>> _logger;
        private readonly AppSettingsModel _setting;

        public ServiceBusMessage(
                    ILogger<ServiceBusMessage<TItem>> logger, AppSettingsModel settings)
        {
            _setting = settings;
            _logger = logger;
        }

        public async Task CreateSessionQueueClient<T>(IEnumerable<T> queueConfiguration, CancellationToken stoppingToken)
        {
            ServiceBusSessionProcessor processor = null;
            try
            {
                var queueConfigModel = (ServiceBusClientConfigDto)((dynamic)queueConfiguration.FirstOrDefault());

                var receiverQueueEndPointUrl = queueConfigModel.ReceiverQueueEndPointUrl;
                var receiverQueueName = queueConfigModel.ReceiverQueueName;
                var noOfConcurrentSessions = queueConfigModel.ReceiverMaxConcurrentSession;
                var prefetchCount = queueConfigModel.ReceiverPreFetchCount;

                _logger.LogInformation("QueueRegistration: " + receiverQueueEndPointUrl + "-" + receiverQueueName);

                var options = new ServiceBusClientOptions
                {
                    TransportType = ServiceBusTransportType.AmqpTcp,
                    RetryOptions = new ServiceBusRetryOptions()
                    {
                        Mode = ServiceBusRetryMode.Exponential,
                        Delay = TimeSpan.FromSeconds(30),
                        MaxRetries = 20,
                        MaxDelay = TimeSpan.FromMinutes(5),
                        TryTimeout = TimeSpan.FromSeconds(60)
                    }
                };

                await using (ServiceBusClient client = new ServiceBusClient(receiverQueueEndPointUrl, options))
                {
                    var receiverOptions = new ServiceBusSessionProcessorOptions
                    {
                        // By default after the message handler returns, the processor will complete the message
                        // If I want more fine-grained control over settlement, I can set this to false.
                        AutoCompleteMessages = false,

                        // I can also allow for processing multiple sessions
                        MaxConcurrentSessions = noOfConcurrentSessions,

                        PrefetchCount = prefetchCount,

                        MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(60),

                        // By default or when AutoCompleteMessages is set to true, the processor will complete the message after executing the message handler
                        // Set AutoCompleteMessages to false to [settle messages](https://docs.microsoft.com/en-us/azure/service-bus-messaging/message-transfers-locks-settlement#peeklock) on your own.
                        // In both cases, if the message handler throws an exception without settling the message, the processor will abandon the message.
                        MaxConcurrentCallsPerSession = 1,

                        ReceiveMode = ServiceBusReceiveMode.PeekLock

                        // Processing can be optionally limited to a subset of session Ids.
                        //SessionIds = { "my-session", "your-session" },
                    };
                    // create a processor that we can use to process the messages
                    processor = client.CreateSessionProcessor(receiverQueueName, receiverOptions);

                    // add handler to process messages
                    processor.ProcessMessageAsync += MessageHandler;

                    // add handler to process any errors
                    processor.ProcessErrorAsync += ErrorHandler;

                    // start processing 
                    await processor.StartProcessingAsync();
                    _logger.LogInformation("Start Receiving messages");

                    while (!stoppingToken.IsCancellationRequested)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }
                    // stop processing                 
                    _logger.LogInformation("Stopped Receiving messages");

                    await processor.StopProcessingAsync();

                    _logger.LogInformation("Stopped Receiving messages");
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Exception: {ex.Message}");
                await Task.Delay(10000, stoppingToken);
            }
            finally
            {
                // add handler to process messages
                processor.ProcessMessageAsync -= MessageHandler;

                // add handler to process any errors
                processor.ProcessErrorAsync -= ErrorHandler;
            }
        }
        // handle received messages
        async Task MessageHandler(ProcessSessionMessageEventArgs args)
        {
            string body = args.Message.Body.ToString();
            _logger.LogInformation($"Received: {body}");

            var IsOperationSuccess = await ProcessMessage(args.Message, args.CancellationToken);
            if (IsOperationSuccess) //delete only when msg saved in table
            {
                // complete the message. messages is deleted from the queue. 
                await args.CompleteMessageAsync(args.Message);
            }
        }

        // handle any errors when receiving messages
        Task ErrorHandler(ProcessErrorEventArgs args)
        {
            _logger.LogInformation(args.Exception.ToString());
            return Task.CompletedTask;
        }

        public async Task<int> WriteMessageToServiceBusEntity<T>(T serviceBusMessageDto, CancellationToken stoppingToken)
        {
            int numberOfSubMessages = 0;
            int subMessageNumber = 1;


            var serviceBusMessage = (ServiceBusMessageDto)((dynamic)serviceBusMessageDto);
            var msgBody = serviceBusMessage.Message;
            var sessionID = serviceBusMessage.SessionID;

            // create a Service Bus client 
            await using (ServiceBusClient client = new ServiceBusClient(serviceBusMessage.SeriveBusEndpointUrl))
            {
                // create a sender for the queue 
                ServiceBusSender sender = client.CreateSender(serviceBusMessage.ServiceBusEntityName);
                Queue<ServiceBusMessage> messages = new Queue<ServiceBusMessage>();


                var messageSize = Encoding.UTF8.GetByteCount(msgBody);
                numberOfSubMessages = (messageSize / SubMessageBodySize);

                if (messageSize % SubMessageBodySize != 0)
                {
                    numberOfSubMessages++;
                }

                Stream bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(msgBody));
                for (int streamOffest = 0; streamOffest < messageSize; streamOffest += SubMessageBodySize)
                {
                    long arraySize = (messageSize - streamOffest) > SubMessageBodySize
                        ? SubMessageBodySize : messageSize - streamOffest;

                    // Create a stream for the sub-message body.
                    byte[] subMessageBytes = new byte[arraySize];
                    int result = bodyStream.Read(subMessageBytes, 0, (int)arraySize);

                    _logger.LogInformation("Number of bytes read: " + result.ToString());

                    MemoryStream subMessageStream = new MemoryStream(subMessageBytes);
                    var subMessageByte = subMessageStream.ToArray();

                    var subMessageBody = new ServiceBusMessage(subMessageByte);

                    subMessageBody.SessionId = serviceBusMessage.SessionID;
                    subMessageBody.MessageId = Guid.NewGuid().ToString();

                    subMessageBody.ApplicationProperties.Add("MESSAGETRANSACTIONID", serviceBusMessage.MessageTransactionID);
                    subMessageBody.ApplicationProperties.Add("CONSUMERNAME", serviceBusMessage.ConsumerName);
                    subMessageBody.ApplicationProperties.Add("NUMBEROFSUBMESSAGES", numberOfSubMessages);
                    subMessageBody.ApplicationProperties.Add("SUBMESSAGENUMBER", subMessageNumber);
                    subMessageBody.Subject = serviceBusMessage.MessageLabel;
                    if (subMessageNumber == numberOfSubMessages)
                    {
                        subMessageBody.Subject = serviceBusMessage.MessageLabel + "-Final";
                    }

                    messages.Enqueue(subMessageBody);
                    subMessageNumber++;
                }
                using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled))
                {
                    // total number of messages to be sent to the Service Bus queue
                    int messageCount = messages.Count;

                    // while all messages are not sent to the Service Bus queue
                    while (messages.Count > 0)
                    {
                        // start a new batch 
                        using ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync();

                        // add the first message to the batch
                        if (messageBatch.TryAddMessage(messages.Peek()))
                        {
                            // dequeue the message from the .NET queue once the message is added to the batch
                            messages.Dequeue();
                        }
                        else
                        {
                            // if the first message can't fit, then it is too large for the batch
                            var errorMessage = $"Message for SessionID {sessionID} and {messageCount - messages.Count} is too large and cannot be sent.";
                            _logger.LogCritical(errorMessage);
                            throw new ArgumentException(errorMessage);
                        }

                        // add as many messages as possible to the current batch
                        while (messages.Count > 0 && messageBatch.TryAddMessage(messages.Peek()))
                        {
                            // dequeue the message from the .NET queue as it has been added to the batch
                            messages.Dequeue();
                        }

                        // now, send the batch
                        await sender.SendMessagesAsync(messageBatch);
                        // if there are any remaining messages in the .NET queue, the while loop repeats 
                    }
                    scope.Complete();
                }
            }


            return numberOfSubMessages;
        }

        public async Task<bool> ProcessMessage<T>(T serviceBusReceivedMessageDto, CancellationToken token)
        {
            bool operationSuccess = false;
            try
            {
                var message = (ServiceBusReceivedMessage)((dynamic)serviceBusReceivedMessageDto);

                // Copy the sub message body to the large message stream.
                var _msgLbl = message.Subject;
                var sessionID = message.SessionId;
                int _MsgTranHistoryId = 1; //API should return
                string _msgtransId = null;
                string consumerName = string.Empty;

                var messageSize = Encoding.UTF8.GetByteCount(message.Body.ToString());

                var messageEnqueuedTime = message.ScheduledEnqueueTime;

                _logger.LogInformation("ProcessMessage: Inbound message received: SessionId: " + sessionID + " MessageEnqueuedTimeUtc: " + messageEnqueuedTime + " MessageLabel: " + _msgLbl);

                    
                    string _rcvLbl = Constants.SENDERLABEL;
                    
                    //Check if its SENDER only
                    if (_msgLbl != null && _msgLbl.ToUpper().Contains(_rcvLbl.ToUpper())
                        ) 
                    {
                        bool IsFullMessageProcessed = false;
                        _rcvLbl = _rcvLbl + Constants.FINALMESSAGELABEL;
                        if (_msgLbl != null && _msgLbl.ToUpper().Contains(_rcvLbl.ToUpper()))
                            IsFullMessageProcessed = true;

                        if (message.ApplicationProperties.ContainsKey("MessageTransactionID") && !string.IsNullOrEmpty(message.ApplicationProperties["MessageTransactionID"].ToString()))
                        {
                            _msgtransId = message.ApplicationProperties["MessageTransactionID"].ToString();                            
                        }
                        
                        MsgTransactionHistoryDto msgRcvrTransHist = new MsgTransactionHistoryDto()
                        {
                            SessionID = sessionID,
                            ClientId = message.CorrelationId,
                            MessageId = message.MessageId,
                            ExpireAtUTC = message.ScheduledEnqueueTime.DateTime,
                            SequenceNo = message.SequenceNumber,
                            Size = messageSize,                           
                            Message = Encoding.UTF8.GetString(message.Body),
                            IsLastMessage = IsFullMessageProcessed,
                            Label = _msgLbl,
                            DateAdded = DateTime.Now,
                            MessageTransactionID = _msgtransId
                        };

                        //Persist each message. This can be combined in the API using the sessionID
                        //var callApiResponse = await scopedApiService.InvokeApi("POST", _setting.ApiBaseUrl, "api/Log/Transaction", JsonConvert.SerializeObject(msgRcvrTransHist));

                        //Int32.TryParse(callApiResponse, out _MsgTranHistoryId);

                        if (_MsgTranHistoryId > 0)
                            operationSuccess = true;

                        _logger.LogInformation("ProcessMessage: inbound message written to QueueMsgTransactionHistory with id: ");

                        if (_msgLbl != null && _msgLbl.ToUpper().Contains(_rcvLbl.ToUpper())
                            ) //Last Message then call the API
                        {
                            ApiModel _apiRequestStruct = new ApiModel();
                            _apiRequestStruct.Header = new Header()
                            {
                                SessionID = sessionID,
                                ClientID = message.CorrelationId,
                                MessageTransactionID = _msgtransId
                            };
                            _apiRequestStruct.ContentData = string.Empty;
                            _logger.LogInformation("ProcessMessage: call inbound api: " + JsonConvert.SerializeObject(_apiRequestStruct));

                            //In this API combine the messages using the entity MsgTransactionHistoryDto
                            //var _ApiProcessedResponse = await scopedApiService.InvokeApi("", _setting.ApiBaseUrl, "api/Process/Message", JsonConvert.SerializeObject(_apiRequestStruct));
                            string _ApiProcessedResponse = "Success";
                            if (!String.IsNullOrEmpty(_ApiProcessedResponse))
                                _logger.LogInformation("ProcessMessage: Received Api response of session id:  " + sessionID);

                            var _outMsgLabel = Constants.RECEIVERLABEL;
                            ServiceBusMessageDto serviceBusMessageDto = new ServiceBusMessageDto()
                            {
                                SessionID = sessionID,  //use same sessionid for response
                                MessageTransactionID = _msgtransId,                               
                                ConsumerName = consumerName,
                                Message = _ApiProcessedResponse,
                                MessageLabel = _outMsgLabel,
                                SeriveBusEndpointUrl = _setting.SenderQueueEndPointUrl,
                                ServiceBusEntityName = _setting.SenderQueueName
                            };

                            var _responseMsgAuditResponse = await SendResponseToReceiverQueue(serviceBusMessageDto
                                                , token);
                            if (!String.IsNullOrEmpty(_responseMsgAuditResponse))
                            {
                                _logger.LogInformation("ProcessMessage: response message written.");
                            }
                        }
                    }
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"ProcessMessage error {ex.Message}", ex);
            }
            return operationSuccess;
        }

        public async Task<string> SendResponseToReceiverQueue(ServiceBusMessageDto serviceBusMessageDto
                                                              , CancellationToken token
                                                              )
        {
            string _returnResponse = Constants.MESSAGESENTSUCCESS;
            int noofMsgSent = 0;
            string messageTransactionID = serviceBusMessageDto.MessageTransactionID;

            if (serviceBusMessageDto.Message != string.Empty && serviceBusMessageDto.MessageLabel.ToUpper().Contains("FROMRECEIVER"))
            {

                var _msgStartDate = DateTime.Now;
                var queueStatus = Constants.MESSAGESENTFAILED;
                noofMsgSent = await WriteMessageToServiceBusEntity<ServiceBusMessageDto>(serviceBusMessageDto, token);

                if (noofMsgSent > 0)
                {
                    queueStatus = Constants.MESSAGESENTSUCCESS;
                    _logger.LogInformation("SendResponseToReceiverQueue: response message sent successfully: " + serviceBusMessageDto.ServiceBusEntityName + " no of message sent: " + noofMsgSent);
                }

                ResponseAuditLogDto auditlog = new ResponseAuditLogDto()
                {
                    TaskStartTime = _msgStartDate,
                    TaskEndTime = DateTime.Now, //To be Changed using stopwatch
                    TaskResponse = serviceBusMessageDto.Message,
                    TaskStatus = queueStatus,
                    SessionId = serviceBusMessageDto.SessionID,
                    MessageTransactionID = serviceBusMessageDto.MessageTransactionID
                };
                //Commented for test purpose
                //_returnResponse = await scopedApiService.InvokeApi("POST", _setting.ApiBaseUrl, "api/Log/Message", JsonConvert.SerializeObject(auditlog));
            }
            return _returnResponse;

        }
    }
}
