import react, { reactCompilerPreset } from '@vitejs/plugin-react'
import babel from '@rolldown/plugin-babel'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    react(),
    // The React Compiler runs at build time and memoizes components for us.
    babel({ presets: [reactCompilerPreset()] }),
  ],
  server: {
    // The API's CORS policy allows exactly http://localhost:3000.
    port: 3000,
    // If 3000 is busy, fail loudly instead of quietly moving to 3001,
    // where every API call would be blocked by CORS.
    strictPort: true,
  },
})
