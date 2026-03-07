# MVP Gap Filling — Implementation Log

> Started: 2026-03-07 | Status: IN PROGRESS

## Context

After completing Phase 1 Pre-MVP Hardening (7 tasks), a readiness assessment identified remaining gaps before June 1 MVP go-live. This log tracks gap-filling work.

## Completed

### Maintenance Module (Full Stack) ✅
**Goal**: Replace mock maintenance page with fully functional backend + wired frontend.

**Backend Files Created:**
- `KLC.Domain.Shared/Enums/MaintenanceTaskStatus.cs` — MaintenanceTaskStatus (Planned/InProgress/Completed/Cancelled) + MaintenanceTaskType (Scheduled/Inspection/Emergency)
- `KLC.Domain/Maintenance/MaintenanceTask.cs` — Entity with state machine (Start/Complete/Cancel), overdue detection
- `KLC.Domain.Shared/KLCDomainErrorCodes.cs` — Added Maintenance.NotFound, Maintenance.InvalidStateTransition
- `KLC.Application.Contracts/Permissions/KLCPermissions.cs` — Maintenance CRUD permissions
- `KLC.Application.Contracts/Permissions/KLCPermissionDefinitionProvider.cs` — Registered permissions
- `KLC.Application.Contracts/Maintenance/MaintenanceDtos.cs` — 7 DTOs (Task, Stats, Create, Update, List, Complete, Cancel)
- `KLC.Application.Contracts/Maintenance/IMaintenanceAppService.cs` — Interface (9 methods)
- `KLC.Application/Maintenance/MaintenanceAppService.cs` — Full CRUD + state transitions + stats
- `KLC.HttpApi/Controllers/Maintenance/MaintenanceController.cs` — 9 API endpoints
- `KLC.EntityFrameworkCore/KLCDbContext.cs` — DbSet + entity config (indexes on StationId, Status, ScheduledDate)
- Migration: `AddMaintenanceTasks`

**Frontend Files Modified:**
- `admin-portal/src/lib/api.ts` — Added `maintenanceApi` with 9 functions + `CreateMaintenanceTaskDto`
- `admin-portal/src/app/(dashboard)/maintenance/page.tsx` — Replaced mock data with real API calls, create/start/complete/cancel mutations

**API Endpoints:**
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/v1/maintenance` | List tasks (filter by status/type/station) |
| GET | `/api/v1/maintenance/{id}` | Get task detail |
| GET | `/api/v1/maintenance/stats` | Get counts (planned/in-progress/completed/overdue) |
| POST | `/api/v1/maintenance` | Create task |
| PUT | `/api/v1/maintenance/{id}` | Update task |
| DELETE | `/api/v1/maintenance/{id}` | Delete task |
| POST | `/api/v1/maintenance/{id}/start` | Start task |
| POST | `/api/v1/maintenance/{id}/complete` | Complete task |
| POST | `/api/v1/maintenance/{id}/cancel` | Cancel task |

**Tests:** 10 new domain tests (constructor, state transitions, overdue, update)

**Status:** ✅ Complete

### Integration Tests for Critical MVP Paths ✅
**Goal**: Add integration tests for payment, voucher, wallet, and maintenance flows.

**New Tests Added (16 total):**

**WalletBffServiceTests** (2 new):
- `TopUp_Should_Fail_When_Monthly_Limit_Exceeded` — SBV Circular 41/2025 compliance: 100M VND monthly cap
- `TopUp_Should_Succeed_When_Under_Monthly_Limit` — Validates top-up works within cap

**PaymentBffServiceTests** (3 new):
- `ProcessPayment_Should_Apply_FreeCharging_Voucher` — VoucherType.FreeCharging covers full session cost
- `ProcessPayment_Should_Apply_Percentage_Voucher_With_MaxDiscount` — Percentage discount capped at MaxDiscountAmount
- `ProcessPayment_Should_Fail_When_Voucher_Already_Used_By_User` — Prevents double-use of vouchers

**MaintenanceAppServiceTests** (11 new, abstract + EF Core):
- `Should_Create_Maintenance_Task` — CRUD create with station validation
- `Should_Throw_When_Creating_With_Invalid_Station` — Non-existent station rejection
- `Should_Get_Task_By_Id` — Single task retrieval
- `Should_List_Tasks_With_Filtering` — List with type/station filters
- `Should_Start_Planned_Task` — Planned → InProgress state transition
- `Should_Complete_InProgress_Task` — InProgress → Completed with notes
- `Should_Cancel_Planned_Task` — Planned → Cancelled with notes
- `Should_Throw_When_Starting_Already_Started_Task` — Invalid state transition error
- `Should_Update_Task_Fields` — Partial field update
- `Should_Get_Stats` — Counts by status
- `Should_Delete_Task` — Soft delete

**Files Created:**
- `test/KLC.Application.Tests/Maintenance/MaintenanceAppServiceTests.cs` — Abstract base (11 tests)
- `test/KLC.EntityFrameworkCore.Tests/EntityFrameworkCore/Applications/EfCoreMaintenanceAppServiceTests.cs` — Concrete EF Core runner

**Files Modified:**
- `test/KLC.EntityFrameworkCore.Tests/EntityFrameworkCore/BffServices/WalletBffServiceTests.cs` — +2 tests (monthly limit)
- `test/KLC.EntityFrameworkCore.Tests/EntityFrameworkCore/BffServices/PaymentBffServiceTests.cs` — +3 tests (voucher types)

**Status:** ✅ Complete

## Remaining MVP Gaps (Require External Dependencies)

| Gap | Blocker | Notes |
|-----|---------|-------|
| Real MoMo payment integration | Need MoMo merchant credentials | Stub exists |
| Real VnPay payment integration | Need VnPay merchant credentials | Stub exists |
| Real SMS service | Need SMS provider account | LogOnly stub exists |
| OCPP vendor profile validation | Need real charger log samples | Profiles exist with TODOs |

## Test Results

| Date | Tests | Pass | Fail | Notes |
|------|-------|------|------|-------|
| 2026-03-07 (maintenance) | 390 | 390 | 0 | 10 new maintenance + 380 existing |
| 2026-03-07 (integration tests) | 406 | 406 | 0 | 16 new integration + 390 existing |
