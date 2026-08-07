import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig, loadEnv } from 'vite'
import { VitePWA } from 'vite-plugin-pwa'

export default defineConfig(({ mode }) => {
  const environment = loadEnv(mode, process.cwd(), '')

  return {
    plugins: [
      react(),
      tailwindcss(),
      VitePWA({
        registerType: 'autoUpdate',
        includeAssets: ['favicon.svg'],
        manifest: {
          name: 'Community Intranet',
          short_name: 'Intranet',
          description:
            'Das humorvolle Intranet für Freundesgruppen und Communities.',
          theme_color: '#121419',
          background_color: '#0b0d10',
          display: 'standalone',
          start_url: '/',
          icons: [
            {
              src: '/favicon.svg',
              sizes: 'any',
              type: 'image/svg+xml',
              purpose: 'any',
            },
          ],
        },
        workbox: {
          navigateFallback: '/index.html',
          // API callbacks and party routes must always reach the network.
          // In particular, Spotify returns with a top-level navigation to the
          // API callback; treating that navigation as an SPA route would serve
          // index.html and the OAuth code would never reach the backend.
          navigateFallbackDenylist: [
            /^\/api\//,
            /^\/party\//,
            /^\/parties(?:\/|$)/,
          ],
          runtimeCaching: [
            {
              urlPattern: /^\/api\//,
              handler: 'NetworkOnly',
            },
          ],
        },
      }),
    ],
    server: {
      host: '0.0.0.0',
      port: 5173,
      strictPort: true,
      proxy: {
        '/api': {
          target: environment.VITE_BACKEND_URL ?? 'http://localhost:5080',
          changeOrigin: true,
        },
      },
    },
    preview: {
      host: '0.0.0.0',
      port: 4173,
    },
    test: {
      environment: 'jsdom',
      setupFiles: './src/test/setup.ts',
    },
  }
})
