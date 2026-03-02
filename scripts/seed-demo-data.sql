-- ============================================================
-- KLC EV Charging CSMS - Demo Seed Data (With Auth)
-- ============================================================
-- Run with: PGPASSWORD=postgres psql -h localhost -p 5433 -U postgres -d KLC -f scripts/seed-demo-data.sql
-- ============================================================

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
(gen_random_uuid(), NULL, 'KLC.RoleManagement.ManagePermissions', 'R', 'admin');

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
(gen_random_uuid(), NULL, 'KLC.Payments.ViewAll', 'R', 'operator');

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
(gen_random_uuid(), NULL, 'KLC.Payments', 'R', 'viewer');

-- ============================================================
-- 1. TARIFF PLANS (3 plans)
-- ============================================================
DELETE FROM "AppTariffPlans" WHERE "Name" IN ('Standard', 'Peak Hours', 'Off-Peak');

INSERT INTO "AppTariffPlans" ("Id", "Name", "Description", "BaseRatePerKwh", "TaxRatePercent", "EffectiveFrom", "EffectiveTo", "IsActive", "IsDefault", "ExtraProperties", "ConcurrencyStamp", "CreationTime", "IsDeleted")
VALUES
('d1111111-1111-1111-1111-111111111111', 'Standard', 'Giá tiêu chuẩn áp dụng 24/7', 4000.00, 10.00, '2026-01-01', NULL, true, true, '{}', 'seed-t001', NOW(), false),
('d2222222-2222-2222-2222-222222222222', 'Peak Hours', 'Giá giờ cao điểm (6h-9h, 17h-22h)', 5500.00, 10.00, '2026-01-01', NULL, true, false, '{}', 'seed-t002', NOW(), false),
('d3333333-3333-3333-3333-333333333333', 'Off-Peak', 'Giá giờ thấp điểm (22h-6h)', 2800.00, 10.00, '2026-01-01', NULL, true, false, '{}', 'seed-t003', NOW(), false);

-- Update stations with tariff plans
UPDATE "AppChargingStations" SET "TariffPlanId" = 'd1111111-1111-1111-1111-111111111111' WHERE "StationCode" LIKE 'KC-%';

-- ============================================================
-- 2. APP USERS (10 users)
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
DELETE FROM "AppMeterValues" WHERE "SessionId" IN (SELECT "Id" FROM "AppChargingSessions" WHERE "IdTag" LIKE 'KLC-%');
DELETE FROM "AppChargingSessions" WHERE "IdTag" LIKE 'KLC-%';

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
-- 5. PAYMENT TRANSACTIONS (20 payments)
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
-- VERIFICATION
-- ============================================================
SELECT 'Seed Data Summary' AS info;
SELECT '=================' AS separator;
SELECT 'ABP Roles' AS entity, COUNT(*) AS count FROM "AbpRoles" WHERE "Name" IN ('admin', 'operator', 'viewer');
SELECT 'ABP Admin Users' AS entity, COUNT(*) AS count FROM "AbpUsers" WHERE "UserName" IN ('admin', 'operator', 'viewer') AND "IsDeleted" = false;
SELECT 'Permission Grants' AS entity, COUNT(*) AS count FROM "AbpPermissionGrants" WHERE "ProviderKey" IN ('admin', 'operator', 'viewer');
SELECT 'Tariff Plans' AS entity, COUNT(*) AS count FROM "AppTariffPlans" WHERE "IsDeleted" = false;
SELECT 'Station Groups' AS entity, COUNT(*) AS count FROM "AppStationGroups" WHERE "IsDeleted" = false;
SELECT 'Charging Stations' AS entity, COUNT(*) AS count FROM "AppChargingStations" WHERE "IsDeleted" = false;
SELECT 'Connectors' AS entity, COUNT(*) AS count FROM "AppConnectors" WHERE "IsDeleted" = false;
SELECT 'App Users' AS entity, COUNT(*) AS count FROM "AppAppUsers" WHERE "IsDeleted" = false;
SELECT 'Vehicles' AS entity, COUNT(*) AS count FROM "AppVehicles" WHERE "IsDeleted" = false;
SELECT 'Charging Sessions' AS entity, COUNT(*) AS count FROM "AppChargingSessions" WHERE "IsDeleted" = false;
SELECT 'Payment Transactions' AS entity, COUNT(*) AS count FROM "AppPaymentTransactions" WHERE "IsDeleted" = false;
SELECT 'Invoices' AS entity, COUNT(*) AS count FROM "AppInvoices" WHERE "IsDeleted" = false;
SELECT 'Faults' AS entity, COUNT(*) AS count FROM "AppFaults" WHERE "IsDeleted" = false;
SELECT 'Notifications' AS entity, COUNT(*) AS count FROM "AppNotifications";
SELECT 'Alerts' AS entity, COUNT(*) AS count FROM "AppAlerts";
SELECT '' AS separator;
SELECT 'Total Revenue (VND)' AS metric, TO_CHAR(SUM("TotalCost"), 'FM999,999,999') AS value FROM "AppChargingSessions" WHERE "Status" = 5 AND "IsDeleted" = false;
SELECT 'Total Energy (kWh)' AS metric, ROUND(SUM("TotalEnergyKwh")::numeric, 2) AS value FROM "AppChargingSessions" WHERE "Status" = 5 AND "IsDeleted" = false;
SELECT '' AS separator;
SELECT '=== Admin User Credentials ===' AS info;
SELECT 'admin@klc.vn / Admin@123 (Full access)' AS credentials;
SELECT 'operator@klc.vn / Admin@123 (Station management)' AS credentials;
SELECT 'viewer@klc.vn / Admin@123 (Read-only)' AS credentials;
