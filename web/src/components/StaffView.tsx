import { useStudents } from '@/api/queries'
import { BursaryApplicationForm } from '@/components/BursaryApplicationForm'
import { BursaryApplicationPages } from '@/components/BursaryApplicationPages'
import { CardSkeletons } from '@/components/CardSkeletons'
import { ErrorView } from '@/components/ErrorView'
import { StudentCard } from '@/components/StudentCard'

// Counsellors and admins: every learner, and every bursary application.
export function StaffView() {
  const students = useStudents()

  return (
    <div className="space-y-10">
      <section className="space-y-4">
        <h2 className="text-xl font-semibold">Students</h2>
        {students.isPending ? (
          <CardSkeletons count={2} label="Loading students" />
        ) : students.isError ? (
          <ErrorView error={students.error} onRetry={() => students.refetch()} />
        ) : students.data.length === 0 ? (
          <p className="text-muted-foreground">No students yet.</p>
        ) : (
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {students.data.map((student) => (
              <StudentCard key={student.id} student={student} />
            ))}
          </div>
        )}
      </section>

      <section className="space-y-4">
        <h2 className="text-xl font-semibold">Bursary applications</h2>
        <BursaryApplicationForm />
        <BursaryApplicationPages />
      </section>
    </div>
  )
}
