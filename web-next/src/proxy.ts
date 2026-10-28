import { NextResponse, type NextRequest } from 'next/server'
import { needsRefresh, refreshOnce } from '@/lib/refresh'
import { openSession, sealSession, SESSION_COOKIE, sessionCookieOptions } from '@/lib/session'

// proxy.ts runs BEFORE every matching request renders anything, and it can
// set cookies. That makes it the only place a refresh can be saved: a
// Server Component can't set cookies once the page has started streaming.
export async function proxy(request: NextRequest) {
  const session = await openSession(request.cookies.get(SESSION_COOKIE)?.value)
  const path = request.nextUrl.pathname

  // 1. Route protection, the quick kind: no session, no signed-in pages.
  // A convenience: pages and actions still check, and the API decides.
  if (session === null) {
    if (path === '/login') return NextResponse.next()
    const login = new URL('/login', request.url)
    if (path !== '/') login.searchParams.set('next', path)
    return NextResponse.redirect(login)
  }

  // 2. Silent refresh, only when the access token is nearly used up.
  if (!needsRefresh(session)) return NextResponse.next()

  const refreshed = await refreshOnce(session)
  if (refreshed === null) {
    // Expired, revoked or reused: sign in again, and say why.
    const response = NextResponse.redirect(new URL('/login?expired=1', request.url))
    response.cookies.delete(SESSION_COOKIE)
    return response
  }

  const sealed = await sealSession(refreshed)
  // For THIS request: the page about to render reads the new session.
  request.cookies.set(SESSION_COOKIE, sealed)
  const response = NextResponse.next({ request: { headers: request.headers } })
  // For the browser: every later request carries the new session.
  response.cookies.set(SESSION_COOKIE, sealed, sessionCookieOptions)
  return response
}

export const config = {
  // Everything except Next.js's own files and the favicon.
  matcher: ['/((?!_next/static|_next/image|favicon.ico).*)'],
}
