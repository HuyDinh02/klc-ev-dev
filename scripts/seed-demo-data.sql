-- ============================================================
-- KLC EV Charging CSMS - Demo Seed Data (With Auth)
-- ============================================================
-- Run with: PGPASSWORD=postgres psql -h localhost -p 5433 -U postgres -d KLC -f scripts/seed-demo-data.sql
-- ============================================================

-- SAFETY CHECK: Prevent accidental execution against production database
DO $$
BEGIN
  IF current_database() NOT IN ('KLC', 'klc_dev', 'klc_test', 'klc_staging') THEN
    RAISE EXCEPTION 'SAFETY: Seed script blocked — current database "%" is not a known dev/test database. '
      'If this is intentional, add the database name to the allowlist in this script.', current_database();
  END IF;

  -- Extra guard: abort if the database has > 1000 real charging sessions (likely production)
  IF (SELECT count(*) FROM "ChargingSessions" WHERE "IsDeleted" = false) > 1000 THEN
    RAISE EXCEPTION 'SAFETY: Seed script blocked — database has > 1000 sessions, which looks like production data.';
  END IF;
END $$;

BEGIN;

-- ============================================================
-- 0. ABP IDENTITY - ADMIN USERS, ROLES & PERMISSIONS
-- ============================================================

-- Clean up existing demo roles and users
DELETE FROM "AbpUserRoles" WHERE "RoleId" IN (
  SELECT "Id" FROM "AbpRoles" WHERE "Name" IN ('admin', 'operator', 'viewer')
);
DELETE FROM "AbpPermissionGrants" WHERE "ProviderKey" IN ('admin', 'operator', 'viewer');
DELETE FROM "AbpRoles" WHERE "Name" IN ('admin', 'operator', 'viewer');
DELETE FROM "AbpUsers" WHERE "UserName" IN ('admin', 'operator', 'viewer', 'demo_admin', 'demo_operator', 'demo_viewer');

-- Create Roles
INSERT INTO "AbpRoles" ("Id", "TenantId", "Name", "NormalizedName", "IsDefault", "IsStatic", "IsPublic", "EntityVersion", "CreationTime", "ExtraProperties", "ConcurrencyStamp")
VALUES
('11111111-aaaa-aaaa-aaaa-111111111111', NULL, 'admin', 'ADMIN', false, true, true, 0, NOW(), '{}', 'seed-role-admin'),
('22222222-aaaa-aaaa-aaaa-222222222222', NULL, 'operator', 'OPERATOR', false, false, true, 0, NOW(), '{}', 'seed-role-operator'),
('33333333-aaaa-aaaa-aaaa-333333333333', NULL, 'viewer', 'VIEWER', false, false, true, 0, NOW(), '{}', 'seed-role-viewer');

-- Create Admin Users
-- Password: Admin@123 (ASP.NET Core Identity v3 hash - generated using PasswordHasher<T>)
-- Note: In production, use proper password hashing. This is a pre-computed hash for demo.
INSERT INTO "AbpUsers" ("Id", "TenantId", "UserName", "NormalizedUserName", "Name", "Surname", "Email", "NormalizedEmail", "EmailConfirmed", "PasswordHash", "SecurityStamp", "IsExternal", "PhoneNumber", "PhoneNumberConfirmed", "IsActive", "TwoFactorEnabled", "LockoutEnd", "LockoutEnabled", "AccessFailedCount", "ShouldChangePasswordOnNextLogin", "EntityVersion", "LastPasswordChangeTime", "ExtraProperties", "ConcurrencyStamp", "CreationTime", "IsDeleted")
VALUES
-- Admin user (full access)
('aaaaaaaa-0001-0001-0001-000000000001', NULL, 'admin', 'ADMIN', 'Admin', 'System', 'admin@klc.vn', 'ADMIN@KLC.VN', true, 'AQAAAAIAAYagAAAAEBU9tr2cNLRUx4IZ+LyvffX2xVYLIM156L5jEOTYPQ6ihyvDGP4kDIToS7WhmeJMmw==', 'SEED-ADMIN-001', false, '0900000001', true, true, false, NULL, true, 0, false, 0, NOW(), '{}', 'seed-user-admin', NOW(), false),
-- Operator user (station management)
('aaaaaaaa-0001-0001-0001-000000000002', NULL, 'operator', 'OPERATOR', 'Operator', 'User', 'operator@klc.vn', 'OPERATOR@KLC.VN', true, 'AQAAAAIAAYagAAAAEBU9tr2cNLRUx4IZ+LyvffX2xVYLIM156L5jEOTYPQ6ihyvDGP4kDIToS7WhmeJMmw==', 'SEED-OP-001', false, '0900000002', true, true, false, NULL, true, 0, false, 0, NOW(), '{}', 'seed-user-op', NOW(), false),
-- Viewer user (read-only)
('aaaaaaaa-0001-0001-0001-000000000003', NULL, 'viewer', 'VIEWER', 'Viewer', 'User', 'viewer@klc.vn', 'VIEWER@KLC.VN', true, 'AQAAAAIAAYagAAAAEBU9tr2cNLRUx4IZ+LyvffX2xVYLIM156L5jEOTYPQ6ihyvDGP4kDIToS7WhmeJMmw==', 'SEED-VIEW-001', false, '0900000003', true, true, false, NULL, true, 0, false, 0, NOW(), '{}', 'seed-user-viewer', NOW(), false);

-- Assign Roles to Users
INSERT INTO "AbpUserRoles" ("UserId", "RoleId", "TenantId")
VALUES
('aaaaaaaa-0001-0001-0001-000000000001', '11111111-aaaa-aaaa-aaaa-111111111111', NULL), -- admin -> admin role
('aaaaaaaa-0001-0001-0001-000000000002', '22222222-aaaa-aaaa-aaaa-222222222222', NULL), -- operator -> operator role
('aaaaaaaa-0001-0001-0001-000000000003', '33333333-aaaa-aaaa-aaaa-333333333333', NULL); -- viewer -> viewer role

