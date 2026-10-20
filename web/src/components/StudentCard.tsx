import { useState } from 'react'
import type { StudentResponse } from '../api/types'

type StudentCardProps = {
  student: StudentResponse
}

export function StudentCard({ student }: StudentCardProps) {
  // Each StudentCard on the page has its OWN copy of this state.
  const [showSubjects, setShowSubjects] = useState(false)

  // Derived, not stored: recalculated from props on every render.
  const belowTarget = student.subjects.filter((s) => s.currentMark < s.targetMark).length

  return (
    <article className="card">
      <h3>{student.fullName}</h3>
      <p className="muted">LRN {student.learnerReferenceNumber}</p>

      {student.subjects.length === 0 ? (
        <p>No subjects captured yet.</p>
      ) : (
        <p>
          {student.subjects.length} subjects
          {belowTarget > 0 && <> · {belowTarget} below target</>}
        </p>
      )}

      {student.subjects.length > 0 && (
        <button type="button" onClick={() => setShowSubjects(!showSubjects)}>
          {showSubjects ? 'Hide subjects' : 'Show subjects'}
        </button>
      )}

      {showSubjects && (
        <ul>
          {student.subjects.map((subject) => (
            // A student can't take the same subject twice (the API enforces
            // it), so the code is a stable, unique key among these siblings.
            <li key={subject.code}>
              {subject.code}: {subject.currentMark}% (target {subject.targetMark}%)
              {subject.currentMark < subject.targetMark ? ' ⚠ below target' : ' ✓ on track'}
            </li>
          ))}
        </ul>
      )}
    </article>
  )
}
