using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace PathwayNavigator.Api.Data;

/// <summary>
/// Central place for everything that makes the API resilient against the connection drops that
/// managed / cloud PostgreSQL providers produce.
///
/// Providers such as Aiven, Neon or Supabase close connections that have been idle for a while
/// (idle-session timeout, maintenance restart, failover, PgBouncer in front of the database).
/// Npgsql keeps those sockets in its connection pool, so the next request can be handed a socket
/// that is already dead. The user then sees:
///
///   Npgsql.NpgsqlException ---> System.IO.EndOfStreamException: Attempted to read past the end of the stream
///   System.InvalidOperationException: An exception has been raised that is likely due to a transient failure
///
/// for a request that succeeds on the very next click. Two settings take that away:
///   1. Pooled connections are recycled long before the server can close them (idle lifetime), and
///      half-open sockets are detected via keep-alives.
///   2. EF Core retries a failed operation automatically (EnableRetryOnFailure), so a connection
///      drop that still slips through never reaches the browser.
///
/// Anything explicitly set in the connection string (appsettings, user secrets, environment) wins
/// over the defaults applied here.
/// </summary>
public static class DatabaseConfiguration
{
    public const string FallbackConnectionString =
        "Host=localhost;Port=5432;Database=PathwayNavigatorDb;Username=postgres;Password=postgres";

    /// <summary>Registers <see cref="AppDbContext"/> with pool recycling and transient-failure retries.</summary>
    public static IServiceCollection AddResilientPostgres(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = BuildResilientConnectionString(
            configuration.GetConnectionString("DefaultConnection") ?? FallbackConnectionString);

        var maxRetryCount = configuration.GetValue("Database:MaxRetryCount", 4);
        var maxRetryDelaySeconds = configuration.GetValue("Database:MaxRetryDelaySeconds", 5);
        var commandTimeoutSeconds = configuration.GetValue<int?>("Database:CommandTimeoutSeconds");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                // Transparently re-run an operation whose connection died mid-flight (EndOfStream,
                // socket reset, failover, ...). Delays stay short so a request is never left hanging.
                // Npgsql's provider overload takes no optional parameters, hence the explicit null
                // for "no extra SQLSTATE codes considered transient".
                npgsql.EnableRetryOnFailure(
                    maxRetryCount,
                    TimeSpan.FromSeconds(maxRetryDelaySeconds),
                    errorCodesToAdd: null);
                npgsql.CommandTimeout(commandTimeoutSeconds);
            }));

        return services;
    }

    /// <summary>
    /// Applies safe pool defaults to a connection string without overwriting explicit settings.
    /// </summary>
    public static string BuildResilientConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return FallbackConnectionString;
        }

        NpgsqlConnectionStringBuilder builder;
        try
        {
            builder = new NpgsqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException)
        {
            // Malformed connection string: leave it untouched so Npgsql reports the real problem.
            return connectionString;
        }

        // 1. Never hand out a connection that has been idle long enough for the server to close it.
        //    Managed providers commonly drop idle sessions after 5-15 minutes; 60s is safely below.
        if (!HasKeyword(connectionString, "Connection Idle Lifetime"))
        {
            builder.ConnectionIdleLifetime = 60;
        }

        if (!HasKeyword(connectionString, "Connection Pruning Interval"))
        {
            builder.ConnectionPruningInterval = 10;
        }

        // 2. Send a lightweight keepalive query during idle periods: the server no longer sees the
        //    session as idle, and a dead socket is noticed instead of blocking the next request.
        if (!HasKeyword(connectionString, "Keepalive"))
        {
            builder.KeepAlive = 30;
        }

        // 3. Small cloud plans cap concurrent connections; stay well inside that limit.
        //    (Connection-string keyword: "Maximum Pool Size"; builder property: MaxPoolSize.)
        if (!HasKeyword(connectionString, "Maximum Pool Size"))
        {
            builder.MaxPoolSize = 20;
        }

        if (!HasKeyword(connectionString, "Timeout"))
        {
            builder.Timeout = 15;
        }

        return builder.ConnectionString;
    }

    /// <summary>
    /// Case-insensitive check for "Keyword=value" inside a raw connection string. Spaces are ignored
    /// so both "Keep Alive=30" and "Keepalive=30" are recognised as the same explicit setting.
    /// </summary>
    private static bool HasKeyword(string connectionString, string keyword)
    {
        var expected = keyword.Replace(" ", string.Empty);

        foreach (var segment in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = segment.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var actual = segment[..separator].Replace(" ", string.Empty);
            if (string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
