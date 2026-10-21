// Types for our own Vite environment variables. Without this,
// import.meta.env.VITE_API_BASE_URL is `any`, and a typo goes unnoticed.
interface ImportMetaEnv {
  readonly VITE_API_BASE_URL: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
