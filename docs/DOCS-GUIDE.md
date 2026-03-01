# Documentation Folder Organization Guide

## Overview

This guide explains the 9-layer documentation architecture for the EV Charging CSMS project. Each layer serves a specific purpose and audience.

## Layer Structure

### Layer 01: Business (`docs/01-business/`)
**Audience:** Stakeholders, BA, Project Managers
**Purpose:** Business context, requirements from client perspective
**Contents:**
- `brd.md` — Business Requirements Document
- `stakeholder-analysis.md` — Who are the stakeholders and what they need
- `market-context.md` — Vietnam EV market landscape
- `originals/` — Source .docx/.xlsx files from client

### Layer 02: Requirements (`docs/02-requirements/`)
**Audience:** BA, Developers, QA
**Purpose:** Translated business needs into technical requirements
**Contents:**
- `functional-requirements.md` — What the system must do
- `non-functional-requirements.md` — How well it must do it (performance, security)
- `integration-requirements.md` — Third-party systems to integrate
- `user-stories.md` — Features from user perspective

### Layer 03: Functional Specifications (`docs/03-functional-specs/`)
**Audience:** Developers, QA, Tech Lead
**Purpose:** Detailed specs for each module — the blueprint for implementation
**Contents:**
- `modules/MOD-{NNN}-{name}.md` — Per-module FRS (one per feature area)
- `wireframes/` — UI designs and mockups
- `api-specs/` — REST API endpoint specifications

**Naming Convention:** `MOD-001` through `MOD-015` for Phase 1 modules

### Layer 04: Architecture (`docs/04-architecture/`)
**Audience:** Tech Lead, Architect, Senior Developers
**Purpose:** System design and technical architecture
**Contents:**
- `system-overview.md` — High-level architecture diagram and description
- `backend-architecture.md` — .NET/ABP layered architecture
- `database-design.md` — PostgreSQL schema design
- `ocpp-architecture.md` — OCPP WebSocket communication design
- `mobile-architecture.md` — React Native app structure
- `deployment-architecture.md` — AWS infrastructure
- `security-architecture.md` — Auth, encryption, access control

### Layer 05: Decisions (`docs/05-decisions/`)
**Audience:** Tech Lead, Architect, Future Developers
**Purpose:** Record WHY decisions were made (not just what)
**Format:** Architecture Decision Records (ADRs)
**Naming:** `ADR-{NNN}-{slug}.md`

### Layer 06: Project Management (`docs/06-project-management/`)
**Audience:** Project Manager, Tech Lead, Client
**Purpose:** Timeline, team, risks, status tracking
**Contents:**
- `project-plan.md` — Milestones and timeline
- `team-structure.md` — Roles and responsibilities
- `risk-register.md` — Risks and mitigations

### Layer 07: Testing (`docs/07-testing/`)
**Audience:** QA, Developers
**Purpose:** Test strategy, plans, and specific test scenarios
**Contents:**
- `test-strategy.md` — Overall approach to testing
- `test-plan.md` — Detailed test plan per module
- `ocpp-test-scenarios.md` — OCPP-specific test cases

### Layer 08: Guides (`docs/08-guides/`)
**Audience:** Developers (new and existing)
**Purpose:** How-to guides for development and operations
**Contents:**
- `dev-setup.md` — Local environment setup
- `coding-conventions.md` — Code style and patterns
- `deployment-guide.md` — How to deploy
- `api-guide.md` — How to use the APIs

### Layer 09: AI Playbook (`docs/09-ai-playbook/`)
**Audience:** AI Agents (Claude, Copilot, etc.)
**Purpose:** Living knowledge base that AI agents read before coding
**Contents:**
- `rules/` — MUST-follow rules (`_master-rules.md`)
- `anti-patterns/` — NEVER-do patterns (learned from mistakes)
- `patterns/` — Proven code patterns to reuse
- `lessons-learned/` — Specific incidents and fixes
- `debug-playbooks/` — Step-by-step debugging guides
- `prompts/` — Reusable prompt templates

## Naming Conventions

| Item | Format | Example |
|------|--------|---------|
| Module FRS | `MOD-{NNN}-{slug}.md` | `MOD-001-station-management.md` |
| ADR | `ADR-{NNN}-{slug}.md` | `ADR-001-abp-framework.md` |
| Anti-pattern | `AP-{NNN}` | `AP-001: Business Logic in AppService` |
| Pattern | `PAT-{NNN}` | `PAT-001: ABP Entity Creation` |
| Lesson | `LL-{NNN}` | `LL-001: OCPP Deserialization Failure` |
| Debug playbook | `DB-{NNN}` | `DB-001: Charger Not Connecting` |
| Prompt template | `PT-{NNN}` | `PT-001: Implement New Module` |

## How to Use

### For Developers
1. Start with `08-guides/dev-setup.md` for environment setup
2. Read `08-guides/coding-conventions.md` for code standards
3. Before implementing a feature, read its `03-functional-specs/modules/MOD-{NNN}.md`

### For AI Agents
1. `CLAUDE.md` is auto-loaded (contains quick reference)
2. Read `09-ai-playbook/rules/_master-rules.md` before any implementation
3. Check `09-ai-playbook/anti-patterns/` to avoid known mistakes
4. Use `09-ai-playbook/patterns/` for proven implementations
5. When stuck, check `09-ai-playbook/debug-playbooks/`

### For Project Managers
1. `06-project-management/project-plan.md` for timeline
2. `06-project-management/risk-register.md` for risk tracking
3. `01-business/brd.md` for business context

## File Lifecycle

```
DRAFT → REVIEW → APPROVED → UPDATED (with version history)
```

All documents start as DRAFT with `<!-- TODO -->` placeholders. They evolve as the project progresses.
