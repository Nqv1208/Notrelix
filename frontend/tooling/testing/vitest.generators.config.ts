import { defineConfig } from 'vitest/config';
import path from 'path';

export default defineConfig({
  root: path.resolve(__dirname, '../../'),
  test: {
    name: 'generators',
    globals: true,
    environment: 'node',
    include: [
      'tooling/generators/**/__tests__/**/*.test.{ts,tsx}',
      'tooling/generators/**/*.test.{ts,tsx}',
    ],
    exclude: ['**/node_modules/**', '**/dist/**'],
  },
});
