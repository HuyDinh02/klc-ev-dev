-- Cleanup orphaned sessions from B0207 ChargeCore retry loop
-- These sessions have UserId = empty GUID (no mobile user linked)
-- and were created by the charger's repeated StartTransaction retries
-- before the B0207 null-field fix was deployed.
--
-- Run: PGPASSWORD=<password> psql -h <host> -p 5432 -U postgres -d KLC -f scripts/cleanup-orphaned-sessions.sql

BEGIN;

-- Soft-delete orphaned sessions (no user, from retry loop)
UPDATE "AppChargingSessions"
SET "IsDeleted" = true,
    "DeletionTime" = NOW(),
    "LastModificationTime" = NOW()
WHERE "IsDeleted" = false
  AND "UserId" = '00000000-0000-0000-0000-000000000000';

-- Also soft-delete Failed sessions from the B0207 loop
-- (sessions with StopReason containing "Superseded" or "Timed out" or "charger never")
UPDATE "AppChargingSessions"
SET "IsDeleted" = true,
    "DeletionTime" = NOW(),
    "LastModificationTime" = NOW()
WHERE "IsDeleted" = false
  AND "Status" = 5  -- Failed
  AND "TotalEnergyKwh" = 0
  AND "TotalCost" = 0
  AND "CreationTime" > '2026-04-08'
  AND ("StopReason" LIKE '%Superseded%'
    OR "StopReason" LIKE '%Timed out%'
    OR "StopReason" LIKE '%charger never%'
    OR "StopReason" LIKE '%Charger did not accept%'
    OR "StopReason" LIKE '%No meter data%');

SELECT 'Cleaned up orphaned sessions' as result,
       (SELECT COUNT(*) FROM "AppChargingSessions" WHERE "IsDeleted" = true AND "DeletionTime" > NOW() - interval '1 minute') as deleted_count;

COMMIT;
