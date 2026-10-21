import { ApiError } from '@/api/errors'
import { Button } from '@/components/ui/button'
import { describeError } from '@/lib/errorMessages'

type ErrorViewProps = {
  error: unknown
  onRetry?: () => void
}

// Every error the user sees: what happened, what they can do, and the
// correlationId that lets support find this exact request in the logs.
export function ErrorView({ error, onRetry }: ErrorViewProps) {
  const correlationId = error instanceof ApiError ? error.correlationId : null

  return (
    <div role="alert" className="space-y-2 rounded-xl border border-destructive/40 p-4 text-sm">
      <p className="font-medium">{describeError(error)}</p>
      {correlationId !== null && (
        <p className="text-muted-foreground">
          Reference: <code className="select-all font-mono text-xs">{correlationId}</code>
        </p>
      )}
      {onRetry !== undefined && (
        <Button variant="outline" size="sm" onClick={onRetry}>
          Try again
        </Button>
      )}
    </div>
  )
}
