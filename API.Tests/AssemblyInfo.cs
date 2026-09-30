// Week 5: every WebApplicationFactory<Program> boot runs Program.cs's
// startup seed block, and that block now writes to one real, shared
// Postgres database instead of a private in-memory dictionary per
// instance. xUnit runs different test classes in parallel by default —
// with an in-memory singleton that was always safe, because each class's
// factory got its own isolated data. Against a shared external database,
// two factories racing the same check-then-insert seed at once can both
// decide "not seeded yet" and both try to insert Thandiwe, and the loser
// crashes on the unique LRN constraint. Disabling parallelization here is
// the correct fix, not a workaround: integration tests against one real
// shared resource have to run serially, the same way two people can't
// safely run the same "if not exists, insert" against production at once
// without a transaction or a upsert.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
