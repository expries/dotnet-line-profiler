# OpenTelemetry Line Profiler

A .NET profiler that instruments application methods and exports line-level probe data through OpenTelemetry.

## Stack

- `profiler/` — native .NET profiler and instrumentation libraries
- `example-app/` — sample application
- `otel-collector/` — OpenTelemetry Collector configuration
- `processing-api/` — API and PostgreSQL-backed trace processor
- `tempo/` — Tempo configuration

## Quick start

Requirements: .NET SDK 9, Docker Compose, and a Linux environment for the profiler build.

```sh
cp .env.example .env
# Set a local POSTGRES_PASSWORD in .env

./profiler/publish-linux64.sh
docker compose up --build
```

The sample application is available on `http://localhost:8080`. Other services use:

- Grafana: `http://localhost:3000`
- Processing API: `http://localhost:9080`
- OTLP gRPC/HTTP: ports `4317`/`4318`

## Development

```sh
dotnet build profiler/Profiler.sln
dotnet test profiler/ILRewriter.Tests/ILRewriter.Tests.csproj
```

Generate sample telemetry with `generate-traces.sh` after installing `telemetrygen`.

## Security

Keep `.env` and signing keys out of Git. Use environment variables or an external secret manager for credentials.
