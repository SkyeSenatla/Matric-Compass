-- Week 5 Day 3: undo scripts/seed-bursary-volume.sql.
--   Get-Content scripts\remove-bursary-volume.sql -Raw | docker exec -i matric-compass-postgres psql -U postgres -d matric_compass
--
-- Only seeded rows have a 'Volume Seed n' funder, so real data is untouched.
DELETE FROM "BursaryApplications" WHERE "Funder" LIKE 'Volume Seed %';

ANALYZE "BursaryApplications";
