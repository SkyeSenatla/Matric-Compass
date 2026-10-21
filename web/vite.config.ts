import path from 'node:path'
import react, { reactCompilerPreset } from '@vitejs/plugin-react'
import babel from '@rolldown/plugin-babel'
import tailwindcss from '@tailwindcss/vite'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    react(),
    // The React Compiler runs at build time and memoizes components for us.
    babel({ presets: [reactCompilerPreset()] }),
    // Tailwind v4 compiles our utility classes at build time.
    tailwindcss(),
  ],
  resolve: {
    // "@/components/ui/badge" means "src/components/ui/badge". TypeScript
    // needs the same alias, in tsconfig.json and tsconfig.app.json.
    alias: {
      '@': path.resolve(import.meta.dirname, './src'),
    },
  },
  server: {
    // The API's CORS policy allows exactly http://localhost:3000.
    port: 3000,
    // If 3000 is busy, fail loudly instead of quietly moving to 3001,
    // where every API call would be blocked by CORS.
    strictPort: true,
  },
})
