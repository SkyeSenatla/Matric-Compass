import Link from 'next/link'
import { Suspense } from 'react'
import { ApiError } from '@/api/errors'
import { CardSkeletons } from '@/components/CardSkeletons'
import { ErrorView } from '@/components/ErrorView'
import { StudentCard } from '@/components/StudentCard'
import { readOrFail, signedInApi } from '@/lib/api'

// Not async: the heading is the same for everyone, so it's on screen the
// moment you click. Only the list waits for the session and the API.
export default function StudentsPage() {
  return (
    <section className="space-y-4">
      <h1 className="text-xl font-semibold">Students</h1>
      <Suspense fallback={<CardSkeletons label="Loading students" />}>
        <StudentList />
      </Suspense>
    </section>
  )
}

async function StudentList() {
  const { api } = await signedInApi()
  // Staff only, decided by the API: a learner gets 403, shown as a message.
  const students = readOrFail(await api.GET('/api/v1/Students'))

  if (students instanceof ApiError) return <ErrorView error={students} />
  if (students.length === 0) return <p className="text-muted-foreground">No students yet.</p>
  return (
    <ul className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {students.map((student) => (
        <li key={student.id} className="space-y-2">
          <StudentCard student={student} />
          <Link href={`/students/${student.id}`} className="text-sm underline underline-offset-4">
            Open {student.fullName}
          </Link>
        </li>
      ))}
    </ul>
  )
}
