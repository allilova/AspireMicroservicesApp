using MongoDB.Driver;
using Shop.NotificationsApi.Data;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.AddMongoDBClient("mongodb");
builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var dbName = sp.GetRequiredService<IConfiguration>()["Mongo:Database"] ?? "notificationsdb";
    return client.GetDatabase(dbName).GetCollection<Notification>("notifications");
});

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var notifications = app.MapGroup("/notifications");

// последните известия, най-новите първи
notifications.MapGet("/", async (IMongoCollection<Notification> col, int? limit) =>
{
    var take = Math.Clamp(limit ?? 50, 1, 200);
    return await col.Find(FilterDefinition<Notification>.Empty)
        .SortByDescending(n => n.CreatedAt)
        .Limit(take)
        .ToListAsync();
});

notifications.MapPost("/", async (NotificationRequest request, IMongoCollection<Notification> col) =>
{
    if (string.IsNullOrWhiteSpace(request.Message))
        return Results.BadRequest("Съобщението е задължително.");

    var n = new Notification
    {
        Type = string.IsNullOrWhiteSpace(request.Type) ? "Info" : request.Type,
        Message = request.Message,
        OrderId = request.OrderId
    };
    await col.InsertOneAsync(n);
    return Results.Created($"/notifications/{n.Id}", n);
});

app.MapDefaultEndpoints();

app.Run();

record NotificationRequest(string? Type, string Message, string? OrderId);