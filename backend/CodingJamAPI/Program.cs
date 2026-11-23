using System.Text.Json;
using System.Text.Json.Serialization;
using CodingJamAPI.Extensions;
using CodingJamAPI.Hubs;
using CodingJamAPI.Infrastructure.Persistence;
using CodingJamAPI.Middleware;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Local runs outside Docker (dotnet run, Visual Studio) read the secrets from the repository .env file.
if (builder.Environment.IsDevelopment())
{
    var env = DotEnv.Read(builder.Environment.ContentRootPath);
    var overrides = new Dictionary<string, string?>();
    if (env.TryGetValue("JWT_SIGNING_KEY", out var signingKey) && signingKey.Length > 0)
    {
        overrides["Jwt:SigningKey"] = signingKey;
    }
    if (DotEnv.HostConnectionString(env) is { } connectionString)
    {
        overrides["ConnectionStrings:DefaultConnection"] = connectionString;
    }
    builder.Configuration.AddInMemoryCollection(overrides);
}

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddJwtCookieAuthentication(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// The API runs behind the nginx container: trust its X-Forwarded-* headers (client IP, scheme).
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

if ((app.Configuration["Jwt:SigningKey"] ?? string.Empty).Length < 32)
{
    app.Logger.LogWarning("Jwt:SigningKey is not configured (JWT_SIGNING_KEY in .env): using a random key, sessions end when the API restarts.");
}

// Apply pending EF Core migrations on startup (needed for docker compose up on a fresh database).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.UseForwardedHeaders();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<CsrfProtectionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<PresenceHub>("/hubs/presence");

app.MapGet("/health/db", async (AppDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct)
        ? Results.Ok(new { database = "up" })
        : Results.Problem("Database is unreachable", statusCode: 503));

app.Run();
