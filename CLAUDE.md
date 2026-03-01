# CLAUDE.md — EV Charging CSMS

## Project
EV Charging Station Management System (CSMS) — B2C platform quản lý trạm sạc xe điện tại Việt Nam.

## Tech Stack
- Backend: .NET 10, C#, ABP Framework
- Database: PostgreSQL + Read Replicas
- Cache: Redis
- Real-time: SignalR
- Mobile: React Native (Expo), TypeScript
- OCPP: OCPP.Core (1.6J/2.0)
- CQRS: MediatR
- Localization: VI (default) + EN

## Architecture
- Driver BFF API (port 5001): .NET Minimal API, Redis cache-first, read replicas
- Admin API (port 5000): Full ABP layered architecture
- Shared Domain Layer between both APIs

## Documentation
- `docs/` — Detailed docs for humans (9 layers, see docs/DOCS-GUIDE.md)
- `memory-bank/` — Condensed context for AI agents (references docs/)

## ⚠️ AI Playbook — READ BEFORE CODING
- **Rules (MUST):** docs/09-ai-playbook/rules/_master-rules.md
- **Anti-patterns (NEVER):** docs/09-ai-playbook/anti-patterns/_index.md
- **Patterns (USE):** docs/09-ai-playbook/patterns/_index.md
- **Lessons (LEARN):** docs/09-ai-playbook/lessons-learned/_index.md
- **Debug (FIX):** docs/09-ai-playbook/debug-playbooks/_index.md

## Before implementing ANY feature:
1. Read FRS module: docs/03-functional-specs/modules/MOD-{NNN}.md
2. Read rules: docs/09-ai-playbook/rules/_master-rules.md
3. Check anti-patterns: docs/09-ai-playbook/anti-patterns/
4. Use prompt template if available: docs/09-ai-playbook/prompts/

## When you encounter an error:
1. Check debug-playbooks/ FIRST
2. If new → fix → create LL-NNN lesson learned
3. If pattern found → create PAT-NNN pattern
4. Update _master-rules.md

## Commands
```bash
# Start infrastructure
docker compose up -d

# Run Admin API
cd src/backend/src/KCharge.HttpApi.Host && dotnet run

# Run Driver BFF
cd src/backend/src/KCharge.Driver.BFF && dotnet run

# Run Admin Portal
cd src/admin-portal && npm run dev

# Run Mobile App
cd src/driver-app && npx expo start

# Run tests
dotnet test src/backend

# DB migration
dotnet ef database update -p src/backend/src/KCharge.EntityFrameworkCore
```

## Key Conventions
- Entity base: ABP `FullAuditedAggregateRoot<Guid>`
- CQRS: IQuery/ICommand + MediatR handlers
- Cache key: `"entity:{id}:field"` (e.g., `"station:123:status"`)
- API error: `{ "code": "MOD_001", "message": "...", "details": {} }`
- Date: dd/MM/yyyy, Timezone: UTC+7
- Currency: VNĐ (9.900đ), dấu chấm phân cách
- Pagination: cursor-based (not offset)
- All UI strings via `IStringLocalizer` (never hardcode)
