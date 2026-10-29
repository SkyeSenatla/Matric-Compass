import { Suspense } from 'react'
import { ApiError } from '@/api/errors'
import type { components } from '@/api/schema'
import type { SubjectResponse } from '@/api/types'
import { BursaryApplicationCard } from '@/components/BursaryApplicationCard'
import { CardSkeletons } from '@/components/CardSkeletons'
import { ErrorView } from '@/components/ErrorView'
import { readOrFail, signedInApi } from '@/lib/api'
import { careersFor, getCareerCatalogue } from '@/lib/careers'
import { DeleteStudentForm } from './DeleteStudentForm'
import { PinStudentButton } from './PinStudentButton'

// Not async: it returns at once, so a click on a link shows the skeletons
// straight away. params, the session and the API calls wait inside Suspense.
export default function StudentPage({ params }: PageProps<'/students/[id]'>) {
  return (
    <Suspense fallback={<CardSkeletons label="Loading the student" />}>
      <StudentDetails params={params} />
    </Suspense>
  )
}

// [id] is a dynamic segment: one file serves every student's URL.
async function StudentDetails({ params }: Pick<PageProps<'/students/[id]'>, 'params'>) {
  // params is a Promise in Next.js 16. And id comes from the URL: the user
  // typed it, so it's untrusted. The API decides who may see what.
  const { id } = await params
  const { api, session } = await signedInApi()

  // Start both requests now, together. Only the student is awaited here:
  // the applications keep loading while the top of the page is sent.
  const studentRequest = api.GET('/api/v1/Students/{id}', { params: { path: { id } } })
  const applications = api
    .GET('/api/v1/bursary-applications', { params: { query: { StudentId: id } } })
    .then(readOrFail)

  // A student you may not see is a 404 from the API (Week 6), and
  // readOrFail turns a 404 into notFound(): never "access denied", which
  // would confirm that the student exists.
  const student = readOrFail(await studentRequest)
  if (student instanceof ApiError) return <ErrorView error={student} />

  return (
    <section className="space-y-6">
      <div>
        <h1 className="text-xl font-semibold">{student.fullName}</h1>
        <p className="text-sm text-muted-foreground">LRN {student.learnerReferenceNumber}</p>
      </div>

      {/* Role-based UI: a convenience. The API refuses anyone but an Admin. */}
      {session.role === 'Admin' && <DeleteStudentForm studentId={student.id} name={student.fullName} />}

      {/* Day 4: pins live in the browser, in a Zustand store. */}
      {session.role !== 'Learner' && <PinStudentButton id={student.id} name={student.fullName} />}

      <div className="space-y-2">
        <h2 className="font-medium">Subjects</h2>
        {student.subjects.length === 0 ? (
          <p className="text-muted-foreground">No subjects captured yet.</p>
        ) : (
          <ul className="space-y-1 text-sm">
            {student.subjects.map((subject) => (
              <li key={subject.code}>
                {subject.code}: {subject.currentMark}% (target {subject.targetMark}%)
              </li>
            ))}
          </ul>
        )}
      </div>

      <div className="space-y-2">
        <h2 className="font-medium">Careers their target marks would open</h2>
        <RecommendedCareers subjects={student.subjects} />
      </div>

      <div className="space-y-2">
        <h2 className="font-medium">Bursary applications</h2>
        {/* Streams in when it's ready. Everything above is already on screen. */}
        <Suspense fallback={<CardSkeletons label="Loading bursary applications" />}>
          <StudentApplications applications={applications} />
        </Suspense>
      </div>
    </section>
  )
}

// The catalogue comes from the cache; the match uses this learner's marks.
async function RecommendedCareers({ subjects }: { subjects: SubjectResponse[] }) {
  const careers = careersFor(await getCareerCatalogue(), subjects)
  if (careers.length === 0) return <p className="text-sm text-muted-foreground">None at these target marks.</p>
  return (
    <ul className="list-inside list-disc text-sm">
      {careers.map((career) => (
        <li key={career.name}>{career.name}</li>
      ))}
    </ul>
  )
}

type ApplicationsPage = components['schemas']['PagedResponseOfBursaryApplicationResponse']

// An async Server Component inside Suspense: it awaits the request that the
// page already started, and React streams its HTML when it's ready.
async function StudentApplications({ applications }: { applications: Promise<ApplicationsPage | ApiError> }) {
  // Demo only: a pause, so the stream is visible on a fast laptop. Delete it.
  await new Promise((resolve) => setTimeout(resolve, 1500))

  const page = await applications
  if (page instanceof ApiError) return <ErrorView error={page} />
  if (page.items.length === 0) {
    return <p className="text-sm text-muted-foreground">No bursary applications for this student.</p>
  }
  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {page.items.map((application) => (
        <BursaryApplicationCard key={application.id} application={application} />
      ))}
    </div>
  )
}
