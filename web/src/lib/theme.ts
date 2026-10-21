import { createContext, useContext } from 'react'

export type Theme = 'light' | 'dark' | 'system'

// The same key the inline script in index.html reads before React starts.
export const THEME_STORAGE_KEY = 'matric-compass-theme'

export type ThemeContextValue = {
  theme: Theme
  setTheme: (theme: Theme) => void
}

// null means "no ThemeProvider above you": useTheme turns that into a clear
// error instead of a silent default.
export const ThemeContext = createContext<ThemeContextValue | null>(null)

export function useTheme(): ThemeContextValue {
  const context = useContext(ThemeContext)
  if (context === null) throw new Error('useTheme must be used inside <ThemeProvider>')
  return context
}

// localStorage can throw (private browsing, blocked storage), so every read
// and write is guarded. Losing the preference is fine; crashing isn't.
export function readStoredTheme(): Theme {
  try {
    const stored = localStorage.getItem(THEME_STORAGE_KEY)
    return stored === 'light' || stored === 'dark' ? stored : 'system'
  } catch {
    return 'system'
  }
}

export function storeTheme(theme: Theme): void {
  try {
    localStorage.setItem(THEME_STORAGE_KEY, theme)
  } catch {
    // Not saved, but the theme still changes for this visit.
  }
}
