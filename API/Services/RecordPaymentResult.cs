namespace API.Services;

using API.Models;

public abstract record RecordPaymentResult
{
    public sealed record Recorded(PaymentResponse Payment) : RecordPaymentResult;
    public sealed record ReplayedFromCache(PaymentResponse Payment) : RecordPaymentResult;
    public sealed record KeyConflict(string IdempotencyKey) : RecordPaymentResult;
    public sealed record StudentNotFound(Guid StudentId) : RecordPaymentResult;
    public sealed record InvalidAmount(decimal Amount) : RecordPaymentResult;
}
