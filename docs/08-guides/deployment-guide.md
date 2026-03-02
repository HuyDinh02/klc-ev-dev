# Deployment Guide

> Status: PUBLISHED | Last Updated: 2026-03-01

Production deployment guide for EV Charging CSMS across multiple environments (Development, Staging, Production) on AWS infrastructure.

## Deployment Architecture

### Phase 1: EC2 + RDS + ElastiCache (Current)
Simple, cost-effective for MVP/early stage:

```
┌─────────────────────────────────────────────────┐
│              AWS VPC (Private)                  │
├─────────────────────────────────────────────────┤
│                                                 │
│  ┌──────────────────────────────────────────┐  │
│  │  EC2 Instances (Auto Scaling Group)      │  │
│  │  ├─ Admin API (5000)                     │  │
│  │  └─ Driver BFF (5001)                    │  │
│  │      OCPP WebSocket (5002)               │  │
│  └──────────────────────────────────────────┘  │
│                    ↑                            │
│        Application Load Balancer               │
│                    ↑                            │
│  ┌──────────────────────────────────────────┐  │
│  │  RDS PostgreSQL (Primary + Replicas)     │  │
│  │  - Main database                         │  │
│  │  - Read replicas for reporting           │  │
│  └──────────────────────────────────────────┘  │
│                                                 │
│  ┌──────────────────────────────────────────┐  │
│  │  ElastiCache Redis Cluster               │  │
│  │  - Session cache                         │  │
│  │  - Real-time data (OCPP status)          │  │
│  └──────────────────────────────────────────┘  │
│                                                 │
└─────────────────────────────────────────────────┘
        ↑
   CloudFront CDN
        ↑
   Internet Users
```

### Phase 2: ECS/EKS (Future Scaling)
For high-availability and auto-scaling:

```
┌──────────────────────────────────────┐
│  EKS Cluster (Kubernetes)            │
├──────────────────────────────────────┤
│                                      │
│  ┌────────────────────────────────┐  │
│  │  Admin API Pod (Multi-replica) │  │
│  └────────────────────────────────┘  │
│                                      │
│  ┌────────────────────────────────┐  │
│  │  Driver BFF Pod (Multi-replica)│  │
│  └────────────────────────────────┘  │
│                                      │
│  ┌────────────────────────────────┐  │
│  │  Background Workers            │  │
│  │  - OCPP handlers               │  │
│  │  - Notification services       │  │
│  └────────────────────────────────┘  │
│                                      │
└──────────────────────────────────────┘
      ↑
  AWS RDS + ElastiCache
```

## Docker Images

### Building Docker Images

**Dockerfile for Admin API** (`src/backend/src/KLC.HttpApi.Host/Dockerfile`):
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and projects
COPY ["EVCharging.sln", "."]
COPY ["src/EVCharging.Admin.HttpApi.Host/", "src/EVCharging.Admin.HttpApi.Host/"]
COPY ["src/EVCharging.Domain/", "src/EVCharging.Domain/"]
COPY ["src/EVCharging.Application/", "src/EVCharging.Application/"]
COPY ["src/EVCharging.EntityFrameworkCore/", "src/EVCharging.EntityFrameworkCore/"]

# Restore and build
RUN dotnet restore "EVCharging.sln"
RUN dotnet build "src/EVCharging.Admin.HttpApi.Host/EVCharging.Admin.HttpApi.Host.csproj" -c Release

