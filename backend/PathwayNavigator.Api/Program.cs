using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PathwayNavigator.Api.Data;
using PathwayNavigator.Api.Middleware;
using PathwayNavigator.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Add standard services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS for Web / Mobile / React SPA
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrEmpty(origin)) return false;
                var host = new Uri(origin).Host;
                return host == "localhost" || host == "127.0.0.1";
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
        }
        else
        {
            var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:5173", "http://localhost:3000" };
            policy.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

// 2. Configure PostgreSQL Database.
//    Resilient against the dropped idle connections of managed providers (Aiven, Neon, ...):
//    pool recycling + automatic retries for transient failures. See DatabaseConfiguration.
builder.Services.AddResilientPostgres(builder.Configuration);

// 3. SECURITY FIRST: Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secret = jwtSettings["Secret"]
    ?? throw new InvalidOperationException("JwtSettings:Secret is not configured.");
var secretKey = Encoding.UTF8.GetBytes(secret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(secretKey)
    };
});

// Add Authorization policies
builder.Services.AddAuthorization();

// Register Application Services
builder.Services.AddHttpClient<IAgentService, AgentService>(client =>
{
    var baseUrl = builder.Configuration["AiServiceSettings:BaseUrl"] ?? "http://localhost:8000";
    client.BaseAddress = new Uri(baseUrl);
    // LLM + market-data calls inside the agents can be slow; keep this below the SPA's 120s axios timeout.
    var timeoutSeconds = builder.Configuration.GetValue<int?>("AiServiceSettings:TimeoutSeconds") ?? 90;
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
});

builder.Services.AddScoped<IStudentProfileService, StudentProfileService>();
builder.Services.AddScoped<IPathwayAnalysisService, PathwayAnalysisService>();
builder.Services.AddScoped<IPathwayPlanService, PathwayPlanService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
// Register Member 4 Review Service
builder.Services.AddScoped<ICounsellorReviewService, CounsellorReviewService>();

// Consultant support layer: context snapshots, the student consultation workflow, the consultant desk
// and the notification spine every workflow publishes into.
builder.Services.AddScoped<IContextSnapshotResolver, ContextSnapshotResolver>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<INotificationPublisher, DbNotificationPublisher>();
builder.Services.AddScoped<IConsultationService, ConsultationService>();
builder.Services.AddScoped<IConsultantQueueService, ConsultantQueueService>();
builder.Services.AddScoped<IConsultantAccountService, ConsultantAccountService>();

// Keeps a PostgreSQL connection warm so the first request after an idle period is not the one
// that discovers the server has already closed the pooled connection.
builder.Services.AddHostedService<DatabaseKeepAliveHostedService>();

// Watches consultation SLA clocks: reminds the assigned consultant, escalates unclaimed P1 cases to
// Admin, and quietly expires cases the student never returned to.
builder.Services.AddHostedService<ConsultationSlaHostedService>();

var app = builder.Build();

// FIRST middleware: turn unhandled exceptions into JSON (no .NET stack traces in the SPA) and
// report transient database drops as 503 instead of an opaque 500.
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// Enable CORS
app.UseCors("AllowReactApp");

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

// In Development the SPA calls the API over plain HTTP (http://localhost:5081). Redirecting to HTTPS
// would answer CORS pre-flight (OPTIONS) requests with a 307 and the browser would block every call.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// 4. PIPELINE SECURITY: Authentication MUST come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();








// Auto-seed default roles if missing.
// A cloud database can be briefly unreachable (or still waking up) while the API boots, so a
// failure here must not stop the application: requests retry transient errors on their own.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PathwayNavigator.Api.Data.AppDbContext>();
    var startupLogger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("PathwayNavigator.Startup");

    try
    {
        if (app.Environment.IsDevelopment() && app.Configuration.GetValue("Database:AutoMigrate", true))
        {
            // Saves a manual `dotnet ef database update` on a fresh local database; no-op when up to date.
            dbContext.Database.Migrate();
        }
        // Per-role upsert instead of the previous all-or-nothing check: the old `if (!Roles.Any("Student"))`
        // guard meant a database created before a new role existed would never receive it. Each missing
        // role is now seeded on its own, so adding a role (e.g. Consultant) is safe on a live database.
        var requiredRoles = new[] { "Student", "Counsellor", "Consultant", "Admin" };
        var existingRoles = dbContext.Roles.Select(r => r.Name).ToList();
        var missingRoles = requiredRoles.Where(name => !existingRoles.Contains(name)).ToList();
        if (missingRoles.Count > 0)
        {
            foreach (var roleName in missingRoles)
            {
                dbContext.Roles.Add(new PathwayNavigator.Api.Models.Role
                {
                    Id = Guid.NewGuid(),
                    Name = roleName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            dbContext.SaveChanges();
        }

        // Students must never be created as staff by the public register endpoint. If a database was
        // seeded before this guard existed nothing needs fixing here - the endpoint below is the gate.
        if (!dbContext.Roles.Any(r => r.Name == "Consultant"))
        {
            startupLogger.LogWarning("The Consultant role is still missing after seeding; the consultant desk will be unreachable.");
        }
    }
    catch (Exception ex)
    {
        startupLogger.LogError(
            ex,
            "Database preparation failed during startup. The API will keep running; requests retry transient failures automatically.");
    }
}









app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
