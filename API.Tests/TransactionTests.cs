namespace API.Tests;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Data;
using Domain.Entities;

// Demo 7: SaveChangesAsync() already wraps itself in an implicit
// transaction — this proves the explicit BeginTransactionAsync()/
// RollbackAsync() mechanism actually works, by deliberately breaking a
// transaction and watching nothing persist.
public class TransactionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TransactionTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task A_rolled_back_transaction_leaves_no_trace()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MatricCompassDbContext>();
        var testLrn = $"LRN-TXN-TEST-{Guid.NewGuid():N}";

        // Demo 4's EnableRetryOnFailure() means SaveChangesAsync() no longer
        // runs as a single operation EF can just retry on its own — a
        // user-started transaction has to be handed to the same execution
        // strategy, so a retry (if one happens) restarts the WHOLE
        // transaction rather than replaying half of it. Skipping this wrapper
        // throws "does not support user-initiated transactions" the moment
        // EnableRetryOnFailure() and BeginTransactionAsync() are used together.
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync();

            dbContext.Students.Add(new Student("Rollback Test Student", testLrn));
            await dbContext.SaveChangesAsync();

            // Deliberately never call transaction.CommitAsync() — this
            // simulates the "something else in this unit of work failed"
            // case a real explicit transaction exists to protect against.
            await transaction.RollbackAsync();
        });

        var found = await dbContext.Students.FirstOrDefaultAsync(s => s.LearnerReferenceNumber == testLrn);
        Assert.Null(found); // proves the insert never actually committed
    }
}