# Publish
FROM build AS publish
RUN dotnet publish "src/EVCharging.Admin.HttpApi.Host/EVCharging.Admin.HttpApi.Host.csproj" \
    -c Release \
    -o /app/publish

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "EVCharging.Admin.HttpApi.Host.dll"]
```

**Dockerfile for Driver BFF** (`src/backend/src/KLC.Driver.BFF/Dockerfile`):
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["EVCharging.sln", "."]
COPY ["src/EVCharging.Driver.BFF/", "src/EVCharging.Driver.BFF/"]
COPY ["src/EVCharging.Domain/", "src/EVCharging.Domain/"]
COPY ["src/EVCharging.EntityFrameworkCore/", "src/EVCharging.EntityFrameworkCore/"]

RUN dotnet restore "EVCharging.sln"
RUN dotnet build "src/EVCharging.Driver.BFF/EVCharging.Driver.BFF.csproj" -c Release

FROM build AS publish
RUN dotnet publish "src/EVCharging.Driver.BFF/EVCharging.Driver.BFF.csproj" \
    -c Release \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 5001 5002
ENV ASPNETCORE_URLS=http://+:5001
ENV ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:5001/health || exit 1

ENTRYPOINT ["dotnet", "EVCharging.Driver.BFF.dll"]
```

### Building Images
```bash
# Build Admin API
docker build -t klc-admin:latest \
  -f src/backend/src/KLC.HttpApi.Host/Dockerfile .

# Build Driver BFF
docker build -t klc-driver:latest \
  -f src/backend/src/KLC.Driver.BFF/Dockerfile .

# Tag for ECR
docker tag ev-charging-admin:latest \
  123456789.dkr.ecr.ap-southeast-1.amazonaws.com/ev-charging-admin:latest

docker tag ev-charging-driver:latest \
  123456789.dkr.ecr.ap-southeast-1.amazonaws.com/ev-charging-driver:latest

# Push to ECR
aws ecr get-login-password --region ap-southeast-1 | \
  docker login --username AWS --password-stdin 123456789.dkr.ecr.ap-southeast-1.amazonaws.com

docker push 123456789.dkr.ecr.ap-southeast-1.amazonaws.com/ev-charging-admin:latest
docker push 123456789.dkr.ecr.ap-southeast-1.amazonaws.com/ev-charging-driver:latest
```

## Docker Compose for Production

**Production docker-compose.yml**:
```yaml
version: '3.8'

services:
  admin-api:
    image: ev-charging-admin:latest
    container_name: admin-api
    ports:
      - "5000:5000"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__Default=Server=postgres;Port=5432;Database=EVCharging;User Id=postgres;Password=${DB_PASSWORD};
      - Redis__Connection=redis:6379
      - AuthServer__Authority=https://${DOMAIN}/
      - Serilog__WriteTo__0__Args__connectionString=Server=postgres;Port=5432;Database=EVCharging_Logs;User Id=postgres;Password=${DB_PASSWORD};
    depends_on:
      - postgres
      - redis
    networks:
      - ev-charging
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5000/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

  driver-api:
    image: ev-charging-driver:latest
    container_name: driver-api
    ports:
      - "5001:5001"
      - "5002:5002"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__Default=Server=postgres;Port=5432;Database=EVCharging;User Id=postgres;Password=${DB_PASSWORD};
      - Redis__Connection=redis:6379
      - AuthServer__Authority=https://${DOMAIN}/
      - OCPP__WebSocketPort=5002
    depends_on:
      - postgres
      - redis
    networks:
      - ev-charging
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5001/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

  postgres:
    image: postgres:16
    container_name: postgres
    environment:
      - POSTGRES_DB=EVCharging
      - POSTGRES_PASSWORD=${DB_PASSWORD}
    volumes:
      - postgres-data:/var/lib/postgresql/data
    networks:
      - ev-charging
    restart: unless-stopped
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5

  redis:
    image: redis:7-alpine
    container_name: redis
    command: redis-server --requirepass ${REDIS_PASSWORD}
    volumes:
      - redis-data:/data
    networks:
      - ev-charging
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

  nginx:
    image: nginx:latest
    container_name: nginx
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf:ro
      - ./ssl/:/etc/nginx/ssl/:ro
    depends_on:
      - admin-api
      - driver-api
    networks:
      - ev-charging
    restart: unless-stopped

volumes:
  postgres-data:
  redis-data:

networks:
  ev-charging:
    driver: bridge
```

