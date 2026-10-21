import type { TertiaryApplicationResponse } from '@/api/types'
import { ApplicationPipeline } from '@/components/ApplicationPipeline'
import { StatusBadge } from '@/components/StatusBadge'
import {
  Card,
  CardAction,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'

type TertiaryApplicationCardProps = {
  application: TertiaryApplicationResponse
}

// Still no state: everything comes from props. The pipeline moved into its
// own component, so this card only arranges the pieces.
export function TertiaryApplicationCard({ application }: TertiaryApplicationCardProps) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>
          <h3>{application.programmeName}</h3>
        </CardTitle>
        <CardDescription>{application.institutionName}</CardDescription>
        <CardAction>
          <StatusBadge status={application.status} />
        </CardAction>
      </CardHeader>
      <CardContent>
        <ApplicationPipeline status={application.status} />
      </CardContent>
    </Card>
  )
}
