namespace API.Services;

using API.Common;
using API.Models;
using Domain.Exceptions;
using Domain.Repositories;

// Week 6 Day 3: builds one screen from two repositories. One request from
// the browser; two queries on the server — and both show up as SQL spans
// under that one request in yesterday's trace view.
//
// A class of its own rather than a new method on StudentService:
// StudentServiceTests construct StudentService by hand, and a new
// constructor parameter there would break tests that have nothing to do
// with dashboards.
public class LearnerDashboardService
{
    private readonly IStudentRepository _students;
    private readonly IBursaryApplicationRepository _bursaryApplications;
    private readonly TimeProvider _clock;

    public LearnerDashboardService(
        IStudentRepository students,
        IBursaryApplicationRepository bursaryApplications,
        TimeProvider clock)
    {
        _students = students;
        _bursaryApplications = bursaryApplications;
        _clock = clock;
    }

    public async Task<LearnerDashboardResponse> GetAsync(Guid studentId)
    {
        var student = await _students.GetByIdAsync(studentId)
            ?? throw new NotFoundException($"Student {studentId} was not found.");
        var applications = await _bursaryApplications.GetByStudentIdAsync(studentId);
        var today = _clock.GetUtcNow().UtcDateTime.Date;

        return new LearnerDashboardResponse(
            student.FullName,
            student.Subjects
                .OrderBy(subject => subject.Code)
                .Select(subject => new DashboardSubject(
                    subject.Code, subject.CurrentMark, subject.TargetMark,
                    OnTrack: subject.CurrentMark >= subject.TargetMark))
                .ToList(),
            applications
                .OrderBy(application => application.Deadline)
                .Select(application => new DashboardBursaryApplication(
                    application.Id,
                    application.Funder,
                    application.Status.ToString(),
                    application.Deadline,
                    DaysUntilDeadline: (int)(application.Deadline.Date - today).TotalDays,
                    AmountInCents: Money.ToCents(application.Amount)))
                .ToList());
    }
}
