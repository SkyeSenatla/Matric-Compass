'use client'

import { useState } from 'react'
import type { StudentResponse } from '@/api/types'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'

type StudentCardProps = {
  student: StudentResponse
}

export function StudentCard({ student }: StudentCardProps) {
  // Each StudentCard on the page has its OWN copy of this state.
  const [showSubjects, setShowSubjects] = useState(false)

  // Derived, not stored: recalculated from props on every render.
  const belowTarget = student.subjects.filter((s) => s.currentMark < s.targetMark).length

  return (
    <Card>
      <CardHeader>
        <CardTitle>
          <h3>{student.fullName}</h3>
        </CardTitle>
        <CardDescription>LRN {student.learnerReferenceNumber}</CardDescription>
      </CardHeader>

      <CardContent className="space-y-3">
        {student.subjects.length === 0 ? (
          <p className="text-muted-foreground">No subjects captured yet.</p>
        ) : (
          <p>
            {student.subjects.length} subjects
            {belowTarget > 0 && <> · {belowTarget} below target</>}
          </p>
        )}

        {student.subjects.length > 0 && (
          <Button variant="outline" size="sm" onClick={() => setShowSubjects(!showSubjects)}>
            {showSubjects ? 'Hide subjects' : 'Show subjects'}
          </Button>
        )}

        {showSubjects && (
          <ul className="space-y-1 text-sm">
            {student.subjects.map((subject) => (
              // A student can't take the same subject twice (the API enforces
              // it), so the code is a stable, unique key among these siblings.
              <li key={subject.code} className="flex justify-between gap-2">
                <span>
                  {subject.code}: {subject.currentMark}% (target {subject.targetMark}%)
                </span>
                <span className="text-muted-foreground">
                  {subject.currentMark < subject.targetMark ? '⚠ below target' : '✓ on track'}
                </span>
              </li>
            ))}
          </ul>
        )}
      </CardContent>
    </Card>
  )
}
