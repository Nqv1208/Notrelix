import { defineConfig } from 'vitest/config';
import path from 'path';

export default defineConfig({
  root: path.resolve(__dirname, '../../'),
  test: {
    name: 'mobile',
    globals: true,
    environment: 'node',
    include: [
      'apps/mobile/**/*.test.{ts,tsx}',
      'apps/mobile/**/__tests__/**/*.test.{ts,tsx}',
      'packages/runtimes/mobile/**/__tests__/**/*.test.{ts,tsx}',
      'packages/ui/mobile/**/__tests__/**/*.test.{ts,tsx}',
      'packages/product/work-management/mobile/**/__tests__/**/*.test.{ts,tsx}',
    ],
    exclude: ['**/node_modules/**', '**/dist/**', '**/.next/**'],
  },
});
