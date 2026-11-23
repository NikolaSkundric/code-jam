import react from '@vitejs/plugin-react'
import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) },
  },
  server: {
    // Local dev: forward API and SignalR calls to the backend (docker compose exposes it on 5125).
    proxy: {
      '/api': 'http://localhost:5125',
      '/hubs': { target: 'http://localhost:5125', ws: true },
    },
  },
})
