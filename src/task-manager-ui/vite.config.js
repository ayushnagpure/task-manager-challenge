import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    // The API's CORS policy allows exactly http://localhost:5173. Fail instead of silently moving to another port.
    port: 5173,
    strictPort: true,
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.js'],
    env: {
      VITE_API_BASE_URL: 'http://api.test',
    },
    restoreMocks: true,
  },
});
