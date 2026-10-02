# Transient database failures (cloud PostgreSQL)

## Symptom

The first sign-in (or any request) after a quiet period fails with:

```
System.InvalidOperationException: An exception has been raised that is likely due to a transient failure.
 ---> Npgsql.NpgsqlException (0x80004005): Exception while reading from stream
 ---> System.IO.EndOfStreamException: Attempted to read past the end of the stream.
   at Npgsql.Internal.NpgsqlReadBuffer...
   at PathwayNavigator.Api.Services.AuthService.LoginAsync(...)
```

and the very same click succeeds immediately afterwards.

## Why it happens

Managed PostgreSQL providers (Aiven, Neon, Supabase, Azure Database, ...) close connections that
have been idle for a while - through an idle-session timeout, a PgBouncer/proxy idle timeout, a
maintenance restart or a failover. Npgsql keeps those (now dead) sockets in its connection pool, so
the next request can be handed a socket that the server already closed. The read then hits EOF, and
EF Core wraps it as "an exception ... likely due to a transient failure".

That is why a second click works: the dead connection is discarded and the next attempt opens a
fresh one.

## What this repository does about it

| Layer | Change | File |
| --- | --- | --- |
| Connection pool | Recycle idle pooled connections (`Connection Idle Lifetime=60`, `Connection Pruning Interval=10`), keepalive query every 30s (`Keepalive=30`), `Maximum Pool Size=20`, connect `Timeout=15` - only applied when the connection string does not already set them | `backend/PathwayNavigator.Api/Data/DatabaseConfiguration.cs` |
| EF Core | `EnableRetryOnFailure(maxRetryCount, maxRetryDelay)` so a dropped connection is transparently retried instead of failing the request | same file |
| Manual transactions | The only `BeginTransactionAsync` (reality-check review insert) now runs through `Database.CreateExecutionStrategy()`, which EF requires once retries are enabled | `backend/PathwayNavigator.Api/Services/CounsellorReviewService.cs` |
| Warm-up / monitoring | `DatabaseKeepAliveHostedService` pings `SELECT 1` at startup and every `Database:KeepAliveSeconds` (default 240s) | `backend/PathwayNavigator.Api/Services/DatabaseKeepAliveHostedService.cs` |
| Startup | Migration + role seeding no longer crash the API when the database is briefly unreachable at boot | `backend/PathwayNavigator.Api/Program.cs` |
| Error responses | `GlobalExceptionHandlingMiddleware` maps transient failures to `503` (database), `502` (AI service) or `504` (timeout) with `application/problem+json`; everything else becomes a generic `500`. Stack traces are only logged, never returned | `backend/PathwayNavigator.Api/Middleware/GlobalExceptionHandlingMiddleware.cs` |
| Web client | 5xx responses get a "server problem, please try again" message instead of the caller's fallback; raw server dumps/HTML are never rendered; `GET` requests and sign-in (`POST`) are retried once or twice when the API is briefly unreachable | `web/PathwayNavigator/src/utils/apiError.js`, `src/api/apiClient.js`, `src/api/authApi.js` |
| Flutter client | Same message sanitising as the web client (`extractErrorMessage`) | `mobile/PathwayNavigator/lib/core/network/api_client.dart` |

## Configuration

All optional; defaults are shown in `appsettings.example.json` under `Database`:

```json
"Database": {
  "AutoMigrate": true,
  "MaxRetryCount": 4,
  "MaxRetryDelaySeconds": 5,
  "CommandTimeoutSeconds": null,
  "KeepAliveEnabled": true,
  "KeepAliveSeconds": 240
}
```

Pool settings can also be overridden per deployment directly in the connection string, e.g.

```
Host=...;Port=12170;Database=defaultdb;Username=...;Password=...;SslMode=Require;Maximum Pool Size=10;Connection Idle Lifetime=30;
```

Useful values for small cloud plans:

- `Maximum Pool Size` - keep it clearly below the connection limit of the plan. Several app
  instances share that limit.
- `Connection Idle Lifetime` - lower it if the provider is known to drop connections aggressively.
- `KeepAliveEnabled` / `KeepAliveSeconds` - raise the interval for serverless databases that scale
  to zero, or set `KeepAliveEnabled=false` to disable the background ping entirely.

## Verifying the fix

1. Start the API, sign in, wait ~10 minutes (longer than the provider's idle timeout) and sign in
   again with a cold browser tab.
2. Watch the API log: a recovered connection shows as a successful request; if a connection really
   was dead, EF Core logs the retry and the request still returns `200`.
3. Force an unreachable database (stop the DB or use a wrong host) and call any endpoint: the client
   now receives a `503` problem response with a friendly message instead of a developer stack trace.
