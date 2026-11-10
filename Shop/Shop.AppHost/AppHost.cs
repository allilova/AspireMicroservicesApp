var builder = DistributedApplication.CreateBuilder(args);

var catalog = builder.AddProject<Projects.Shop_CatalogApi>("catalog-api")
    .WithHttpHealthCheck("/health");

var orders = builder.AddProject<Projects.Shop_OrdersApi>("orders-api")
    .WithHttpHealthCheck("/health")
    .WithReference(catalog)
    .WaitFor(catalog);

builder.AddProject<Projects.Shop_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(catalog)
    .WithReference(orders)
    .WaitFor(catalog);

builder.Build().Run();