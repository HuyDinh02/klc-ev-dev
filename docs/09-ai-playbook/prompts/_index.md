# Prompt Templates Index

> 📋 Reusable prompt templates for AI agents

## PT-001: Implement New Module
```
Read the FRS: docs/03-functional-specs/modules/MOD-{NNN}.md
Read rules: docs/09-ai-playbook/rules/_master-rules.md
Check anti-patterns: docs/09-ai-playbook/anti-patterns/_index.md

Implement:
1. Domain entities (Domain layer)
2. Repository interfaces (Domain layer)
3. Application DTOs (Application.Contracts)
4. Application services (Application layer)
5. EF Core mapping (EntityFrameworkCore layer)
6. API controllers (HttpApi layer)
7. Unit tests
8. Integration tests
```

## PT-002: Add OCPP Message Handler
```
Read: docs/03-functional-specs/modules/MOD-006-ocpp.md
Read: docs/09-ai-playbook/rules/_master-rules.md (R2: OCPP Rules)

Implement:
1. Message model (strongly-typed)
2. Handler service
3. Idempotency check
4. Persistence
5. Response generation
6. Unit tests
```

<!-- Add more prompt templates as needed -->
