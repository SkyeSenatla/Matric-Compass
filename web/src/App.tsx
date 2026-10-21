import { BursaryApplicationList } from '@/components/BursaryApplicationList'
import { ModeToggle } from '@/components/ModeToggle'
import { StudentCard } from '@/components/StudentCard'
import { TertiaryApplicationCard } from '@/components/TertiaryApplicationCard'
import { bursaryApplications, students, tertiaryApplications } from '@/data/sample'

// App's one job now: page layout. Sorting moved into BursaryApplicationList.
export default function App() {
  return (
    <div className="min-h-svh bg-background text-foreground">
      <header className="border-b">
        <div className="mx-auto flex max-w-6xl items-center justify-between gap-4 px-4 py-3">
          <h1 className="text-lg font-semibold">Matric Compass</h1>
          <ModeToggle />
        </div>
      </header>

      <main className="mx-auto max-w-6xl space-y-10 px-4 py-6">
        <section className="space-y-4">
          <h2 className="text-xl font-semibold">Students</h2>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {students.map((student) => (
              <StudentCard key={student.id} student={student} />
            ))}
          </div>
        </section>

        <section className="space-y-4">
          <h2 className="text-xl font-semibold">Bursary applications</h2>
          <BursaryApplicationList applications={bursaryApplications} />
        </section>

        <section className="space-y-4">
          <h2 className="text-xl font-semibold">Tertiary applications</h2>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {tertiaryApplications.map((application) => (
              <TertiaryApplicationCard key={application.id} application={application} />
            ))}
          </div>
        </section>
      </main>
    </div>
  )
}
