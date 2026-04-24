-- Wipes all schedule entries (Work, WorkChange, Expenses, Break) on the Dev database
-- so that the Wizard training benchmark starts from an empty calendar.
--
-- Usage (from WSL):
--   powershell.exe -File 'C:\SourceCode\Klacks.E2ETest\Scripts\clean-work-for-training.ps1'
-- Or directly:
--   psql -h localhost -p 5434 -U postgres -d klacks -f clean-work-for-training.sql
--
-- Targets the Dev database on port 5434 (admin/admin). Master data (clients,
-- contracts, shifts, groups, group_items, schedule_command, shift_preference)
-- is preserved; only volatile schedule rows are removed.

BEGIN;

SELECT 'Before cleanup' AS phase,
       (SELECT COUNT(*) FROM work)        AS work,
       (SELECT COUNT(*) FROM work_change) AS work_change,
       (SELECT COUNT(*) FROM expenses)    AS expenses,
       (SELECT COUNT(*) FROM break)       AS break;

TRUNCATE TABLE work, work_change, expenses, break RESTART IDENTITY CASCADE;

SELECT 'After cleanup' AS phase,
       (SELECT COUNT(*) FROM work)        AS work,
       (SELECT COUNT(*) FROM work_change) AS work_change,
       (SELECT COUNT(*) FROM expenses)    AS expenses,
       (SELECT COUNT(*) FROM break)       AS break;

COMMIT;
