# 05 - Architecture Decision Records (ADRs)

This directory documents key architecture decisions for the EV Charging CSMS using the Architecture Decision Record (ADR) format.

Each ADR captures:
- **Context**: Business & technical problem
- **Decision**: What we chose and why
- **Consequences**: Positive impacts, trade-offs, and risks
- **Alternatives Considered**: Why we rejected other options

## ADR Index

### Core Technology Stack

| ADR | Title | Status | Date | Key Decision |
|-----|-------|--------|------|--------------|
| **ADR-001** | [Use ABP Framework](./ADR-001-abp-framework.md) | ACCEPTED | 2026-03-01 | ABP Framework for DDD, multi-tenancy, enterprise features |
| **ADR-002** | [PostgreSQL as Primary Database](./ADR-002-postgresql.md) | ACCEPTED | 2026-03-01 | PostgreSQL with read replicas; open-source, JSON support, Vietnam cost-effective |
| **ADR-003** | [OCPP 1.6J Protocol](./ADR-003-ocpp-16j.md) | ACCEPTED | 2026-03-01 | OCPP 1.6J (JSON/WebSocket) with 2.0.1 migration path; 90% hardware support in Vietnam |

### Architecture & Patterns

| ADR | Title | Status | Date | Key Decision |
|-----|-------|--------|------|--------------|
| **ADR-004** | [Modular Monolith for Phase 1](./ADR-004-modular-monolith.md) | ACCEPTED | 2026-03-01 | Modular monolith with module extraction path; fast MVP, future microservices |
| **ADR-005** | [CQRS with MediatR](./ADR-005-cqrs-mediatr.md) | ACCEPTED | 2026-03-01 | CQRS for dual API (Admin + Driver BFF); read replicas, event-driven foundation |

## Decision Makers & Review Cycles

- **Approval Authority**: CTO, Architecture Lead
- **Implementation Owner**: Engineering Leads
- **Review Cycle**: Quarterly; decisions evolve as product scales

## How to Use This Directory

1. **Before implementing a feature**: Check if ADR covers architectural impact
2. **Adding a new ADR**: Copy template, follow format, get CTO approval
3. **Updating an ADR**: Status changes only (ACCEPTED → SUPERSEDED); add superseding ADR if needed
4. **Phase 2 Migration (2027+)**: Refer to Phase 2 sections in ADR-003, ADR-004, ADR-005

## Key Architecture Principles

These ADRs collectively define the CSMS architecture as:

- **Modular**: ABP modules enforce boundaries; separation of concerns via CQRS
- **Scalable**: Read replicas for query volume; CQRS enables independent scaling
- **Event-Driven**: Foundation for Phase 2 microservices; event bus ready
- **Operational Simple**: Single monolith deployment; centralized logging, monitoring
- **Cost-Effective**: Open-source tech stack; minimum infrastructure for Vietnam startup
- **Standards-Based**: OCPP 1.6J for charger communication; no proprietary protocols
- **Flexible**: OCPP 2.0.1 migration path; module extraction to microservices in Phase 2

## Phase Timeline

```
Phase 1 (2026-03-01 → 2026-12-31)
├─ MVP Launch (Modular Monolith)
├─ Admin API + Driver BFF (CQRS + MediatR)
├─ OCPP 1.6J (90% hardware compatibility)
└─ PostgreSQL + read replicas (cost-effective scaling)

Phase 2 (2027-01-01 → 2027-12-31)
├─ Extract SessionModule → SessionService
├─ Add OCPP 2.0.1 support (parallel with 1.6J)
├─ Event-driven inter-service communication
└─ Consider: ADR-006 (event bus), ADR-007 (API Gateway)

Phase 3 (2028+)
├─ Full microservices (if traffic justifies)
├─ ISO 15118 V2G support (optional)
└─ Global expansion (region-specific databases)
```

## Related Documentation

- **Technology Decisions**: See each ADR for detailed rationale
- **Coding Standards**: [docs/08-guides/coding-conventions.md](../08-guides/coding-conventions.md)
- **Module Architecture**: [ADR-004](./ADR-004-modular-monolith.md) for module design
- **API Design**: [docs/08-guides/api-guide.md](../08-guides/api-guide.md) for REST/CQRS patterns
- **Deployment**: [docs/08-guides/deployment-guide.md](../08-guides/deployment-guide.md) for infrastructure

## Adding a New ADR

When a significant architectural decision needs documentation:

1. **Create file**: `ADR-NNN-short-title.md` (increment NNN)
2. **Use template**: Copy structure from ADR-001
3. **Fill sections**: Context, Decision, Consequences, Alternatives Considered
4. **Get approval**: CTO/Architecture Lead review before merge
5. **Update README**: Add row to ADR Index table above

### ADR Template

```markdown
# ADR-NNN: [Title]

> Status: ACCEPTED | Date: YYYY-MM-DD

## Context
[Problem statement, background, constraints]

## Decision
[What we decided and why]

## Consequences
### Positive
[List positive outcomes]

### Negative
[List trade-offs, downsides]

### Risks
[Table of risks and mitigations]

## Alternatives Considered
[Detailed analysis of rejected options]

## Related Decisions
[Links to other ADRs that depend on or inform this decision]

## References
[Links to external documentation]
```

## Questions?

Refer to:
- Individual ADR documents for detailed rationale
- [docs/09-ai-playbook/rules/_master-rules.md](../09-ai-playbook/rules/_master-rules.md) for coding standards
- CTO for architectural guidance or exceptions