**nginx.conf** (Reverse Proxy):
```nginx
upstream admin_api {
    server admin-api:5000;
}

upstream driver_api {
    server driver-api:5001;
}

upstream websocket {
    server driver-api:5002;
}

server {
    listen 80;
    server_name _;
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl http2;
    server_name api.klc.vn;

    ssl_certificate /etc/nginx/ssl/cert.pem;
    ssl_certificate_key /etc/nginx/ssl/key.pem;

    # Security headers
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-Frame-Options "DENY" always;

    # Admin API
    location /admin/ {
        proxy_pass http://admin_api/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # Driver BFF API
    location /driver/ {
        proxy_pass http://driver_api/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # OCPP WebSocket
    location /ocpp {
        proxy_pass http://websocket;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_buffering off;
    }
}
```

**Start production environment:**
```bash
# Set environment variables
export DB_PASSWORD="secure_password_here"
export REDIS_PASSWORD="redis_secure_password"
export DOMAIN="api.klc.vn"

# Start containers
docker compose -f docker-compose.prod.yml up -d

# View logs
docker compose logs -f admin-api
docker compose logs -f driver-api
```

## AWS Deployment (Phase 1)

### 1. RDS PostgreSQL Setup

**AWS CLI:**
```bash
# Create RDS instance
aws rds create-db-instance \
    --db-instance-identifier ev-charging-prod \
    --db-instance-class db.t4g.medium \
    --engine postgres \
    --engine-version 16.2 \
    --master-username postgres \
    --master-user-password SecurePassword123 \
    --allocated-storage 100 \
    --storage-type gp3 \
    --vpc-security-group-ids sg-0123456789abcdef0 \
    --db-subnet-group-name ev-charging-db-subnet \
    --publicly-accessible false \
    --backup-retention-period 30 \
    --multi-az true \
    --storage-encrypted true

# Create read replica (for driver BFF reporting queries)
aws rds create-db-instance-read-replica \
    --db-instance-identifier ev-charging-read-replica \
    --source-db-instance-identifier ev-charging-prod \
    --db-instance-class db.t4g.medium
```

### 2. ElastiCache Redis Setup

```bash
# Create Redis cluster
aws elasticache create-cache-cluster \
    --cache-cluster-id ev-charging-redis \
    --cache-node-type cache.t4g.medium \
    --engine redis \
    --engine-version 7.0 \
    --num-cache-nodes 1 \
    --cache-subnet-group-name ev-charging-redis-subnet \
    --security-group-ids sg-0123456789abcdef1 \
    --automatic-failover-enabled

# Enable encryption at rest
aws elasticache create-cache-cluster \
    --cache-cluster-id ev-charging-redis \
    --at-rest-encryption-enabled
```

### 3. EC2 Auto Scaling Group Setup

**Create Launch Template:**
```bash
aws ec2 create-launch-template \
    --launch-template-name ev-charging-template \
    --version-description "EV Charging CSMS Deployment" \
    --launch-template-data '{
        "ImageId": "ami-0c55b159cbfafe1f0",
        "InstanceType": "t4g.large",
        "KeyName": "ev-charging-key",
        "SecurityGroupIds": ["sg-0123456789abcdef2"],
        "IamInstanceProfile": {"Name": "EC2-ECS-Role"},
        "UserData": "base64_encoded_init_script",
        "TagSpecifications": [{
            "ResourceType": "instance",
            "Tags": [{"Key": "Name", "Value": "EV-Charging-API"}]
        }]
    }'
```

**Create Auto Scaling Group:**
```bash
aws autoscaling create-auto-scaling-group \
    --auto-scaling-group-name ev-charging-asg \
    --launch-template LaunchTemplateName=ev-charging-template,Version='$Latest' \
    --min-size 2 \
    --max-size 6 \
    --desired-capacity 2 \
    --vpc-zone-identifier "subnet-12345,subnet-67890" \
    --target-group-arns arn:aws:elasticloadbalancing:ap-southeast-1:123456789:targetgroup/ev-charging/abc123
```

### 4. Application Load Balancer (ALB)

