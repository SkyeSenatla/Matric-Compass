import { useState } from 'react'
import { api } from '@/api/client'
import { authStats, expireAccessTokenForDemo } from '@/auth/session'
import { Button } from '@/components/ui/button'


export function DevAuthPanel() {
  const [result, setResult] = useState<string | null>(null)

  async function fireFiveRequests() {
    const before = authStats.refreshCalls
    expireAccessTokenForDemo()
    const responses = await Promise.all(Array.from({ length: 5 }, () => api.GET('/api/v1/me')))
    const statuses = responses.map((r) => r.response.status).join(', ')
    setResult(`5 requests, ${authStats.refreshCalls - before} refresh call(s). Statuses: ${statuses}`)
  }

  return (
    <aside className="space-y-2 rounded-xl border border-dashed p-4 text-sm">
      <p className="font-medium">Demo: the five-parallel-401 problem</p>
      <Button variant="outline" size="sm" onClick={fireFiveRequests}>
        Expire my token, then send 5 requests at once
      </Button>
      {result !== null && <p data-testid="demo-result">{result}</p>}
    </aside>
  )
}