import basicSsl from '@vitejs/plugin-basic-ssl'
import vue from '@vitejs/plugin-vue'
import { defineConfig } from 'vite'

// La API se alcanza por el mismo origen (/api) para que la cookie de sesión funcione.
// API_PROXY_TARGET permite apuntar a otro puerto, p. ej. https://localhost:7010.
const proxy = {
  '/api': { target: process.env.API_PROXY_TARGET ?? 'https://localhost:7001', secure: false },
}

export default defineConfig({
  plugins: [vue(), basicSsl()],
  server: {
    host: 'localhost',
    port: 5173,
    strictPort: true,
    proxy,
  },
  preview: { proxy },
})
