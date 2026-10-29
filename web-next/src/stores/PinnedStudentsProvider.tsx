'use client'

import { createContext, use, useEffect, useState } from 'react'
import { useStore } from 'zustand'
import { createPinnedStore, type PinnedState } from './pinnedStudents'

type PinnedStore = ReturnType<typeof createPinnedStore>

const PinnedStoreContext = createContext<PinnedStore | null>(null)

// One store per provider, so one per tab in the browser and one per request
// on the server, never one shared by everyone. Server Components never read
// it: it's browser state, and the server has none.
export function PinnedStudentsProvider({ children }: { children: React.ReactNode }) {
  const [store] = useState(createPinnedStore)

  // Load the saved pins after the first render, so it matches the server's.
  useEffect(() => {
    void store.persist.rehydrate()
  }, [store])

  return <PinnedStoreContext value={store}>{children}</PinnedStoreContext>
}

// Subscribe to one slice: a component re-renders only when that slice changes.
export function usePinnedStudents<T>(selector: (state: PinnedState) => T): T {
  const store = use(PinnedStoreContext)
  if (store === null) throw new Error('usePinnedStudents must be used inside PinnedStudentsProvider')
  return useStore(store, selector)
}
