# API Design Guide

> Status: PUBLISHED | Last Updated: 2026-03-01

Complete guide to RESTful API design, authentication, versioning, and usage patterns for EV Charging CSMS.

## API Architecture Overview

### Dual API Design
- **Admin API** (Port 5000): Full ABP layered architecture for internal operations
- **Driver BFF API** (Port 5001): Minimal API, cache-first design for mobile/driver app

```
┌─────────────────────┐           ┌──────────────────────┐
│   Admin Dashboard   │           │   Driver Mobile App  │
│   (Web, Browser)    │           │   (React Native)     │
└──────────┬──────────┘           └──────────┬───────────┘
           │                                  │
           │ HTTPS/JWT                        │ HTTPS/JWT
           │                                  │
           ▼                                  ▼
    ┌─────────────────┐           ┌──────────────────────┐
    │   Admin API     │           │   Driver BFF API     │
    │    Port 5000    │           │    Port 5001 + 5002  │
    │  Full DDD/CQRS  │           │  Cache-First/Minimal │
    └────────┬────────┘           └──────────┬───────────┘
             │                               │
             └──────────────┬────────────────┘
                            │
                    ┌───────▼────────┐
                    │  PostgreSQL    │
                    │  + Read Replica│
                    └────────────────┘
                    ┌────────────────┐
                    │  Redis Cache   │
                    └────────────────┘
```

## RESTful Design Principles

### HTTP Methods
- **GET** — Retrieve resource(s), no state change
- **POST** — Create new resource
- **PUT** — Replace entire resource (full update)
- **PATCH** — Partial resource update
- **DELETE** — Remove resource

### Request/Response Format

#### JSON Request
```json
{
  "name": "Trạm sạc ABC",
  "address": "123 Đường Nguyễn Huệ, TP.HCM",
  "latitude": 10.7769,
  "longitude": 106.6966,
  "provinceCode": "HCM"
}
```

#### JSON Response
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Trạm sạc ABC",
  "address": "123 Đường Nguyễn Huệ, TP.HCM",
  "latitude": 10.7769,
  "longitude": 106.6966,
  "provinceCode": "HCM",
  "creationTime": "2026-03-01T10:30:00+07:00",
  "lastModificationTime": "2026-03-01T14:45:30+07:00"
}
```

### HTTP Status Codes
- **200 OK** — Successful request
- **201 Created** — Resource created successfully
- **204 No Content** — Successful request with no response body
- **400 Bad Request** — Invalid request format/validation error
- **401 Unauthorized** — Missing or invalid authentication token
- **403 Forbidden** — Authenticated but no permission
- **404 Not Found** — Resource not found
- **409 Conflict** — Duplicate resource (e.g., station already exists)
- **429 Too Many Requests** — Rate limit exceeded
- **500 Internal Server Error** — Server error
- **503 Service Unavailable** — Temporarily unavailable

## API Versioning

### Version Strategy
Use URL path versioning (not headers):

```
https://api.klc.vn/admin/api/v1/stations
https://api.klc.vn/driver/api/v1/charging-sessions
```

### Version Lifecycle
- Version 1 (v1): Current stable API
- Version 2 (v2): Planned changes (announce 3 months ahead)
- Sunset policy: Support minimum 12 months after new version release

### Breaking Changes
For backwards-incompatible changes:
1. Release new API version (v2)
2. Keep v1 operational for 12 months
3. Document migration guide in changelog
4. Notify all API clients 3 months before sunset

Example migration guide:
```markdown
# Migration from v1 to v2

## Breaking Changes
- Pagination: `offset` removed, use `cursor` instead
- Error format: Code changed from `STATION_NOT_FOUND` to `MOD_001`

