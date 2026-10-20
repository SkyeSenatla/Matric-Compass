import type { TertiaryApplicationResponse } from '../api/types'

// The order an application moves through. Accepted and Rejected are both
// final, so they share the last step.
const PIPELINE = ['Researching', 'Applied', 'DocumentsSubmitted', 'Offered', 'Decision'] as const

type TertiaryApplicationCardProps = {
  application: TertiaryApplicationResponse
}

export function TertiaryApplicationCard({ application }: TertiaryApplicationCardProps) {
  const isFinal = application.status === 'Accepted' || application.status === 'Rejected'
  const currentStep = isFinal ? 'Decision' : application.status
  const currentIndex = PIPELINE.findIndex((step) => step === currentStep)

  return (
    <article className="card">
      <h3>{application.programmeName}</h3>
      <p className="muted">{application.institutionName}</p>
      <p>
        Status: <strong>{application.status}</strong>
      </p>

      {/* An unknown status still shows as text above; we just skip the steps. */}
      {currentIndex !== -1 && (
        <ol className="pipeline">
          {PIPELINE.map((step, index) => (
            <li key={step} className={index <= currentIndex ? 'done' : undefined}>
              {step === 'Decision' && isFinal ? application.status : step}
              {index === currentIndex && ' ← now'}
            </li>
          ))}
        </ol>
      )}
    </article>
  )
}
