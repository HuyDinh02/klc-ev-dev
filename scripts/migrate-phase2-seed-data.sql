-- ============================================================
-- Phase 2 Demo Seed Data Migration
-- ============================================================
-- Run: PGPASSWORD=<pass> psql -h <host> -p 5432 -U <user> -d KLC -f scripts/migrate-phase2-seed-data.sql
-- Idempotent: deletes existing demo data before inserting.
-- ============================================================

BEGIN;

-- ============================================================
-- 1. POWER SHARING GROUPS
-- ============================================================
DELETE FROM "AppSiteLoadProfiles" WHERE "PowerSharingGroupId"::text LIKE 'aa000001-%';
DELETE FROM "AppPowerSharingGroupMembers" WHERE "PowerSharingGroupId"::text LIKE 'aa000001-%';
DELETE FROM "AppPowerSharingGroups" WHERE "Id"::text LIKE 'aa000001-%';

-- Group 1: LINK mode, Dynamic strategy, 120kW capacity (Hanoi)
-- Group 2: LOOP mode, Average strategy, 200kW capacity (HCM)
INSERT INTO "AppPowerSharingGroups" ("Id", "Name", "MaxCapacityKw", "Mode", "DistributionStrategy", "IsActive", "MinPowerPerConnectorKw", "StationGroupId", "ExtraProperties", "ConcurrencyStamp", "CreationTime", "IsDeleted")
VALUES
('aa000001-0001-0001-0001-000000000001', 'Times City - Chia sẻ công suất', 120.00, 0, 2, true, 7.00, 'a0000001-0001-0001-0001-000000000001', '{}', 'seed-psg001', NOW() - INTERVAL '30 days', false),
('aa000001-0001-0001-0001-000000000002', 'HCM Cluster - Cân bằng tải', 200.00, 1, 0, true, 5.00, 'a0000001-0001-0001-0001-000000000002', '{}', 'seed-psg002', NOW() - INTERVAL '25 days', false);

-- Members: Times City group (KC-HN-001 + KC-HN-002 = 4 connectors)
INSERT INTO "AppPowerSharingGroupMembers" ("Id", "PowerSharingGroupId", "StationId", "ConnectorId", "Priority", "AllocatedPowerKw", "CreationTime", "IsDeleted")
VALUES
('ab000001-0001-0001-0001-000000000001', 'aa000001-0001-0001-0001-000000000001', 'b1111111-1111-1111-1111-111111111111', 'c1000000-0001-0001-0001-000000000001', 1, 40.00, NOW() - INTERVAL '30 days', false),
('ab000001-0001-0001-0001-000000000002', 'aa000001-0001-0001-0001-000000000001', 'b1111111-1111-1111-1111-111111111111', 'c1000000-0001-0001-0001-000000000002', 2, 20.00, NOW() - INTERVAL '30 days', false),
('ab000001-0001-0001-0001-000000000003', 'aa000001-0001-0001-0001-000000000001', 'b1111111-1111-1111-1111-111111111112', 'c1000000-0001-0001-0001-000000000003', 1, 35.00, NOW() - INTERVAL '30 days', false),
('ab000001-0001-0001-0001-000000000004', 'aa000001-0001-0001-0001-000000000001', 'b1111111-1111-1111-1111-111111111112', 'c1000000-0001-0001-0001-000000000004', 3, 20.00, NOW() - INTERVAL '30 days', false);

-- Members: HCM Cluster group (KC-HCM-001 + KC-HCM-002 = 5 connectors)
INSERT INTO "AppPowerSharingGroupMembers" ("Id", "PowerSharingGroupId", "StationId", "ConnectorId", "Priority", "AllocatedPowerKw", "CreationTime", "IsDeleted")
VALUES
('ab000001-0001-0001-0001-000000000005', 'aa000001-0001-0001-0001-000000000002', 'b2222222-2222-2222-2222-222222222221', 'c1000000-0001-0001-0001-000000000008', 1, 50.00, NOW() - INTERVAL '25 days', false),
('ab000001-0001-0001-0001-000000000006', 'aa000001-0001-0001-0001-000000000002', 'b2222222-2222-2222-2222-222222222221', 'c1000000-0001-0001-0001-000000000009', 2, 22.00, NOW() - INTERVAL '25 days', false),
('ab000001-0001-0001-0001-000000000007', 'aa000001-0001-0001-0001-000000000002', 'b2222222-2222-2222-2222-222222222222', 'c1000000-0001-0001-0001-000000000010', 1, 50.00, NOW() - INTERVAL '25 days', false),
('ab000001-0001-0001-0001-000000000008', 'aa000001-0001-0001-0001-000000000002', 'b2222222-2222-2222-2222-222222222222', 'c1000000-0001-0001-0001-000000000011', 2, 22.00, NOW() - INTERVAL '25 days', false),
('ab000001-0001-0001-0001-000000000009', 'aa000001-0001-0001-0001-000000000002', 'b2222222-2222-2222-2222-222222222222', 'c1000000-0001-0001-0001-000000000012', 3, 50.00, NOW() - INTERVAL '25 days', false);