## Migration Steps
1. Replace offset/limit with cursor in list requests
2. Update error handling to use new error codes
3. Verify all endpoints tested in staging
```

## Authentication & Authorization

### OAuth2 with OpenID Connect (ABP Identity + OpenIddict)

#### Get Access Token

**Request:**
```bash
curl -X POST https://api.klc.vn/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&username=driver@klc.vn&password=password123&client_id=EVCharging_Driver&client_secret=SECRET&scope=openid%20profile%20email"
```

**Response:**
```json
{
  "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "RTK_REFRESH_TOKEN_HERE"
}
```

#### Use Bearer Token

All subsequent API requests must include Authorization header:

```bash
curl -X GET https://api.klc.vn/driver/api/v1/stations \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

### Token Claims
JWT tokens include:
```json
{
  "sub": "550e8400-e29b-41d4-a716-446655440000",  // User ID
  "email": "driver@klc.vn",
  "name": "Nguyễn Văn A",
  "role": ["Driver"],  // Authorization role
  "iat": 1677600600,   // Issued at
  "exp": 1677604200    // Expires at (1 hour)
}
```

### Refresh Token Flow
When access token expires, use refresh token:

```bash
curl -X POST https://api.klc.vn/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=refresh_token&refresh_token=RTK_REFRESH_TOKEN&client_id=EVCharging_Driver&client_secret=SECRET"
```

## Error Response Format

### Standard Error Response
```json
{
  "error": {
    "code": "MOD_001",
    "message": "Trạm sạc không tìm thấy",
    "details": {
      "stationId": "550e8400-e29b-41d4-a716-446655440000"
    },
    "timestamp": "2026-03-01T10:30:00+07:00"
  }
}
```

### Error Code Patterns
```
Module_Sequence_Version

MOD_001  — Stations module, first error, v1
MOD_002  — Stations module, second error
MOD_003  — Stations module, third error

CHG_001  — Charging Sessions module, first error
PAY_001  — Payment module, first error
AUTH_001 — Authentication module, first error
VAL_001  — Validation error, first
SYS_001  — System error, first
```

### Common Error Codes
```
MOD_001  — Charging station not found
MOD_002  — Connector not found
MOD_003  — Station already exists (duplicate)
MOD_004  — Invalid station status
MOD_005  — Station capacity exceeded

CHG_001  — Charging session not found
CHG_002  — No available connector
CHG_003  — Invalid session duration
CHG_004  — Session already started
CHG_005  — Session not active

PAY_001  — Payment method not found
PAY_002  — Insufficient balance
PAY_003  — Payment processing failed
PAY_004  — Transaction not found

AUTH_001 — Unauthorized (missing token)
AUTH_002 — Invalid token format
AUTH_003 — Token expired
AUTH_004 — Insufficient permissions
AUTH_005 — User account disabled

VAL_001  — Validation error (generic)
VAL_002  — Invalid input format
VAL_003  — Required field missing

SYS_001  — Internal server error
SYS_002  — Service unavailable
SYS_003  — Database error
```

### Validation Error Response
```json
{
  "error": {
    "code": "VAL_001",
    "message": "Validation failed",
    "details": {
      "name": [
        "Tên trạm sạc không được để trống",
        "Tên phải dài hơn 3 ký tự"
      ],
      "address": [
        "Địa chỉ không được để trống"
      ],
      "latitude": [
        "Tọa độ phải hợp lệ"
      ]
    }
  }
}
```

## Pagination

### Cursor-Based Pagination (Required)
Never use offset-based pagination in production (scale issue with large datasets):

#### Request
```bash
# First page
GET /api/v1/stations?pageSize=20

# Next page (use cursor from previous response)
GET /api/v1/stations?pageSize=20&cursor=NWUxZDEzODAtZTI5Yi00MWQ0LWE3MTYtNDQ2NjU1NDQwMDAwPg==
```

#### Response
```json
{
  "data": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "name": "Trạm sạc ABC",
      "address": "123 Đường Nguyễn Huệ, TP.HCM"
    },
    {
      "id": "660f9411-f3ac-52e5-b827-557766551111",
      "name": "Trạm sạc XYZ",
      "address": "456 Đường Tạ Quang Bửu, TP.HCM"
    }
  ],
  "pagination": {
    "nextCursor": "NWUxZDEzODAtZTI5Yi00MWQ0LWE3MTYtNDQ2NjU1NDQwMDAwPg==",
    "hasMore": true,
    "pageSize": 20,
    "totalItems": 1500
  }
}
```

