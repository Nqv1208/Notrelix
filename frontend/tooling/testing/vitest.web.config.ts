import { defineConfig } from 'vitest/config';
import path from 'path';

export default defineConfig({
  root: path.resolve(__dirname, '../../'),
  resolve: {
    alias: {
      '@': path.resolve(__dirname, '../../apps/web/src'),
    },
  },
  test: {
    name: 'web',
    globals: true,
    environment: 'jsdom',
    setupFiles: [path.resolve(__dirname, './src/setup-web.ts')],
    include: [
      'apps/**/*.component.test.{ts,tsx}',
      'apps/**/*.integration.test.{ts,tsx}',
      'apps/**/__tests__/**/*.test.{ts,tsx}',
      'packages/**/*.component.test.{ts,tsx}',
      'packages/**/*.integration.test.{ts,tsx}',
    ],
    exclude: ['**/node_modules/**', '**/dist/**', '**/.next/**'],
  },
});
