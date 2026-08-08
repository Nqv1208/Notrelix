import { defineConfig } from 'vitest/config';
import path from 'path';

export default defineConfig({
  root: path.resolve(__dirname, '../../'),
  test: {
    name: 'integration',
    globals: true,
    environment: 'node',
    include: [
      'packages/**/*.integration.test.{ts,tsx}',
      'packages/**/integration/**/*.test.{ts,tsx}',
      'apps/**/*.integration.test.{ts,tsx}',
    ],
    exclude: ['**/node_modules/**', '**/dist/**', '**/.next/**'],
  },
});
