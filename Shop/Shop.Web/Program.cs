using MudBlazor.Services;
using Shop.Web;
using Shop.Web.Components;
using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;
using Shop.Web.Date;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOutputCache();

builder.Services.AddHttpClient<CatalogApiClient>(c => c.BaseAddress = new("https+http://catalog-api"));
builder.Services.AddHttpClient<OrdersApiClient>(c => c.BaseAddress = new("https+http://orders-api"));
builder.Services.AddHttpClient<NotificationsApiClient>(c => c.BaseAddress = new("https+http://notifications-api"));
builder.Services.AddScoped<CartService>();
builder.AddMongoDBClient("mongodb");
builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var dbName = sp.GetRequiredService<IConfiguration>()["Mongo:Database"] ?? "usersdb";
    return client.GetDatabase(dbName).GetCollection<AppUser>("users");
});
builder.Services.AddSingleton<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();

builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", o =>
    {
        o.LoginPath = "/login";
        o.AccessDeniedPath = "/access-denied";
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddMudServices();


var app = builder.Build();
var users = app.Services.GetRequiredService<IMongoCollection<AppUser>>();
await users.Indexes.CreateOneAsync(new CreateIndexModel<AppUser>(
    Builders<AppUser>.IndexKeys.Ascending(u => u.Email),
    new CreateIndexOptions { Unique = true }));

if (!await users.Find(u => u.Role == "Admin").AnyAsync())
{
    var hasher = app.Services.GetRequiredService<IPasswordHasher<AppUser>>();
    var admin = new AppUser { Email = "admin@shop.local", FullName = "Администратор", Role = "Admin" };
    admin.PasswordHash = hasher.HashPassword(admin, "Admin123!");
    await users.InsertOneAsync(admin);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
