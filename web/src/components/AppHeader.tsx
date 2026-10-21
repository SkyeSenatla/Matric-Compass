import { useMutation } from '@tanstack/react-query'
import type { MeResponse } from '@/api/types'
import { logout } from '@/auth/session'
import { ModeToggle } from '@/components/ModeToggle'
import { Button } from '@/components/ui/button'

type AppHeaderProps = {
  me: MeResponse
}

export function AppHeader({ me }: AppHeaderProps) {
  // logout() revokes the session on the API, forgets the token, and tells
  // main.tsx, which wipes every cached query (see main.tsx).
  const signOut = useMutation({ mutationFn: logout })

  return (
    <header className="border-b">
      <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-3 px-4 py-3">
        <h1 className="text-lg font-semibold">Matric Compass</h1>
        <div className="flex flex-wrap items-center gap-3">
          <span className="text-sm text-muted-foreground">
            {me.email} · {me.role}
          </span>
          <ModeToggle />
          <Button variant="outline" size="sm" disabled={signOut.isPending} onClick={() => signOut.mutate()}>
            Sign out
          </Button>
        </div>
      </div>
    </header>
  )
}
