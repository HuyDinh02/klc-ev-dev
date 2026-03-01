# AGENTS.md — AI Agent Configuration

## Agent Roles

### 🏗️ Architect Agent
- **Scope:** Architecture decisions, system design
- **Read:** docs/04-architecture/, docs/05-decisions/, memory-bank/architecture.md
- **Write:** ADR files, architecture docs

### 💻 Backend Agent
- **Scope:** .NET implementation, ABP modules, CQRS handlers
- **Read:** docs/03-functional-specs/modules/, docs/09-ai-playbook/rules/, memory-bank/api-contracts.md
- **Write:** src/ code, unit tests

### 📱 Mobile Agent
- **Scope:** React Native (Expo) driver app
- **Read:** docs/03-functional-specs/wireframes/, memory-bank/driver-app-rn.md
- **Write:** mobile/ code

### 🔌 OCPP Agent
- **Scope:** OCPP.Core integration, WebSocket handlers
- **Read:** docs/03-functional-specs/modules/MOD-006-ocpp.md, memory-bank/tech-stack.md
- **Write:** OCPP integration code

### 🧪 QA Agent
- **Scope:** Test creation, test execution
- **Read:** docs/07-testing/, docs/09-ai-playbook/rules/testing-rules.md
- **Write:** Test files

## Workflow

1. Read `CLAUDE.md` (auto-loaded)
2. Read relevant `memory-bank/` file
3. Read relevant FRS module in `docs/03-functional-specs/modules/`
4. Read rules: `docs/09-ai-playbook/rules/_master-rules.md`
5. Implement
6. Run tests
7. If error → check `docs/09-ai-playbook/debug-playbooks/`
8. Commit with conventional message
