var builder = DistributedApplication.CreateBuilder(args);


var mongo = builder.AddConnectionString("mongodb");


var dbSuffix = builder.Configuration["DbSuffix"] ?? "";

var catalog = builder.AddProject<Projects.Shop_CatalogApi>("catalog-api")
    .WithHttpHealthCheck("/health")
    .WithReference(mongo)
    .WithEnvironment("Mongo__Database", "catalogdb" + dbSuffix);

var orders = builder.AddProject<Projects.Shop_OrdersApi>("orders-api")
    .WithHttpHealthCheck("/health")
    .WithReference(mongo)
    .WithReference(catalog)
    .WithEnvironment("Mongo__Database", "ordersdb" + dbSuffix)
    .WaitFor(catalog);

builder.AddProject<Projects.Shop_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(catalog)
    .WithReference(orders)
    .WaitFor(catalog);

builder.Build().Run();