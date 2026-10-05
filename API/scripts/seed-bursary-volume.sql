-- Week 5 Day 3: realistic volume for EXPLAIN ANALYZE. NOT run on startup -
-- run it by hand, on purpose:
--   Get-Content scripts\seed-bursary-volume.sql -Raw | docker exec -i matric-compass-postgres psql -U postgres -d matric_compass
--
-- 100,000 bursary applications spread across the two seeded students.
-- Every funder name is unique ('Volume Seed n') so this data can never
-- collide with today's "one active application per funder" constraint.
-- Undo with scripts/remove-bursary-volume.sql.
INSERT INTO "BursaryApplications" ("Id", "StudentId", "Funder", "Amount", "Deadline", "Status")
SELECT gen_random_uuid(),
       s."Id",
       'Volume Seed ' || g,
       1000 + (g % 50000),
       now() + (g % 365) * interval '1 day' + (g % 86400) * interval '1 second',
       g % 5
FROM generate_series(1, 100000) AS g
JOIN "Students" AS s
  ON s."LearnerReferenceNumber" = CASE WHEN g % 2 = 0 THEN 'LRN-2026-00114' ELSE 'LRN-2026-00287' END;

-- Refresh the planner's statistics now, rather than waiting for autovacuum -
-- otherwise EXPLAIN plans against stale row estimates.
ANALYZE "BursaryApplications";
