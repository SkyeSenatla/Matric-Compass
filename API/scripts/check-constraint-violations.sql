-- Week 5 Day 3: run BEFORE applying the constraints migration. Every query
-- must return zero rows - a constraint can't be added to a table that
-- already breaks it, and the migration will fail if any of these find data.
--   Get-Content scripts\check-constraint-violations.sql -Raw | docker exec -i matric-compass-postgres psql -U postgres -d matric_compass

\echo '1. Same subject code twice for one student (must be 0 rows):'
SELECT "StudentId", "Code", count(*)
FROM "Subjects"
GROUP BY "StudentId", "Code"
HAVING count(*) > 1;

\echo '2. Two ACTIVE (not Rejected = 4) applications, same student + funder (must be 0 rows):'
SELECT "StudentId", "Funder", count(*)
FROM "BursaryApplications"
WHERE "Status" <> 4
GROUP BY "StudentId", "Funder"
HAVING count(*) > 1;

\echo '3. Amount not greater than zero (must be 0 rows):'
SELECT "Id", "Amount" FROM "BursaryApplications" WHERE "Amount" <= 0;
