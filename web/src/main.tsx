import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { ThemeProvider } from '@/components/ThemeProvider'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    {/* Outermost, so every component below can call useTheme(). */}
    <ThemeProvider>
      <App />
    </ThemeProvider>
  </StrictMode>,
)
