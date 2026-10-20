import { useState } from 'react'
import { BursaryApplicationCard } from './components/BursaryApplicationCard'
import { StudentCard } from './components/StudentCard'
import { TertiaryApplicationCard } from './components/TertiaryApplicationCard'
import { bursaryApplications, students, tertiaryApplications } from './data/sample'

export default function App() {
  const [soonestFirst, setSoonestFirst] = useState(true)

  // Derived from state on every render. toSorted returns a NEW array;
  // .sort() would mutate the imported data in place.
  const sortedApplications = bursaryApplications.toSorted((a, b) =>
    soonestFirst
      ? a.daysUntilDeadline - b.daysUntilDeadline
      : b.daysUntilDeadline - a.daysUntilDeadline,
  )

  return (
    <main>
      <h1>Matric Compass</h1>

      <section>
        <h2>Students</h2>
        <div className="grid">
          {students.map((student) => (
            <StudentCard key={student.id} student={student} />
          ))}
        </div>
      </section>

      <section>
        <h2>Bursary applications</h2>
        <button type="button" onClick={() => setSoonestFirst(!soonestFirst)}>
          Sort: {soonestFirst ? 'soonest deadline first' : 'latest deadline first'}
        </button>
        {sortedApplications.length === 0 ? (
          <p>No bursary applications yet.</p>
        ) : (
          <div className="grid">
            {sortedApplications.map((application) => (
              <BursaryApplicationCard key={application.id} application={application} />
            ))}
          </div>
        )}
      </section>

      <section>
        <h2>Tertiary applications</h2>
        <div className="grid">
          {tertiaryApplications.map((application) => (
            <TertiaryApplicationCard key={application.id} application={application} />
          ))}
        </div>
      </section>
    </main>
  )
}
