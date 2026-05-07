/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL: string | undefined
  readonly VITE_SMOKE_LOGIN_EMAIL: string | undefined
  readonly VITE_SMOKE_LOGIN_PASSWORD: string | undefined
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
