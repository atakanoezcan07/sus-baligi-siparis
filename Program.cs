using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SusBaligiSiparis.Data;

var builder = WebApplication.CreateBuilder(args);

// Railway/Render inject the port to bind to via PORT.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var connectionString = ResolveConnectionString(builder.Configuration);
builder.Services.AddDbContext<SiparisDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddRazorPages();

// Halka açık, girişsiz bir site: IP başına genel bir hız sınırlama - vergi no sorgulama
// uç noktası, sınır olmasa müşteri listesini taramak için kötüye kullanılabilir.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "bilinmeyen",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseRateLimiter();

app.MapRazorPages();

app.Run();

// Ana SusBaligiTakip uygulamasındaki mantığın birebir aynısı - iki uygulama da aynı
// Railway Postgres eklentisine bağlanır, bu yüzden aynı ortam değişkeni zincirini izlemeli.
static string ResolveConnectionString(IConfiguration configuration)
{
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (!string.IsNullOrEmpty(databaseUrl))
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':', 2);
        var fromUrl = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Username = userInfo[0],
            Password = userInfo.Length > 1 ? userInfo[1] : string.Empty,
            Database = uri.AbsolutePath.TrimStart('/'),
            SslMode = Npgsql.SslMode.Require,
        };
        return fromUrl.ConnectionString;
    }

    var pgHost = Environment.GetEnvironmentVariable("PGHOST");
    if (!string.IsNullOrEmpty(pgHost))
    {
        var fromPgVars = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = pgHost,
            Port = int.TryParse(Environment.GetEnvironmentVariable("PGPORT"), out var p) ? p : 5432,
            Username = Environment.GetEnvironmentVariable("PGUSER"),
            Password = Environment.GetEnvironmentVariable("PGPASSWORD"),
            Database = Environment.GetEnvironmentVariable("PGDATABASE"),
            SslMode = Npgsql.SslMode.Require,
        };
        return fromPgVars.ConnectionString;
    }

    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PORT")))
    {
        throw new InvalidOperationException(
            "No DATABASE_URL or PGHOST environment variable found. " +
            "In Railway, open this service's Variables tab and add DATABASE_URL " +
            "referencing the Postgres service's DATABASE_URL (same one the main app uses).");
    }

    return configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}