-- Site load profiles: hourly snapshots (last 6 hours)
INSERT INTO "AppSiteLoadProfiles" ("Id", "PowerSharingGroupId", "Timestamp", "TotalLoadKw", "AvailableCapacityKw", "ActiveSessionCount", "TotalConnectorCount", "PeakLoadKw", "CreationTime")
VALUES
('ac000001-0001-0001-0001-000000000001', 'aa000001-0001-0001-0001-000000000001', NOW() - INTERVAL '6 hours', 45.50, 74.50, 1, 4, 95.00, NOW() - INTERVAL '6 hours'),
('ac000001-0001-0001-0001-000000000002', 'aa000001-0001-0001-0001-000000000001', NOW() - INTERVAL '5 hours', 82.30, 37.70, 2, 4, 95.00, NOW() - INTERVAL '5 hours'),
('ac000001-0001-0001-0001-000000000003', 'aa000001-0001-0001-0001-000000000001', NOW() - INTERVAL '4 hours', 95.00, 25.00, 3, 4, 95.00, NOW() - INTERVAL '4 hours'),
('ac000001-0001-0001-0001-000000000004', 'aa000001-0001-0001-0001-000000000001', NOW() - INTERVAL '3 hours', 60.20, 59.80, 2, 4, 95.00, NOW() - INTERVAL '3 hours'),
('ac000001-0001-0001-0001-000000000005', 'aa000001-0001-0001-0001-000000000001', NOW() - INTERVAL '2 hours', 40.00, 80.00, 1, 4, 95.00, NOW() - INTERVAL '2 hours'),
('ac000001-0001-0001-0001-000000000006', 'aa000001-0001-0001-0001-000000000001', NOW() - INTERVAL '1 hour', 75.80, 44.20, 2, 4, 95.00, NOW() - INTERVAL '1 hour'),
('ac000001-0001-0001-0001-000000000007', 'aa000001-0001-0001-0001-000000000002', NOW() - INTERVAL '6 hours', 110.00, 90.00, 3, 5, 165.00, NOW() - INTERVAL '6 hours'),
('ac000001-0001-0001-0001-000000000008', 'aa000001-0001-0001-0001-000000000002', NOW() - INTERVAL '5 hours', 145.50, 54.50, 4, 5, 165.00, NOW() - INTERVAL '5 hours'),
('ac000001-0001-0001-0001-000000000009', 'aa000001-0001-0001-0001-000000000002', NOW() - INTERVAL '4 hours', 165.00, 35.00, 5, 5, 165.00, NOW() - INTERVAL '4 hours'),
('ac000001-0001-0001-0001-000000000010', 'aa000001-0001-0001-0001-000000000002', NOW() - INTERVAL '3 hours', 130.20, 69.80, 3, 5, 165.00, NOW() - INTERVAL '3 hours'),
('ac000001-0001-0001-0001-000000000011', 'aa000001-0001-0001-0001-000000000002', NOW() - INTERVAL '2 hours', 88.00, 112.00, 2, 5, 165.00, NOW() - INTERVAL '2 hours'),
('ac000001-0001-0001-0001-000000000012', 'aa000001-0001-0001-0001-000000000002', NOW() - INTERVAL '1 hour', 155.30, 44.70, 4, 5, 165.00, NOW() - INTERVAL '1 hour');

-- ============================================================
-- 2. OPERATORS (B2B)
-- ============================================================
DELETE FROM "AppOperatorWebhookLogs" WHERE "OperatorId"::text LIKE 'bb000001-%';
DELETE FROM "AppOperatorStations" WHERE "OperatorId"::text LIKE 'bb000001-%';
DELETE FROM "AppOperators" WHERE "Id"::text LIKE 'bb000001-%';

