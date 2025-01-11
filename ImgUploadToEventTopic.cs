using System;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Microsoft.Azure.EventGrid.Models;
using Newtonsoft.Json;
using Azure.Messaging.EventGrid;

namespace az_doc_func
{
    public class ImgUploadToEventTopic
    {
        private readonly ILogger _logger;
        private readonly string _eventGridTopicEndpoint;
        private readonly string _eventGridTopicKey;
        private readonly string _cosmosDbUri;
        private readonly string _cosmosDbKey;
        private readonly string _cosmosDbDatabaseId;
        private readonly string _cosmosDbContainerId;
        private readonly string _cosmosDbConnectionString;

        public ImgUploadToEventTopic(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<ImgUploadToEventTopic>();

            // Get values from IConfiguration
            _eventGridTopicEndpoint = configuration["EventGrid:TopicEndpoint"];
            _eventGridTopicKey = configuration["EventGrid:TopicKey"];
            _cosmosDbUri = configuration["CosmosDB:URI"];
            _cosmosDbKey = configuration["CosmosDB:Key"];
            _cosmosDbDatabaseId = configuration["CosmosDB:DatabaseId"];
            _cosmosDbContainerId = configuration["CosmosDB:ContainerId"];
            _cosmosDbConnectionString = configuration["CosmosDB:ConnectionString"];
        }

        [Function("ImgUploadToEventTopic")]
        public async Task Run(
        [CosmosDBTrigger(
            databaseName: "DocumentDB",  // Use direct value from configuration, no `%` placeholders
            containerName: "documentmetacontainer", // Use direct value
            Connection= "CosmosDB:ConnectionString",
            LeaseContainerName = "leases",
            CreateLeaseContainerIfNotExists = true)] IReadOnlyList<MyDocument> input)
        {
            if (input != null && input.Count > 0)
            {
                _logger.LogInformation("Documents modified: " + input.Count);
                _logger.LogInformation("First document Id: " + input[0].id);

                // Serialize the document metadata to JSON
                var jsonData = JsonConvert.SerializeObject(input[0]);

                // Create BinaryData from the serialized JSON
                var binaryData = new BinaryData(jsonData);

                // Create the EventGridEvent
                var eventGridEvent = new Azure.Messaging.EventGrid.EventGridEvent(
                    subject: "Image uploaded",
                    eventType: "Image uploaded",
                    dataVersion: "1.0",
                    data: binaryData // Use BinaryData here
                );
                await SendEventToEventGrid(eventGridEvent);
                 
            }
        }

        private async Task SendEventToEventGrid(Azure.Messaging.EventGrid.EventGridEvent eventGridEvent)
        {
            // Create Event Grid client and prepare the request
            var eventGridClient = new EventGridPublisherClient(new Uri(_eventGridTopicEndpoint), new AzureKeyCredential(_eventGridTopicKey));

            try
            {
                // Send the event to Event Grid
                await eventGridClient.SendEventAsync(eventGridEvent);
                _logger.LogInformation("Event sent to Event Grid successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending event to Event Grid: {ex.Message}");
            }
        }
    }

    public class MyDocument
    {
        public string id { get; set; }
        public string userId { get; set; }
        public string fileName { get; set; }
        public string documentUrl { get; set; }
        public DateTime uploadDate { get; set; }
        public DateTime expirationDate { get; set; }
    }
}
