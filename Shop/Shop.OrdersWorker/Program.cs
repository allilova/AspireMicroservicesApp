using Shop.OrdersWorker;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

builder.Services.Configure<WorkerOptions>(builder.Configuration.GetSection("Worker"));
builder.Services.AddHttpClient("orders", c => c.BaseAddress = new("https+http://orders-api"));
builder.Services.AddHostedService<OrderProgressWorker>();

var host = builder.Build();
host.Run();