INSERT INTO "AppOperators" ("Id", "Name", "ApiKeyHash", "ContactEmail", "WebhookUrl", "Description", "IsActive", "RateLimitPerMinute", "ExtraProperties", "ConcurrencyStamp", "CreationTime", "IsDeleted")
VALUES
('bb000001-0001-0001-0001-000000000001', 'EVN Smart Charging', 'a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2', 'api@evn-smart.vn', 'https://api.evn-smart.vn/webhooks/klc', 'Đối tác EVN - Quản lý trạm sạc khu vực phía Bắc', true, 500, '{}', 'seed-op001', NOW() - INTERVAL '20 days', false),
('bb000001-0001-0001-0001-000000000002', 'Grab EV Services', 'b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3', 'ev-api@grab.vn', 'https://ev-services.grab.vn/hooks/charging', 'Grab Vietnam - Đội xe điện TP.HCM', true, 1000, '{}', 'seed-op002', NOW() - INTERVAL '15 days', false);

-- EVN → Hanoi stations, Grab → HCM stations
INSERT INTO "AppOperatorStations" ("Id", "OperatorId", "StationId", "CreationTime", "IsDeleted")
VALUES
('bc000001-0001-0001-0001-000000000001', 'bb000001-0001-0001-0001-000000000001', 'b1111111-1111-1111-1111-111111111111', NOW() - INTERVAL '20 days', false),
('bc000001-0001-0001-0001-000000000002', 'bb000001-0001-0001-0001-000000000001', 'b1111111-1111-1111-1111-111111111112', NOW() - INTERVAL '20 days', false),
('bc000001-0001-0001-0001-000000000003', 'bb000001-0001-0001-0001-000000000001', 'b1111111-1111-1111-1111-111111111113', NOW() - INTERVAL '20 days', false),
('bc000001-0001-0001-0001-000000000004', 'bb000001-0001-0001-0001-000000000001', 'b1111111-1111-1111-1111-111111111114', NOW() - INTERVAL '20 days', false),
('bc000001-0001-0001-0001-000000000005', 'bb000001-0001-0001-0001-000000000002', 'b2222222-2222-2222-2222-222222222221', NOW() - INTERVAL '15 days', false),
('bc000001-0001-0001-0001-000000000006', 'bb000001-0001-0001-0001-000000000002', 'b2222222-2222-2222-2222-222222222222', NOW() - INTERVAL '15 days', false);

-- Webhook logs
INSERT INTO "AppOperatorWebhookLogs" ("Id", "OperatorId", "EventType", "PayloadJson", "HttpStatusCode", "Success", "ErrorMessage", "AttemptCount", "CreationTime")
VALUES
('bd000001-0001-0001-0001-000000000001', 'bb000001-0001-0001-0001-000000000001', 0, '{"sessionId":"11111111-0001-0001-0001-000000000001","stationCode":"KC-HN-001","connectorNumber":1}', 200, true, NULL, 1, NOW() - INTERVAL '2 days'),
('bd000001-0001-0001-0001-000000000002', 'bb000001-0001-0001-0001-000000000001', 1, '{"sessionId":"11111111-0001-0001-0001-000000000001","stationCode":"KC-HN-001","totalEnergyKwh":35.5}', 200, true, NULL, 1, NOW() - INTERVAL '2 days' + INTERVAL '90 minutes'),
('bd000001-0001-0001-0001-000000000003', 'bb000001-0001-0001-0001-000000000001', 2, '{"stationCode":"KC-HN-003","faultType":"ReaderFailure"}', 200, true, NULL, 1, NOW() - INTERVAL '1 day'),
('bd000001-0001-0001-0001-000000000004', 'bb000001-0001-0001-0001-000000000001', 4, '{"stationCode":"KC-HN-001","connectorNumber":2,"oldStatus":"Available","newStatus":"Charging"}', 200, true, NULL, 1, NOW() - INTERVAL '15 minutes'),
('bd000001-0001-0001-0001-000000000005', 'bb000001-0001-0001-0001-000000000001', 3, '{"stationCode":"KC-HN-002","lastHeartbeat":"2026-03-12T08:00:00Z"}', NULL, false, 'HttpRequestException: Connection timed out', 3, NOW() - INTERVAL '4 hours'),
('bd000001-0001-0001-0001-000000000006', 'bb000001-0001-0001-0001-000000000002', 0, '{"sessionId":"11111111-0001-0001-0001-000000000008","stationCode":"KC-HCM-001"}', 200, true, NULL, 1, NOW() - INTERVAL '6 hours'),
('bd000001-0001-0001-0001-000000000007', 'bb000001-0001-0001-0001-000000000002', 1, '{"sessionId":"11111111-0001-0001-0001-000000000008","totalEnergyKwh":25.0}', 200, true, NULL, 1, NOW() - INTERVAL '5 hours'),
('bd000001-0001-0001-0001-000000000008', 'bb000001-0001-0001-0001-000000000002', 4, '{"stationCode":"KC-HCM-002","connectorNumber":1,"oldStatus":"Charging","newStatus":"Available"}', 200, true, NULL, 1, NOW() - INTERVAL '30 minutes');

