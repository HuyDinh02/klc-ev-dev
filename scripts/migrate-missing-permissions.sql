-- ============================================================
-- Missing Permission Grants Migration
-- ============================================================
-- Run with: PGPASSWORD=<password> psql -h <host> -p 5432 -U postgres -d KLC -f scripts/migrate-missing-permissions.sql
-- This script is idempotent — safe to run multiple times.
--
-- Root cause: seed-demo-data.sql is not idempotent (no ON CONFLICT),
-- so permissions added after initial seed were never applied to production.
-- This script grants ALL defined KLC permissions to the admin role,
-- skipping any that already exist.
-- ============================================================

BEGIN;

-- ============================================================
-- ADMIN ROLE: Grant ALL KLC permissions
-- ============================================================
INSERT INTO "AbpPermissionGrants" ("Id", "TenantId", "Name", "ProviderName", "ProviderKey")
SELECT gen_random_uuid(), NULL, p.name, 'R', 'admin'
FROM (VALUES
  -- Stations
  ('KLC.Stations'), ('KLC.Stations.Create'), ('KLC.Stations.Update'), ('KLC.Stations.Delete'), ('KLC.Stations.Decommission'),
  -- Connectors
  ('KLC.Connectors'), ('KLC.Connectors.Create'), ('KLC.Connectors.Update'), ('KLC.Connectors.Delete'), ('KLC.Connectors.Enable'), ('KLC.Connectors.Disable'),
  -- Tariffs
  ('KLC.Tariffs'), ('KLC.Tariffs.Create'), ('KLC.Tariffs.Update'), ('KLC.Tariffs.Activate'), ('KLC.Tariffs.Deactivate'),
  -- Sessions
  ('KLC.Sessions'), ('KLC.Sessions.ViewAll'),
  -- Faults
  ('KLC.Faults'), ('KLC.Faults.Update'),
  -- Alerts
  ('KLC.Alerts'), ('KLC.Alerts.Acknowledge'),
  -- Monitoring
  ('KLC.Monitoring'), ('KLC.Monitoring.Dashboard'), ('KLC.Monitoring.StatusHistory'), ('KLC.Monitoring.EnergySummary'),
  -- Station Groups
  ('KLC.StationGroups'), ('KLC.StationGroups.Create'), ('KLC.StationGroups.Update'), ('KLC.StationGroups.Delete'), ('KLC.StationGroups.Assign'),
  -- Payments
  ('KLC.Payments'), ('KLC.Payments.ViewAll'), ('KLC.Payments.Refund'),
  -- Audit Logs
  ('KLC.AuditLogs'), ('KLC.AuditLogs.Export'),
  -- E-Invoices
  ('KLC.EInvoices'), ('KLC.EInvoices.Generate'), ('KLC.EInvoices.Retry'), ('KLC.EInvoices.Cancel'),
  -- User Management
  ('KLC.UserManagement'), ('KLC.UserManagement.Create'), ('KLC.UserManagement.Update'), ('KLC.UserManagement.Delete'), ('KLC.UserManagement.ManageRoles'), ('KLC.UserManagement.ManagePermissions'),
  -- Role Management
  ('KLC.RoleManagement'), ('KLC.RoleManagement.Create'), ('KLC.RoleManagement.Update'), ('KLC.RoleManagement.Delete'), ('KLC.RoleManagement.ManagePermissions'),
  -- Mobile Users
  ('KLC.MobileUsers'), ('KLC.MobileUsers.ViewAll'), ('KLC.MobileUsers.Suspend'), ('KLC.MobileUsers.WalletAdjust'),
  -- Vouchers
  ('KLC.Vouchers'), ('KLC.Vouchers.Create'), ('KLC.Vouchers.Update'), ('KLC.Vouchers.Delete'),
  -- Promotions
  ('KLC.Promotions'), ('KLC.Promotions.Create'), ('KLC.Promotions.Update'), ('KLC.Promotions.Delete'),
  -- Feedback
  ('KLC.Feedback'), ('KLC.Feedback.Respond'),
  -- Notifications
  ('KLC.Notifications'), ('KLC.Notifications.Broadcast'),
  -- Maintenance
  ('KLC.Maintenance'), ('KLC.Maintenance.Create'), ('KLC.Maintenance.Update'), ('KLC.Maintenance.Delete'),
  -- Settings
  ('KLC.Settings'), ('KLC.Settings.Update'),
  -- Power Sharing
  ('KLC.PowerSharing'), ('KLC.PowerSharing.Create'), ('KLC.PowerSharing.Update'), ('KLC.PowerSharing.Delete'), ('KLC.PowerSharing.ManageMembers'),
  -- Operators
  ('KLC.Operators'), ('KLC.Operators.Create'), ('KLC.Operators.Update'), ('KLC.Operators.Delete'), ('KLC.Operators.ManageStations'), ('KLC.Operators.ManageWebhooks'),
  -- Fleets
  ('KLC.Fleets'), ('KLC.Fleets.Create'), ('KLC.Fleets.Update'), ('KLC.Fleets.Delete'), ('KLC.Fleets.ManageVehicles'), ('KLC.Fleets.ManageSchedules'), ('KLC.Fleets.ViewAnalytics')
) AS p(name)
WHERE NOT EXISTS (
  SELECT 1 FROM "AbpPermissionGrants"
  WHERE "Name" = p.name AND "ProviderName" = 'R' AND "ProviderKey" = 'admin'
);

