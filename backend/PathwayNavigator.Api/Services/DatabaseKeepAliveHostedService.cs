using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PathwayNavigator.Api.Data;

namespace PathwayNavigator.Api.Services;

/// <summary>
/// Runs a cheap "SELECT 1" against PostgreSQL right after startup and then every few minutes.
///
/// On managed databases (Aiven &amp; co.) the first request after a long quiet period is the one that
/// used to fail, because the server had already dropped the pooled connection. This keeps a
/// connection warm and, more importantly, surfaces database outages in the log instead of leaving
/// them for the next user request. Failures are logged and swallowed - the API stays up either way.
/// </summary>
public sealed class DatabaseKeepAliveHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DatabaseKeepAliveHostedService> _logger;
    private readonly TimeSpan _interval;
    private readonly bool _enabled;

    public DatabaseKeepAliveHostedService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<DatabaseKeepAliveHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _enabled = configuration.GetValue("Database:KeepAliveEnabled", true);

        // Default 240s: below the idle-session timeout that managed providers usually apply.
        var seconds = configuration.GetValue("Database:KeepAliveSeconds", 240);
        _interval = TimeSpan.FromSeconds(Math.Clamp(seconds, 30, 3600));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enabled)
        {
            return;
        }

        await PingAsync(stoppingToken);

        try
        {
            using var timer = new PeriodicTimer(_interval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await PingAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }

    private async Task PingAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "PostgreSQL keep-alive ping failed. The API keeps running; the next request will open a fresh connection.");
        }
    }
}