#### Cursor Encoding
```csharp
// Encode: Base64(LastItemId)
string cursor = Convert.ToBase64String(
    Encoding.UTF8.GetBytes(lastItem.Id.ToString()));

// Decode: Decode from Base64
string lastId = Encoding.UTF8.GetString(
    Convert.FromBase64String(cursor));
```

## Rate Limiting

### Rate Limit Headers
All responses include rate limit info:

```
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 1677604200
```

### Rate Limit Policies

**Admin API:**
- 1000 requests/hour per authenticated user
- 100 requests/minute burst

**Driver BFF API:**
- 500 requests/hour per authenticated user
- 50 requests/minute burst

**Unauthenticated (Public Endpoints):**
- 100 requests/hour per IP
- 10 requests/minute burst

### Rate Limit Exceeded Response
```json
{
  "error": {
    "code": "RATE_LIMIT_EXCEEDED",
    "message": "Rate limit exceeded",
    "retryAfter": 3600
  }
}
```

Client should wait `retryAfter` seconds before retrying.

## Request/Response Examples

### Admin API Endpoints

#### 1. Create Charging Station

**Request:**
```bash
POST /api/v1/stations HTTP/1.1
Host: api.klc.vn
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "name": "Trạm sạc Phú Mỹ Hưng",
  "address": "123 Đường Nguyễn Hữu Cảnh, TP.HCM",
  "latitude": 10.7769,
  "longitude": 106.6966,
  "provinceCode": "HCM",
  "districtCode": "7",
  "wardCode": "27",
  "totalConnectors": 8,
  "operatorId": "550e8400-e29b-41d4-a716-446655440001"
}
```

**Response (201 Created):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Trạm sạc Phú Mỹ Hưng",
  "address": "123 Đường Nguyễn Hữu Cảnh, TP.HCM",
  "latitude": 10.7769,
  "longitude": 106.6966,
  "provinceCode": "HCM",
  "totalConnectors": 8,
  "status": "Active",
  "creationTime": "2026-03-01T10:30:00+07:00"
}
```

#### 2. Get Station Details

**Request:**
```bash
GET /api/v1/stations/550e8400-e29b-41d4-a716-446655440000 HTTP/1.1
Host: api.klc.vn
Authorization: Bearer {access_token}
```

**Response (200 OK):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Trạm sạc Phú Mỹ Hưng",
  "address": "123 Đường Nguyễn Hữu Cảnh, TP.HCM",
  "latitude": 10.7769,
  "longitude": 106.6966,
  "provinceCode": "HCM",
  "totalConnectors": 8,
  "activeConnectors": 7,
  "status": "Active",
  "creationTime": "2026-03-01T10:30:00+07:00",
  "connectors": [
    {
      "id": "660f9411-f3ac-52e5-b827-557766551111",
      "connectorId": 1,
      "type": "Type2",
      "status": "Available",
      "power": 22000
    },
    {
      "id": "660f9411-f3ac-52e5-b827-557766551112",
      "connectorId": 2,
      "type": "CCS",
      "status": "Charging",
      "power": 350000
    }
  ]
}
```

#### 3. Update Station

**Request:**
```bash
PUT /api/v1/stations/550e8400-e29b-41d4-a716-446655440000 HTTP/1.1
Host: api.klc.vn
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "name": "Trạm sạc Phú Mỹ Hưng (Updated)",
  "address": "123 Đường Nguyễn Hữu Cảnh, TP.HCM",
  "latitude": 10.7769,
  "longitude": 106.6966,
  "totalConnectors": 10
}
```