```bash
# Create ALB
aws elbv2 create-load-balancer \
    --name ev-charging-alb \
    --subnets subnet-12345 subnet-67890 \
    --security-groups sg-0123456789abcdef3 \
    --scheme internet-facing

# Create target groups
aws elbv2 create-target-group \
    --name ev-charging-admin \
    --protocol HTTP \
    --port 5000 \
    --vpc-id vpc-12345678

aws elbv2 create-target-group \
    --name ev-charging-driver \
    --protocol HTTP \
    --port 5001 \
    --vpc-id vpc-12345678
```

## Environment Configuration

### Staging Environment
`.env.staging`:
```env
ASPNETCORE_ENVIRONMENT=Staging
ConnectionStrings__Default=Server=ev-charging-staging.c9akciq32.us-east-1.rds.amazonaws.com;Database=EVCharging_Staging;User Id=postgres;Password=***;
Redis__Connection=ev-charging-redis-staging.abc123.ng.0001.use1.cache.amazonaws.com:6379
AuthServer__Authority=https://staging-api.klc.vn
Serilog__MinimumLevel=Information
OCPP__MaxConnections=500
```

### Production Environment
`.env.production`:
```env
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__Default=Server=ev-charging-prod.c9akciq32.us-east-1.rds.amazonaws.com;Database=EVCharging;User Id=postgres;Password=***;
ConnectionStrings__ReadReplica=Server=ev-charging-read-replica.c9akciq32.us-east-1.rds.amazonaws.com;Database=EVCharging;User Id=postgres;Password=***;
Redis__Connection=ev-charging-redis-prod.abc123.ng.0001.use1.cache.amazonaws.com:6379
AuthServer__Authority=https://api.klc.vn
Serilog__MinimumLevel=Warning
OCPP__MaxConnections=5000
CORS__Origins=https://app.klc.vn,https://admin.klc.vn
```

## Health Checks & Monitoring

### Health Check Endpoint (appsettings.json)
```json
{
  "HealthChecks": {
    "Enabled": true,
    "Endpoints": {
      "Health": "/health",
      "Ready": "/health/ready"
    },
    "Checks": {
      "Database": true,
      "Redis": true,
      "OCPP": true
    }
  }
}
```

### Health Check Implementation
```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddDbContextCheck<EVChargingDbContext>()
    .AddRedis(builder.Configuration["Redis:Connection"])
    .AddCheck<OcppConnectionHealthCheck>("ocpp");

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

### CloudWatch Monitoring

**CloudWatch Agent Configuration** (`cloudwatch-config.json`):
```json
{
  "metrics": {
    "namespace": "EV-Charging/API",
    "metrics_collected": {
      "cpu": {
        "measurement": [
          {"name": "cpu_usage_idle", "rename": "CPU_IDLE", "unit": "Percent"},
          {"name": "cpu_usage_iowait", "rename": "CPU_IOWAIT", "unit": "Percent"}
        ],
        "metrics_collection_interval": 60
      },
      "mem": {
        "measurement": [
          {"name": "mem_used_percent", "rename": "MEM_USED", "unit": "Percent"}
        ],
        "metrics_collection_interval": 60
      }
    }
  },
  "logs": {
    "logs_collected": {
      "files": {
        "collect_list": [
          {
            "file_path": "/var/log/dotnet/admin-api.log",
            "log_group_name": "/aws/ec2/ev-charging/admin-api",
            "log_stream_name": "{instance_id}"
          },
          {
            "file_path": "/var/log/dotnet/driver-api.log",
            "log_group_name": "/aws/ec2/ev-charging/driver-api",
            "log_stream_name": "{instance_id}"
          }
        ]
      }
    }
  }
}
```

### Serilog Configuration (Structured Logging)
```csharp
// Program.cs
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.PostgreSQL(
        connectionString: builder.Configuration["ConnectionStrings:Default"],
        tableName: "Logs")
    .WriteTo.CloudWatch(
        logGroupName: "/aws/ec2/ev-charging/admin-api",
        textFormatter: new CompactJsonFormatter())
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentUserName()
    .CreateLogger();
