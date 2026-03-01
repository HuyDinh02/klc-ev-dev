# MOD-009: Vehicle Management

> Status: APPROVED | Priority: Phase 1 | Last Updated: 2026-03-01

## 1. Overview
Allows EV drivers to register and manage their electric vehicles in the mobile app. Supports selecting an active vehicle for charging sessions.

## 2. Actors
| Actor | Role |
|-------|------|
| EV Driver | Add, edit, delete vehicles; select active vehicle |

## 3. Functional Requirements
| ID | Requirement | Priority |
|----|------------|----------|
| FR-009-01 | Add a new vehicle with make, model, and basic information | Must |
| FR-009-02 | Edit vehicle information | Must |
| FR-009-03 | Delete a vehicle (soft delete) | Must |
| FR-009-04 | Select active vehicle for charging sessions | Must |
| FR-009-05 | View list of registered vehicles | Must |

## 4. Business Rules
| ID | Rule |
|----|------|
| BR-009-01 | User must have at least one vehicle to start a charging session |
| BR-009-02 | Only one active vehicle at a time per user |
| BR-009-03 | Vehicle with active charging session cannot be deleted |
| BR-009-04 | Vehicle information includes: make, model, year, license plate (optional), connector type preference |

## 5. Data Model
### Vehicle (Entity)
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Id | Guid | Yes | Auto-generated |
| UserId | Guid | Yes | FK to AppUser |
| Make | string(100) | Yes | Manufacturer |
| Model | string(100) | Yes | Vehicle model |
| Year | int? | No | Manufacturing year |
| LicensePlate | string(20) | No | License plate number |
| BatteryCapacityKwh | decimal? | No | Battery capacity |
| PreferredConnectorType | ConnectorType? | No | Type2, CCS, CHAdeMO |
| IsActive | bool | Yes | Currently selected vehicle |

## 6. API Endpoints (Driver BFF)
| Method | Path | Description | Auth |
|--------|------|-------------|------|
| POST | /api/v1/vehicles | Add vehicle | Driver |
| GET | /api/v1/vehicles | List user's vehicles | Driver |
| GET | /api/v1/vehicles/{id} | Get vehicle detail | Driver |
| PUT | /api/v1/vehicles/{id} | Update vehicle | Driver |
| DELETE | /api/v1/vehicles/{id} | Delete vehicle | Driver |
| POST | /api/v1/vehicles/{id}/set-active | Set as active vehicle | Driver |

## 7. Error Handling
| Code | Message | HTTP Status |
|------|---------|-------------|
| MOD_009_001 | Vehicle not found | 404 |
| MOD_009_002 | Cannot delete vehicle with active session | 400 |

## 8. Testing Scenarios
| ID | Scenario | Expected Result |
|----|----------|----------------|
| TC-009-01 | Add vehicle with valid data | Vehicle created |
| TC-009-02 | Set vehicle as active | Previous active deselected, new one active |
| TC-009-03 | Delete vehicle with no session | Soft deleted |
| TC-009-04 | Delete vehicle with active session | 400 error |