**Response (200 OK):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Trạm sạc Phú Mỹ Hưng (Updated)",
  "address": "123 Đường Nguyễn Hữu Cảnh, TP.HCM",
  "totalConnectors": 10,
  "lastModificationTime": "2026-03-01T14:45:30+07:00"
}
```

#### 4. List Stations

**Request:**
```bash
GET /api/v1/stations?pageSize=20&filter=Phú%20Mỹ%20Hưng HTTP/1.1
Host: api.klc.vn
Authorization: Bearer {access_token}
```

**Response (200 OK):**
```json
{
  "data": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "name": "Trạm sạc Phú Mỹ Hưng",
      "address": "123 Đường Nguyễn Hữu Cảnh, TP.HCM",
      "provinceCode": "HCM",
      "totalConnectors": 8,
      "status": "Active"
    }
  ],
  "pagination": {
    "nextCursor": "NWUxZDEzODAtZTI5Yi00MWQ0LWE3MTYtNDQ2NjU1NDQwMDAwPg==",
    "hasMore": false,
    "pageSize": 20,
    "totalItems": 1
  }
}
```

#### 5. Delete Station

**Request:**
```bash
DELETE /api/v1/stations/550e8400-e29b-41d4-a716-446655440000 HTTP/1.1
Host: api.klc.vn
Authorization: Bearer {access_token}
```

**Response (204 No Content):**
(Empty body)

### Driver BFF API Endpoints

#### 1. List Nearby Stations

**Request:**
```bash
GET /api/v1/stations/nearby?latitude=10.7769&longitude=106.6966&distance=10 HTTP/1.1
Host: api.klc.vn
Authorization: Bearer {access_token}
```

**Response (200 OK):**
```json
{
  "data": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "name": "Trạm sạc Phú Mỹ Hưng",
      "address": "123 Đường Nguyễn Hữu Cảnh, TP.HCM",
      "latitude": 10.7769,
      "longitude": 106.6966,
      "distance": 0.5,
      "availableConnectors": 5,
      "totalConnectors": 8,
      "status": "Active",
      "connectors": [
        {
          "id": "660f9411-f3ac-52e5-b827-557766551111",
          "connectorId": 1,
          "type": "Type2",
          "status": "Available",
          "power": 22000
        }
      ]
    }
  ]
}
```

#### 2. Start Charging Session

**Request:**
```bash
POST /api/v1/charging-sessions/start HTTP/1.1
Host: api.klc.vn
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "stationId": "550e8400-e29b-41d4-a716-446655440000",
  "connectorId": 1,
  "carId": "CAR_12345"
}
```

**Response (201 Created):**
```json
{
  "sessionId": "770g0511-g4bd-63f6-c929-558877662222",
  "stationId": "550e8400-e29b-41d4-a716-446655440000",
  "connectorId": 1,
  "status": "Charging",
  "startTime": "2026-03-01T10:30:00+07:00",
  "estimatedEndTime": "2026-03-01T12:30:00+07:00",
  "currentPower": 22000,
  "energyConsumed": 0
}
```

#### 3. Get Session Status

**Request:**
```bash
GET /api/v1/charging-sessions/770g0511-g4bd-63f6-c929-558877662222 HTTP/1.1
Host: api.klc.vn
Authorization: Bearer {access_token}
```

**Response (200 OK):**
```json
{
  "sessionId": "770g0511-g4bd-63f6-c929-558877662222",
  "stationId": "550e8400-e29b-41d4-a716-446655440000",
  "connectorId": 1,
  "status": "Charging",
  "startTime": "2026-03-01T10:30:00+07:00",
  "currentTime": "2026-03-01T11:15:00+07:00",
  "estimatedEndTime": "2026-03-01T12:30:00+07:00",
  "currentPower": 22000,
  "energyConsumed": 15.5,
  "estimatedTotalCost": 3875000
}
```

#### 4. Stop Charging Session

**Request:**
```bash
POST /api/v1/charging-sessions/770g0511-g4bd-63f6-c929-558877662222/stop HTTP/1.1
Host: api.klc.vn
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "reason": "Driver requested"
}
```

**Response (200 OK):**
```json
{
  "sessionId": "770g0511-g4bd-63f6-c929-558877662222",
  "status": "Stopped",
  "startTime": "2026-03-01T10:30:00+07:00",
  "endTime": "2026-03-01T11:15:00+07:00",
  "durationMinutes": 45,
  "energyConsumed": 15.5,
  "totalCost": 3875000,
  "transactionId": "TXN_20260301_001"
}
```

## Swagger/OpenAPI Documentation

### Admin API Swagger
Access at: `https://api.klc.vn/admin/swagger`

