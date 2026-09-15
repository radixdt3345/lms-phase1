import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';
import path from 'path';

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      'react/jsx-runtime': path.resolve('./node_modules/react/jsx-runtime.js'),
      'react/jsx-dev-runtime': path.resolve('./node_modules/react/jsx-dev-runtime.js'),
    },
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: './src/tests/setup.ts',
  },
  server: {
    proxy: { '/api': { target: 'http://localhost:5000', changeOrigin: true } },
  },
});
