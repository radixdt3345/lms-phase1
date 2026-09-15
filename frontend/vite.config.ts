import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      'react/jsx-runtime': path.resolve(__dirname, 'node_modules/react/jsx-runtime.js'),
      'react/jsx-dev-runtime': path.resolve(__dirname, 'node_modules/react/jsx-dev-runtime.js'),
      'use-sync-external-store/with-selector.js': path.resolve('./node_modules/use-sync-external-store/shim/with-selector.js'),
      'use-sync-external-store/with-selector': path.resolve('./node_modules/use-sync-external-store/shim/with-selector.js'),
    },
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: './src/tests/setup.ts',
    server: {
      deps: {
        inline: [/@mui\//, /@emotion\//, /react-redux/, /use-sync-external-store/, /react-is/],
      },
    },
  },
  server: {
    proxy: { '/api': { target: 'http://localhost:5000', changeOrigin: true } },
  },
});
