import type { NextConfig } from 'next'

const nextConfig: NextConfig = {
  // Week 7 ran with the React Compiler on, and the Day 4 form depends on how
  // it behaves. In Next.js 16 it's off unless you say so.
  reactCompiler: true,
  // Day 2: Cache Components. Everything is dynamic until you say "use
  // cache", and request data (cookies, params) must be read inside Suspense.
  cacheComponents: true,
  // Goes with it: links prefetch each page's static shell. The docs ask for
  // it to be set explicitly whenever cacheComponents is on.
  partialPrefetching: true,
  // next dev writes an AGENTS.md for AI coding assistants unless told not
  // to. This course doesn't use them.
  agentRules: false,
  turbopack: {
    rules: {
      '*.css': {
        loaders: ['@tailwindcss/turbopack'],
        as: '*.css',
      },
    },
  },
}

export default nextConfig
