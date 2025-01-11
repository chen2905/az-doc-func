using System;
using System.Text.Json;
using Azure.Messaging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;


namespace az_doc_func
{
    public class ImgUploadSubscription
    {
        private readonly ILogger<ImgUploadSubscription> _logger;

        public ImgUploadSubscription(ILogger<ImgUploadSubscription> logger)
        {
            _logger = logger;
        }

        [Function("ImgUploadSubscription")]
        public void Run([EventGridTrigger] CloudEvent cloudEvent)
        {
            // Log a warning when an image upload event occurs
            _logger.LogWarning("Wow, someone uploaded an image!");

            // Optional: Log the full event details
            string eventData = JsonSerializer.Serialize(cloudEvent.Data);
            _logger.LogInformation($"Event Details: {eventData}");
        }
    }
}
