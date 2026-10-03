# syntax=docker/dockerfile:1
#
# ASP.NET Core 8 Web API.
#
# Build context is the `backend/` directory (NOT the repo root), so that the
# .NET SDK image never has to receive `web/`, `mobile/` or `ai_service/`.
#
#   docker build -f backend/Dockerfile -t pn-api backend/
#
# There is deliberately no `dotnet ef database update` here. The runtime image
# carries no EF tools, and migrations are applied by the deploy pipeline instead
# (see .github/workflows/deploy.yml). Program.cs only auto-migrates when
# ASPNETCORE_ENVIRONMENT=Development (Program.cs:165), so production needs that
# pipeline step or the database stays empty.

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore against the csproj alone first: the Api project has no ProjectReference,
# so this layer survives every source-only change.
COPY PathwayNavigator.Api/PathwayNavigator.Api.csproj PathwayNavigator.Api/
RUN dotnet restore PathwayNavigator.Api/PathwayNavigator.Api.csproj

COPY PathwayNavigator.Api/ PathwayNavigator.Api/
RUN dotnet publish PathwayNavigator.Api/PathwayNavigator.Api.csproj \
        --configuration Release \
        --output /app/publish \
        /p:UseAppHost=false \
        /p:DebugType=None \
        /p:DebugSymbols=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# ASPNETCORE_ENVIRONMENT=Production makes Program.cs take the non-development
# branches: CORS comes from Cors:AllowedOrigins and UseHttpsRedirection is enabled.
#
# ASPNETCORE_FORWARDEDHEADERS_ENABLED=true is mandatory here. Caddy terminates TLS
# and forwards plain HTTP, so without it ASP.NET sees scheme=http and
# UseHttpsRedirection (Program.cs:117) answers every request with a 307 back to
# HTTPS -> Caddy forwards as HTTP again -> infinite redirect loop. Setting this
# variable makes the runtime install the forwarded-headers middleware ahead of the
# rest of the pipeline, so X-Forwarded-Proto is honoured and the loop disappears.
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    DOTNET_gcServer=0 \
    DOTNET_EnableDiagnostics=0

COPY --from=build /app/publish .

# The .NET runtime images do NOT ship curl, so a curl-based HEALTHCHECK would
# always fail and the container would sit in "unhealthy" forever. Install it
# before dropping privileges.
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*

# .NET 8 images ship a non-root `app` user; $APP_UID is set by the base image.
USER $APP_UID

EXPOSE 8080

# The API has no /health endpoint, so probe the Swagger UI that Program.cs maps
# unconditionally. If you later gate Swagger behind IsDevelopment, point this at a
# real health endpoint instead or the container will be marked unhealthy forever.
HEALTHCHECK --interval=30s --timeout=5s --start-period=25s --retries=3 \
    CMD ["/bin/sh", "-c", "curl -fsS http://127.0.0.1:8080/swagger/index.html > /dev/null || exit 1"]

ENTRYPOINT ["dotnet", "PathwayNavigator.Api.dll"]
