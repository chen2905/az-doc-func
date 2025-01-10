using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Cosmos;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;

public class GenerateDownloadUrlFunction
{
    private readonly CosmosClient _cosmosClient;
    private readonly Microsoft.Azure.Cosmos.Container _container;
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;

    public GenerateDownloadUrlFunction(CosmosClient cosmosClient, IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        _cosmosClient = cosmosClient;
        _configuration = configuration;
        _logger = loggerFactory.CreateLogger<GenerateDownloadUrlFunction>();

        // Retrieve Cosmos DB container information from configuration
        var databaseId = _configuration["CosmosDB:DatabaseId"];
        var containerId = _configuration["CosmosDB:ContainerId"];
        _logger.LogInformation($"CosmosDB URI: {_configuration["CosmosDB:URI"]}");
        _logger.LogInformation($"CosmosDB DatabaseId: {_configuration["CosmosDB:DatabaseId"]}");

        // Initialize Cosmos container
        _container = _cosmosClient.GetContainer(databaseId, containerId);
    }

    [Function("UpdateUrlToMetaData")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "generate-url")] HttpRequestData req)
    {
        _logger.LogInformation("Processing request to generate URL and update metadata...");

        // Parse the incoming metadata (assumed to be in JSON format)
        var requestBody = await req.ReadAsStringAsync();
        var metadata = JsonConvert.DeserializeObject<DocumentMetadata>(requestBody);

        // Retrieve the base URL for blob storage from settings
        //var blobBaseUrl = _configuration["BlobStorage:BaseUrl"];
        var cdnBaseUrl = _configuration["CDN:BaseUrl"];
        // Generate the document URL by appending the file name to the base URL
        var documentUrl = $"{cdnBaseUrl}{metadata.fileName}";

        // Generate expiration date (24 hours from the current time)
        var expirationDate = DateTime.UtcNow.AddHours(24);

        // Step 1: Retrieve the existing document from Cosmos DB by 'id'
        var response = await _container.ReadItemAsync<DocumentMetadata>(
            metadata.id,                      // Item ID
            new PartitionKey(metadata.userId) // Partition key (userId)
        );
        // Step 2: Update the document with the new URL and expiration date
        var updatedMetadata = response.Resource;
        updatedMetadata.documentUrl = documentUrl;
        updatedMetadata.expirationDate = expirationDate;

        // Step 3: Replace the existing item in Cosmos DB with the updated metadata
        await _container.ReplaceItemAsync(updatedMetadata, updatedMetadata.id, new PartitionKey(updatedMetadata.userId));

        // Create the response
        var responseMessage = req.CreateResponse(HttpStatusCode.OK);
        await responseMessage.WriteAsJsonAsync(updatedMetadata);

        return responseMessage;
    }
}

public class DocumentMetadata
{
    public string id { get; set; }

    public string userId { get; set; }
    public string fileName { get; set; }
    public string documentUrl { get; set; }
    public DateTime uploadDate { get; set; }
    public DateTime expirationDate { get; set; } // Expiration date added here
}
