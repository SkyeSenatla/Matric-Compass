'use client'

import { logout } from '@/app/actions/auth'
import { Button } from '@/components/ui/button'
import { forgetPinnedStudents } from '@/stores/pinnedStudents'

// Sign-out is still Day 1's Server Action. The pins live in this browser's
// localStorage, which the server can't touch, so the browser forgets them
// here, on the way out (POPIA, shared computers).
export function SignOutButton() {
  return (
    <form action={logout} onSubmit={forgetPinnedStudents}>
      <Button type="submit" variant="outline" size="sm">
        Sign out
      </Button>
    </form>
  )
}
