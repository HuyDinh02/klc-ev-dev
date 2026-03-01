# Anti-Patterns Index

> ❌ NEVER do these things

## AP-001: Business Logic in Application Service
**Wrong:** Putting validation/business rules in AppService
**Right:** Put them in Domain Entity or Domain Service

## AP-002: Exposing Domain Entities via API
**Wrong:** Returning `ChargingStation` entity from API
**Right:** Map to `ChargingStationDto` using AutoMapper

## AP-003: Offset-based Pagination
**Wrong:** `?page=5&pageSize=20`
**Right:** `?cursor=abc123&limit=20`

## AP-004: Hardcoded UI Strings
**Wrong:** `"Trạm sạc không khả dụng"` in code
**Right:** `L["StationNotAvailable"]` via IStringLocalizer

## AP-005: Manual Database Changes
**Wrong:** Running ALTER TABLE directly
**Right:** Create migration via ABP DbMigrator

<!-- Add more anti-patterns as discovered -->
