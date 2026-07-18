import { defineConfig } from 'vitest/config';

export default defineConfig({
  resolve: {
    tsconfigPaths: true,
  },
  test: {
    globals: true,
    environment: 'node',
    include: [
      '../../packages/**/__tests__/**/*.test.ts',
      '../../packages/**/__tests__/**/*.test.tsx',
    ],
    exclude: ['node_modules', 'dist', '.next'],
  },
});
