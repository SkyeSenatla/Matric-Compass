namespace API.Models;

// Used by POST /api/students/{id}/payments to record a tuition fee payment.
public record RecordPaymentRequest(decimal Amount);

public record PaymentResponse(Guid Id, Guid StudentId, decimal Amount, DateTime RecordedAt);
