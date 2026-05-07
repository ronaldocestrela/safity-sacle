import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  // Destino do proxy `/api` no dev. O `dotnet run` padrão usa o perfil `http` → Kestrel em http://localhost:5003.
  // Com `--launch-profile https`, use VITE_DEV_API_PROXY_TARGET=https://localhost:7104
  const apiTarget =
    env.VITE_DEV_API_PROXY_TARGET || 'http://localhost:5003'

  return {
    plugins: [react()],
    server: {
      port: 4863,
      proxy: {
        '/api': {
          target: apiTarget,
          changeOrigin: true,
          secure: false,
        },
      },
    },
    test: {
      globals: true,
      environment: 'node',
    },
  }
})
