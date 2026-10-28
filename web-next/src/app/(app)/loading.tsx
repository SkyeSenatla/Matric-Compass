import { CardSkeletons } from '@/components/CardSkeletons'

// Shown while any page in (app) is waiting for its data. Next.js wraps the
// page in a Suspense boundary with this as the fallback.
export default function Loading() {
  return <CardSkeletons label="Loading" />
}
