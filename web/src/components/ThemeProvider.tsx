import { useEffect, useState, type ReactNode } from 'react'
import { readStoredTheme, storeTheme, ThemeContext, type Theme } from '@/lib/theme'

type ThemeProviderProps = {
  children: ReactNode
}

// The theme is needed all over the app (the toggle, and any component that
// draws a chart or an image later), so it's one of the few good uses of
// context. Bursary data is NOT: that still travels as props.
export function ThemeProvider({ children }: ThemeProviderProps) {
  const [theme, setThemeState] = useState<Theme>(readStoredTheme)

  useEffect(() => {
    const root = document.documentElement
    const media = window.matchMedia('(prefers-color-scheme: dark)')

    function apply() {
      const dark = theme === 'dark' || (theme === 'system' && media.matches)
      root.classList.toggle('dark', dark)
      // Native scrollbars and form controls follow along too.
      root.style.colorScheme = dark ? 'dark' : 'light'
    }

    apply()

    // Only "system" needs to react when the operating system's theme changes
    // while the page is open.
    if (theme !== 'system') return

    media.addEventListener('change', apply)
    // Cleanup: stop listening when the theme changes or the app unmounts.
    return () => media.removeEventListener('change', apply)
  }, [theme])

  function setTheme(next: Theme) {
    storeTheme(next)
    setThemeState(next)
  }

  return <ThemeContext value={{ theme, setTheme }}>{children}</ThemeContext>
}