-- ============================================================
-- 3. FLEETS
-- ============================================================
DELETE FROM "AppFleetAllowedStations" WHERE "FleetId"::text LIKE 'cc000001-%';
DELETE FROM "AppFleetChargingSchedules" WHERE "FleetId"::text LIKE 'cc000001-%';
DELETE FROM "AppFleetVehicles" WHERE "FleetId"::text LIKE 'cc000001-%';
DELETE FROM "AppFleets" WHERE "Id"::text LIKE 'cc000001-%';

-- Fleet 1: ABC Corp (DailyEnergyLimit=3, 50M budget, 35% spent)
-- Fleet 2: Grab Vietnam (ApprovedStationsOnly=2, 200M budget, 62% spent)
INSERT INTO "AppFleets" ("Id", "Name", "OperatorUserId", "Description", "MaxMonthlyBudgetVnd", "CurrentMonthSpentVnd", "ChargingPolicy", "IsActive", "BudgetAlertThresholdPercent", "ExtraProperties", "ConcurrencyStamp", "CreationTime", "IsDeleted")
VALUES
('cc000001-0001-0001-0001-000000000001', 'ABC Corp - Đội xe doanh nghiệp', 'e1111111-1111-1111-1111-111111111118', 'Đội xe điện công ty ABC Corp. 3 xe VinFast phục vụ nhân viên.', 50000000.00, 17500000.00, 3, true, 80, '{}', 'seed-fl001', NOW() - INTERVAL '20 days', false),
('cc000001-0001-0001-0001-000000000002', 'Grab Vietnam - Đội xe điện', 'e1111111-1111-1111-1111-111111111119', 'Đội xe điện Grab tại TP.HCM. Chỉ sạc tại trạm được phê duyệt.', 200000000.00, 124000000.00, 2, true, 75, '{}', 'seed-fl002', NOW() - INTERVAL '15 days', false);

-- Fleet vehicles (referencing existing seeded vehicles)
INSERT INTO "AppFleetVehicles" ("Id", "FleetId", "VehicleId", "DriverUserId", "DailyChargingLimitKwh", "CurrentDayEnergyKwh", "CurrentMonthEnergyKwh", "IsActive", "CreationTime", "IsDeleted")
VALUES
-- ABC Corp: 3 VinFasts (user e...118 owns them)
('cd000001-0001-0001-0001-000000000001', 'cc000001-0001-0001-0001-000000000001', 'f1111111-1111-1111-1111-111111111110', 'e1111111-1111-1111-1111-111111111118', 40.0, 12.5, 285.0, true, NOW() - INTERVAL '20 days', false),
('cd000001-0001-0001-0001-000000000002', 'cc000001-0001-0001-0001-000000000001', 'f1111111-1111-1111-1111-111111111111', 'e1111111-1111-1111-1111-111111111118', 40.0, 0.0, 220.0, true, NOW() - INTERVAL '20 days', false),
('cd000001-0001-0001-0001-000000000003', 'cc000001-0001-0001-0001-000000000001', 'f1111111-1111-1111-1111-111111111112', NULL, 80.0, 35.2, 410.0, true, NOW() - INTERVAL '18 days', false),
-- Grab: 2 BYD e6 (user e...119 owns them)
('cd000001-0001-0001-0001-000000000004', 'cc000001-0001-0001-0001-000000000002', 'f1111111-1111-1111-1111-111111111113', 'e1111111-1111-1111-1111-111111111119', 60.0, 28.3, 520.0, true, NOW() - INTERVAL '15 days', false),
('cd000001-0001-0001-0001-000000000005', 'cc000001-0001-0001-0001-000000000002', 'f1111111-1111-1111-1111-111111111114', 'e1111111-1111-1111-1111-111111111119', 60.0, 45.1, 630.0, true, NOW() - INTERVAL '15 days', false);

