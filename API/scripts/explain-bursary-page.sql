-- Week 5 Day 3: the exact SQL GET /api/bursary-applications?studentId=...
-- sends (copied from the EF Core command log), run under EXPLAIN ANALYZE.
--   Get-Content scripts\explain-bursary-page.sql -Raw | docker exec -i matric-compass-postgres psql -U postgres -d matric_compass
--
-- EXPLAIN ANALYZE really EXECUTES the query. Harmless for a SELECT; wrap
-- anything that writes in BEGIN; ... ROLLBACK;.
SELECT "Id" AS sid FROM "Students" WHERE "LearnerReferenceNumber" = 'LRN-2026-00114' \gset

\echo '=== 1. First page (keyset, no cursor yet) ==='
EXPLAIN ANALYZE
SELECT b."Id", b."Amount", b."Deadline", b."Funder", b."Status", b."StudentId"
FROM "BursaryApplications" AS b
WHERE b."StudentId" = :'sid'
ORDER BY b."Deadline", b."Id"
LIMIT 11;

-- A row 80% of the way through this student's applications (row ~40,000
-- once the volume seed has run), to compare a deep page both ways.
SELECT (count(*) * 4 / 5)::int AS off
FROM "BursaryApplications" WHERE "StudentId" = :'sid' \gset
SELECT b."Deadline" AS d, b."Id" AS i
FROM "BursaryApplications" AS b
WHERE b."StudentId" = :'sid'
ORDER BY b."Deadline", b."Id"
OFFSET :off LIMIT 1 \gset

\echo '=== 2. Deep page, OFFSET style (what ?page=N would do) ==='
EXPLAIN ANALYZE
SELECT b."Id", b."Amount", b."Deadline", b."Funder", b."Status", b."StudentId"
FROM "BursaryApplications" AS b
WHERE b."StudentId" = :'sid'
ORDER BY b."Deadline", b."Id"
OFFSET :off LIMIT 11;

\echo '=== 3a. Same deep page, KEYSET, predicate as Demo 2 first writes it ==='
EXPLAIN ANALYZE
SELECT b."Id", b."Amount", b."Deadline", b."Funder", b."Status", b."StudentId"
FROM "BursaryApplications" AS b
WHERE b."StudentId" = :'sid'
  AND (b."Deadline" > :'d' OR (b."Deadline" = :'d' AND b."Id" > :'i'))
ORDER BY b."Deadline", b."Id"
LIMIT 11;

\echo '=== 3b. Same deep page, KEYSET, with the index-friendly >= added (Demo 3 fix) ==='
EXPLAIN ANALYZE
SELECT b."Id", b."Amount", b."Deadline", b."Funder", b."Status", b."StudentId"
FROM "BursaryApplications" AS b
WHERE b."StudentId" = :'sid'
  AND b."Deadline" >= :'d'
  AND (b."Deadline" > :'d' OR b."Id" > :'i')
ORDER BY b."Deadline", b."Id"
LIMIT 11;
