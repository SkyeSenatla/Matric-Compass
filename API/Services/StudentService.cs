namespace API.Services;

using API.Data;
using API.Models;

public class StudentService : IStudentService
{
    private readonly IStudentRepository _studentRepository;
    private readonly IIdempotencyStore _idempotencyStore;

    public StudentService(IStudentRepository studentRepository, IIdempotencyStore idempotencyStore)
    {
        _studentRepository = studentRepository;
        _idempotencyStore = idempotencyStore;
    }

    public async Task<CreateStudentResult> CreateStudentAsync(StudentCreateRequest request)
    {
        // The rule the controller had no good place for: it spans a READ
        // and a decision, not just the validation of one entity.
        var existing = await _studentRepository.GetByLrnAsync(request.LearnerReferenceNumber);
        if (existing is not null)
            return new CreateStudentResult.DuplicateLearnerReferenceNumber(request.LearnerReferenceNumber);

        var student = new Student(request.FullName, request.LearnerReferenceNumber);
        await _studentRepository.AddAsync(student);

        return new CreateStudentResult.Created(StudentResponse.FromEntity(student));
    }

    public async Task<RecordPaymentResult> RecordFeePaymentAsync(
        Guid studentId, string idempotencyKey, RecordPaymentRequest request)
    {
        // Simplified for the demo: production hashes the full serialized
        // body, not just one field.
        var requestHash = request.Amount.ToString("F2");

        var existing = await _idempotencyStore.FindAsync(idempotencyKey);
        if (existing is not null)
        {
            // Null ResponseBody means another request reserved this key and
            // hasn't finished — a genuine concurrent double-tap, not a
            // sequential retry. Either way, this request does not proceed.
            if (existing.ResponseBody is null || existing.RequestHash != requestHash)
                return new RecordPaymentResult.KeyConflict(idempotencyKey);

            return new RecordPaymentResult.ReplayedFromCache((PaymentResponse)existing.ResponseBody);
        }

        if (!await _idempotencyStore.TryReserveAsync(idempotencyKey))
            return new RecordPaymentResult.KeyConflict(idempotencyKey); // lost the race to another request

        var student = await _studentRepository.GetByIdAsync(studentId);
        if (student is null)
            return new RecordPaymentResult.StudentNotFound(studentId);

        if (request.Amount <= 0)
            return new RecordPaymentResult.InvalidAmount(request.Amount);

        var payment = new PaymentResponse(Guid.NewGuid(), studentId, request.Amount, DateTime.UtcNow);
        await _idempotencyStore.SaveAsync(idempotencyKey, new IdempotencyRecord(requestHash, payment));

        return new RecordPaymentResult.Recorded(payment);
    }
}