```

## CI/CD Pipeline (GitHub Actions)

**Workflow file** (`.github/workflows/deploy.yml`):
```yaml
name: Deploy to Production

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

env:
  AWS_REGION: ap-southeast-1
  ECR_REGISTRY: 123456789.dkr.ecr.ap-southeast-1.amazonaws.com
  ADMIN_API_IMAGE: ev-charging-admin
  DRIVER_API_IMAGE: ev-charging-driver

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      - run: dotnet restore
      - run: dotnet build
      - run: dotnet test /p:CollectCoverage=true

  build:
    needs: test
    runs-on: ubuntu-latest
    if: github.event_name == 'push' && github.ref == 'refs/heads/main'
    steps:
      - uses: actions/checkout@v3

      - uses: aws-actions/configure-aws-credentials@v2
        with:
          aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          aws-region: ${{ env.AWS_REGION }}

      - uses: aws-actions/amazon-ecr-login@v1
        id: login-ecr

      - name: Build and push Admin API
        run: |
          docker build -t $ECR_REGISTRY/$ADMIN_API_IMAGE:latest \
            -f src/backend/src/KLC.HttpApi.Host/Dockerfile .
          docker push $ECR_REGISTRY/$ADMIN_API_IMAGE:latest

      - name: Build and push Driver API
        run: |
          docker build -t $ECR_REGISTRY/$DRIVER_API_IMAGE:latest \
            -f src/backend/src/KLC.Driver.BFF/Dockerfile .
          docker push $ECR_REGISTRY/$DRIVER_API_IMAGE:latest

  deploy:
    needs: build
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - uses: aws-actions/configure-aws-credentials@v2
        with:
          aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          aws-region: ${{ env.AWS_REGION }}

      - name: Update EC2 instances
        run: |
          aws ec2-instance-connect send-ssh-public-key \
            --instance-id i-1234567890abcdef0 \
            --os-user ec2-user \
            --ssh-public-key-file ~/.ssh/id_rsa.pub

          ssh -i ~/.ssh/id_rsa ec2-user@your-instance \
            "cd /opt/ev-charging && \
             docker compose pull && \
             docker compose up -d"

      - name: Verify deployment
        run: |
          curl -f https://api.klc.vn/health || exit 1
          curl -f https://api.klc.vn/admin/health || exit 1
```

## Rollback Procedure

If deployment fails:

```bash
# View deployment history
docker pull <previous-image>:latest

# Rollback in docker-compose
docker compose down
docker compose up -d  # Uses previous image

# Verify health
curl http://localhost:5000/health
curl http://localhost:5001/health

# Check logs
docker compose logs admin-api
docker compose logs driver-api
```

## Disaster Recovery

### Database Backup
```bash
# Automated RDS backups (30-day retention)
aws rds create-db-snapshot \
    --db-instance-identifier ev-charging-prod \
    --db-snapshot-identifier ev-charging-backup-$(date +%Y%m%d)

# Restore from snapshot
aws rds restore-db-instance-from-db-snapshot \
    --db-instance-identifier ev-charging-restored \
    --db-snapshot-identifier ev-charging-backup-20260301
```

### Redis Persistence
Enable AOF (Append-Only File) in ElastiCache:
```json
{
  "ParameterGroupName": "ev-charging-redis-params",
  "Parameters": [
    {"ParameterName": "appendonly", "ParameterValue": "yes"},
    {"ParameterName": "appendfsync", "ParameterValue": "everysec"}
  ]
}
```

## Performance Optimization

### Database Query Optimization
- Index frequently filtered columns (ProvinceCode, Status)
- Use read replicas for reporting/analytics
- Implement query result caching (Redis)

### API Response Caching
- Cache station list for 1 hour
- Cache connector status for 5 minutes
- Invalidate on updates

### Load Testing
```bash
# Using Apache Bench
ab -n 10000 -c 100 https://api.klc.vn/driver/api/v1/stations

# Using k6
k6 run load-test.js
```
