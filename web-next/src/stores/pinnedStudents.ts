import { createJSONStorage, persist } from 'zustand/middleware'
import { createStore } from 'zustand/vanilla'

// Pinned students: a counsellor's short list of learners they're following.
// Not in the URL (it isn't part of one page), not server state (the API
// doesn't store pins), not local (the sidebar and every student page need
// it). That's what a small global store is for, and only that: no API data
// is ever copied in here.
export type PinnedStudent = { id: string; name: string }

export type PinnedState = {
  pinned: PinnedStudent[]
  pin: (student: PinnedStudent) => void
  unpin: (id: string) => void
}

export const PINNED_STORAGE_KEY = 'matric-compass:pinned-students'

// A factory, not a module-level store: on the Next.js server one module is
// shared by every request, so a global store would be shared by every user.
// The provider calls this once per browser tab (and once per server render).
export function createPinnedStore() {
  return createStore<PinnedState>()(
    persist(
      (set) => ({
        pinned: [],
        pin: (student) =>
          set((state) =>
            state.pinned.some((p) => p.id === student.id) ? state : { pinned: [...state.pinned, student] },
          ),
        unpin: (id) => set((state) => ({ pinned: state.pinned.filter((p) => p.id !== id) })),
      }),
      {
        name: PINNED_STORAGE_KEY,
        storage: createJSONStorage(() => localStorage),
        // The server renders with no pins. Loading them before the browser's
        // first render would make the two disagree (a hydration error), so
        // the provider loads them in an effect, after that first render.
        skipHydration: true,
      },
    ),
  )
}

// Pins belong to the person who made them. On a shared computer the next
// person must not see them: called on sign-out, and by the sign-in page in
// case the last session ended without one (it expired, or was revoked).
export function forgetPinnedStudents() {
  localStorage.removeItem(PINNED_STORAGE_KEY)
}