-- Grant Permissions to Admin Role (all permissions)
INSERT INTO "AbpPermissionGrants" ("Id", "TenantId", "Name", "ProviderName", "ProviderKey")
VALUES
-- Admin gets all KLC permissions
(gen_random_uuid(), NULL, 'KLC.Stations', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Stations.Create', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Stations.Update', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Stations.Delete', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Stations.Decommission', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Connectors', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Connectors.Create', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Connectors.Update', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Connectors.Delete', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Connectors.Enable', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Connectors.Disable', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Tariffs', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Tariffs.Create', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Tariffs.Update', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Tariffs.Activate', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Tariffs.Deactivate', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Sessions', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Sessions.ViewAll', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Faults', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Faults.Update', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Alerts', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Alerts.Acknowledge', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Monitoring', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Monitoring.Dashboard', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Monitoring.StatusHistory', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Monitoring.EnergySummary', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.StationGroups', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.StationGroups.Create', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.StationGroups.Update', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.StationGroups.Delete', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.StationGroups.Assign', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Payments', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Payments.ViewAll', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Payments.Refund', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.AuditLogs', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.AuditLogs.Export', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.EInvoices', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.EInvoices.Generate', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.EInvoices.Retry', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.EInvoices.Cancel', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.UserManagement', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.UserManagement.Create', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.UserManagement.Update', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.UserManagement.Delete', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.UserManagement.ManageRoles', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.UserManagement.ManagePermissions', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.RoleManagement', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.RoleManagement.Create', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.RoleManagement.Update', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.RoleManagement.Delete', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.RoleManagement.ManagePermissions', 'R', 'admin'),
-- New module permissions for admin
(gen_random_uuid(), NULL, 'KLC.Vouchers', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Vouchers.Create', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Vouchers.Update', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Vouchers.Delete', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Promotions', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Promotions.Create', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Promotions.Update', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Promotions.Delete', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Feedback', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Feedback.Respond', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.MobileUsers', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.MobileUsers.ViewAll', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.MobileUsers.Suspend', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.MobileUsers.WalletAdjust', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Notifications', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Notifications.Broadcast', 'R', 'admin'),
-- Phase 2 Module: Power Sharing permissions
(gen_random_uuid(), NULL, 'KLC.PowerSharing', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.PowerSharing.Create', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.PowerSharing.Update', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.PowerSharing.Delete', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.PowerSharing.ManageMembers', 'R', 'admin'),
-- Phase 2 Module: Operators permissions
(gen_random_uuid(), NULL, 'KLC.Operators', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Operators.Create', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Operators.Update', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Operators.Delete', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Operators.ManageStations', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Operators.ManageWebhooks', 'R', 'admin'),
-- Phase 2 Module: Fleets permissions
(gen_random_uuid(), NULL, 'KLC.Fleets', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Fleets.Create', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Fleets.Update', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Fleets.Delete', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Fleets.ManageVehicles', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Fleets.ManageSchedules', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Fleets.ViewAnalytics', 'R', 'admin'),
-- Maintenance permissions
(gen_random_uuid(), NULL, 'KLC.Maintenance', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Maintenance.Create', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Maintenance.Update', 'R', 'admin'),
(gen_random_uuid(), NULL, 'KLC.Maintenance.Delete', 'R', 'admin');

-- Grant Permissions to Operator Role (station management, monitoring, faults)
INSERT INTO "AbpPermissionGrants" ("Id", "TenantId", "Name", "ProviderName", "ProviderKey")
VALUES
(gen_random_uuid(), NULL, 'KLC.Stations', 'R', 'operator'),
(gen_random_uuid(), NULL, 'KLC.Stations.Update', 'R', 'operator'),
(gen_random_uuid(), NULL, 'KLC.Connectors', 'R', 'operator'),
(gen_random_uuid(), NULL, 'KLC.Connectors.Update', 'R', 'operator'),
(gen_random_uuid(), NULL, 'KLC.Connectors.Enable', 'R', 'operator'),
(gen_random_uuid(), NULL, 'KLC.Connectors.Disable', 'R', 'operator'),
(gen_random_uuid(), NULL, 'KLC.Tariffs', 'R', 'operator'),
(gen_random_uuid(), NULL, 'KLC.Sessions', 'R', 'operator'),
(gen_random_uuid(), NULL, 'KLC.Sessions.ViewAll', 'R', 'operator'),
(gen_random_uuid(), NULL, 'KLC.Faults', 'R', 'operator'),
(gen_random_uuid(), NULL, 'KLC.Faults.Update', 'R', 'operator'),
(gen_random_uuid(), NULL, 'KLC.Alerts', 'R', 'operator'),
(gen_random_uuid(), NULL, 'KLC.Alerts.Acknowledge', 'R', 'operator'),
(gen_random_uuid(), NULL, 'KLC.Monitoring', 'R', 'operator'),
(gen_random_uuid(), NULL, 'KLC.Monitoring.Dashboard', 'R', 'operator'),
(gen_random_uuid(), NULL, 'KLC.Monitoring.StatusHistory', 'R', 'operator'),
(gen_random_uuid(), NULL, 'KLC.Monitoring.EnergySummary', 'R', 'operator'),
(gen_random_uuid(), NULL, 'KLC.StationGroups', 'R', 'operator'),
(gen_random_uuid(), NULL, 'KLC.Payments', 'R', 'operator'),
(gen_random_uuid(), NULL, 'KLC.Payments.ViewAll', 'R', 'operator'),
(gen_random_uuid(), NULL, 'KLC.Feedback', 'R', 'operator'),
(gen_random_uuid(), NULL, 'KLC.Feedback.Respond', 'R', 'operator'),
(gen_random_uuid(), NULL, 'KLC.MobileUsers', 'R', 'operator'),
(gen_random_uuid(), NULL, 'KLC.MobileUsers.ViewAll', 'R', 'operator'),
-- Phase 2: operator read-only access
(gen_random_uuid(), NULL, 'KLC.PowerSharing', 'R', 'operator'),
(gen_random_uuid(), NULL, 'KLC.Operators', 'R', 'operator'),
(gen_random_uuid(), NULL, 'KLC.Fleets', 'R', 'operator'),
(gen_random_uuid(), NULL, 'KLC.Fleets.ViewAnalytics', 'R', 'operator'),
(gen_random_uuid(), NULL, 'KLC.Maintenance', 'R', 'operator');

-- Grant Permissions to Viewer Role (read-only)
INSERT INTO "AbpPermissionGrants" ("Id", "TenantId", "Name", "ProviderName", "ProviderKey")
VALUES
(gen_random_uuid(), NULL, 'KLC.Stations', 'R', 'viewer'),
(gen_random_uuid(), NULL, 'KLC.Connectors', 'R', 'viewer'),
(gen_random_uuid(), NULL, 'KLC.Tariffs', 'R', 'viewer'),
(gen_random_uuid(), NULL, 'KLC.Sessions', 'R', 'viewer'),
(gen_random_uuid(), NULL, 'KLC.Faults', 'R', 'viewer'),
(gen_random_uuid(), NULL, 'KLC.Alerts', 'R', 'viewer'),
(gen_random_uuid(), NULL, 'KLC.Monitoring', 'R', 'viewer'),
(gen_random_uuid(), NULL, 'KLC.Monitoring.Dashboard', 'R', 'viewer'),
(gen_random_uuid(), NULL, 'KLC.StationGroups', 'R', 'viewer'),
(gen_random_uuid(), NULL, 'KLC.Payments', 'R', 'viewer'),
-- Phase 2: viewer read-only access
(gen_random_uuid(), NULL, 'KLC.PowerSharing', 'R', 'viewer'),
(gen_random_uuid(), NULL, 'KLC.Operators', 'R', 'viewer'),
(gen_random_uuid(), NULL, 'KLC.Fleets', 'R', 'viewer'),
(gen_random_uuid(), NULL, 'KLC.Maintenance', 'R', 'viewer');

-- ============================================================
-- 1. STATION GROUPS (3 groups)
-- ============================================================
-- Must delete connectors -> stations -> groups in FK order
DELETE FROM "AppConnectors" WHERE "StationId" IN (
  'b1111111-1111-1111-1111-111111111111', 'b1111111-1111-1111-1111-111111111112',
  'b1111111-1111-1111-1111-111111111113', 'b1111111-1111-1111-1111-111111111114',
  'b2222222-2222-2222-2222-222222222221', 'b2222222-2222-2222-2222-222222222222',
  'b3333333-3333-3333-3333-333333333331', 'b3333333-3333-3333-3333-333333333332'
);
DELETE FROM "AppChargingStations" WHERE "StationCode" LIKE 'KC-%';
DELETE FROM "AppStationGroups" WHERE "Name" LIKE 'Khu vực%';

INSERT INTO "AppStationGroups" ("Id", "Name", "Description", "Region", "IsActive", "ExtraProperties", "ConcurrencyStamp", "CreationTime", "IsDeleted")
VALUES
('a0000001-0001-0001-0001-000000000001', 'Khu vực Hà Nội', 'Các trạm sạc tại Hà Nội và phía Bắc', 'Northern Vietnam', true, '{}', 'seed-sg001', NOW() - INTERVAL '90 days', false),
('a0000001-0001-0001-0001-000000000002', 'Khu vực TP.HCM', 'Các trạm sạc tại TP.HCM và phía Nam', 'Southern Vietnam', true, '{}', 'seed-sg002', NOW() - INTERVAL '90 days', false),
('a0000001-0001-0001-0001-000000000003', 'Khu vực miền Trung', 'Các trạm sạc tại Đà Nẵng và miền Trung', 'Central Vietnam', true, '{}', 'seed-sg003', NOW() - INTERVAL '90 days', false);

-- ============================================================
-- 2. TARIFF PLANS (3 plans)
-- NOTE: Stations reference these via TariffPlanId
-- ============================================================
DELETE FROM "AppTariffPlans" WHERE "Name" IN ('Standard', 'Peak Hours', 'Off-Peak');

INSERT INTO "AppTariffPlans" ("Id", "Name", "Description", "BaseRatePerKwh", "TaxRatePercent", "EffectiveFrom", "EffectiveTo", "IsActive", "IsDefault", "ExtraProperties", "ConcurrencyStamp", "CreationTime", "IsDeleted")
VALUES
('d1111111-1111-1111-1111-111111111111', 'Standard', 'Giá tiêu chuẩn áp dụng 24/7', 4000.00, 10.00, '2026-01-01', NULL, true, true, '{}', 'seed-t001', NOW(), false),
('d2222222-2222-2222-2222-222222222222', 'Peak Hours', 'Giá giờ cao điểm (6h-9h, 17h-22h)', 5500.00, 10.00, '2026-01-01', NULL, true, false, '{}', 'seed-t002', NOW(), false),
('d3333333-3333-3333-3333-333333333333', 'Off-Peak', 'Giá giờ thấp điểm (22h-6h)', 2800.00, 10.00, '2026-01-01', NULL, true, false, '{}', 'seed-t003', NOW(), false);

-- ============================================================
-- 3. CHARGING STATIONS (8 stations)
-- ============================================================
-- (Connectors + stations already deleted in section 1 for FK order)

INSERT INTO "AppChargingStations" ("Id", "StationCode", "Name", "Address", "Latitude", "Longitude", "Status", "FirmwareVersion", "Model", "Vendor", "SerialNumber", "StationGroupId", "TariffPlanId", "LastHeartbeat", "IsEnabled", "ExtraProperties", "ConcurrencyStamp", "CreationTime", "IsDeleted")
VALUES
-- Hanoi stations
('b1111111-1111-1111-1111-111111111111', 'KC-HN-001', 'KLC Times City', '458 Minh Khai, Hai Bà Trưng, Hà Nội', 21.0037, 105.8680, 1, 'v1.5.2', 'Wallbox Quasar 2', 'Wallbox', 'WB-HN-001', 'a0000001-0001-0001-0001-000000000001', 'd1111111-1111-1111-1111-111111111111', NOW() - INTERVAL '5 minutes', true, '{}', 'seed-cs001', NOW() - INTERVAL '60 days', false),
('b1111111-1111-1111-1111-111111111112', 'KC-HN-002', 'KLC Vincom Long Biên', 'Vincom Mega Mall, Long Biên, Hà Nội', 21.0367, 105.9167, 1, 'v1.5.2', 'ABB Terra AC', 'ABB', 'ABB-HN-002', 'a0000001-0001-0001-0001-000000000001', 'd1111111-1111-1111-1111-111111111111', NOW() - INTERVAL '3 minutes', true, '{}', 'seed-cs002', NOW() - INTERVAL '55 days', false),
('b1111111-1111-1111-1111-111111111113', 'KC-HN-003', 'KLC Aeon Hà Đông', 'Aeon Mall Hà Đông, Hà Nội', 20.9696, 105.7462, 0, 'v1.4.8', 'Schneider EVlink', 'Schneider', 'SCH-HN-003', 'a0000001-0001-0001-0001-000000000001', 'd1111111-1111-1111-1111-111111111111', NOW() - INTERVAL '35 minutes', true, '{}', 'seed-cs003', NOW() - INTERVAL '50 days', false),
('b1111111-1111-1111-1111-111111111114', 'KC-HN-004', 'KLC Vincom Bà Triệu', '191 Bà Triệu, Hai Bà Trưng, Hà Nội', 21.0115, 105.8492, 1, 'v1.5.2', 'ABB Terra DC', 'ABB', 'ABB-HN-004', 'a0000001-0001-0001-0001-000000000001', 'd2222222-2222-2222-2222-222222222222', NOW() - INTERVAL '2 minutes', true, '{}', 'seed-cs004', NOW() - INTERVAL '45 days', false),
-- HCM stations
('b2222222-2222-2222-2222-222222222221', 'KC-HCM-001', 'KLC Bitexco Tower', 'Tầng hầm B2, Bitexco Financial Tower, Q.1, TP.HCM', 10.7714, 106.7043, 2, 'v1.5.2', 'Delta AC Max', 'Delta', 'DLT-HCM-001', 'a0000001-0001-0001-0001-000000000002', 'd1111111-1111-1111-1111-111111111111', NOW() - INTERVAL '1 minute', true, '{}', 'seed-cs005', NOW() - INTERVAL '40 days', false),
('b2222222-2222-2222-2222-222222222222', 'KC-HCM-002', 'KLC SC VivoCity', 'SC VivoCity, Q.7, TP.HCM', 10.7234, 106.6973, 1, 'v1.5.1', 'Wallbox Pulsar Plus', 'Wallbox', 'WB-HCM-002', 'a0000001-0001-0001-0001-000000000002', 'd1111111-1111-1111-1111-111111111111', NOW() - INTERVAL '4 minutes', true, '{}', 'seed-cs006', NOW() - INTERVAL '35 days', false),
-- Da Nang stations
('b3333333-3333-3333-3333-333333333331', 'KC-DN-001', 'KLC Vincom Đà Nẵng', 'Vincom Plaza, Ngô Quyền, Đà Nẵng', 16.0572, 108.2200, 1, 'v1.5.0', 'ABB Terra AC', 'ABB', 'ABB-DN-001', 'a0000001-0001-0001-0001-000000000003', 'd1111111-1111-1111-1111-111111111111', NOW() - INTERVAL '8 minutes', true, '{}', 'seed-cs007', NOW() - INTERVAL '30 days', false),
('b3333333-3333-3333-3333-333333333332', 'KC-DN-002', 'KLC Indochina Riverside', 'Indochina Riverside Tower, Đà Nẵng', 16.0656, 108.2248, 3, 'v1.4.5', 'Schneider EVlink', 'Schneider', 'SCH-DN-002', 'a0000001-0001-0001-0001-000000000003', 'd3333333-3333-3333-3333-333333333333', NULL, false, '{}', 'seed-cs008', NOW() - INTERVAL '25 days', false);

-- ============================================================
-- 4. CONNECTORS (16 connectors across 8 stations)
-- ============================================================
INSERT INTO "AppConnectors" ("Id", "StationId", "ConnectorNumber", "ConnectorType", "MaxPowerKw", "Status", "IsEnabled", "CreationTime", "IsDeleted")
VALUES
-- KC-HN-001: CCS2 60kW (Available) + Type2 22kW (Charging)
('c1000000-0001-0001-0001-000000000001', 'b1111111-1111-1111-1111-111111111111', 1, 1, 60.00, 0, true, NOW() - INTERVAL '60 days', false),
('c1000000-0001-0001-0001-000000000002', 'b1111111-1111-1111-1111-111111111111', 2, 0, 22.00, 2, true, NOW() - INTERVAL '60 days', false),
-- KC-HN-002: CCS2 50kW (Available) + Type2 22kW (Available)
('c1000000-0001-0001-0001-000000000003', 'b1111111-1111-1111-1111-111111111112', 1, 1, 50.00, 0, true, NOW() - INTERVAL '55 days', false),
('c1000000-0001-0001-0001-000000000004', 'b1111111-1111-1111-1111-111111111112', 2, 0, 22.00, 0, true, NOW() - INTERVAL '55 days', false),
-- KC-HN-003: CCS2 50kW (Faulted - RFID reader failure)
('c1000000-0001-0001-0001-000000000005', 'b1111111-1111-1111-1111-111111111113', 1, 1, 50.00, 8, true, NOW() - INTERVAL '50 days', false),
-- KC-HN-004: 2x CCS2 120kW DC fast chargers (Available)
('c1000000-0001-0001-0001-000000000006', 'b1111111-1111-1111-1111-111111111114', 1, 1, 120.00, 0, true, NOW() - INTERVAL '45 days', false),
('c1000000-0001-0001-0001-000000000007', 'b1111111-1111-1111-1111-111111111114', 2, 1, 120.00, 0, true, NOW() - INTERVAL '45 days', false),
-- KC-HCM-001: CCS2 60kW (Charging) + Type2 22kW (Available)
('c1000000-0001-0001-0001-000000000008', 'b2222222-2222-2222-2222-222222222221', 1, 1, 60.00, 2, true, NOW() - INTERVAL '40 days', false),
('c1000000-0001-0001-0001-000000000009', 'b2222222-2222-2222-2222-222222222221', 2, 0, 22.00, 0, true, NOW() - INTERVAL '40 days', false),
-- KC-HCM-002: CCS2 50kW + Type2 22kW + CHAdeMO 50kW (all Available)
('c1000000-0001-0001-0001-000000000010', 'b2222222-2222-2222-2222-222222222222', 1, 1, 50.00, 0, true, NOW() - INTERVAL '35 days', false),
('c1000000-0001-0001-0001-000000000011', 'b2222222-2222-2222-2222-222222222222', 2, 0, 22.00, 0, true, NOW() - INTERVAL '35 days', false),
('c1000000-0001-0001-0001-000000000012', 'b2222222-2222-2222-2222-222222222222', 3, 2, 50.00, 0, true, NOW() - INTERVAL '35 days', false),
-- KC-DN-001: CCS2 50kW (Available) + Type2 22kW (Faulted - EV comm error)
('c1000000-0001-0001-0001-000000000013', 'b3333333-3333-3333-3333-333333333331', 1, 1, 50.00, 0, true, NOW() - INTERVAL '30 days', false),
('c1000000-0001-0001-0001-000000000014', 'b3333333-3333-3333-3333-333333333331', 2, 0, 22.00, 8, true, NOW() - INTERVAL '30 days', false),
-- KC-DN-002: CCS2 + Type2 (Unavailable - maintenance)
('c1000000-0001-0001-0001-000000000015', 'b3333333-3333-3333-3333-333333333332', 1, 1, 50.00, 7, false, NOW() - INTERVAL '25 days', false),
('c1000000-0001-0001-0001-000000000016', 'b3333333-3333-3333-3333-333333333332', 2, 0, 22.00, 7, false, NOW() - INTERVAL '25 days', false);

-- ============================================================
-- 5. APP USERS (10 users)
-- ============================================================
DELETE FROM "AppAppUsers" WHERE "Email" LIKE '%@demo.klc.vn' OR "Email" LIKE '%@abc-corp.vn' OR "Email" LIKE '%@grab.vn';
DELETE FROM "AppAppUsers" WHERE "Id" IN (
  'e1111111-1111-1111-1111-111111111111', 'e1111111-1111-1111-1111-111111111112', 'e1111111-1111-1111-1111-111111111113',
  'e1111111-1111-1111-1111-111111111114', 'e1111111-1111-1111-1111-111111111115', 'e1111111-1111-1111-1111-111111111116',
  'e1111111-1111-1111-1111-111111111117', 'e1111111-1111-1111-1111-111111111118', 'e1111111-1111-1111-1111-111111111119',
  'e1111111-1111-1111-1111-111111111120'
);

INSERT INTO "AppAppUsers" ("Id", "IdentityUserId", "FullName", "PhoneNumber", "Email", "IsPhoneVerified", "IsEmailVerified", "AvatarUrl", "PreferredLanguage", "IsNotificationsEnabled", "FcmToken", "WalletBalance", "IsActive", "LastLoginAt", "CreationTime", "IsDeleted")
VALUES
('e1111111-1111-1111-1111-111111111111', 'e1111111-1111-1111-1111-111111111111', 'Nguyễn Văn An', '0901234001', 'nguyen.an@demo.klc.vn', true, true, NULL, 'vi', true, NULL, 2500000, true, NOW() - INTERVAL '1 hour', NOW() - INTERVAL '30 days', false),
('e1111111-1111-1111-1111-111111111112', 'e1111111-1111-1111-1111-111111111112', 'Trần Thị Bình', '0901234002', 'tran.binh@demo.klc.vn', true, true, NULL, 'vi', true, NULL, 1800000, true, NOW() - INTERVAL '2 hours', NOW() - INTERVAL '25 days', false),
('e1111111-1111-1111-1111-111111111113', 'e1111111-1111-1111-1111-111111111113', 'Lê Văn Cường', '0901234003', 'le.cuong@demo.klc.vn', true, false, NULL, 'vi', true, NULL, 3200000, true, NOW() - INTERVAL '3 hours', NOW() - INTERVAL '20 days', false),
('e1111111-1111-1111-1111-111111111114', 'e1111111-1111-1111-1111-111111111114', 'Phạm Thị Dung', '0901234004', 'pham.dung@demo.klc.vn', true, true, NULL, 'vi', false, NULL, 500000, true, NOW() - INTERVAL '1 day', NOW() - INTERVAL '15 days', false),
('e1111111-1111-1111-1111-111111111115', 'e1111111-1111-1111-1111-111111111115', 'Hoàng Văn Em', '0901234005', 'hoang.em@demo.klc.vn', false, false, NULL, 'vi', true, NULL, 4500000, true, NOW() - INTERVAL '2 days', NOW() - INTERVAL '10 days', false),
('e1111111-1111-1111-1111-111111111116', 'e1111111-1111-1111-1111-111111111116', 'John Smith', '0901234006', 'john.smith@demo.klc.vn', true, true, NULL, 'en', true, NULL, 5000000, true, NOW() - INTERVAL '5 hours', NOW() - INTERVAL '28 days', false),
('e1111111-1111-1111-1111-111111111117', 'e1111111-1111-1111-1111-111111111117', 'Sarah Johnson', '0901234007', 'sarah.j@demo.klc.vn', true, true, NULL, 'en', true, NULL, 2200000, true, NOW() - INTERVAL '6 hours', NOW() - INTERVAL '22 days', false),
('e1111111-1111-1111-1111-111111111118', 'e1111111-1111-1111-1111-111111111118', 'Công ty ABC Corp', '0901234008', 'fleet@abc-corp.vn', true, true, NULL, 'vi', true, NULL, 50000000, true, NOW() - INTERVAL '30 minutes', NOW() - INTERVAL '60 days', false),
('e1111111-1111-1111-1111-111111111119', 'e1111111-1111-1111-1111-111111111119', 'Grab Vietnam', '0901234009', 'ev-fleet@grab.vn', true, true, NULL, 'vi', true, NULL, 100000000, true, NOW() - INTERVAL '15 minutes', NOW() - INTERVAL '90 days', false),
('e1111111-1111-1111-1111-111111111120', 'e1111111-1111-1111-1111-111111111120', 'Võ Thị Giang', '0901234010', 'vo.giang@demo.klc.vn', true, true, NULL, 'vi', true, NULL, 1500000, true, NOW() - INTERVAL '4 hours', NOW() - INTERVAL '5 days', false);

-- ============================================================
-- 3. VEHICLES (15 vehicles)
-- ============================================================
DELETE FROM "AppVehicles" WHERE "LicensePlate" LIKE '%-DEMO';

INSERT INTO "AppVehicles" ("Id", "UserId", "Make", "Model", "LicensePlate", "Color", "Year", "BatteryCapacityKwh", "PreferredConnectorType", "IsActive", "IsDefault", "Nickname", "CreationTime", "IsDeleted")
VALUES
('f1111111-1111-1111-1111-111111111101', 'e1111111-1111-1111-1111-111111111111', 'VinFast', 'VF e34', '30A-12345-DEMO', 'Đỏ', 2024, 42.0, 0, true, true, 'Xe đi làm', NOW(), false),
('f1111111-1111-1111-1111-111111111102', 'e1111111-1111-1111-1111-111111111111', 'VinFast', 'VF 8', '30A-12346-DEMO', 'Xanh', 2025, 87.7, 1, true, false, 'Xe gia đình', NOW(), false),
('f1111111-1111-1111-1111-111111111103', 'e1111111-1111-1111-1111-111111111112', 'Hyundai', 'Kona Electric', '29A-54321-DEMO', 'Trắng', 2023, 64.0, 1, true, true, NULL, NOW(), false),
('f1111111-1111-1111-1111-111111111104', 'e1111111-1111-1111-1111-111111111113', 'Tesla', 'Model 3', '30H-11111-DEMO', 'Đen', 2024, 60.0, 1, true, true, 'Tesla của tôi', NOW(), false),
('f1111111-1111-1111-1111-111111111105', 'e1111111-1111-1111-1111-111111111113', 'BYD', 'Atto 3', '30H-11112-DEMO', 'Xanh dương', 2024, 60.5, 0, true, false, NULL, NOW(), false),
('f1111111-1111-1111-1111-111111111106', 'e1111111-1111-1111-1111-111111111114', 'MG', 'ZS EV', '51A-99999-DEMO', 'Bạc', 2023, 44.5, 0, true, true, NULL, NOW(), false),
('f1111111-1111-1111-1111-111111111107', 'e1111111-1111-1111-1111-111111111115', 'VinFast', 'VF 9', '30A-88888-DEMO', 'Xám', 2025, 123.0, 1, true, true, 'SUV điện', NOW(), false),
('f1111111-1111-1111-1111-111111111108', 'e1111111-1111-1111-1111-111111111116', 'Tesla', 'Model Y', '29H-77777-DEMO', 'White', 2024, 75.0, 1, true, true, 'My Tesla', NOW(), false),
('f1111111-1111-1111-1111-111111111109', 'e1111111-1111-1111-1111-111111111117', 'Polestar', '2', '30A-66666-DEMO', 'Snow', 2024, 78.0, 1, true, true, NULL, NOW(), false),
('f1111111-1111-1111-1111-111111111110', 'e1111111-1111-1111-1111-111111111118', 'VinFast', 'VF e34', '30A-ABC01-DEMO', 'Trắng', 2024, 42.0, 0, true, true, 'Fleet #1', NOW(), false),
('f1111111-1111-1111-1111-111111111111', 'e1111111-1111-1111-1111-111111111118', 'VinFast', 'VF e34', '30A-ABC02-DEMO', 'Trắng', 2024, 42.0, 0, true, false, 'Fleet #2', NOW(), false),
('f1111111-1111-1111-1111-111111111112', 'e1111111-1111-1111-1111-111111111118', 'VinFast', 'VF 8', '30A-ABC03-DEMO', 'Đen', 2025, 87.7, 1, true, false, 'Fleet #3', NOW(), false),
('f1111111-1111-1111-1111-111111111113', 'e1111111-1111-1111-1111-111111111119', 'BYD', 'e6', '51A-GRB01-DEMO', 'Xanh lá', 2024, 71.7, 1, true, true, 'Grab EV #1', NOW(), false),
('f1111111-1111-1111-1111-111111111114', 'e1111111-1111-1111-1111-111111111119', 'BYD', 'e6', '51A-GRB02-DEMO', 'Xanh lá', 2024, 71.7, 1, true, false, 'Grab EV #2', NOW(), false),
('f1111111-1111-1111-1111-111111111115', 'e1111111-1111-1111-1111-111111111120', 'Kia', 'EV6', '43A-55555-DEMO', 'Xanh ngọc', 2024, 77.4, 1, true, true, NULL, NOW(), false);

-- ============================================================
-- 4. CHARGING SESSIONS (20 completed sessions)
-- ============================================================
DELETE FROM "AppMeterValues" WHERE "SessionId"::text LIKE '11111111-0001-%';
DELETE FROM "AppChargingSessions" WHERE "IdTag" LIKE 'KLC-%' OR "Id"::text LIKE '11111111-0001-%';

INSERT INTO "AppChargingSessions" ("Id", "UserId", "VehicleId", "StationId", "ConnectorNumber", "OcppTransactionId", "Status", "StartTime", "EndTime", "MeterStart", "MeterStop", "TotalEnergyKwh", "TotalCost", "TariffPlanId", "RatePerKwh", "StopReason", "IdTag", "ExtraProperties", "ConcurrencyStamp", "CreationTime", "IsDeleted")
VALUES
-- Completed sessions (Status=5)
('11111111-0001-0001-0001-000000000001', 'e1111111-1111-1111-1111-111111111111', 'f1111111-1111-1111-1111-111111111101', 'b1111111-1111-1111-1111-111111111111', 1, 1001, 5, NOW() - INTERVAL '2 days 14 hours', NOW() - INTERVAL '2 days 12 hours 30 minutes', 0, 35500, 35.500, 156200, 'd1111111-1111-1111-1111-111111111111', 4400.00, 'Local', 'KLC-001', '{}', 'seed-s001', NOW() - INTERVAL '2 days 14 hours', false),
('11111111-0001-0001-0001-000000000002', 'e1111111-1111-1111-1111-111111111112', 'f1111111-1111-1111-1111-111111111103', 'b1111111-1111-1111-1111-111111111112', 1, 1002, 5, NOW() - INTERVAL '2 days 10 hours', NOW() - INTERVAL '2 days 8 hours 45 minutes', 0, 28000, 28.000, 123200, 'd1111111-1111-1111-1111-111111111111', 4400.00, 'Remote', 'KLC-002', '{}', 'seed-s002', NOW() - INTERVAL '2 days 10 hours', false),
('11111111-0001-0001-0001-000000000003', 'e1111111-1111-1111-1111-111111111113', 'f1111111-1111-1111-1111-111111111104', 'b2222222-2222-2222-2222-222222222221', 1, 1003, 5, NOW() - INTERVAL '1 day 16 hours', NOW() - INTERVAL '1 day 15 hours 20 minutes', 0, 42000, 42.000, 184800, 'd1111111-1111-1111-1111-111111111111', 4400.00, 'EVDisconnected', 'KLC-003', '{}', 'seed-s003', NOW() - INTERVAL '1 day 16 hours', false),
('11111111-0001-0001-0001-000000000004', 'e1111111-1111-1111-1111-111111111114', 'f1111111-1111-1111-1111-111111111106', 'b2222222-2222-2222-2222-222222222222', 1, 1004, 5, NOW() - INTERVAL '1 day 12 hours', NOW() - INTERVAL '1 day 10 hours 15 minutes', 0, 22500, 22.500, 99000, 'd1111111-1111-1111-1111-111111111111', 4400.00, 'Local', 'KLC-001', '{}', 'seed-s004', NOW() - INTERVAL '1 day 12 hours', false),
('11111111-0001-0001-0001-000000000005', 'e1111111-1111-1111-1111-111111111115', 'f1111111-1111-1111-1111-111111111107', 'b3333333-3333-3333-3333-333333333331', 1, 1005, 5, NOW() - INTERVAL '1 day 8 hours', NOW() - INTERVAL '1 day 6 hours', 0, 55000, 55.000, 242000, 'd1111111-1111-1111-1111-111111111111', 4400.00, 'Local', 'KLC-VIP-001', '{}', 'seed-s005', NOW() - INTERVAL '1 day 8 hours', false),
('11111111-0001-0001-0001-000000000006', 'e1111111-1111-1111-1111-111111111116', 'f1111111-1111-1111-1111-111111111108', 'b1111111-1111-1111-1111-111111111111', 2, 1006, 5, NOW() - INTERVAL '10 hours', NOW() - INTERVAL '9 hours 30 minutes', 0, 18000, 18.000, 79200, 'd1111111-1111-1111-1111-111111111111', 4400.00, 'Local', 'KLC-001', '{}', 'seed-s006', NOW() - INTERVAL '10 hours', false),
('11111111-0001-0001-0001-000000000007', 'e1111111-1111-1111-1111-111111111117', 'f1111111-1111-1111-1111-111111111109', 'b1111111-1111-1111-1111-111111111112', 2, 1007, 5, NOW() - INTERVAL '8 hours', NOW() - INTERVAL '6 hours 45 minutes', 0, 32000, 32.000, 140800, 'd1111111-1111-1111-1111-111111111111', 4400.00, 'Remote', 'KLC-002', '{}', 'seed-s007', NOW() - INTERVAL '8 hours', false),
('11111111-0001-0001-0001-000000000008', 'e1111111-1111-1111-1111-111111111118', 'f1111111-1111-1111-1111-111111111110', 'b2222222-2222-2222-2222-222222222221', 2, 1008, 5, NOW() - INTERVAL '6 hours', NOW() - INTERVAL '5 hours', 0, 25000, 25.000, 110000, 'd1111111-1111-1111-1111-111111111111', 4400.00, 'Local', 'KLC-VIP-001', '{}', 'seed-s008', NOW() - INTERVAL '6 hours', false),
('11111111-0001-0001-0001-000000000009', 'e1111111-1111-1111-1111-111111111118', 'f1111111-1111-1111-1111-111111111111', 'b2222222-2222-2222-2222-222222222222', 2, 1009, 5, NOW() - INTERVAL '5 hours', NOW() - INTERVAL '4 hours 20 minutes', 0, 20000, 20.000, 88000, 'd1111111-1111-1111-1111-111111111111', 4400.00, 'Local', 'KLC-VIP-001', '{}', 'seed-s009', NOW() - INTERVAL '5 hours', false),
('11111111-0001-0001-0001-000000000010', 'e1111111-1111-1111-1111-111111111119', 'f1111111-1111-1111-1111-111111111113', 'b1111111-1111-1111-1111-111111111111', 1, 1010, 5, NOW() - INTERVAL '4 hours', NOW() - INTERVAL '3 hours', 0, 38000, 38.000, 167200, 'd1111111-1111-1111-1111-111111111111', 4400.00, 'Local', 'KLC-VIP-001', '{}', 'seed-s010', NOW() - INTERVAL '4 hours', false),
('11111111-0001-0001-0001-000000000011', 'e1111111-1111-1111-1111-111111111120', 'f1111111-1111-1111-1111-111111111115', 'b3333333-3333-3333-3333-333333333331', 2, 1011, 5, NOW() - INTERVAL '3 hours', NOW() - INTERVAL '2 hours 15 minutes', 0, 28500, 28.500, 125400, 'd1111111-1111-1111-1111-111111111111', 4400.00, 'Remote', 'KLC-003', '{}', 'seed-s011', NOW() - INTERVAL '3 hours', false),
('11111111-0001-0001-0001-000000000012', 'e1111111-1111-1111-1111-111111111111', 'f1111111-1111-1111-1111-111111111102', 'b1111111-1111-1111-1111-111111111112', 1, 1012, 5, NOW() - INTERVAL '2 hours', NOW() - INTERVAL '1 hour 30 minutes', 0, 15000, 15.000, 66000, 'd1111111-1111-1111-1111-111111111111', 4400.00, 'Local', 'KLC-001', '{}', 'seed-s012', NOW() - INTERVAL '2 hours', false),
('11111111-0001-0001-0001-000000000013', 'e1111111-1111-1111-1111-111111111112', 'f1111111-1111-1111-1111-111111111103', 'b1111111-1111-1111-1111-111111111113', 1, 1013, 5, NOW() - INTERVAL '3 days 9 hours', NOW() - INTERVAL '3 days 7 hours', 0, 45000, 45.000, 198000, 'd1111111-1111-1111-1111-111111111111', 4400.00, 'Local', 'KLC-002', '{}', 'seed-s013', NOW() - INTERVAL '3 days 9 hours', false),
('11111111-0001-0001-0001-000000000014', 'e1111111-1111-1111-1111-111111111113', 'f1111111-1111-1111-1111-111111111105', 'b2222222-2222-2222-2222-222222222221', 1, 1014, 5, NOW() - INTERVAL '4 days 11 hours', NOW() - INTERVAL '4 days 9 hours 30 minutes', 0, 38500, 38.500, 169400, 'd1111111-1111-1111-1111-111111111111', 4400.00, 'EVDisconnected', 'KLC-003', '{}', 'seed-s014', NOW() - INTERVAL '4 days 11 hours', false),
('11111111-0001-0001-0001-000000000015', 'e1111111-1111-1111-1111-111111111116', 'f1111111-1111-1111-1111-111111111108', 'b2222222-2222-2222-2222-222222222222', 3, 1015, 5, NOW() - INTERVAL '5 days 15 hours', NOW() - INTERVAL '5 days 14 hours', 0, 52000, 52.000, 228800, 'd1111111-1111-1111-1111-111111111111', 4400.00, 'Local', 'KLC-001', '{}', 'seed-s015', NOW() - INTERVAL '5 days 15 hours', false),
('11111111-0001-0001-0001-000000000016', 'e1111111-1111-1111-1111-111111111117', 'f1111111-1111-1111-1111-111111111109', 'b3333333-3333-3333-3333-333333333331', 1, 1016, 5, NOW() - INTERVAL '6 days 8 hours', NOW() - INTERVAL '6 days 6 hours 30 minutes', 0, 41000, 41.000, 180400, 'd1111111-1111-1111-1111-111111111111', 4400.00, 'Remote', 'KLC-002', '{}', 'seed-s016', NOW() - INTERVAL '6 days 8 hours', false),
('11111111-0001-0001-0001-000000000017', 'e1111111-1111-1111-1111-111111111114', 'f1111111-1111-1111-1111-111111111106', 'b1111111-1111-1111-1111-111111111111', 1, 1017, 5, NOW() - INTERVAL '1 day 18 hours', NOW() - INTERVAL '1 day 16 hours 45 minutes', 0, 30000, 30.000, 181500, 'd2222222-2222-2222-2222-222222222222', 6050.00, 'Local', 'KLC-001', '{}', 'seed-s017', NOW() - INTERVAL '1 day 18 hours', false),
('11111111-0001-0001-0001-000000000018', 'e1111111-1111-1111-1111-111111111115', 'f1111111-1111-1111-1111-111111111107', 'b1111111-1111-1111-1111-111111111112', 2, 1018, 5, NOW() - INTERVAL '2 days 7 hours', NOW() - INTERVAL '2 days 5 hours', 0, 60000, 60.000, 363000, 'd2222222-2222-2222-2222-222222222222', 6050.00, 'Local', 'KLC-VIP-001', '{}', 'seed-s018', NOW() - INTERVAL '2 days 7 hours', false),
('11111111-0001-0001-0001-000000000019', 'e1111111-1111-1111-1111-111111111118', 'f1111111-1111-1111-1111-111111111112', 'b2222222-2222-2222-2222-222222222221', 1, 1019, 5, NOW() - INTERVAL '1 day 4 hours', NOW() - INTERVAL '1 day 2 hours', 0, 70000, 70.000, 215600, 'd3333333-3333-3333-3333-333333333333', 3080.00, 'Local', 'KLC-VIP-001', '{}', 'seed-s019', NOW() - INTERVAL '1 day 4 hours', false),
('11111111-0001-0001-0001-000000000020', 'e1111111-1111-1111-1111-111111111119', 'f1111111-1111-1111-1111-111111111114', 'b2222222-2222-2222-2222-222222222222', 1, 1020, 5, NOW() - INTERVAL '3 days 3 hours', NOW() - INTERVAL '3 days 1 hour', 0, 55000, 55.000, 169400, 'd3333333-3333-3333-3333-333333333333', 3080.00, 'Local', 'KLC-VIP-001', '{}', 'seed-s020', NOW() - INTERVAL '3 days 3 hours', false);

-- ============================================================
-- 7a. METER VALUES (25 readings for 5 sessions)
-- ============================================================
INSERT INTO "AppMeterValues" ("Id", "SessionId", "StationId", "ConnectorNumber", "Timestamp", "EnergyKwh", "CurrentAmps", "VoltageVolts", "PowerKw", "SocPercent")
VALUES
-- Session 1: 35.5 kWh over ~1.5h at KC-HN-001 C1 (VF e34)
('a1000000-0001-0001-0001-000000000001', '11111111-0001-0001-0001-000000000001', 'b1111111-1111-1111-1111-111111111111', 1, NOW() - INTERVAL '2 days 14 hours', 0.000, 95.00, 400.00, 38.000, 15.00),
('a1000000-0001-0001-0001-000000000002', '11111111-0001-0001-0001-000000000001', 'b1111111-1111-1111-1111-111111111111', 1, NOW() - INTERVAL '2 days 13 hours 40 minutes', 8.500, 92.00, 400.00, 36.800, 35.00),
('a1000000-0001-0001-0001-000000000003', '11111111-0001-0001-0001-000000000001', 'b1111111-1111-1111-1111-111111111111', 1, NOW() - INTERVAL '2 days 13 hours 20 minutes', 18.200, 85.00, 398.00, 33.830, 55.00),
('a1000000-0001-0001-0001-000000000004', '11111111-0001-0001-0001-000000000001', 'b1111111-1111-1111-1111-111111111111', 1, NOW() - INTERVAL '2 days 13 hours', 27.000, 70.00, 395.00, 27.650, 75.00),
('a1000000-0001-0001-0001-000000000005', '11111111-0001-0001-0001-000000000001', 'b1111111-1111-1111-1111-111111111111', 1, NOW() - INTERVAL '2 days 12 hours 30 minutes', 35.500, 10.00, 400.00, 4.000, 95.00),
-- Session 3: 42 kWh over ~40min at KC-HCM-001 C1 (Tesla Model 3)
('a1000000-0001-0001-0001-000000000006', '11111111-0001-0001-0001-000000000003', 'b2222222-2222-2222-2222-222222222221', 1, NOW() - INTERVAL '1 day 16 hours', 0.000, 120.00, 400.00, 48.000, 10.00),
('a1000000-0001-0001-0001-000000000007', '11111111-0001-0001-0001-000000000003', 'b2222222-2222-2222-2222-222222222221', 1, NOW() - INTERVAL '1 day 15 hours 50 minutes', 8.000, 120.00, 400.00, 48.000, 23.00),
('a1000000-0001-0001-0001-000000000008', '11111111-0001-0001-0001-000000000003', 'b2222222-2222-2222-2222-222222222221', 1, NOW() - INTERVAL '1 day 15 hours 40 minutes', 16.500, 115.00, 398.00, 45.770, 37.00),
('a1000000-0001-0001-0001-000000000009', '11111111-0001-0001-0001-000000000003', 'b2222222-2222-2222-2222-222222222221', 1, NOW() - INTERVAL '1 day 15 hours 30 minutes', 28.000, 100.00, 395.00, 39.500, 57.00),
('a1000000-0001-0001-0001-000000000010', '11111111-0001-0001-0001-000000000003', 'b2222222-2222-2222-2222-222222222221', 1, NOW() - INTERVAL '1 day 15 hours 20 minutes', 42.000, 15.00, 400.00, 6.000, 80.00),
-- Session 5: 55 kWh over 2h at KC-DN-001 C1 (VF 9)
('a1000000-0001-0001-0001-000000000011', '11111111-0001-0001-0001-000000000005', 'b3333333-3333-3333-3333-333333333331', 1, NOW() - INTERVAL '1 day 8 hours', 0.000, 100.00, 400.00, 40.000, 5.00),
('a1000000-0001-0001-0001-000000000012', '11111111-0001-0001-0001-000000000005', 'b3333333-3333-3333-3333-333333333331', 1, NOW() - INTERVAL '1 day 7 hours 30 minutes', 14.000, 98.00, 400.00, 39.200, 16.00),
('a1000000-0001-0001-0001-000000000013', '11111111-0001-0001-0001-000000000005', 'b3333333-3333-3333-3333-333333333331', 1, NOW() - INTERVAL '1 day 7 hours', 28.500, 90.00, 398.00, 35.820, 28.00),
('a1000000-0001-0001-0001-000000000014', '11111111-0001-0001-0001-000000000005', 'b3333333-3333-3333-3333-333333333331', 1, NOW() - INTERVAL '1 day 6 hours 30 minutes', 42.000, 75.00, 395.00, 29.625, 39.00),
('a1000000-0001-0001-0001-000000000015', '11111111-0001-0001-0001-000000000005', 'b3333333-3333-3333-3333-333333333331', 1, NOW() - INTERVAL '1 day 6 hours', 55.000, 12.00, 400.00, 4.800, 50.00),
-- Session 10: 38 kWh over 1h at KC-HN-001 C1 (Grab BYD e6)
('a1000000-0001-0001-0001-000000000016', '11111111-0001-0001-0001-000000000010', 'b1111111-1111-1111-1111-111111111111', 1, NOW() - INTERVAL '4 hours', 0.000, 110.00, 400.00, 44.000, 20.00),
('a1000000-0001-0001-0001-000000000017', '11111111-0001-0001-0001-000000000010', 'b1111111-1111-1111-1111-111111111111', 1, NOW() - INTERVAL '3 hours 45 minutes', 10.000, 108.00, 400.00, 43.200, 34.00),
('a1000000-0001-0001-0001-000000000018', '11111111-0001-0001-0001-000000000010', 'b1111111-1111-1111-1111-111111111111', 1, NOW() - INTERVAL '3 hours 30 minutes', 20.500, 95.00, 398.00, 37.810, 48.00),
('a1000000-0001-0001-0001-000000000019', '11111111-0001-0001-0001-000000000010', 'b1111111-1111-1111-1111-111111111111', 1, NOW() - INTERVAL '3 hours 15 minutes', 30.000, 60.00, 395.00, 23.700, 62.00),
('a1000000-0001-0001-0001-000000000020', '11111111-0001-0001-0001-000000000010', 'b1111111-1111-1111-1111-111111111111', 1, NOW() - INTERVAL '3 hours', 38.000, 8.00, 400.00, 3.200, 73.00),
-- Session 17: 30 kWh peak hours at KC-HN-001 C1 (MG ZS EV)
('a1000000-0001-0001-0001-000000000021', '11111111-0001-0001-0001-000000000017', 'b1111111-1111-1111-1111-111111111111', 1, NOW() - INTERVAL '1 day 18 hours', 0.000, 90.00, 400.00, 36.000, 25.00),
('a1000000-0001-0001-0001-000000000022', '11111111-0001-0001-0001-000000000017', 'b1111111-1111-1111-1111-111111111111', 1, NOW() - INTERVAL '1 day 17 hours 40 minutes', 7.500, 88.00, 400.00, 35.200, 42.00),
('a1000000-0001-0001-0001-000000000023', '11111111-0001-0001-0001-000000000017', 'b1111111-1111-1111-1111-111111111111', 1, NOW() - INTERVAL '1 day 17 hours 20 minutes', 15.000, 80.00, 398.00, 31.840, 59.00),
('a1000000-0001-0001-0001-000000000024', '11111111-0001-0001-0001-000000000017', 'b1111111-1111-1111-1111-111111111111', 1, NOW() - INTERVAL '1 day 17 hours', 23.000, 55.00, 395.00, 21.725, 76.00),
('a1000000-0001-0001-0001-000000000025', '11111111-0001-0001-0001-000000000017', 'b1111111-1111-1111-1111-111111111111', 1, NOW() - INTERVAL '1 day 16 hours 45 minutes', 30.000, 10.00, 400.00, 4.000, 93.00);

-- ============================================================
-- 8. PAYMENT TRANSACTIONS (20 payments)
-- ============================================================
DELETE FROM "AppPaymentTransactions" WHERE "ReferenceCode" LIKE 'PAY-DEMO-%';

INSERT INTO "AppPaymentTransactions" ("Id", "SessionId", "UserId", "Gateway", "Amount", "Status", "GatewayTransactionId", "ReferenceCode", "ErrorMessage", "CompletedAt", "CreationTime", "IsDeleted")
VALUES
('22222222-0001-0001-0001-000000000001', '11111111-0001-0001-0001-000000000001', 'e1111111-1111-1111-1111-111111111111', 0, 156200, 2, 'ZLP_202603011001', 'PAY-DEMO-0001', NULL, NOW() - INTERVAL '2 days 12 hours 25 minutes', NOW() - INTERVAL '2 days 12 hours 30 minutes', false),
('22222222-0001-0001-0001-000000000002', '11111111-0001-0001-0001-000000000002', 'e1111111-1111-1111-1111-111111111112', 1, 123200, 2, 'MOMO_202603011002', 'PAY-DEMO-0002', NULL, NOW() - INTERVAL '2 days 8 hours 40 minutes', NOW() - INTERVAL '2 days 8 hours 45 minutes', false),
('22222222-0001-0001-0001-000000000003', '11111111-0001-0001-0001-000000000003', 'e1111111-1111-1111-1111-111111111113', 2, 184800, 2, 'ONEPAY_202603011003', 'PAY-DEMO-0003', NULL, NOW() - INTERVAL '1 day 15 hours 15 minutes', NOW() - INTERVAL '1 day 15 hours 20 minutes', false),
('22222222-0001-0001-0001-000000000004', '11111111-0001-0001-0001-000000000004', 'e1111111-1111-1111-1111-111111111114', 3, 99000, 2, 'WALLET_202603011004', 'PAY-DEMO-0004', NULL, NOW() - INTERVAL '1 day 10 hours 10 minutes', NOW() - INTERVAL '1 day 10 hours 15 minutes', false),
('22222222-0001-0001-0001-000000000005', '11111111-0001-0001-0001-000000000005', 'e1111111-1111-1111-1111-111111111115', 0, 242000, 2, 'ZLP_202603011005', 'PAY-DEMO-0005', NULL, NOW() - INTERVAL '1 day 5 hours 55 minutes', NOW() - INTERVAL '1 day 6 hours', false),
('22222222-0001-0001-0001-000000000006', '11111111-0001-0001-0001-000000000006', 'e1111111-1111-1111-1111-111111111116', 1, 79200, 2, 'MOMO_202603011006', 'PAY-DEMO-0006', NULL, NOW() - INTERVAL '9 hours 25 minutes', NOW() - INTERVAL '9 hours 30 minutes', false),
('22222222-0001-0001-0001-000000000007', '11111111-0001-0001-0001-000000000007', 'e1111111-1111-1111-1111-111111111117', 0, 140800, 2, 'ZLP_202603011007', 'PAY-DEMO-0007', NULL, NOW() - INTERVAL '6 hours 40 minutes', NOW() - INTERVAL '6 hours 45 minutes', false),
('22222222-0001-0001-0001-000000000008', '11111111-0001-0001-0001-000000000008', 'e1111111-1111-1111-1111-111111111118', 3, 110000, 2, 'WALLET_202603011008', 'PAY-DEMO-0008', NULL, NOW() - INTERVAL '4 hours 55 minutes', NOW() - INTERVAL '5 hours', false),
('22222222-0001-0001-0001-000000000009', '11111111-0001-0001-0001-000000000009', 'e1111111-1111-1111-1111-111111111118', 3, 88000, 2, 'WALLET_202603011009', 'PAY-DEMO-0009', NULL, NOW() - INTERVAL '4 hours 15 minutes', NOW() - INTERVAL '4 hours 20 minutes', false),
('22222222-0001-0001-0001-000000000010', '11111111-0001-0001-0001-000000000010', 'e1111111-1111-1111-1111-111111111119', 3, 167200, 2, 'WALLET_202603011010', 'PAY-DEMO-0010', NULL, NOW() - INTERVAL '2 hours 55 minutes', NOW() - INTERVAL '3 hours', false),
('22222222-0001-0001-0001-000000000011', '11111111-0001-0001-0001-000000000011', 'e1111111-1111-1111-1111-111111111120', 1, 125400, 2, 'MOMO_202603011011', 'PAY-DEMO-0011', NULL, NOW() - INTERVAL '2 hours 10 minutes', NOW() - INTERVAL '2 hours 15 minutes', false),
('22222222-0001-0001-0001-000000000012', '11111111-0001-0001-0001-000000000012', 'e1111111-1111-1111-1111-111111111111', 0, 66000, 2, 'ZLP_202603011012', 'PAY-DEMO-0012', NULL, NOW() - INTERVAL '1 hour 25 minutes', NOW() - INTERVAL '1 hour 30 minutes', false),
('22222222-0001-0001-0001-000000000013', '11111111-0001-0001-0001-000000000013', 'e1111111-1111-1111-1111-111111111112', 2, 198000, 2, 'ONEPAY_202603011013', 'PAY-DEMO-0013', NULL, NOW() - INTERVAL '3 days 6 hours 55 minutes', NOW() - INTERVAL '3 days 7 hours', false),
('22222222-0001-0001-0001-000000000014', '11111111-0001-0001-0001-000000000014', 'e1111111-1111-1111-1111-111111111113', 1, 169400, 2, 'MOMO_202603011014', 'PAY-DEMO-0014', NULL, NOW() - INTERVAL '4 days 9 hours 25 minutes', NOW() - INTERVAL '4 days 9 hours 30 minutes', false),
('22222222-0001-0001-0001-000000000015', '11111111-0001-0001-0001-000000000015', 'e1111111-1111-1111-1111-111111111116', 0, 228800, 2, 'ZLP_202603011015', 'PAY-DEMO-0015', NULL, NOW() - INTERVAL '5 days 13 hours 55 minutes', NOW() - INTERVAL '5 days 14 hours', false),
('22222222-0001-0001-0001-000000000016', '11111111-0001-0001-0001-000000000016', 'e1111111-1111-1111-1111-111111111117', 1, 180400, 2, 'MOMO_202603011016', 'PAY-DEMO-0016', NULL, NOW() - INTERVAL '6 days 6 hours 25 minutes', NOW() - INTERVAL '6 days 6 hours 30 minutes', false),
('22222222-0001-0001-0001-000000000017', '11111111-0001-0001-0001-000000000017', 'e1111111-1111-1111-1111-111111111114', 0, 181500, 2, 'ZLP_202603011017', 'PAY-DEMO-0017', NULL, NOW() - INTERVAL '1 day 16 hours 40 minutes', NOW() - INTERVAL '1 day 16 hours 45 minutes', false),
('22222222-0001-0001-0001-000000000018', '11111111-0001-0001-0001-000000000018', 'e1111111-1111-1111-1111-111111111115', 3, 363000, 2, 'WALLET_202603011018', 'PAY-DEMO-0018', NULL, NOW() - INTERVAL '2 days 4 hours 55 minutes', NOW() - INTERVAL '2 days 5 hours', false),
('22222222-0001-0001-0001-000000000019', '11111111-0001-0001-0001-000000000019', 'e1111111-1111-1111-1111-111111111118', 3, 215600, 2, 'WALLET_202603011019', 'PAY-DEMO-0019', NULL, NOW() - INTERVAL '1 day 1 hour 55 minutes', NOW() - INTERVAL '1 day 2 hours', false),
('22222222-0001-0001-0001-000000000020', '11111111-0001-0001-0001-000000000020', 'e1111111-1111-1111-1111-111111111119', 3, 169400, 2, 'WALLET_202603011020', 'PAY-DEMO-0020', NULL, NOW() - INTERVAL '3 days 55 minutes', NOW() - INTERVAL '3 days 1 hour', false);

-- ============================================================
-- 6. INVOICES (20 invoices)
-- ============================================================
DELETE FROM "AppInvoices" WHERE "InvoiceNumber" LIKE 'INV-2026-DEMO-%';

INSERT INTO "AppInvoices" ("Id", "PaymentTransactionId", "InvoiceNumber", "EnergyKwh", "BaseAmount", "TaxAmount", "TotalAmount", "TaxRatePercent", "RatePerKwh", "IssuedAt", "CreationTime", "IsDeleted")
VALUES
('33333333-0001-0001-0001-000000000001', '22222222-0001-0001-0001-000000000001', 'INV-2026-DEMO-0001', 35.50, 142000, 14200, 156200, 10.00, 4000.00, NOW() - INTERVAL '2 days 12 hours 20 minutes', NOW() - INTERVAL '2 days 12 hours 25 minutes', false),
('33333333-0001-0001-0001-000000000002', '22222222-0001-0001-0001-000000000002', 'INV-2026-DEMO-0002', 28.00, 112000, 11200, 123200, 10.00, 4000.00, NOW() - INTERVAL '2 days 8 hours 35 minutes', NOW() - INTERVAL '2 days 8 hours 40 minutes', false),
('33333333-0001-0001-0001-000000000003', '22222222-0001-0001-0001-000000000003', 'INV-2026-DEMO-0003', 42.00, 168000, 16800, 184800, 10.00, 4000.00, NOW() - INTERVAL '1 day 15 hours 10 minutes', NOW() - INTERVAL '1 day 15 hours 15 minutes', false),
('33333333-0001-0001-0001-000000000004', '22222222-0001-0001-0001-000000000004', 'INV-2026-DEMO-0004', 22.50, 90000, 9000, 99000, 10.00, 4000.00, NOW() - INTERVAL '1 day 10 hours 5 minutes', NOW() - INTERVAL '1 day 10 hours 10 minutes', false),
('33333333-0001-0001-0001-000000000005', '22222222-0001-0001-0001-000000000005', 'INV-2026-DEMO-0005', 55.00, 220000, 22000, 242000, 10.00, 4000.00, NOW() - INTERVAL '1 day 5 hours 50 minutes', NOW() - INTERVAL '1 day 5 hours 55 minutes', false),
('33333333-0001-0001-0001-000000000006', '22222222-0001-0001-0001-000000000006', 'INV-2026-DEMO-0006', 18.00, 72000, 7200, 79200, 10.00, 4000.00, NOW() - INTERVAL '9 hours 20 minutes', NOW() - INTERVAL '9 hours 25 minutes', false),
('33333333-0001-0001-0001-000000000007', '22222222-0001-0001-0001-000000000007', 'INV-2026-DEMO-0007', 32.00, 128000, 12800, 140800, 10.00, 4000.00, NOW() - INTERVAL '6 hours 35 minutes', NOW() - INTERVAL '6 hours 40 minutes', false),
('33333333-0001-0001-0001-000000000008', '22222222-0001-0001-0001-000000000008', 'INV-2026-DEMO-0008', 25.00, 100000, 10000, 110000, 10.00, 4000.00, NOW() - INTERVAL '4 hours 50 minutes', NOW() - INTERVAL '4 hours 55 minutes', false),
('33333333-0001-0001-0001-000000000009', '22222222-0001-0001-0001-000000000009', 'INV-2026-DEMO-0009', 20.00, 80000, 8000, 88000, 10.00, 4000.00, NOW() - INTERVAL '4 hours 10 minutes', NOW() - INTERVAL '4 hours 15 minutes', false),
('33333333-0001-0001-0001-000000000010', '22222222-0001-0001-0001-000000000010', 'INV-2026-DEMO-0010', 38.00, 152000, 15200, 167200, 10.00, 4000.00, NOW() - INTERVAL '2 hours 50 minutes', NOW() - INTERVAL '2 hours 55 minutes', false),
('33333333-0001-0001-0001-000000000011', '22222222-0001-0001-0001-000000000011', 'INV-2026-DEMO-0011', 28.50, 114000, 11400, 125400, 10.00, 4000.00, NOW() - INTERVAL '2 hours 5 minutes', NOW() - INTERVAL '2 hours 10 minutes', false),
('33333333-0001-0001-0001-000000000012', '22222222-0001-0001-0001-000000000012', 'INV-2026-DEMO-0012', 15.00, 60000, 6000, 66000, 10.00, 4000.00, NOW() - INTERVAL '1 hour 20 minutes', NOW() - INTERVAL '1 hour 25 minutes', false),
('33333333-0001-0001-0001-000000000013', '22222222-0001-0001-0001-000000000013', 'INV-2026-DEMO-0013', 45.00, 180000, 18000, 198000, 10.00, 4000.00, NOW() - INTERVAL '3 days 6 hours 50 minutes', NOW() - INTERVAL '3 days 6 hours 55 minutes', false),
('33333333-0001-0001-0001-000000000014', '22222222-0001-0001-0001-000000000014', 'INV-2026-DEMO-0014', 38.50, 154000, 15400, 169400, 10.00, 4000.00, NOW() - INTERVAL '4 days 9 hours 20 minutes', NOW() - INTERVAL '4 days 9 hours 25 minutes', false),
('33333333-0001-0001-0001-000000000015', '22222222-0001-0001-0001-000000000015', 'INV-2026-DEMO-0015', 52.00, 208000, 20800, 228800, 10.00, 4000.00, NOW() - INTERVAL '5 days 13 hours 50 minutes', NOW() - INTERVAL '5 days 13 hours 55 minutes', false),
('33333333-0001-0001-0001-000000000016', '22222222-0001-0001-0001-000000000016', 'INV-2026-DEMO-0016', 41.00, 164000, 16400, 180400, 10.00, 4000.00, NOW() - INTERVAL '6 days 6 hours 20 minutes', NOW() - INTERVAL '6 days 6 hours 25 minutes', false),
('33333333-0001-0001-0001-000000000017', '22222222-0001-0001-0001-000000000017', 'INV-2026-DEMO-0017', 30.00, 165000, 16500, 181500, 10.00, 5500.00, NOW() - INTERVAL '1 day 16 hours 35 minutes', NOW() - INTERVAL '1 day 16 hours 40 minutes', false),
('33333333-0001-0001-0001-000000000018', '22222222-0001-0001-0001-000000000018', 'INV-2026-DEMO-0018', 60.00, 330000, 33000, 363000, 10.00, 5500.00, NOW() - INTERVAL '2 days 4 hours 50 minutes', NOW() - INTERVAL '2 days 4 hours 55 minutes', false),
('33333333-0001-0001-0001-000000000019', '22222222-0001-0001-0001-000000000019', 'INV-2026-DEMO-0019', 70.00, 196000, 19600, 215600, 10.00, 2800.00, NOW() - INTERVAL '1 day 1 hour 50 minutes', NOW() - INTERVAL '1 day 1 hour 55 minutes', false),
('33333333-0001-0001-0001-000000000020', '22222222-0001-0001-0001-000000000020', 'INV-2026-DEMO-0020', 55.00, 154000, 15400, 169400, 10.00, 2800.00, NOW() - INTERVAL '3 days 50 minutes', NOW() - INTERVAL '3 days 55 minutes', false);

-- ============================================================
-- 7. FAULTS (5 faults)
-- ============================================================
DELETE FROM "AppFaults" WHERE "VendorErrorCode" LIKE '%00_';

INSERT INTO "AppFaults" ("Id", "StationId", "ConnectorNumber", "ErrorCode", "ErrorInfo", "VendorErrorCode", "Status", "Priority", "DetectedAt", "ResolvedAt", "ResolvedByUserId", "ResolutionNotes", "CreationTime", "IsDeleted")
VALUES
('44444444-0001-0001-0001-000000000001', 'b1111111-1111-1111-1111-111111111113', 1, 'GroundFailure', 'Ground fault detected on connector 1', 'GF001', 2, 1, NOW() - INTERVAL '5 days', NOW() - INTERVAL '4 days 20 hours', NULL, 'Replaced faulty ground connection.', NOW() - INTERVAL '5 days', false),
('44444444-0001-0001-0001-000000000002', 'b2222222-2222-2222-2222-222222222221', 1, 'ConnectorLockFailure', 'Unable to lock connector', 'CLF002', 1, 2, NOW() - INTERVAL '2 days', NULL, NULL, NULL, NOW() - INTERVAL '2 days', false),
('44444444-0001-0001-0001-000000000003', 'b1111111-1111-1111-1111-111111111112', 2, 'ReaderFailure', 'RFID reader not responding', 'RF003', 0, 3, NOW() - INTERVAL '1 day', NULL, NULL, NULL, NOW() - INTERVAL '1 day', false),
('44444444-0001-0001-0001-000000000004', 'b2222222-2222-2222-2222-222222222222', 1, 'OverCurrentWarning', 'Over current detected', 'OC004', 2, 2, NOW() - INTERVAL '3 days', NOW() - INTERVAL '2 days 18 hours', NULL, 'Firmware update resolved the issue.', NOW() - INTERVAL '3 days', false),
('44444444-0001-0001-0001-000000000005', 'b3333333-3333-3333-3333-333333333331', 2, 'EVCommunicationError', 'Communication timeout with vehicle', 'EVC005', 0, 3, NOW() - INTERVAL '2 hours', NULL, NULL, NULL, NOW() - INTERVAL '2 hours', false);

-- ============================================================
-- 8. NOTIFICATIONS (15 notifications - simplified schema)
-- ============================================================
DELETE FROM "AppNotifications" WHERE "Id"::text LIKE '55555555-%';

INSERT INTO "AppNotifications" ("Id", "UserId", "Type", "Title", "Body", "IsRead", "ReadAt", "Data", "ActionUrl", "IsPushSent", "PushSentAt", "CreationTime")
VALUES
('55555555-0001-0001-0001-000000000001', 'e1111111-1111-1111-1111-111111111111', 0, 'Sạc bắt đầu', 'VinFast VF e34 đang sạc tại KC-HN-001', true, NOW() - INTERVAL '2 days 13 hours', '{}', '/sessions', true, NOW() - INTERVAL '2 days 14 hours', NOW() - INTERVAL '2 days 14 hours'),
('55555555-0001-0001-0001-000000000002', 'e1111111-1111-1111-1111-111111111111', 1, 'Sạc hoàn tất', 'Đã sạc 35.5 kWh. Chi phí: 156.200đ', true, NOW() - INTERVAL '2 days 12 hours', '{}', '/sessions', true, NOW() - INTERVAL '2 days 12 hours 30 minutes', NOW() - INTERVAL '2 days 12 hours 30 minutes'),
('55555555-0001-0001-0001-000000000003', 'e1111111-1111-1111-1111-111111111111', 3, 'Thanh toán thành công', 'Đã thanh toán 156.200đ qua ZaloPay', true, NOW() - INTERVAL '2 days 12 hours', '{}', '/payments', true, NOW() - INTERVAL '2 days 12 hours 25 minutes', NOW() - INTERVAL '2 days 12 hours 25 minutes'),
('55555555-0001-0001-0001-000000000004', 'e1111111-1111-1111-1111-111111111112', 0, 'Sạc bắt đầu', 'Hyundai Kona đang sạc tại KC-HN-002', true, NOW() - INTERVAL '2 days 9 hours', '{}', '/sessions', true, NOW() - INTERVAL '2 days 10 hours', NOW() - INTERVAL '2 days 10 hours'),
('55555555-0001-0001-0001-000000000005', 'e1111111-1111-1111-1111-111111111112', 1, 'Sạc hoàn tất', 'Đã sạc 28 kWh. Chi phí: 123.200đ', true, NOW() - INTERVAL '2 days 8 hours', '{}', '/sessions', true, NOW() - INTERVAL '2 days 8 hours 45 minutes', NOW() - INTERVAL '2 days 8 hours 45 minutes'),
('55555555-0001-0001-0001-000000000006', 'e1111111-1111-1111-1111-111111111113', 1, 'Sạc hoàn tất', 'Tesla Model 3 đã sạc 42 kWh', false, NULL, '{}', '/sessions', true, NOW() - INTERVAL '1 day 15 hours 20 minutes', NOW() - INTERVAL '1 day 15 hours 20 minutes'),
('55555555-0001-0001-0001-000000000007', 'e1111111-1111-1111-1111-111111111116', 0, 'Charging Started', 'Tesla Model Y is now charging', true, NOW() - INTERVAL '9 hours', '{}', '/sessions', true, NOW() - INTERVAL '10 hours', NOW() - INTERVAL '10 hours'),
('55555555-0001-0001-0001-000000000008', 'e1111111-1111-1111-1111-111111111116', 1, 'Charging Complete', 'Charged 18 kWh. Cost: 79,200đ', true, NOW() - INTERVAL '9 hours', '{}', '/sessions', true, NOW() - INTERVAL '9 hours 30 minutes', NOW() - INTERVAL '9 hours 30 minutes'),
('55555555-0001-0001-0001-000000000009', 'e1111111-1111-1111-1111-111111111118', 1, 'Sạc hoàn tất - Fleet #1', 'Xe Fleet #1 đã sạc 25 kWh', true, NOW() - INTERVAL '4 hours 30 minutes', '{}', '/sessions', true, NOW() - INTERVAL '5 hours', NOW() - INTERVAL '5 hours'),
('55555555-0001-0001-0001-000000000010', 'e1111111-1111-1111-1111-111111111119', 1, 'Grab EV #1 sạc xong', '38 kWh đã được sạc', true, NOW() - INTERVAL '2 hours 30 minutes', '{}', '/sessions', true, NOW() - INTERVAL '3 hours', NOW() - INTERVAL '3 hours'),
('55555555-0001-0001-0001-000000000011', 'e1111111-1111-1111-1111-111111111111', 0, 'Sạc bắt đầu', 'VF 8 đang sạc tại Times City', false, NULL, '{}', '/sessions', true, NOW() - INTERVAL '2 hours', NOW() - INTERVAL '2 hours'),
('55555555-0001-0001-0001-000000000012', 'e1111111-1111-1111-1111-111111111111', 1, 'Sạc hoàn tất', 'Đã sạc 15 kWh. Chi phí: 66.000đ', false, NULL, '{}', '/sessions', true, NOW() - INTERVAL '1 hour 30 minutes', NOW() - INTERVAL '1 hour 30 minutes'),
('55555555-0001-0001-0001-000000000013', 'e1111111-1111-1111-1111-111111111120', 1, 'Sạc hoàn tất', 'Kia EV6 đã sạc 28.5 kWh', false, NULL, '{}', '/sessions', true, NOW() - INTERVAL '2 hours 15 minutes', NOW() - INTERVAL '2 hours 15 minutes'),
('55555555-0001-0001-0001-000000000014', 'e1111111-1111-1111-1111-111111111111', 5, 'Khuyến mãi mới', 'Giảm 20% phí sạc vào khung giờ 22h-6h', false, NULL, '{}', '/promotions', true, NOW() - INTERVAL '12 hours', NOW() - INTERVAL '12 hours'),
('55555555-0001-0001-0001-000000000015', 'e1111111-1111-1111-1111-111111111114', 7, 'Số dư thấp', 'Ví KLC còn 500.000đ', false, NULL, '{}', '/wallet', true, NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day');

-- ============================================================
-- 9. ALERTS (5 admin alerts)
-- ============================================================
DELETE FROM "AppAlerts" WHERE "Message" LIKE '%KC-%';

INSERT INTO "AppAlerts" ("Id", "StationId", "ConnectorNumber", "Type", "Priority", "Status", "Message", "AcknowledgedAt", "AcknowledgedByUserId", "ResolvedAt", "ResolvedByUserId", "ResolutionNotes", "Data", "CreationTime")
VALUES
('66666666-0001-0001-0001-000000000001', 'b1111111-1111-1111-1111-111111111113', NULL, 0, 1, 1, 'Trạm KC-HN-003 mất kết nối hơn 30 phút', NOW() - INTERVAL '3 hours 30 minutes', NULL, NULL, NULL, NULL, '{}', NOW() - INTERVAL '4 hours'),
('66666666-0001-0001-0001-000000000002', 'b2222222-2222-2222-2222-222222222221', 1, 1, 1, 0, 'Lỗi khóa connector tại KC-HCM-001', NULL, NULL, NULL, NULL, NULL, '{}', NOW() - INTERVAL '2 days'),
('66666666-0001-0001-0001-000000000003', 'b2222222-2222-2222-2222-222222222222', NULL, 3, 2, 1, 'Tỷ lệ sử dụng KC-HCM-002 trên 95%', NOW() - INTERVAL '20 hours', NULL, NULL, NULL, NULL, '{}', NOW() - INTERVAL '1 day'),
('66666666-0001-0001-0001-000000000004', 'b1111111-1111-1111-1111-111111111111', NULL, 4, 3, 0, 'Firmware mới v1.5.3 có sẵn cho KC-HN-001', NULL, NULL, NULL, NULL, NULL, '{}', NOW() - INTERVAL '12 hours'),
('66666666-0001-0001-0001-000000000005', 'b3333333-3333-3333-3333-333333333331', 2, 1, 2, 0, 'Lỗi giao tiếp OCPP tại KC-DN-001', NULL, NULL, NULL, NULL, NULL, '{}', NOW() - INTERVAL '2 hours');

-- ============================================================
-- 10. WALLET TRANSACTIONS (10 transactions)
-- ============================================================
DELETE FROM "AppWalletTransactions" WHERE "ReferenceCode" LIKE 'WTX-DEMO-%';

INSERT INTO "AppWalletTransactions" ("Id", "UserId", "Type", "Amount", "BalanceAfter", "PaymentGateway", "GatewayTransactionId", "RelatedSessionId", "Status", "Description", "ReferenceCode", "CreationTime", "CreatorId")
VALUES
-- Top-ups
('77777777-0001-0001-0001-000000000001', 'e1111111-1111-1111-1111-111111111111', 0, 500000, 3000000, 0, 'ZLP_TOPUP_001', NULL, 1, 'Nạp ví qua ZaloPay', 'WTX-DEMO-0001', NOW() - INTERVAL '7 days', 'e1111111-1111-1111-1111-111111111111'),
('77777777-0001-0001-0001-000000000002', 'e1111111-1111-1111-1111-111111111112', 0, 1000000, 2800000, 1, 'MOMO_TOPUP_001', NULL, 1, 'Nạp ví qua MoMo', 'WTX-DEMO-0002', NOW() - INTERVAL '6 days', 'e1111111-1111-1111-1111-111111111112'),
('77777777-0001-0001-0001-000000000003', 'e1111111-1111-1111-1111-111111111118', 0, 10000000, 60000000, 3, 'WALLET_TOPUP_001', NULL, 1, 'Nạp ví doanh nghiệp', 'WTX-DEMO-0003', NOW() - INTERVAL '5 days', 'e1111111-1111-1111-1111-111111111118'),
-- Session payments (debit)
('77777777-0001-0001-0001-000000000004', 'e1111111-1111-1111-1111-111111111111', 1, -156200, 2343800, 3, NULL, '11111111-0001-0001-0001-000000000001', 1, 'Thanh toán phiên sạc', 'WTX-DEMO-0004', NOW() - INTERVAL '2 days 12 hours', 'e1111111-1111-1111-1111-111111111111'),
('77777777-0001-0001-0001-000000000005', 'e1111111-1111-1111-1111-111111111118', 1, -110000, 49890000, 3, NULL, '11111111-0001-0001-0001-000000000008', 1, 'Thanh toán Fleet #1', 'WTX-DEMO-0005', NOW() - INTERVAL '5 hours', 'e1111111-1111-1111-1111-111111111118'),
-- Voucher credit
('77777777-0001-0001-0001-000000000006', 'e1111111-1111-1111-1111-111111111114', 4, 200000, 700000, NULL, NULL, NULL, 1, 'Nhận voucher khuyến mãi', 'WTX-DEMO-0006', NOW() - INTERVAL '3 days', 'e1111111-1111-1111-1111-111111111114'),
-- Refund
('77777777-0001-0001-0001-000000000007', 'e1111111-1111-1111-1111-111111111113', 2, 50000, 3250000, 3, NULL, NULL, 1, 'Hoàn tiền phiên sạc lỗi', 'WTX-DEMO-0007', NOW() - INTERVAL '4 days', NULL),
-- Pending top-up
('77777777-0001-0001-0001-000000000008', 'e1111111-1111-1111-1111-111111111115', 0, 2000000, 6500000, 4, 'VNPAY_TOPUP_001', NULL, 0, 'Nạp ví qua VnPay', 'WTX-DEMO-0008', NOW() - INTERVAL '30 minutes', 'e1111111-1111-1111-1111-111111111115'),
-- Failed top-up
('77777777-0001-0001-0001-000000000009', 'e1111111-1111-1111-1111-111111111120', 0, 500000, 1500000, 1, NULL, NULL, 2, 'Giao dịch MoMo thất bại', 'WTX-DEMO-0009', NOW() - INTERVAL '1 day', 'e1111111-1111-1111-1111-111111111120'),
-- Admin adjustment
('77777777-0001-0001-0001-000000000010', 'e1111111-1111-1111-1111-111111111116', 3, 100000, 5100000, NULL, NULL, NULL, 1, 'Điều chỉnh số dư bởi admin', 'WTX-DEMO-0010', NOW() - INTERVAL '2 days', NULL);

-- ============================================================
-- 11. DEVICE TOKENS (8 tokens)
-- ============================================================
DELETE FROM "AppDeviceTokens" WHERE "Token" LIKE 'demo_%';

INSERT INTO "AppDeviceTokens" ("Id", "UserId", "Token", "Platform", "IsActive", "RegisteredAt", "CreationTime", "CreatorId")
VALUES
('88888888-0001-0001-0001-000000000001', 'e1111111-1111-1111-1111-111111111111', 'demo_fcm_token_an_ios_001', 0, true, NOW() - INTERVAL '30 days', NOW() - INTERVAL '30 days', 'e1111111-1111-1111-1111-111111111111'),
('88888888-0001-0001-0001-000000000002', 'e1111111-1111-1111-1111-111111111112', 'demo_fcm_token_binh_android_001', 1, true, NOW() - INTERVAL '25 days', NOW() - INTERVAL '25 days', 'e1111111-1111-1111-1111-111111111112'),
('88888888-0001-0001-0001-000000000003', 'e1111111-1111-1111-1111-111111111113', 'demo_fcm_token_cuong_android_001', 1, true, NOW() - INTERVAL '20 days', NOW() - INTERVAL '20 days', 'e1111111-1111-1111-1111-111111111113'),
('88888888-0001-0001-0001-000000000004', 'e1111111-1111-1111-1111-111111111116', 'demo_fcm_token_john_ios_001', 0, true, NOW() - INTERVAL '28 days', NOW() - INTERVAL '28 days', 'e1111111-1111-1111-1111-111111111116'),
('88888888-0001-0001-0001-000000000005', 'e1111111-1111-1111-1111-111111111116', 'demo_fcm_token_john_android_001', 1, false, NOW() - INTERVAL '40 days', NOW() - INTERVAL '40 days', 'e1111111-1111-1111-1111-111111111116'),
('88888888-0001-0001-0001-000000000006', 'e1111111-1111-1111-1111-111111111118', 'demo_fcm_token_abc_fleet_001', 1, true, NOW() - INTERVAL '60 days', NOW() - INTERVAL '60 days', 'e1111111-1111-1111-1111-111111111118'),
('88888888-0001-0001-0001-000000000007', 'e1111111-1111-1111-1111-111111111119', 'demo_fcm_token_grab_001', 1, true, NOW() - INTERVAL '90 days', NOW() - INTERVAL '90 days', 'e1111111-1111-1111-1111-111111111119'),
('88888888-0001-0001-0001-000000000008', 'e1111111-1111-1111-1111-111111111120', 'demo_fcm_token_giang_ios_001', 0, true, NOW() - INTERVAL '5 days', NOW() - INTERVAL '5 days', 'e1111111-1111-1111-1111-111111111120');

-- ============================================================
-- 12. NOTIFICATION PREFERENCES (5 preferences)
-- ============================================================
DELETE FROM "AppNotificationPreferences" WHERE "UserId" IN (
  'e1111111-1111-1111-1111-111111111111', 'e1111111-1111-1111-1111-111111111112',
  'e1111111-1111-1111-1111-111111111114', 'e1111111-1111-1111-1111-111111111116',
  'e1111111-1111-1111-1111-111111111120'
);

INSERT INTO "AppNotificationPreferences" ("Id", "UserId", "ChargingComplete", "PaymentAlerts", "FaultAlerts", "Promotions")
VALUES
('99999999-0001-0001-0001-000000000001', 'e1111111-1111-1111-1111-111111111111', true, true, true, true),
('99999999-0001-0001-0001-000000000002', 'e1111111-1111-1111-1111-111111111112', true, true, false, true),
('99999999-0001-0001-0001-000000000003', 'e1111111-1111-1111-1111-111111111114', true, false, false, false),
('99999999-0001-0001-0001-000000000004', 'e1111111-1111-1111-1111-111111111116', true, true, true, false),
('99999999-0001-0001-0001-000000000005', 'e1111111-1111-1111-1111-111111111120', true, true, true, true);

-- ============================================================
-- 13. FAVORITE STATIONS (8 favorites)
-- ============================================================
DELETE FROM "AppFavoriteStations" WHERE "Id"::text LIKE 'aaa00000-%';

INSERT INTO "AppFavoriteStations" ("Id", "UserId", "StationId", "CreationTime", "CreatorId")
VALUES
('aaa00000-0001-0001-0001-000000000001', 'e1111111-1111-1111-1111-111111111111', 'b1111111-1111-1111-1111-111111111111', NOW() - INTERVAL '20 days', 'e1111111-1111-1111-1111-111111111111'),
('aaa00000-0001-0001-0001-000000000002', 'e1111111-1111-1111-1111-111111111111', 'b2222222-2222-2222-2222-222222222221', NOW() - INTERVAL '15 days', 'e1111111-1111-1111-1111-111111111111'),
('aaa00000-0001-0001-0001-000000000003', 'e1111111-1111-1111-1111-111111111112', 'b1111111-1111-1111-1111-111111111112', NOW() - INTERVAL '18 days', 'e1111111-1111-1111-1111-111111111112'),
('aaa00000-0001-0001-0001-000000000004', 'e1111111-1111-1111-1111-111111111113', 'b2222222-2222-2222-2222-222222222221', NOW() - INTERVAL '10 days', 'e1111111-1111-1111-1111-111111111113'),
('aaa00000-0001-0001-0001-000000000005', 'e1111111-1111-1111-1111-111111111116', 'b1111111-1111-1111-1111-111111111111', NOW() - INTERVAL '25 days', 'e1111111-1111-1111-1111-111111111116'),
('aaa00000-0001-0001-0001-000000000006', 'e1111111-1111-1111-1111-111111111118', 'b2222222-2222-2222-2222-222222222221', NOW() - INTERVAL '50 days', 'e1111111-1111-1111-1111-111111111118'),
('aaa00000-0001-0001-0001-000000000007', 'e1111111-1111-1111-1111-111111111118', 'b2222222-2222-2222-2222-222222222222', NOW() - INTERVAL '50 days', 'e1111111-1111-1111-1111-111111111118'),
('aaa00000-0001-0001-0001-000000000008', 'e1111111-1111-1111-1111-111111111119', 'b1111111-1111-1111-1111-111111111111', NOW() - INTERVAL '80 days', 'e1111111-1111-1111-1111-111111111119');

-- ============================================================
-- 14. STATION AMENITIES (15 amenities across stations)
-- ============================================================
DELETE FROM "AppStationAmenities" WHERE "Id"::text LIKE 'bbb00000-%';

INSERT INTO "AppStationAmenities" ("Id", "StationId", "AmenityType")
VALUES
-- Station KC-HN-001: Wifi, Parking, Canopy, Security24h
('bbb00000-0001-0001-0001-000000000001', 'b1111111-1111-1111-1111-111111111111', 0),
('bbb00000-0001-0001-0001-000000000002', 'b1111111-1111-1111-1111-111111111111', 3),
('bbb00000-0001-0001-0001-000000000003', 'b1111111-1111-1111-1111-111111111111', 7),
('bbb00000-0001-0001-0001-000000000004', 'b1111111-1111-1111-1111-111111111111', 8),
-- Station KC-HN-002: Wifi, Restroom, CoffeeShop, WaitingRoom
('bbb00000-0001-0001-0001-000000000005', 'b1111111-1111-1111-1111-111111111112', 0),
('bbb00000-0001-0001-0001-000000000006', 'b1111111-1111-1111-1111-111111111112', 1),
('bbb00000-0001-0001-0001-000000000007', 'b1111111-1111-1111-1111-111111111112', 2),
('bbb00000-0001-0001-0001-000000000008', 'b1111111-1111-1111-1111-111111111112', 6),
-- Station KC-HCM-001: Wifi, Parking, Restaurant, Security24h
('bbb00000-0001-0001-0001-000000000009', 'b2222222-2222-2222-2222-222222222221', 0),
('bbb00000-0001-0001-0001-000000000010', 'b2222222-2222-2222-2222-222222222221', 3),
('bbb00000-0001-0001-0001-000000000011', 'b2222222-2222-2222-2222-222222222221', 4),
('bbb00000-0001-0001-0001-000000000012', 'b2222222-2222-2222-2222-222222222221', 8),
-- Station KC-HCM-002: Parking, ConvenienceStore
('bbb00000-0001-0001-0001-000000000013', 'b2222222-2222-2222-2222-222222222222', 3),
('bbb00000-0001-0001-0001-000000000014', 'b2222222-2222-2222-2222-222222222222', 5),
-- Station KC-DN-001: Wifi, Parking, Canopy
('bbb00000-0001-0001-0001-000000000015', 'b3333333-3333-3333-3333-333333333331', 0),
('bbb00000-0001-0001-0001-000000000016', 'b3333333-3333-3333-3333-333333333331', 3),
('bbb00000-0001-0001-0001-000000000017', 'b3333333-3333-3333-3333-333333333331', 7);

-- ============================================================
-- 15. STATION PHOTOS (6 photos)
-- ============================================================
DELETE FROM "AppStationPhotos" WHERE "Id"::text LIKE 'ccc00000-%';

INSERT INTO "AppStationPhotos" ("Id", "StationId", "Url", "ThumbnailUrl", "IsPrimary", "SortOrder", "CreationTime", "CreatorId")
VALUES
('ccc00000-0001-0001-0001-000000000001', 'b1111111-1111-1111-1111-111111111111', '/uploads/stations/kc-hn-001-front.jpg', '/uploads/stations/kc-hn-001-front-thumb.jpg', true, 0, NOW() - INTERVAL '30 days', NULL),
('ccc00000-0001-0001-0001-000000000002', 'b1111111-1111-1111-1111-111111111111', '/uploads/stations/kc-hn-001-chargers.jpg', '/uploads/stations/kc-hn-001-chargers-thumb.jpg', false, 1, NOW() - INTERVAL '30 days', NULL),
('ccc00000-0001-0001-0001-000000000003', 'b1111111-1111-1111-1111-111111111112', '/uploads/stations/kc-hn-002-front.jpg', '/uploads/stations/kc-hn-002-front-thumb.jpg', true, 0, NOW() - INTERVAL '25 days', NULL),
('ccc00000-0001-0001-0001-000000000004', 'b2222222-2222-2222-2222-222222222221', '/uploads/stations/kc-hcm-001-aerial.jpg', '/uploads/stations/kc-hcm-001-aerial-thumb.jpg', true, 0, NOW() - INTERVAL '20 days', NULL),
('ccc00000-0001-0001-0001-000000000005', 'b2222222-2222-2222-2222-222222222221', '/uploads/stations/kc-hcm-001-night.jpg', '/uploads/stations/kc-hcm-001-night-thumb.jpg', false, 1, NOW() - INTERVAL '20 days', NULL),
('ccc00000-0001-0001-0001-000000000006', 'b3333333-3333-3333-3333-333333333331', '/uploads/stations/kc-dn-001-front.jpg', '/uploads/stations/kc-dn-001-front-thumb.jpg', true, 0, NOW() - INTERVAL '15 days', NULL);

-- ============================================================
-- 16. VOUCHERS (4 vouchers)
-- ============================================================
DELETE FROM "AppUserVouchers" WHERE "VoucherId" IN (SELECT "Id" FROM "AppVouchers" WHERE "Code" LIKE 'DEMO%');
DELETE FROM "AppVouchers" WHERE "Code" LIKE 'DEMO%';

INSERT INTO "AppVouchers" ("Id", "Code", "Type", "Value", "MinOrderAmount", "MaxDiscountAmount", "ExpiryDate", "TotalQuantity", "UsedQuantity", "IsActive", "Description", "ExtraProperties", "ConcurrencyStamp", "CreationTime", "IsDeleted")
VALUES
('dd000000-0001-0001-0001-000000000001', 'DEMO-WELCOME', 0, 50000, NULL, NULL, '2026-12-31', 1000, 3, true, 'Voucher chào mừng tài khoản mới - Giảm 50.000đ', '{}', 'seed-v001', NOW() - INTERVAL '30 days', false),
('dd000000-0001-0001-0001-000000000002', 'DEMO-20PCT', 1, 20, 100000, 200000, '2026-06-30', 500, 1, true, 'Giảm 20% tối đa 200.000đ cho đơn từ 100.000đ', '{}', 'seed-v002', NOW() - INTERVAL '20 days', false),
('dd000000-0001-0001-0001-000000000003', 'DEMO-FREE30', 2, 30, NULL, NULL, '2026-06-30', 100, 0, true, 'Miễn phí sạc 30 phút đầu tiên', '{}', 'seed-v003', NOW() - INTERVAL '15 days', false),
('dd000000-0001-0001-0001-000000000004', 'DEMO-EXPIRED', 0, 100000, NULL, NULL, '2026-02-28', 200, 45, false, 'Voucher đã hết hạn', '{}', 'seed-v004', NOW() - INTERVAL '60 days', false);

-- ============================================================
-- 17. USER VOUCHERS (4 redemptions)
-- ============================================================
INSERT INTO "AppUserVouchers" ("Id", "UserId", "VoucherId", "IsUsed", "UsedAt", "CreationTime", "CreatorId")
VALUES
('ee000000-0001-0001-0001-000000000001', 'e1111111-1111-1111-1111-111111111111', 'dd000000-0001-0001-0001-000000000001', true, NOW() - INTERVAL '25 days', NOW() - INTERVAL '28 days', 'e1111111-1111-1111-1111-111111111111'),
('ee000000-0001-0001-0001-000000000002', 'e1111111-1111-1111-1111-111111111112', 'dd000000-0001-0001-0001-000000000001', true, NOW() - INTERVAL '20 days', NOW() - INTERVAL '22 days', 'e1111111-1111-1111-1111-111111111112'),
('ee000000-0001-0001-0001-000000000003', 'e1111111-1111-1111-1111-111111111114', 'dd000000-0001-0001-0001-000000000001', true, NOW() - INTERVAL '3 days', NOW() - INTERVAL '5 days', 'e1111111-1111-1111-1111-111111111114'),
('ee000000-0001-0001-0001-000000000004', 'e1111111-1111-1111-1111-111111111116', 'dd000000-0001-0001-0001-000000000002', false, NULL, NOW() - INTERVAL '10 days', 'e1111111-1111-1111-1111-111111111116');

-- ============================================================
-- 18. PROMOTIONS (3 promotions)
-- ============================================================
DELETE FROM "AppPromotions" WHERE "Title" LIKE '%DEMO%' OR "Title" LIKE '%demo%' OR "Id"::text LIKE 'ff000000-%';

INSERT INTO "AppPromotions" ("Id", "Title", "Description", "ImageUrl", "StartDate", "EndDate", "Type", "IsActive", "ExtraProperties", "ConcurrencyStamp", "CreationTime", "IsDeleted")
VALUES
('ff000000-0001-0001-0001-000000000001', 'Giảm 20% giờ thấp điểm', 'Sạc xe điện từ 22h-6h chỉ còn 2.240đ/kWh. Áp dụng tại tất cả trạm KLC.', '/uploads/promotions/off-peak-banner.jpg', '2026-03-01', '2026-06-30', 0, true, '{}', 'seed-p001', NOW() - INTERVAL '5 days', false),
('ff000000-0001-0001-0001-000000000002', 'Miễn phí sạc lần đầu', 'Đăng ký tài khoản mới, nhận ngay 30 phút sạc miễn phí!', '/uploads/promotions/first-charge-free.jpg', '2026-03-01', '2026-12-31', 2, true, '{}', 'seed-p002', NOW() - INTERVAL '5 days', false),
('ff000000-0001-0001-0001-000000000003', 'Flash Sale cuối tuần', 'Giảm 50% phí sạc vào Thứ 7 & Chủ nhật. Số lượng có hạn!', '/uploads/promotions/weekend-flash-sale.jpg', '2026-02-01', '2026-02-28', 1, false, '{}', 'seed-p003', NOW() - INTERVAL '35 days', false);

-- ============================================================
-- 19. USER FEEDBACK (5 feedback items)
-- ============================================================
DELETE FROM "AppUserFeedbacks" WHERE "Subject" LIKE '%DEMO%' OR "Subject" LIKE '%demo%' OR "Id"::text LIKE '11000000-%';

INSERT INTO "AppUserFeedbacks" ("Id", "UserId", "Type", "Subject", "Message", "Status", "AdminResponse", "RespondedAt", "RespondedBy", "CreationTime", "IsDeleted")
VALUES
('11000000-0001-0001-0001-000000000001', 'e1111111-1111-1111-1111-111111111111', 2, '[DEMO] Không sạc được tại KC-HN-003', 'Đã cắm sạc nhưng xe không nhận điện. Connector 1 có vẻ bị lỏng.', 2, 'Cảm ơn bạn đã phản hồi. Đội kỹ thuật đã sửa connector. Mong bạn thử lại.', NOW() - INTERVAL '3 days', NULL, NOW() - INTERVAL '5 days', false),
('11000000-0001-0001-0001-000000000002', 'e1111111-1111-1111-1111-111111111112', 3, '[DEMO] Thanh toán bị trừ tiền 2 lần', 'Phiên sạc ngày 28/02 bị trừ tiền 2 lần. Số tiền 123.200đ x2. Mong được hoàn tiền.', 1, NULL, NULL, NULL, NOW() - INTERVAL '3 days', false),
('11000000-0001-0001-0001-000000000003', 'e1111111-1111-1111-1111-111111111116', 1, '[DEMO] Request: Apple Pay support', 'It would be great to have Apple Pay as a payment option for the wallet top-up.', 0, NULL, NULL, NULL, NOW() - INTERVAL '2 days', false),
('11000000-0001-0001-0001-000000000004', 'e1111111-1111-1111-1111-111111111118', 0, '[DEMO] App crash khi xem lịch sử sạc', 'App bị crash khi mở trang lịch sử với nhiều phiên sạc (>50). Dùng Android 14.', 0, NULL, NULL, NULL, NOW() - INTERVAL '1 day', false),
('11000000-0001-0001-0001-000000000005', 'e1111111-1111-1111-1111-111111111120', 4, '[DEMO] Cảm ơn đội ngũ KLC', 'Trải nghiệm sạc rất tốt, app dễ dùng. Cảm ơn!', 3, 'Cảm ơn bạn đã gửi nhận xét tích cực! Chúng tôi sẽ tiếp tục cải thiện dịch vụ.', NOW() - INTERVAL '6 hours', NULL, NOW() - INTERVAL '1 day', false);

-- ============================================================
-- 20. E-INVOICES (10 e-invoices for completed invoices)
-- ============================================================
DELETE FROM "AppEInvoices" WHERE "Id"::text LIKE '12000000-%';

INSERT INTO "AppEInvoices" ("Id", "InvoiceId", "Provider", "ExternalInvoiceId", "EInvoiceNumber", "Status", "ViewUrl", "PdfUrl", "SignatureHash", "IssuedAt", "ErrorMessage", "RetryCount", "CreationTime", "IsDeleted")
VALUES
-- Issued e-invoices (MISA provider) - linked to first 5 invoices
('12000000-0001-0001-0001-000000000001', '33333333-0001-0001-0001-000000000001', 0, 'MISA-2026-00001', 'EI-KLC-2026-000001', 2, 'https://einvoice.misa.vn/view/MISA-2026-00001', 'https://einvoice.misa.vn/pdf/MISA-2026-00001', 'a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6', NOW() - INTERVAL '2 days 12 hours', NULL, 0, NOW() - INTERVAL '2 days 12 hours 20 minutes', false),
('12000000-0001-0001-0001-000000000002', '33333333-0001-0001-0001-000000000002', 0, 'MISA-2026-00002', 'EI-KLC-2026-000002', 2, 'https://einvoice.misa.vn/view/MISA-2026-00002', 'https://einvoice.misa.vn/pdf/MISA-2026-00002', 'b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7', NOW() - INTERVAL '2 days 8 hours', NULL, 0, NOW() - INTERVAL '2 days 8 hours 35 minutes', false),
('12000000-0001-0001-0001-000000000003', '33333333-0001-0001-0001-000000000003', 1, 'VTL-2026-00001', 'EI-KLC-2026-000003', 2, 'https://einvoice.viettel.vn/view/VTL-2026-00001', 'https://einvoice.viettel.vn/pdf/VTL-2026-00001', 'c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8', NOW() - INTERVAL '1 day 15 hours', NULL, 0, NOW() - INTERVAL '1 day 15 hours 10 minutes', false),
('12000000-0001-0001-0001-000000000004', '33333333-0001-0001-0001-000000000004', 1, 'VTL-2026-00002', 'EI-KLC-2026-000004', 2, 'https://einvoice.viettel.vn/view/VTL-2026-00002', 'https://einvoice.viettel.vn/pdf/VTL-2026-00002', 'd4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9', NOW() - INTERVAL '1 day 10 hours', NULL, 0, NOW() - INTERVAL '1 day 10 hours 5 minutes', false),
('12000000-0001-0001-0001-000000000005', '33333333-0001-0001-0001-000000000005', 0, 'MISA-2026-00003', 'EI-KLC-2026-000005', 2, 'https://einvoice.misa.vn/view/MISA-2026-00003', 'https://einvoice.misa.vn/pdf/MISA-2026-00003', 'e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0', NOW() - INTERVAL '1 day 5 hours', NULL, 0, NOW() - INTERVAL '1 day 5 hours 50 minutes', false),
-- Processing e-invoice (VNPT provider)
('12000000-0001-0001-0001-000000000006', '33333333-0001-0001-0001-000000000006', 2, NULL, NULL, 1, NULL, NULL, NULL, NULL, NULL, 0, NOW() - INTERVAL '9 hours 20 minutes', false),
-- Failed e-invoice (MISA) - retryable
('12000000-0001-0001-0001-000000000007', '33333333-0001-0001-0001-000000000007', 0, NULL, NULL, 3, NULL, NULL, NULL, NULL, 'Connection timeout to MISA API server', 1, NOW() - INTERVAL '6 hours 35 minutes', false),
-- Pending e-invoices (not yet submitted)
('12000000-0001-0001-0001-000000000008', '33333333-0001-0001-0001-000000000008', 1, NULL, NULL, 0, NULL, NULL, NULL, NULL, NULL, 0, NOW() - INTERVAL '4 hours 50 minutes', false),
('12000000-0001-0001-0001-000000000009', '33333333-0001-0001-0001-000000000009', 0, NULL, NULL, 0, NULL, NULL, NULL, NULL, NULL, 0, NOW() - INTERVAL '4 hours 10 minutes', false),
-- Cancelled e-invoice (customer requested cancellation)
('12000000-0001-0001-0001-000000000010', '33333333-0001-0001-0001-000000000010', 0, 'MISA-2026-00004', 'EI-KLC-2026-000010', 4, NULL, NULL, NULL, NOW() - INTERVAL '2 hours 45 minutes', 'Cancelled by admin - customer requested correction', 0, NOW() - INTERVAL '2 hours 50 minutes', false);

-- ============================================================
-- 23. STATUS CHANGE LOGS (15 logs)
-- ============================================================
DELETE FROM "AppStatusChangeLogs" WHERE "Id"::text LIKE '5c000000-%';

INSERT INTO "AppStatusChangeLogs" ("Id", "StationId", "ConnectorNumber", "PreviousStatus", "NewStatus", "Timestamp", "Source", "Details")
VALUES
-- KC-HN-001 C1: Available -> Charging -> Available (recent session)
('5c000000-0001-0001-0001-000000000001', 'b1111111-1111-1111-1111-111111111111', 1, 'Available', 'Preparing', NOW() - INTERVAL '4 hours 5 minutes', 'OCPP', 'Vehicle connected'),
('5c000000-0001-0001-0001-000000000002', 'b1111111-1111-1111-1111-111111111111', 1, 'Preparing', 'Charging', NOW() - INTERVAL '4 hours', 'OCPP', 'Charging started - Transaction 1010'),
('5c000000-0001-0001-0001-000000000003', 'b1111111-1111-1111-1111-111111111111', 1, 'Charging', 'Available', NOW() - INTERVAL '3 hours', 'OCPP', 'Charging complete - 38 kWh delivered'),
-- KC-HN-003: Station went Offline
('5c000000-0001-0001-0001-000000000004', 'b1111111-1111-1111-1111-111111111113', NULL, 'Available', 'Offline', NOW() - INTERVAL '35 minutes', 'System', 'Heartbeat timeout - no response for 30 minutes'),
-- KC-HN-003 C1: Fault history
('5c000000-0001-0001-0001-000000000005', 'b1111111-1111-1111-1111-111111111113', 1, 'Available', 'Faulted', NOW() - INTERVAL '5 days', 'OCPP', 'GroundFailure detected'),
('5c000000-0001-0001-0001-000000000006', 'b1111111-1111-1111-1111-111111111113', 1, 'Faulted', 'Available', NOW() - INTERVAL '4 days 20 hours', 'Admin', 'Fault resolved - ground connection replaced'),
('5c000000-0001-0001-0001-000000000007', 'b1111111-1111-1111-1111-111111111113', 1, 'Available', 'Faulted', NOW() - INTERVAL '1 day', 'OCPP', 'ReaderFailure - RFID not responding'),
-- KC-HCM-001 C1: Currently Charging
('5c000000-0001-0001-0001-000000000008', 'b2222222-2222-2222-2222-222222222221', 1, 'Available', 'Preparing', NOW() - INTERVAL '20 minutes', 'OCPP', 'Vehicle connected'),
('5c000000-0001-0001-0001-000000000009', 'b2222222-2222-2222-2222-222222222221', 1, 'Preparing', 'Charging', NOW() - INTERVAL '15 minutes', 'OCPP', 'Charging started'),
-- KC-DN-001 C2: Faulted
('5c000000-0001-0001-0001-000000000010', 'b3333333-3333-3333-3333-333333333331', 2, 'Available', 'Faulted', NOW() - INTERVAL '2 hours', 'OCPP', 'EVCommunicationError - timeout with vehicle'),
-- KC-DN-002: Scheduled maintenance
('5c000000-0001-0001-0001-000000000011', 'b3333333-3333-3333-3333-333333333332', NULL, 'Available', 'Unavailable', NOW() - INTERVAL '3 days', 'Admin', 'Scheduled maintenance - firmware upgrade'),
('5c000000-0001-0001-0001-000000000012', 'b3333333-3333-3333-3333-333333333332', 1, 'Available', 'Unavailable', NOW() - INTERVAL '3 days', 'Admin', 'Connector disabled for maintenance'),
('5c000000-0001-0001-0001-000000000013', 'b3333333-3333-3333-3333-333333333332', 2, 'Available', 'Unavailable', NOW() - INTERVAL '3 days', 'Admin', 'Connector disabled for maintenance'),
-- KC-HN-002 C1: Normal daily operations
('5c000000-0001-0001-0001-000000000014', 'b1111111-1111-1111-1111-111111111112', 1, 'Available', 'Charging', NOW() - INTERVAL '2 hours', 'OCPP', 'Session started - Transaction 1012'),
('5c000000-0001-0001-0001-000000000015', 'b1111111-1111-1111-1111-111111111112', 1, 'Charging', 'Available', NOW() - INTERVAL '1 hour 30 minutes', 'OCPP', 'Session completed - 15 kWh delivered');

-- ============================================================
-- 10. POWER SHARING GROUPS (2 groups with members + load profiles)
-- ============================================================
DELETE FROM "AppSiteLoadProfiles" WHERE "PowerSharingGroupId"::text LIKE 'aa000001-%';
DELETE FROM "AppPowerSharingGroupMembers" WHERE "PowerSharingGroupId"::text LIKE 'aa000001-%';
DELETE FROM "AppPowerSharingGroups" WHERE "Id"::text LIKE 'aa000001-%';

INSERT INTO "AppPowerSharingGroups" ("Id", "Name", "MaxCapacityKw", "Mode", "DistributionStrategy", "MinPowerPerConnectorKw", "IsActive", "StationGroupId", "ExtraProperties", "ConcurrencyStamp", "CreationTime", "IsDeleted")
VALUES
-- LINK mode group: Times City Hà Nội (4 connectors sharing 120kW)
('aa000001-0001-0001-0001-000000000001', 'Times City - Chia sẻ công suất', 120.00, 0, 2, 7.00, true, 'a0000001-0001-0001-0001-000000000001', '{}', 'seed-psg001', NOW() - INTERVAL '30 days', false),
-- LOOP mode group: HCM cluster (5 connectors sharing 200kW)
('aa000001-0001-0001-0001-000000000002', 'HCM Cluster - Cân bằng tải', 200.00, 1, 1, 5.00, true, 'a0000001-0001-0001-0001-000000000002', '{}', 'seed-psg002', NOW() - INTERVAL '25 days', false);

-- Power sharing members: Times City group (KC-HN-001 + KC-HN-002 connectors)
INSERT INTO "AppPowerSharingGroupMembers" ("Id", "PowerSharingGroupId", "StationId", "ConnectorId", "Priority", "AllocatedPowerKw", "CreationTime", "IsDeleted")
VALUES
('ab000001-0001-0001-0001-000000000001', 'aa000001-0001-0001-0001-000000000001', 'b1111111-1111-1111-1111-111111111111', 'c1000000-0001-0001-0001-000000000001', 1, 40.00,  NOW() - INTERVAL '30 days', false),
('ab000001-0001-0001-0001-000000000002', 'aa000001-0001-0001-0001-000000000001', 'b1111111-1111-1111-1111-111111111111', 'c1000000-0001-0001-0001-000000000002', 2, 20.00,  NOW() - INTERVAL '30 days', false),
('ab000001-0001-0001-0001-000000000003', 'aa000001-0001-0001-0001-000000000001', 'b1111111-1111-1111-1111-111111111112', 'c1000000-0001-0001-0001-000000000003', 1, 35.00,  NOW() - INTERVAL '30 days', false),
('ab000001-0001-0001-0001-000000000004', 'aa000001-0001-0001-0001-000000000001', 'b1111111-1111-1111-1111-111111111112', 'c1000000-0001-0001-0001-000000000004', 3, 20.00,  NOW() - INTERVAL '30 days', false);

-- Power sharing members: HCM Cluster group (KC-HCM-001 + KC-HCM-002 connectors)
INSERT INTO "AppPowerSharingGroupMembers" ("Id", "PowerSharingGroupId", "StationId", "ConnectorId", "Priority", "AllocatedPowerKw", "CreationTime", "IsDeleted")
VALUES
('ab000001-0001-0001-0001-000000000005', 'aa000001-0001-0001-0001-000000000002', 'b2222222-2222-2222-2222-222222222221', 'c1000000-0001-0001-0001-000000000008', 1, 50.00,  NOW() - INTERVAL '25 days', false),
('ab000001-0001-0001-0001-000000000006', 'aa000001-0001-0001-0001-000000000002', 'b2222222-2222-2222-2222-222222222221', 'c1000000-0001-0001-0001-000000000009', 2, 22.00,  NOW() - INTERVAL '25 days', false),
('ab000001-0001-0001-0001-000000000007', 'aa000001-0001-0001-0001-000000000002', 'b2222222-2222-2222-2222-222222222222', 'c1000000-0001-0001-0001-000000000010', 1, 50.00,  NOW() - INTERVAL '25 days', false),
('ab000001-0001-0001-0001-000000000008', 'aa000001-0001-0001-0001-000000000002', 'b2222222-2222-2222-2222-222222222222', 'c1000000-0001-0001-0001-000000000011', 2, 22.00,  NOW() - INTERVAL '25 days', false),
('ab000001-0001-0001-0001-000000000009', 'aa000001-0001-0001-0001-000000000002', 'b2222222-2222-2222-2222-222222222222', 'c1000000-0001-0001-0001-000000000012', 3, 50.00,  NOW() - INTERVAL '25 days', false);

-- Site load profiles (recent snapshots for the groups)
INSERT INTO "AppSiteLoadProfiles" ("Id", "PowerSharingGroupId", "Timestamp", "TotalLoadKw", "AvailableCapacityKw", "ActiveSessionCount", "TotalConnectorCount", "PeakLoadKw", "CreationTime")
VALUES
-- Times City group load profiles (last 6 hours)
('ac000001-0001-0001-0001-000000000001', 'aa000001-0001-0001-0001-000000000001', NOW() - INTERVAL '6 hours', 45.50, 74.50, 1, 4, 95.00, NOW() - INTERVAL '6 hours'),
('ac000001-0001-0001-0001-000000000002', 'aa000001-0001-0001-0001-000000000001', NOW() - INTERVAL '5 hours', 82.30, 37.70, 2, 4, 95.00, NOW() - INTERVAL '5 hours'),
('ac000001-0001-0001-0001-000000000003', 'aa000001-0001-0001-0001-000000000001', NOW() - INTERVAL '4 hours', 95.00, 25.00, 3, 4, 95.00, NOW() - INTERVAL '4 hours'),
('ac000001-0001-0001-0001-000000000004', 'aa000001-0001-0001-0001-000000000001', NOW() - INTERVAL '3 hours', 60.20, 59.80, 2, 4, 95.00, NOW() - INTERVAL '3 hours'),
('ac000001-0001-0001-0001-000000000005', 'aa000001-0001-0001-0001-000000000001', NOW() - INTERVAL '2 hours', 40.00, 80.00, 1, 4, 95.00, NOW() - INTERVAL '2 hours'),
('ac000001-0001-0001-0001-000000000006', 'aa000001-0001-0001-0001-000000000001', NOW() - INTERVAL '1 hour', 75.80, 44.20, 2, 4, 95.00, NOW() - INTERVAL '1 hour'),
-- HCM Cluster group load profiles (last 6 hours)
('ac000001-0001-0001-0001-000000000007', 'aa000001-0001-0001-0001-000000000002', NOW() - INTERVAL '6 hours', 110.00, 90.00, 3, 5, 165.00, NOW() - INTERVAL '6 hours'),
('ac000001-0001-0001-0001-000000000008', 'aa000001-0001-0001-0001-000000000002', NOW() - INTERVAL '5 hours', 145.50, 54.50, 4, 5, 165.00, NOW() - INTERVAL '5 hours'),
('ac000001-0001-0001-0001-000000000009', 'aa000001-0001-0001-0001-000000000002', NOW() - INTERVAL '4 hours', 165.00, 35.00, 5, 5, 165.00, NOW() - INTERVAL '4 hours'),
('ac000001-0001-0001-0001-000000000010', 'aa000001-0001-0001-0001-000000000002', NOW() - INTERVAL '3 hours', 130.20, 69.80, 3, 5, 165.00, NOW() - INTERVAL '3 hours'),
('ac000001-0001-0001-0001-000000000011', 'aa000001-0001-0001-0001-000000000002', NOW() - INTERVAL '2 hours', 88.00, 112.00, 2, 5, 165.00, NOW() - INTERVAL '2 hours'),
('ac000001-0001-0001-0001-000000000012', 'aa000001-0001-0001-0001-000000000002', NOW() - INTERVAL '1 hour', 155.30, 44.70, 4, 5, 165.00, NOW() - INTERVAL '1 hour');

-- ============================================================
-- 11. OPERATORS (2 B2B operators with station assignments + webhook logs)
-- ============================================================
DELETE FROM "AppOperatorWebhookLogs" WHERE "OperatorId"::text LIKE 'bb000001-%';
DELETE FROM "AppOperatorStations" WHERE "OperatorId"::text LIKE 'bb000001-%';
DELETE FROM "AppOperators" WHERE "Id"::text LIKE 'bb000001-%';

-- API Key hashes: SHA256 of the raw key (for demo only)
-- Operator 1 key: "demo-op-key-evn-2026" → SHA256
-- Operator 2 key: "demo-op-key-grab-2026" → SHA256
INSERT INTO "AppOperators" ("Id", "Name", "ApiKeyHash", "ContactEmail", "WebhookUrl", "Description", "IsActive", "RateLimitPerMinute", "ExtraProperties", "ConcurrencyStamp", "CreationTime", "IsDeleted")
VALUES
('bb000001-0001-0001-0001-000000000001', 'EVN Smart Charging', 'a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2', 'api@evn-smart.vn', 'https://api.evn-smart.vn/webhooks/klc', 'Đối tác EVN - Quản lý trạm sạc khu vực phía Bắc. Tích hợp API để giám sát phiên sạc và trạng thái trạm.', true, 500, '{}', 'seed-op001', NOW() - INTERVAL '20 days', false),
('bb000001-0001-0001-0001-000000000002', 'Grab EV Services', 'b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3', 'ev-api@grab.vn', 'https://ev-services.grab.vn/hooks/charging', 'Grab Vietnam - Đội xe điện. Theo dõi phiên sạc và trạng thái connector cho đội xe Grab.', true, 1000, '{}', 'seed-op002', NOW() - INTERVAL '15 days', false);

-- Station assignments: EVN → Hanoi stations, Grab → HCM stations
INSERT INTO "AppOperatorStations" ("Id", "OperatorId", "StationId", "CreationTime", "IsDeleted")
VALUES
-- EVN manages all Hanoi stations
('bc000001-0001-0001-0001-000000000001', 'bb000001-0001-0001-0001-000000000001', 'b1111111-1111-1111-1111-111111111111',  NOW() - INTERVAL '20 days', false),
('bc000001-0001-0001-0001-000000000002', 'bb000001-0001-0001-0001-000000000001', 'b1111111-1111-1111-1111-111111111112',  NOW() - INTERVAL '20 days', false),
('bc000001-0001-0001-0001-000000000003', 'bb000001-0001-0001-0001-000000000001', 'b1111111-1111-1111-1111-111111111113',  NOW() - INTERVAL '20 days', false),
('bc000001-0001-0001-0001-000000000004', 'bb000001-0001-0001-0001-000000000001', 'b1111111-1111-1111-1111-111111111114',  NOW() - INTERVAL '20 days', false),
-- Grab manages HCM stations
('bc000001-0001-0001-0001-000000000005', 'bb000001-0001-0001-0001-000000000002', 'b2222222-2222-2222-2222-222222222221',  NOW() - INTERVAL '15 days', false),
('bc000001-0001-0001-0001-000000000006', 'bb000001-0001-0001-0001-000000000002', 'b2222222-2222-2222-2222-222222222222',  NOW() - INTERVAL '15 days', false);

-- Webhook logs (recent events)
INSERT INTO "AppOperatorWebhookLogs" ("Id", "OperatorId", "EventType", "PayloadJson", "HttpStatusCode", "Success", "ErrorMessage", "AttemptCount", "CreationTime")
VALUES
-- EVN: successful session events
('bd000001-0001-0001-0001-000000000001', 'bb000001-0001-0001-0001-000000000001', 0, '{"sessionId":"11111111-0001-0001-0001-000000000001","stationCode":"KC-HN-001","connectorNumber":1,"startTime":"2026-03-10T10:00:00Z"}', 200, true, NULL, 1, NOW() - INTERVAL '2 days'),
('bd000001-0001-0001-0001-000000000002', 'bb000001-0001-0001-0001-000000000001', 1, '{"sessionId":"11111111-0001-0001-0001-000000000001","stationCode":"KC-HN-001","totalEnergyKwh":35.5,"totalCost":156200}', 200, true, NULL, 1, NOW() - INTERVAL '2 days' + INTERVAL '90 minutes'),
('bd000001-0001-0001-0001-000000000003', 'bb000001-0001-0001-0001-000000000001', 2, '{"stationCode":"KC-HN-003","connectorNumber":1,"faultType":"ReaderFailure","timestamp":"2026-03-11T15:00:00Z"}', 200, true, NULL, 1, NOW() - INTERVAL '1 day'),
('bd000001-0001-0001-0001-000000000004', 'bb000001-0001-0001-0001-000000000001', 4, '{"stationCode":"KC-HN-001","connectorNumber":2,"oldStatus":"Available","newStatus":"Charging"}', 200, true, NULL, 1, NOW() - INTERVAL '15 minutes'),
-- EVN: one failed webhook (timeout)
('bd000001-0001-0001-0001-000000000005', 'bb000001-0001-0001-0001-000000000001', 3, '{"stationCode":"KC-HN-002","lastHeartbeat":"2026-03-12T08:00:00Z"}', NULL, false, 'HttpRequestException: Connection timed out', 3, NOW() - INTERVAL '4 hours'),
-- Grab: successful events
('bd000001-0001-0001-0001-000000000006', 'bb000001-0001-0001-0001-000000000002', 0, '{"sessionId":"11111111-0001-0001-0001-000000000008","stationCode":"KC-HCM-001","connectorNumber":2,"startTime":"2026-03-12T04:00:00Z"}', 200, true, NULL, 1, NOW() - INTERVAL '6 hours'),
('bd000001-0001-0001-0001-000000000007', 'bb000001-0001-0001-0001-000000000002', 1, '{"sessionId":"11111111-0001-0001-0001-000000000008","stationCode":"KC-HCM-001","totalEnergyKwh":25.0,"totalCost":110000}', 200, true, NULL, 1, NOW() - INTERVAL '5 hours'),
('bd000001-0001-0001-0001-000000000008', 'bb000001-0001-0001-0001-000000000002', 4, '{"stationCode":"KC-HCM-002","connectorNumber":1,"oldStatus":"Charging","newStatus":"Available"}', 200, true, NULL, 1, NOW() - INTERVAL '30 minutes');

-- ============================================================
-- 12. FLEETS (2 fleets with vehicles, schedules, and allowed stations)
-- ============================================================
DELETE FROM "AppFleetAllowedStations" WHERE "FleetId"::text LIKE 'cc000001-%';
DELETE FROM "AppFleetChargingSchedules" WHERE "FleetId"::text LIKE 'cc000001-%';
DELETE FROM "AppFleetVehicles" WHERE "FleetId"::text LIKE 'cc000001-%';
DELETE FROM "AppFleets" WHERE "Id"::text LIKE 'cc000001-%';

INSERT INTO "AppFleets" ("Id", "Name", "OperatorUserId", "Description", "MaxMonthlyBudgetVnd", "CurrentMonthSpentVnd", "ChargingPolicy", "IsActive", "BudgetAlertThresholdPercent", "ExtraProperties", "ConcurrencyStamp", "CreationTime", "IsDeleted")
VALUES
-- ABC Corp fleet: DailyEnergyLimit policy, 50M VND budget, 35% spent this month
('cc000001-0001-0001-0001-000000000001', 'ABC Corp - Đội xe doanh nghiệp', 'e1111111-1111-1111-1111-111111111118', 'Đội xe điện công ty ABC Corp. 3 xe VinFast phục vụ nhân viên đi công tác khu vực Hà Nội và TP.HCM.', 50000000.00, 17500000.00, 3, 80, true, '{}', 'seed-fl001', NOW() - INTERVAL '20 days', false),
-- Grab EV fleet: ApprovedStationsOnly policy, 200M VND budget, 62% spent
('cc000001-0001-0001-0001-000000000002', 'Grab Vietnam - Đội xe điện', 'e1111111-1111-1111-1111-111111111119', 'Đội xe điện Grab phục vụ dịch vụ đặt xe tại TP.HCM. Chỉ sạc tại các trạm được phê duyệt.', 200000000.00, 124000000.00, 2, 75, true, '{}', 'seed-fl002', NOW() - INTERVAL '15 days', false);

-- Fleet vehicles
INSERT INTO "AppFleetVehicles" ("Id", "FleetId", "VehicleId", "DriverUserId", "DailyChargingLimitKwh", "CurrentDayEnergyKwh", "CurrentMonthEnergyKwh", "IsActive", "CreationTime", "IsDeleted")
VALUES
-- ABC Corp vehicles (3 VinFasts, user e1111111-...-111118 owns them)
('cd000001-0001-0001-0001-000000000001', 'cc000001-0001-0001-0001-000000000001', 'f1111111-1111-1111-1111-111111111110', 'e1111111-1111-1111-1111-111111111118', 40.0, 12.5, 285.0, true,  NOW() - INTERVAL '20 days', false),
('cd000001-0001-0001-0001-000000000002', 'cc000001-0001-0001-0001-000000000001', 'f1111111-1111-1111-1111-111111111111', 'e1111111-1111-1111-1111-111111111118', 40.0, 0.0, 220.0, true,  NOW() - INTERVAL '20 days', false),
('cd000001-0001-0001-0001-000000000003', 'cc000001-0001-0001-0001-000000000001', 'f1111111-1111-1111-1111-111111111112', NULL, 80.0, 35.2, 410.0, true,  NOW() - INTERVAL '18 days', false),
-- Grab EV vehicles (2 BYD e6s, user e1111111-...-111119 owns them)
('cd000001-0001-0001-0001-000000000004', 'cc000001-0001-0001-0001-000000000002', 'f1111111-1111-1111-1111-111111111113', 'e1111111-1111-1111-1111-111111111119', 60.0, 28.3, 520.0, true,  NOW() - INTERVAL '15 days', false),
('cd000001-0001-0001-0001-000000000005', 'cc000001-0001-0001-0001-000000000002', 'f1111111-1111-1111-1111-111111111114', 'e1111111-1111-1111-1111-111111111119', 60.0, 45.1, 630.0, true,  NOW() - INTERVAL '15 days', false);

-- Charging schedules for ABC Corp (Mon-Fri 6AM-10PM UTC+7 = 23:00-15:00 UTC)
INSERT INTO "AppFleetChargingSchedules" ("Id", "FleetId", "DayOfWeek", "StartTimeUtc", "EndTimeUtc", "CreationTime")
VALUES
('ce000001-0001-0001-0001-000000000001', 'cc000001-0001-0001-0001-000000000001', 1, '23:00:00', '15:00:00', NOW() - INTERVAL '20 days'),
('ce000001-0001-0001-0001-000000000002', 'cc000001-0001-0001-0001-000000000001', 2, '23:00:00', '15:00:00', NOW() - INTERVAL '20 days'),
('ce000001-0001-0001-0001-000000000003', 'cc000001-0001-0001-0001-000000000001', 3, '23:00:00', '15:00:00', NOW() - INTERVAL '20 days'),
('ce000001-0001-0001-0001-000000000004', 'cc000001-0001-0001-0001-000000000001', 4, '23:00:00', '15:00:00', NOW() - INTERVAL '20 days'),
('ce000001-0001-0001-0001-000000000005', 'cc000001-0001-0001-0001-000000000001', 5, '23:00:00', '15:00:00', NOW() - INTERVAL '20 days');

-- Grab fleet: 24/7 charging allowed (Mon-Sun)
INSERT INTO "AppFleetChargingSchedules" ("Id", "FleetId", "DayOfWeek", "StartTimeUtc", "EndTimeUtc", "CreationTime")
VALUES
('ce000001-0001-0001-0001-000000000006', 'cc000001-0001-0001-0001-000000000002', 0, '00:00:00', '23:59:59', NOW() - INTERVAL '15 days'),
('ce000001-0001-0001-0001-000000000007', 'cc000001-0001-0001-0001-000000000002', 1, '00:00:00', '23:59:59', NOW() - INTERVAL '15 days'),
('ce000001-0001-0001-0001-000000000008', 'cc000001-0001-0001-0001-000000000002', 2, '00:00:00', '23:59:59', NOW() - INTERVAL '15 days'),
('ce000001-0001-0001-0001-000000000009', 'cc000001-0001-0001-0001-000000000002', 3, '00:00:00', '23:59:59', NOW() - INTERVAL '15 days'),
('ce000001-0001-0001-0001-000000000010', 'cc000001-0001-0001-0001-000000000002', 4, '00:00:00', '23:59:59', NOW() - INTERVAL '15 days'),
('ce000001-0001-0001-0001-000000000011', 'cc000001-0001-0001-0001-000000000002', 5, '00:00:00', '23:59:59', NOW() - INTERVAL '15 days'),
('ce000001-0001-0001-0001-000000000012', 'cc000001-0001-0001-0001-000000000002', 6, '00:00:00', '23:59:59', NOW() - INTERVAL '15 days');

-- Allowed station groups: Grab → HCM only
INSERT INTO "AppFleetAllowedStations" ("Id", "FleetId", "StationGroupId", "CreationTime", "IsDeleted")
VALUES
('cf000001-0001-0001-0001-000000000001', 'cc000001-0001-0001-0001-000000000002', 'a0000001-0001-0001-0001-000000000002',  NOW() - INTERVAL '15 days', false);

COMMIT;

-- ============================================================
-- VERIFICATION (runs outside transaction so it reads committed data)
-- ============================================================
SELECT 'Seed Data Summary' AS info;
SELECT '=================' AS separator;
SELECT 'ABP Roles' AS entity, COUNT(*) AS count FROM "AbpRoles" WHERE "Name" IN ('admin', 'operator', 'viewer');
SELECT 'ABP Admin Users' AS entity, COUNT(*) AS count FROM "AbpUsers" WHERE "UserName" IN ('admin', 'operator', 'viewer') AND "IsDeleted" = false;
SELECT 'Permission Grants' AS entity, COUNT(*) AS count FROM "AbpPermissionGrants" WHERE "ProviderKey" IN ('admin', 'operator', 'viewer');
SELECT 'Station Groups' AS entity, COUNT(*) AS count FROM "AppStationGroups" WHERE "IsDeleted" = false;
SELECT 'Tariff Plans' AS entity, COUNT(*) AS count FROM "AppTariffPlans" WHERE "IsDeleted" = false;
SELECT 'Charging Stations' AS entity, COUNT(*) AS count FROM "AppChargingStations" WHERE "IsDeleted" = false;
SELECT 'Connectors' AS entity, COUNT(*) AS count FROM "AppConnectors" WHERE "IsDeleted" = false;
SELECT 'App Users' AS entity, COUNT(*) AS count FROM "AppAppUsers" WHERE "IsDeleted" = false;
SELECT 'Vehicles' AS entity, COUNT(*) AS count FROM "AppVehicles" WHERE "IsDeleted" = false;
SELECT 'Charging Sessions' AS entity, COUNT(*) AS count FROM "AppChargingSessions" WHERE "IsDeleted" = false;
SELECT 'Meter Values' AS entity, COUNT(*) AS count FROM "AppMeterValues";
SELECT 'Payment Transactions' AS entity, COUNT(*) AS count FROM "AppPaymentTransactions" WHERE "IsDeleted" = false;
SELECT 'Invoices' AS entity, COUNT(*) AS count FROM "AppInvoices" WHERE "IsDeleted" = false;
SELECT 'Faults' AS entity, COUNT(*) AS count FROM "AppFaults" WHERE "IsDeleted" = false;
SELECT 'Status Change Logs' AS entity, COUNT(*) AS count FROM "AppStatusChangeLogs";
SELECT 'Notifications' AS entity, COUNT(*) AS count FROM "AppNotifications";
SELECT 'Alerts' AS entity, COUNT(*) AS count FROM "AppAlerts";
SELECT 'Wallet Transactions' AS entity, COUNT(*) AS count FROM "AppWalletTransactions";
SELECT 'Device Tokens' AS entity, COUNT(*) AS count FROM "AppDeviceTokens";
SELECT 'Notification Prefs' AS entity, COUNT(*) AS count FROM "AppNotificationPreferences";
SELECT 'Favorite Stations' AS entity, COUNT(*) AS count FROM "AppFavoriteStations";
SELECT 'Station Amenities' AS entity, COUNT(*) AS count FROM "AppStationAmenities";
SELECT 'Station Photos' AS entity, COUNT(*) AS count FROM "AppStationPhotos";
SELECT 'Vouchers' AS entity, COUNT(*) AS count FROM "AppVouchers" WHERE "IsDeleted" = false;
SELECT 'User Vouchers' AS entity, COUNT(*) AS count FROM "AppUserVouchers";
SELECT 'Promotions' AS entity, COUNT(*) AS count FROM "AppPromotions" WHERE "IsDeleted" = false;
SELECT 'User Feedbacks' AS entity, COUNT(*) AS count FROM "AppUserFeedbacks" WHERE "IsDeleted" = false;
SELECT 'E-Invoices' AS entity, COUNT(*) AS count FROM "AppEInvoices" WHERE "IsDeleted" = false;
SELECT 'Power Sharing Groups' AS entity, COUNT(*) AS count FROM "AppPowerSharingGroups" WHERE "IsDeleted" = false;
SELECT 'PS Group Members' AS entity, COUNT(*) AS count FROM "AppPowerSharingGroupMembers" WHERE "IsDeleted" = false;
SELECT 'Site Load Profiles' AS entity, COUNT(*) AS count FROM "AppSiteLoadProfiles";
SELECT 'Operators' AS entity, COUNT(*) AS count FROM "AppOperators" WHERE "IsDeleted" = false;
SELECT 'Operator Stations' AS entity, COUNT(*) AS count FROM "AppOperatorStations" WHERE "IsDeleted" = false;
SELECT 'Operator Webhooks' AS entity, COUNT(*) AS count FROM "AppOperatorWebhookLogs";
SELECT 'Fleets' AS entity, COUNT(*) AS count FROM "AppFleets" WHERE "IsDeleted" = false;
SELECT 'Fleet Vehicles' AS entity, COUNT(*) AS count FROM "AppFleetVehicles" WHERE "IsDeleted" = false;
SELECT 'Fleet Schedules' AS entity, COUNT(*) AS count FROM "AppFleetChargingSchedules";
SELECT 'Fleet Allowed Stations' AS entity, COUNT(*) AS count FROM "AppFleetAllowedStations" WHERE "IsDeleted" = false;
SELECT '' AS separator;
SELECT 'Total Revenue (VND)' AS metric, TO_CHAR(SUM("TotalCost"), 'FM999,999,999') AS value FROM "AppChargingSessions" WHERE "Status" = 5 AND "IsDeleted" = false;
SELECT 'Total Energy (kWh)' AS metric, ROUND(SUM("TotalEnergyKwh")::numeric, 2) AS value FROM "AppChargingSessions" WHERE "Status" = 5 AND "IsDeleted" = false;
SELECT '' AS separator;
SELECT '=== Admin User Credentials ===' AS info;
SELECT 'admin@klc.vn / Admin@123 (Full access)' AS credentials;
SELECT 'operator@klc.vn / Admin@123 (Station management)' AS credentials;
SELECT 'viewer@klc.vn / Admin@123 (Read-only)' AS credentials;
