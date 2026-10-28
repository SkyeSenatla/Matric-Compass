import 'server-only'
import { cookies } from 'next/headers'
import { EncryptJWT, jwtDecrypt } from 'jose'

// The session lives on the Next.js server's side of the wire: an ENCRYPTED,
// HttpOnly cookie. The browser stores it and sends it back, but can't read
// it (HttpOnly), and even someone holding it can't read the tokens inside
// (encrypted, not just signed). Only this file ever opens it.
//
// 'server-only' makes the build fail if a Client Component imports this.

export type Session = {
  accessToken: string
  // proxy.ts swaps this for a new access token before the 5 minutes run out.
  refreshToken: string | null
  email: string
  role: string
}

export const SESSION_COOKIE = 'session'
// A working day. Every refresh re-seals the cookie, so this counts from the
// last time the user did something, not from when they signed in.
const MAX_AGE_SECONDS = 60 * 60 * 8

export const sessionCookieOptions = {
  httpOnly: true, // no JavaScript can read it, ours included
  secure: true, // HTTPS only (browsers treat localhost as secure)
  sameSite: 'lax', // not sent on cross-site POSTs
  path: '/',
  maxAge: MAX_AGE_SECONDS,
} as const

// 32 random bytes, base64-encoded, in SESSION_SECRET (.env.local).
function key(): Uint8Array {
  const secret = process.env.SESSION_SECRET
  const bytes = secret ? Buffer.from(secret, 'base64') : Buffer.alloc(0)
  if (bytes.length !== 32) {
    throw new Error('SESSION_SECRET must be 32 random bytes, base64-encoded. See .env.example.')
  }
  return bytes
}

// Day 3: sealing and opening are split from cookies(), because proxy.ts
// reads the cookie from its request and writes it on its response.
export async function sealSession(session: Session): Promise<string> {
  return new EncryptJWT(session)
    .setProtectedHeader({ alg: 'dir', enc: 'A256GCM' })
    .setIssuedAt()
    .setExpirationTime(`${MAX_AGE_SECONDS}s`)
    .encrypt(key())
}

export async function openSession(sealed: string | undefined): Promise<Session | null> {
  if (sealed === undefined) return null
  try {
    const { payload } = await jwtDecrypt(sealed, key())
    // A cookie we can decrypt was written by us, but check the shape anyway.
    if (typeof payload.accessToken !== 'string' || typeof payload.role !== 'string') return null
    return {
      accessToken: payload.accessToken,
      refreshToken: typeof payload.refreshToken === 'string' ? payload.refreshToken : null,
      email: String(payload.email ?? ''),
      role: payload.role,
    }
  } catch {
    // Tampered with, expired, or sealed with an old SESSION_SECRET.
    return null
  }
}

// Only call from a Server Action or Route Handler: a Server Component
// can't set cookies, because the page may already be streaming.
export async function createSession(session: Session): Promise<void> {
  ;(await cookies()).set(SESSION_COOKIE, await sealSession(session), sessionCookieOptions)
}

// Reading is allowed anywhere on the server, Server Components included.
export async function getSession(): Promise<Session | null> {
  return openSession((await cookies()).get(SESSION_COOKIE)?.value)
}

export async function deleteSession(): Promise<void> {
  ;(await cookies()).delete(SESSION_COOKIE)
}
