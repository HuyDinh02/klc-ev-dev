-- ============================================================
-- Phase 2 Permission Grants Migration
-- ============================================================
-- Run with: PGPASSWORD=<password> psql -h <host> -p 5432 -U postgres -d KLC -f scripts/migrate-phase2-permissions.sql
-- This script is idempotent — safe to run multiple times.
-- ============================================================

BEGIN;

-- Grant Phase 2 permissions to admin role (skip if already exists)
INSERT INTO "AbpPermissionGrants" ("Id", "TenantId", "Name", "ProviderName", "ProviderKey")
SELECT gen_random_uuid(), NULL, p.name, 'R', 'admin'
FROM (VALUES
  ('KLC.PowerSharing'),
  ('KLC.PowerSharing.Create'),
  ('KLC.PowerSharing.Update'),
  ('KLC.PowerSharing.Delete'),
  ('KLC.PowerSharing.ManageMembers'),
  ('KLC.Operators'),
  ('KLC.Operators.Create'),
  ('KLC.Operators.Update'),
  ('KLC.Operators.Delete'),
  ('KLC.Operators.ManageStations'),
  ('KLC.Operators.ManageWebhooks'),
  ('KLC.Fleets'),
  ('KLC.Fleets.Create'),
  ('KLC.Fleets.Update'),
  ('KLC.Fleets.Delete'),
  ('KLC.Fleets.ManageVehicles'),
  ('KLC.Fleets.ManageSchedules'),
  ('KLC.Fleets.ViewAnalytics'),
  ('KLC.Maintenance'),
  ('KLC.Maintenance.Create'),
  ('KLC.Maintenance.Update'),
  ('KLC.Maintenance.Delete')
) AS p(name)
WHERE NOT EXISTS (
  SELECT 1 FROM "AbpPermissionGrants"
  WHERE "Name" = p.name AND "ProviderName" = 'R' AND "ProviderKey" = 'admin'
);

-- Grant Phase 2 read-only permissions to operator role
INSERT INTO "AbpPermissionGrants" ("Id", "TenantId", "Name", "ProviderName", "ProviderKey")
SELECT gen_random_uuid(), NULL, p.name, 'R', 'operator'
FROM (VALUES
  ('KLC.PowerSharing'),
  ('KLC.Operators'),
  ('KLC.Fleets'),
  ('KLC.Fleets.ViewAnalytics'),
  ('KLC.Maintenance')
) AS p(name)
WHERE NOT EXISTS (
  SELECT 1 FROM "AbpPermissionGrants"
  WHERE "Name" = p.name AND "ProviderName" = 'R' AND "ProviderKey" = 'operator'
);

-- Grant Phase 2 read-only permissions to viewer role
INSERT INTO "AbpPermissionGrants" ("Id", "TenantId", "Name", "ProviderName", "ProviderKey")
SELECT gen_random_uuid(), NULL, p.name, 'R', 'viewer'
FROM (VALUES
  ('KLC.PowerSharing'),
  ('KLC.Operators'),
  ('KLC.Fleets'),
  ('KLC.Maintenance')
) AS p(name)
WHERE NOT EXISTS (
  SELECT 1 FROM "AbpPermissionGrants"
  WHERE "Name" = p.name AND "ProviderName" = 'R' AND "ProviderKey" = 'viewer'
);

COMMIT;

-- Verify
SELECT "ProviderKey" AS role, COUNT(*) AS permission_count
FROM "AbpPermissionGrants"
WHERE "ProviderName" = 'R'
GROUP BY "ProviderKey"
ORDER BY "ProviderKey";
