var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.Shop_CatalogApi>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.Shop_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.AddProject<Projects.Shop_OrdersApi>("shop-ordersapi");

builder.Build().Run();
