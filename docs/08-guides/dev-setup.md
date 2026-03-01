# Development Environment Setup

> Status: PUBLISHED | Last Updated: 2026-03-01

Complete guide for setting up the EV Charging CSMS development environment on Windows, macOS, or Linux.

## Prerequisites

### Required Software
- **.NET 10 SDK** — [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
  - Verify: `dotnet --version` (should be 10.x.x)
- **Node.js 20+** — [Download](https://nodejs.org/)
  - Verify: `node --version` and `npm --version`
- **Docker & Docker Compose** — [Download](https://www.docker.com/products/docker-desktop)
  - Verify: `docker --version` and `docker compose version`
- **PostgreSQL 16** — Either:
  - Local installation from [postgresql.org](https://www.postgresql.org/)
  - Or via Docker (recommended, see Quick Start below)
- **Redis 7** — Either:
  - Local installation from [redis.io](https://redis.io/)
  - Or via Docker (recommended, see Quick Start below)
- **IDE** — One of:
  - JetBrains Rider (recommended for .NET)
  - Visual Studio 2022 Community/Professional
  - VS Code with C# Dev Kit

### Hardware Recommendations
- CPU: 4+ cores
- RAM: 8+ GB (16 GB recommended)
- Disk: 20+ GB free space

### Optional but Recommended
- **Git** — For version control
- **Docker Desktop** — For easy container management (includes Docker Compose)
- **Postman** or **Insomnia** — For API testing
- **DBeaver** or **pgAdmin** — For database management

## Step 1: Clone Repository

```bash
# SSH (recommended if you have SSH key configured)
git clone git@github.com:EMESOFT/ev-charging-csms.git
cd ev-charging-csms

# HTTPS (if SSH is not configured)
git clone https://github.com/EMESOFT/ev-charging-csms.git
cd ev-charging-csms
```

## Step 2: Setup Infrastructure (Docker)

Start PostgreSQL and Redis containers using Docker Compose:

```bash
# Start all services (PostgreSQL, Redis)
docker compose up -d

# Verify containers are running
docker compose ps
```

Expected output:
```
NAME                    STATUS
ev-charging-postgres    Up X seconds
ev-charging-redis       Up X seconds
```

### Docker Compose Services
The `docker-compose.yml` includes:
- **PostgreSQL 16** — Port 5432
  - Username: `postgres`
  - Password: `postgres`
  - Database: `EVCharging`
- **Redis 7** — Port 6379
  - No authentication (development only)

### Alternative: Local PostgreSQL & Redis
If you prefer local installation (not Docker):

**PostgreSQL:**
```bash
# Windows (with Chocolatey)
choco install postgresql16

# macOS (with Homebrew)
brew install postgresql@16

# Linux (Ubuntu/Debian)
sudo apt-get install postgresql-16
```

**Redis:**
```bash
# Windows (with Chocolatey)
choco install redis-64

# macOS (with Homebrew)
brew install redis

# Linux (Ubuntu/Debian)
sudo apt-get install redis-server
```

## Step 3: Restore NuGet Packages

```bash
# From repository root
dotnet restore
```

This may take 1-2 minutes on first run.

## Step 4: Configure Environment Variables

Create `.env` file in repository root:

```env
# Database
ASPNETCORE_ENVIRONMENT=Development
ConnectionStrings__Default=Server=localhost;Port=5432;Database=EVCharging;User Id=postgres;Password=postgres;

# Redis
Redis__Connection=localhost:6379

# JWT (OpenID Connect)
AuthServer__Authority=http://localhost:5000
AuthServer__ClientId=EVCharging_Admin
AuthServer__ClientSecret=YOUR_SECRET_HERE

# Logging
Serilog__MinimumLevel=Debug

# OCPP WebSocket (Driver API)
OCPP__WebSocketPort=5002
OCPP__MaxConnections=1000

# Email (optional, for testing)
Email__SmtpServer=smtp.mailtrap.io
Email__SmtpPort=587
Email__UserName=
Email__Password=
```

### Per-Project Environment Variables

**Admin API** — `src/EVCharging.Admin.HttpApi.Host/appsettings.Development.json`
```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Port=5432;Database=EVCharging;User Id=postgres;Password=postgres;"
  },
  "Redis": {
    "Connection": "localhost:6379"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

**Driver BFF API** — `src/EVCharging.Driver.BFF/appsettings.Development.json`
```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Port=5432;Database=EVCharging;User Id=postgres;Password=postgres;",
    "ReadReplica": "Server=localhost-read-replica;Port=5432;Database=EVCharging;User Id=postgres;Password=postgres;"
  },
  "Redis": {
    "Connection": "localhost:6379"
  }
}
```

## Step 5: Database Migration

Run Entity Framework migrations to create database schema:

```bash
# Navigate to EF project
cd src/EVCharging.EntityFrameworkCore

# Apply migrations
dotnet ef database update

# Return to root
cd ../../..
```

Expected output:
```
Build started...
Build succeeded.
Done. Migrated EVCharging.EntityFrameworkCore to 2026-03-01_001_InitialCreate
```

### Verify Database
```bash
# Connect to PostgreSQL
psql -h localhost -U postgres -d EVCharging

# List tables
\dt

# Exit
\q
```

## Step 6: Run Admin API (Port 5000)

```bash
# Terminal 1 — Admin API
cd src/EVCharging.Admin.HttpApi.Host
dotnet run
```

Expected output:
```
info: Microsoft.Hosting.Lifetime[0]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to stop.
```

### Access Admin API
- Swagger UI: http://localhost:5000/swagger
- Health Check: http://localhost:5000/health

## Step 7: Run Driver BFF API (Port 5001)

```bash
# Terminal 2 — Driver BFF
cd src/EVCharging.Driver.BFF
dotnet run
```

Expected output:
```
info: Microsoft.Hosting.Lifetime[0]
      Now listening on: http://localhost:5001
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to stop.
```

### Access Driver BFF API
- Swagger UI: http://localhost:5001/swagger
- Health Check: http://localhost:5001/health

## Step 8: Start Frontend (Optional)

**Mobile Frontend** (React Native with Expo):
```bash
# Terminal 3
cd frontend/mobile
npm install
npm start

# Scan QR code with Expo Go app (iOS/Android)
```

**Web Dashboard** (React/Next.js):
```bash
# Terminal 4
cd frontend/web
npm install
npm run dev

# Open http://localhost:3000
```

## Development Workflow

### Running Tests
```bash
# All tests
dotnet test

# Specific test project
dotnet test src/EVCharging.Tests/EVCharging.Tests.csproj

# With coverage
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover
```

### Creating New Migration
```bash
cd src/EVCharging.EntityFrameworkCore
dotnet ef migrations add MigrationName
dotnet ef database update
```

### Code Generation
ABP Framework provides scaffolding:
```bash
abp generate-proxy -t csharp
```

### Debugging
- **Rider** — F5 or Run > Run
- **Visual Studio** — F5
- **VS Code** — Ctrl+F5 (with Debugger for C#)

## Troubleshooting

### Docker Containers Not Starting
```bash
# Check logs
docker compose logs postgresql
docker compose logs redis

# Stop and remove containers
docker compose down

# Rebuild
docker compose up -d
```

### Database Connection Error
```
Npgsql.NpgsqlException: Server not responding
```
- Verify Docker container is running: `docker compose ps`
- Verify connection string in `appsettings.json`
- Check PostgreSQL logs: `docker compose logs postgresql`

### Port Already in Use
```
System.IO.IOException: Failed to bind to address 127.0.0.1:5000
```
- Kill process: `lsof -ti:5000 | xargs kill -9` (macOS/Linux)
- Or change port in `appsettings.json` or `Program.cs`

### NuGet Package Restore Fails
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore again
dotnet restore
```

### Migration Fails
```bash
# Rollback last migration
dotnet ef migrations remove

# Or drop database and start fresh
dotnet ef database drop
dotnet ef database update
```

## Performance Tips

- Use **Rider** or **Visual Studio** for better IntelliSense and debugging
- Enable **Hot Reload**: Available in VS2022 and Rider
- Use **Redis** for caching to reduce database queries
- Run **read replicas** for reporting queries (Driver BFF)

## Next Steps

1. Read [CLAUDE.md](../../CLAUDE.md) for project conventions
2. Review [Coding Conventions](coding-conventions.md)
3. Check [AI Playbook Rules](../09-ai-playbook/rules/_master-rules.md)
4. Start with Admin API architecture: `docs/01-architecture/`
5. Explore functional specs: `docs/03-functional-specs/`

## Getting Help

- **Documentation**: See `docs/` directory
- **Memory Bank**: See `memory-bank/` for quick reference
- **Issues**: GitHub Issues for bugs and feature requests
- **Discussions**: GitHub Discussions for questions
