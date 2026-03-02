# Phase 1 — Cowork Research Prompt

> Paste this prompt into a Claude Project conversation with the 5 project files uploaded.

---

## Instructions

You are helping build the EV Charging CSMS (KLC) project. I have uploaded the following project files:

1. **BRD** — Business Requirements Document
2. **Kickoff Document** — Project kickoff presentation/notes
3. **UI/Wireframes** — App and Web interface designs
4. **Features List** — Charging Station Management Features (Excel)
5. **Reference Images** — Screenshots and mockups

Based on these documents, please generate the following outputs:

### Part A: docs/ Content (Fill in TODO placeholders)

For each file below, read the uploaded documents and fill in the `<!-- TODO -->` sections with real content extracted from the project files:

1. `docs/01-business/brd.md` — Extract from BRD document
2. `docs/01-business/stakeholder-analysis.md` — Extract stakeholders
3. `docs/01-business/market-context.md` — Vietnam EV context from kickoff
4. `docs/02-requirements/functional-requirements.md` — Detailed FRs with acceptance criteria
5. `docs/02-requirements/non-functional-requirements.md` — NFRs from BRD/kickoff
6. `docs/02-requirements/integration-requirements.md` — Integration details
7. `docs/02-requirements/user-stories.md` — User stories with acceptance criteria
8. `docs/03-functional-specs/modules/MOD-001-station-management.md` through `MOD-015-e-invoice.md` — Fill all 15 module FRS
9. `docs/04-architecture/system-overview.md` — System architecture from kickoff
10. `docs/04-architecture/database-design.md` — Entity relationships from features list

### Part B: memory-bank/ Files

Create these condensed context files for AI agents:

1. `memory-bank/project-brief.md` — 1-page project summary
2. `memory-bank/tech-stack.md` — Technology choices and rationale
3. `memory-bank/architecture.md` — Architecture overview
4. `memory-bank/api-contracts.md` — API endpoint contracts
5. `memory-bank/data-model.md` — Database entities and relationships
6. `memory-bank/ocpp-flows.md` — OCPP message flows
7. `memory-bank/driver-app-rn.md` — Mobile app context
8. `memory-bank/admin-portal.md` — Admin portal context
9. `memory-bank/payment-integration.md` — Payment gateway details
10. `memory-bank/active-context.md` — Current sprint/focus context

### Output Format

For each file, output:
```
=== FILE: {path} ===
{content}
=== END FILE ===
```

This allows easy copy-paste into the repo.
