namespace API.Services;

using API.Models;

public interface IStudentService
{
    Task<CreateStudentResult> CreateStudentAsync(StudentCreateRequest request);
    Task<RecordPaymentResult> RecordFeePaymentAsync(Guid studentId, string idempotencyKey, RecordPaymentRequest request);
}