Swagger configuration in `Program.cs`:
```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EV Charging CSMS - Admin API",
        Version = "v1",
        Description = "Administration API for managing charging stations",
        Contact = new OpenApiContact
        {
            Name = "EMESOFT Support",
            Email = "support@emesoft.vn"
        }
    });

    // Add JWT Bearer authentication
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter your valid JWT token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "EV Charging Admin API v1");
    options.RoutePrefix = "swagger";
});
```

### Endpoint Documentation Example
```csharp
[HttpGet("{id}")]
[ProducesResponseType(typeof(ChargingStationDto), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public async Task<ActionResult<ChargingStationDto>> GetAsync(
    [FromRoute] Guid id,
    [FromServices] IMediator mediator)
{
    var query = new GetChargingStationQuery { StationId = id };
    return Ok(await mediator.Send(query));
}
```

## Testing APIs

### Using cURL
```bash
# Get token
TOKEN=$(curl -s -X POST https://api.klc.vn/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&username=driver@klc.vn&password=password&client_id=EVCharging_Driver&client_secret=SECRET" \
  | jq -r '.access_token')

# Make request
curl -X GET https://api.klc.vn/driver/api/v1/stations \
  -H "Authorization: Bearer $TOKEN"
```

### Using Postman
1. Import Swagger JSON: `https://api.klc.vn/admin/swagger/v1/swagger.json`
2. Configure OAuth2 collection variable:
   - Token URL: `https://api.klc.vn/connect/token`
   - Client ID: `EVCharging_Admin`
   - Client Secret: `{SECRET}`
3. Requests auto-include Authorization header

### Using Insomnia
1. File → Import → URL: `https://api.klc.vn/admin/swagger/v1/swagger.json`
2. Create OAuth2 request environment:
   - Grant Type: Password
   - Access Token URL: `https://api.klc.vn/connect/token`
   - Client ID: `EVCharging_Admin`

## API Backward Compatibility

### Deprecation Policy
- New features added without breaking existing endpoints
- Deprecated fields marked in Swagger with `deprecated: true`
- Minimum 12-month support for deprecated fields

### Example: Deprecated Field
```csharp
/// <summary>
/// Old field name (deprecated, use 'name' instead)
/// </summary>
[Obsolete("Use 'name' instead", false)]
public string StationName { get; set; }

/// <summary>
/// Station name
/// </summary>
public string Name { get; set; }
```

## Performance Best Practices

### Request Optimization
- Use field filtering: `/api/v1/stations?fields=id,name,status`
- Batch operations: POST `/api/v1/stations/batch` with array
- Use cursor pagination (not offset)

### Caching Strategy
- List endpoints: Cache-Control: max-age=300 (5 minutes)
- Details endpoints: Cache-Control: max-age=3600 (1 hour)
- Real-time status: Cache-Control: no-cache

### Response Compression
- Enable gzip compression on API
- Typical response size reduction: 70-80%

## Security Headers

All API responses include:
```
Strict-Transport-Security: max-age=31536000; includeSubDomains
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 1; mode=block
Content-Security-Policy: default-src 'self'
```

## Monitoring & Analytics

### API Metrics
- Request count by endpoint
- Response time percentiles (p50, p95, p99)
- Error rate by status code
- Cache hit rate

### Logging
All requests logged with:
- Request ID (for tracing)
- User ID
- Endpoint
- Status code
- Response time
- Error (if applicable)
