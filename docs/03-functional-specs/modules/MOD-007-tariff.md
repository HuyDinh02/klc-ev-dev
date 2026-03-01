# MOD-007: Tariff Configuration

> Status: APPROVED | Priority: Phase 1 | Last Updated: 2026-03-01

## 1. Overview
Manages pricing rules for charging sessions. Supports per-kWh pricing, time-of-use rates, location-based pricing, membership discounts, and tax configuration.

## 2. Actors
| Actor | Role |
|-------|------|
| Admin | Create, configure, and manage tariff plans |
| Finance | View pricing configuration, validate billing |

## 3. Functional Requirements
| ID | Requirement | Priority |
|----|------------|----------|
| FR-007-01 | Create tariff plans with per-kWh base rate | Must |
| FR-007-02 | Configure time-of-use pricing (peak/off-peak rates) | Phase 2 |
| FR-007-03 | Assign tariff plans to stations or station groups | Must |
| FR-007-04 | Configure tax rates per tariff plan | Must |
| FR-007-05 | Set effective date ranges for tariff plans | Must |
| FR-007-06 | Create membership tiers with discount percentages | Should |
| FR-007-07 | Auto-apply discounts during cost calculation | Should |
| FR-007-08 | View tariff plan history and changes | Should |

## 4. Business Rules
| ID | Rule |
|----|------|
| BR-007-01 | Each station must have an assigned tariff plan |
| BR-007-02 | Only one active tariff plan per station at any time |
| BR-007-03 | Tariff changes apply to new sessions only, not in-progress sessions |
| BR-007-04 | Tax rate applied on top of base energy cost |
| BR-007-05 | Membership discount applied before tax calculation |

## 5. Data Model
### TariffPlan (Aggregate Root)
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Id | Guid | Yes | ABP auto-generated |
| Name | string(200) | Yes | Plan name |
| Description | string(1000) | No | Plan description |
| BaseRatePerKwh | decimal | Yes | Base price per kWh (VNĐ) |
| PeakRatePerKwh | decimal? | No | Peak hour rate (Phase 2) |
| OffPeakRatePerKwh | decimal? | No | Off-peak rate (Phase 2) |
| TaxRatePercent | decimal | Yes | Tax percentage |
| EffectiveFrom | DateTime | Yes | Start date |
| EffectiveTo | DateTime? | No | End date (null = no end) |
| IsActive | bool | Yes | Currently active |

### MembershipDiscount (Entity)
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Id | Guid | Yes | Auto-generated |
| TariffPlanId | Guid | Yes | FK |
| MembershipTier | string(100) | Yes | Tier name (Silver, Gold, etc.) |
| DiscountPercent | decimal | Yes | Discount percentage |

## 6. API Endpoints
| Method | Path | Description | Auth |
|--------|------|-------------|------|
| POST | /api/v1/tariffs | Create tariff plan | Admin |
| GET | /api/v1/tariffs | List tariff plans | Admin, Finance |
| GET | /api/v1/tariffs/{id} | Get tariff detail | Admin, Finance |
| PUT | /api/v1/tariffs/{id} | Update tariff plan | Admin |
| POST | /api/v1/tariffs/{id}/deactivate | Deactivate plan | Admin |

## 7. Cost Calculation Formula
```
energyCost = totalKwh × ratePerKwh
discount = energyCost × membershipDiscountPercent / 100
subtotal = energyCost - discount
tax = subtotal × taxRatePercent / 100
totalCost = subtotal + tax
```

## 8. Error Handling
| Code | Message | HTTP Status |
|------|---------|-------------|
| MOD_007_001 | Tariff plan not found | 404 |
| MOD_007_002 | Overlapping effective dates for same station | 409 |
| MOD_007_003 | Cannot deactivate plan with assigned stations | 400 |

## 9. Testing Scenarios
| ID | Scenario | Expected Result |
|----|----------|----------------|
| TC-007-01 | Create tariff with valid base rate | Plan created, active |
| TC-007-02 | Calculate cost for 10 kWh session | Correct cost with tax |
| TC-007-03 | Apply membership discount | Discount applied before tax |
| TC-007-04 | Change tariff during active session | Old tariff used for current session |
