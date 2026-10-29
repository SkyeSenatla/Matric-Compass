import type { Metadata } from 'next'
import { NuqsAdapter } from 'nuqs/adapters/next/app'
import './globals.css'

export const metadata: Metadata = {
  title: 'Matric Compass',
  description: 'Bursaries, marks and applications for Grade 12 learners.',
}

// Runs before the page paints: dark mode follows the operating system.
const darkModeScript = `if (matchMedia('(prefers-color-scheme: dark)').matches) document.documentElement.classList.add('dark')`

export default function RootLayout({ children }: LayoutProps<'/'>) {
  return (
    // The script changes <html>'s class before React hydrates.
    <html lang="en-ZA" suppressHydrationWarning>
      <head>
        <script dangerouslySetInnerHTML={{ __html: darkModeScript }} />
      </head>
      <body>
        {/* Day 4: lets nuqs read and write the URL's query string. */}
        <NuqsAdapter>{children}</NuqsAdapter>
      </body>
    </html>
  )
}
