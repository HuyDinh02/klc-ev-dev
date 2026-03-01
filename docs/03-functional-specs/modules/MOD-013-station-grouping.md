# MOD-013: Station Grouping

> Status: APPROVED | Priority: Phase 2 (Basic in Phase 1) | Last Updated: 2026-03-01

## 1. Overview
Organizes charging stations into logical groups by site, region, or operator for management and reporting purposes. Phase 1 provides basic grouping by region.

## 2. Actors
| Actor | Role |
|-------|------|
| Admin | Create, manage, and assign station groups |

## 3. Functional Requirements
| ID | Requirement | Priority |
|----|------------|----------|
| FR-013-01 | Create station groups with name and description | Must |
| FR-013-02 | Assign stations to groups | Must |
| FR-013-03 | View stations by group | Must |
| FR-013-04 | Filter monitoring dashboard by group | Should |
| FR-013-05 | Group-level reporting (aggregate energy, revenue) | Phase 2 |

## 4. Business Rules
| ID | Rule |
|----|------|
| BR-013-01 | A station can belong to one group at a time |
| BR-013-02 | Groups can be nested (region → site) in Phase 2 |
| BR-013-03 | Deleting a group unassigns stations (doesn't delete them) |

## 5. Data Model
### StationGroup (Aggregate Root)
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Id | Guid | Yes | Auto-generated |
| Name | string(200) | Yes | Group name |
| Description | string(1000) | No | Group description |
| Region | string(100) | No | Region identifier |
| ParentGroupId | Guid? | No | Parent group (Phase 2) |

## 6. API Endpoints
| Method | Path | Description | Auth |
|--------|------|-------------|------|
| POST | /api/v1/station-groups | Create group | Admin |
| GET | /api/v1/station-groups | List groups | Admin, Operator |
| PUT | /api/v1/station-groups/{id} | Update group | Admin |
| DELETE | /api/v1/station-groups/{id} | Delete group | Admin |
| POST | /api/v1/station-groups/{id}/assign | Assign station to group | Admin |

## 7. Testing Scenarios
| ID | Scenario | Expected Result |
|----|----------|----------------|
| TC-013-01 | Create group | Group created |
| TC-013-02 | Assign station to group | Station grouped correctly |
| TC-013-03 | Delete group | Stations unassigned, group deleted |
| TC-013-04 | Filter dashboard by group | Correct stations shown |