-- Charging schedules: ABC Corp Mon-Fri 6AM-10PM (UTC+7 → 23:00-15:00 UTC)
INSERT INTO "AppFleetChargingSchedules" ("Id", "FleetId", "DayOfWeek", "StartTimeUtc", "EndTimeUtc", "CreationTime")
VALUES
('ce000001-0001-0001-0001-000000000001', 'cc000001-0001-0001-0001-000000000001', 1, '23:00:00', '15:00:00', NOW() - INTERVAL '20 days'),
('ce000001-0001-0001-0001-000000000002', 'cc000001-0001-0001-0001-000000000001', 2, '23:00:00', '15:00:00', NOW() - INTERVAL '20 days'),
('ce000001-0001-0001-0001-000000000003', 'cc000001-0001-0001-0001-000000000001', 3, '23:00:00', '15:00:00', NOW() - INTERVAL '20 days'),
('ce000001-0001-0001-0001-000000000004', 'cc000001-0001-0001-0001-000000000001', 4, '23:00:00', '15:00:00', NOW() - INTERVAL '20 days'),
('ce000001-0001-0001-0001-000000000005', 'cc000001-0001-0001-0001-000000000001', 5, '23:00:00', '15:00:00', NOW() - INTERVAL '20 days'),
-- Grab: 24/7 (all 7 days)
('ce000001-0001-0001-0001-000000000006', 'cc000001-0001-0001-0001-000000000002', 0, '00:00:00', '23:59:59', NOW() - INTERVAL '15 days'),
('ce000001-0001-0001-0001-000000000007', 'cc000001-0001-0001-0001-000000000002', 1, '00:00:00', '23:59:59', NOW() - INTERVAL '15 days'),
('ce000001-0001-0001-0001-000000000008', 'cc000001-0001-0001-0001-000000000002', 2, '00:00:00', '23:59:59', NOW() - INTERVAL '15 days'),
('ce000001-0001-0001-0001-000000000009', 'cc000001-0001-0001-0001-000000000002', 3, '00:00:00', '23:59:59', NOW() - INTERVAL '15 days'),
('ce000001-0001-0001-0001-000000000010', 'cc000001-0001-0001-0001-000000000002', 4, '00:00:00', '23:59:59', NOW() - INTERVAL '15 days'),
('ce000001-0001-0001-0001-000000000011', 'cc000001-0001-0001-0001-000000000002', 5, '00:00:00', '23:59:59', NOW() - INTERVAL '15 days'),
('ce000001-0001-0001-0001-000000000012', 'cc000001-0001-0001-0001-000000000002', 6, '00:00:00', '23:59:59', NOW() - INTERVAL '15 days');

-- Allowed station groups: Grab → HCM region only
INSERT INTO "AppFleetAllowedStations" ("Id", "FleetId", "StationGroupId", "CreationTime", "IsDeleted")
VALUES
('cf000001-0001-0001-0001-000000000001', 'cc000001-0001-0001-0001-000000000002', 'a0000001-0001-0001-0001-000000000002', NOW() - INTERVAL '15 days', false);

COMMIT;

-- Verify
SELECT 'Power Sharing Groups' AS entity, COUNT(*) AS count FROM "AppPowerSharingGroups" WHERE "IsDeleted" = false;
SELECT 'PS Members' AS entity, COUNT(*) AS count FROM "AppPowerSharingGroupMembers" WHERE "IsDeleted" = false;
SELECT 'Site Load Profiles' AS entity, COUNT(*) AS count FROM "AppSiteLoadProfiles";
SELECT 'Operators' AS entity, COUNT(*) AS count FROM "AppOperators" WHERE "IsDeleted" = false;
SELECT 'Operator Stations' AS entity, COUNT(*) AS count FROM "AppOperatorStations" WHERE "IsDeleted" = false;
SELECT 'Webhook Logs' AS entity, COUNT(*) AS count FROM "AppOperatorWebhookLogs";
SELECT 'Fleets' AS entity, COUNT(*) AS count FROM "AppFleets" WHERE "IsDeleted" = false;
SELECT 'Fleet Vehicles' AS entity, COUNT(*) AS count FROM "AppFleetVehicles" WHERE "IsDeleted" = false;
SELECT 'Fleet Schedules' AS entity, COUNT(*) AS count FROM "AppFleetChargingSchedules";
SELECT 'Fleet Allowed Stations' AS entity, COUNT(*) AS count FROM "AppFleetAllowedStations" WHERE "IsDeleted" = false;
