var builder = DistributedApplication.CreateBuilder(args);

// MongoDB е външен (вече инсталиран), затова го описваме само като connection string
var mongo = builder.AddConnectionString("mongodb");

// наставка за имената на базите: тестовете подават "_test", за да не пипат реалните данни
var dbSuffix = builder.Configuration["DbSuffix"] ?? "";

var catalog = builder.AddProject<Projects.Shop_CatalogApi>("catalog-api")
    .WithHttpHealthCheck("/health")
    .WithReference(mongo)
    .WithEnvironment("Mongo__Database", "catalogdb" + dbSuffix);

var notifications = builder.AddProject<Projects.Shop_NotificationsApi>("notifications-api")
    .WithHttpHealthCheck("/health")
    .WithReference(mongo)
    .WithEnvironment("Mongo__Database", "notificationsdb" + dbSuffix);

// Orders има референция към notifications, но НЕ чака за нея (WaitFor):
// известията са по желание, и поръчките трябва да работят и без тях
var orders = builder.AddProject<Projects.Shop_OrdersApi>("orders-api")
    .WithHttpHealthCheck("/health")
    .WithReference(mongo)
    .WithReference(catalog)
    .WithReference(notifications)
    .WithEnvironment("Mongo__Database", "ordersdb" + dbSuffix)
    .WaitFor(catalog);

builder.AddProject<Projects.Shop_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(catalog)
    .WithReference(orders)
    .WithReference(notifications)
    .WaitFor(catalog);


// фонов процес без HTTP адрес, затова няма health check
builder.AddProject<Projects.Shop_OrdersWorker>("orders-worker")
    .WithReference(orders)
    .WaitFor(orders);

builder.Build().Run();