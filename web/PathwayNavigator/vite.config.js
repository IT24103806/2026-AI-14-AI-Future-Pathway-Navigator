import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')

  return {
    plugins: [react()],
    server: {
      // Reachable from containers / remote previews, not only from localhost.
      host: true,
      allowedHosts: true,
      // Optional: set VITE_API_BASE_URL=/api in .env.local so the browser talks to the Vite dev
      // server only and Vite forwards /api to the ASP.NET Core backend (no CORS involved).
      proxy: {
        '/api': {
          target: env.VITE_PROXY_TARGET || 'http://localhost:5081',
          changeOrigin: true,
        },
      },
    },
  }
})