-- ============================================================
-- OPERATOR ROLE: Grant read + operational permissions
-- ============================================================
INSERT INTO "AbpPermissionGrants" ("Id", "TenantId", "Name", "ProviderName", "ProviderKey")
SELECT gen_random_uuid(), NULL, p.name, 'R', 'operator'
FROM (VALUES
  ('KLC.Stations'), ('KLC.Stations.Update'),
  ('KLC.Connectors'), ('KLC.Connectors.Update'), ('KLC.Connectors.Enable'), ('KLC.Connectors.Disable'),
  ('KLC.Tariffs'),
  ('KLC.Sessions'), ('KLC.Sessions.ViewAll'),
  ('KLC.Faults'), ('KLC.Faults.Update'),
  ('KLC.Alerts'), ('KLC.Alerts.Acknowledge'),
  ('KLC.Monitoring'), ('KLC.Monitoring.Dashboard'), ('KLC.Monitoring.StatusHistory'), ('KLC.Monitoring.EnergySummary'),
  ('KLC.StationGroups'),
  ('KLC.Payments'), ('KLC.Payments.ViewAll'),
  ('KLC.Feedback'), ('KLC.Feedback.Respond'),
  ('KLC.MobileUsers'), ('KLC.MobileUsers.ViewAll'),
  ('KLC.PowerSharing'), ('KLC.Operators'), ('KLC.Fleets'), ('KLC.Fleets.ViewAnalytics'),
  ('KLC.Maintenance'), ('KLC.Settings')
) AS p(name)
WHERE NOT EXISTS (
  SELECT 1 FROM "AbpPermissionGrants"
  WHERE "Name" = p.name AND "ProviderName" = 'R' AND "ProviderKey" = 'operator'
);

-- ============================================================
-- VIEWER ROLE: Grant read-only permissions
-- ============================================================
INSERT INTO "AbpPermissionGrants" ("Id", "TenantId", "Name", "ProviderName", "ProviderKey")
SELECT gen_random_uuid(), NULL, p.name, 'R', 'viewer'
FROM (VALUES
  ('KLC.Stations'),
  ('KLC.Connectors'),
  ('KLC.Tariffs'),
  ('KLC.Sessions'), ('KLC.Sessions.ViewAll'),
  ('KLC.Faults'),
  ('KLC.Alerts'),
  ('KLC.Monitoring'), ('KLC.Monitoring.Dashboard'), ('KLC.Monitoring.StatusHistory'), ('KLC.Monitoring.EnergySummary'),
  ('KLC.StationGroups'),
  ('KLC.Payments'), ('KLC.Payments.ViewAll'),
  ('KLC.AuditLogs'),
  ('KLC.EInvoices'),
  ('KLC.MobileUsers'), ('KLC.MobileUsers.ViewAll'),
  ('KLC.Feedback'),
  ('KLC.Notifications'),
  ('KLC.PowerSharing'), ('KLC.Operators'), ('KLC.Fleets'), ('KLC.Maintenance'),
  ('KLC.Settings')
) AS p(name)
WHERE NOT EXISTS (
  SELECT 1 FROM "AbpPermissionGrants"
  WHERE "Name" = p.name AND "ProviderName" = 'R' AND "ProviderKey" = 'viewer'
);

COMMIT;

-- Verify: show permission counts per role
SELECT "ProviderKey" AS role, COUNT(*) AS permission_count
FROM "AbpPermissionGrants"
WHERE "ProviderName" = 'R'
GROUP BY "ProviderKey"
ORDER BY "ProviderKey";
