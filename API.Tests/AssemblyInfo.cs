// Week 5 Day 1: every WebApplicationFactory<Program> boot ran Program.cs's
// startup seed block against one real, shared Postgres database. xUnit runs
// different test classes in parallel by default, and back then every class
// got its OWN factory — so two classes could each boot their own copy of
// the app, both run the seed block's check-then-insert at once, both decide
// "not seeded yet", and the loser crash on the unique LRN constraint.
// [assembly: CollectionBehavior(DisableTestParallelization = true)] was the
// fix: force every class serial, so only one seed block ever ran at a time.
//
// Week 5 Day 4: narrowed, not just removed. All eight integration test
// classes now share ONE [Collection("Postgres collection")] fixture
// (API.Tests/TestSupport/PostgresApiFactory.cs) — meaning they share a
// single WebApplicationFactory instance, which boots the app and runs the
// seed block exactly ONCE per test run, full stop. There is no second
// factory left to race against. That's a structural guarantee, not a timing
// coincidence: xUnit never runs two classes in the same collection
// concurrently, by design, regardless of this assembly-level setting.
//
// The two classes outside that collection — Services/StudentServiceTests
// and Services/BursaryApplicationServiceTests — never touch Postgres or
// WebApplicationFactory at all; each test constructs its own fresh
// InMemoryStudentRepository/InMemoryBursaryApplicationRepository instance
// locally, so there's no shared state for parallel execution to corrupt.
//
// Verified, not assumed: with the assembly-level attribute removed, the
// full suite passed 34/34 across multiple consecutive runs. Left removed.
