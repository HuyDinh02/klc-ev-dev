# Master Rules — AI Agent Guidelines

> ⚠️ MUST READ before any implementation work

## Rule Categories

### R1: ABP Framework Rules
- R1.1: Always use ABP conventions for entities, services, repositories
- R1.2: Business logic belongs in Domain layer, NOT in Application Services
- R1.3: Use `FullAuditedAggregateRoot<Guid>` for aggregate roots
- R1.4: Use ABP's built-in features before building custom solutions
- R1.5: Never expose domain entities through API — always use DTOs

### R2: OCPP Rules
- R2.1: Handle OCPP messages idempotently (chargers may retry)
- R2.2: Persist transaction data immediately for billing accuracy
- R2.3: Handle charger reconnection gracefully
- R2.4: Use strongly-typed OCPP message models

### R3: API Rules
- R3.1: All APIs must have Swagger documentation
- R3.2: Use cursor-based pagination (not offset)
- R3.3: Standard error format: `{ "code": "MOD_001", "message": "...", "details": {} }`
- R3.4: API versioning on all endpoints

### R4: Database Rules
- R4.1: Use code-first migrations (never manual DB changes)
- R4.2: Add indexes on frequently queried columns
- R4.3: Use soft delete via ABP `ISoftDelete`
- R4.4: Cache hot data in Redis

### R5: Testing Rules
- R5.1: Unit tests for all domain logic
- R5.2: Integration tests for application services
- R5.3: Test OCPP message flows end-to-end
- R5.4: Use ABP test infrastructure

### R6: Code Quality Rules
- R6.1: All code in English
- R6.2: Vietnamese only in localization files
- R6.3: Conventional commits (`feat:`, `fix:`, `chore:`, etc.)
- R6.4: No hardcoded strings in UI — use IStringLocalizer

<!-- This file will grow as we learn more -->
