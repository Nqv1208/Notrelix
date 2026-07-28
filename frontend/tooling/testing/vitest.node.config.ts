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
    name: 'node',
    globals: true,
    environment: 'node',
    include: [
      'packages/**/*.unit.test.ts',
      'packages/**/__tests__/**/*.test.ts',
      'tooling/**/*.test.ts',
      'apps/**/*.unit.test.ts',
    ],
    exclude: ['**/node_modules/**', '**/dist/**', '**/.next/**'],
  },
});
