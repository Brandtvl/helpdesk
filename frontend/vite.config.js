import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      // перенаправляем /api запросы на бэкенд
      '/api': {
        target: 'http://localhost:5292',
        changeOrigin: true
      }
    }
  }
})
