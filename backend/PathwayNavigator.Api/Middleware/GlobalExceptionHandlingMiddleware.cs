using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace PathwayNavigator.Api.Middleware;

/// <summary>
/// Converts every unhandled exception into a clean JSON problem response.
///
/// Why this exists: without an explicit handler, ASP.NET Core answers with the developer exception
/// page in Development, and the SPA rendered that whole HTML/text dump (stack trace, request
/// headers, connection error) inside its error banner for a single transient database blip.
///
/// Transient failures - a dropped PostgreSQL connection, a dead socket, a database timeout, the
/// Python AI service being unreachable - are reported as 503/502/504 with a "please try again"
/// message, because the exact same request already succeeds on a retry. Everything else becomes a
/// generic 500 so internals never leak. Full details are always logged server-side together with
/// the trace id returned to the client.
/// </summary>
public sealed class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // The browser closed the tab / navigated away: there is nobody left to answer.
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogDebug(
                "Request {Method} {Path} was cancelled by the client.",
                context.Request.Method,
                context.Request.Path);
            return;
        }

        if (context.Response.HasStarted)
        {
            _logger.LogError(
                exception,
                "Unhandled exception after the response had started for {Method} {Path}; aborting the connection.",
                context.Request.Method,
                context.Request.Path);
            context.Abort();
            return;
        }

        var failure = Classify(exception);
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var retryable = failure != FailureKind.Unexpected;

        _logger.LogError(
            exception,
            "Unhandled {FailureKind} failure for {Method} {Path}. TraceId: {TraceId}",
            failure,
            context.Request.Method,
            context.Request.Path,
            traceId);

        var problem = new ProblemDetails
        {
            Status = StatusCodeFor(failure),
            Title = TitleFor(failure),
            Detail = DetailFor(failure),
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = traceId;

        // Helpful while developing; never a stack trace, and never sent in Production.
        if (_environment.IsDevelopment())
        {
            problem.Extensions["exception"] = exception.GetType().Name;
            problem.Extensions["exceptionMessage"] = exception.Message;
        }

        // The response has not started, so the status/headers can still be replaced. Anything an
        // inner middleware set for the failed attempt (e.g. Content-Length) must not survive.
        context.Response.StatusCode = problem.Status.Value;
        context.Response.ContentType = "application/problem+json";
        context.Response.Headers.Remove("Content-Length");
        if (retryable)
        {
            context.Response.Headers["Retry-After"] = "3";
        }

        await context.Response.WriteAsJsonAsync(problem);
    }

    internal enum FailureKind
    {
        /// <summary>Something unexpected: a bug, an unhandled business rule, a SQL error.</summary>
        Unexpected,

        /// <summary>A dropped/closed PostgreSQL connection (EndOfStreamException and friends).</summary>
        DatabaseUnavailable,

        /// <summary>The Python AI microservice could not be reached.</summary>
        AiServiceUnavailable,

        /// <summary>An operation ran out of time (slow AI call, slow query).</summary>
        Timeout
    }

    /// <summary>
    /// Walks the whole inner-exception chain because Npgsql, HttpClient and EF Core each wrap the
    /// original error at least once. The outermost matching type wins, which keeps e.g. an
    /// <see cref="HttpRequestException"/> from being reported as a database problem.
    /// </summary>
    internal static FailureKind Classify(Exception? exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            switch (current)
            {
                case HttpRequestException:
                    return FailureKind.AiServiceUnavailable;

                // EndOfStreamException is an IOException: the socket was closed while reading.
                case IOException:
                case SocketException:
                    return FailureKind.DatabaseUnavailable;

                // Npgsql connection-level errors (PostgresException means a real SQL error such as a
                // constraint violation, which must not be reported as "temporarily unavailable").
                case NpgsqlException when current is not PostgresException:
                    return FailureKind.DatabaseUnavailable;

                // HttpClient/EF timeouts and tokens cancelled by an internal time budget.
                case OperationCanceledException:
                case TimeoutException:
                    return FailureKind.Timeout;
            }
        }

        return FailureKind.Unexpected;
    }

    private static int StatusCodeFor(FailureKind failure) => failure switch
    {
        FailureKind.DatabaseUnavailable => StatusCodes.Status503ServiceUnavailable,
        FailureKind.AiServiceUnavailable => StatusCodes.Status502BadGateway,
        FailureKind.Timeout => StatusCodes.Status504GatewayTimeout,
        _ => StatusCodes.Status500InternalServerError
    };

    private static string TitleFor(FailureKind failure) => failure switch
    {
        FailureKind.DatabaseUnavailable => "The service is temporarily unavailable.",
        FailureKind.AiServiceUnavailable => "The career assistant is temporarily unavailable.",
        FailureKind.Timeout => "The request took too long to complete.",
        _ => "An unexpected error occurred while processing the request."
    };

    private static string DetailFor(FailureKind failure) => failure switch
    {
        FailureKind.DatabaseUnavailable =>
            "The database connection was interrupted. Your request was not applied and can safely be retried.",
        FailureKind.AiServiceUnavailable =>
            "The AI service did not respond. Please try again in a moment.",
        FailureKind.Timeout =>
            "The operation exceeded its time budget. Please try again.",
        _ => "Please try again. If the problem continues, report the reference id below."
    };
}
