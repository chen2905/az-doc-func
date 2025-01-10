using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Cosmos;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureFunctionsWebApplication(worker =>
    {
        // Add services and configuration for the worker
        worker.Services.AddSingleton<CosmosClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var cosmosConnectionString = config.GetValue<string>("CosmosDB:ConnectionString")
                ?? throw new InvalidOperationException("Cosmos DB connection string not configured.");
            Console.WriteLine("cosmosConnectionString:" + cosmosConnectionString);
            //var cosmosConnectionString = "AccountEndpoint=https://documentdatabaseaccount.documents.azure.com:443/;AccountKey=swfkGTH7sGjoKGpB2Z2ADpaW7TRiIjgfUKgaDVHcYW9P8xx2bkOdDGAOGlivFqgGadDbqfLAS8TTACDbOzj4Zw==;";
            return new CosmosClient(cosmosConnectionString);
        });
    })
    .Build();

await builder.RunAsync();
