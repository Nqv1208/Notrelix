import { defineConfig } from 'vitest/config';
import path from 'path';

export default defineConfig({
  test: {
    globals: true,
    environment: 'node',
    include: [
      '../../packages/**/__tests__/**/*.test.ts',
      '../../packages/**/__tests__/**/*.test.tsx',
    ],
    exclude: ['node_modules', 'dist', '.next'],
  },
  resolve: {
    alias: {
      '@notrelix/kernel': path.resolve(__dirname, '../../packages/foundation/kernel/src/index.ts'),
      '@notrelix/platform': path.resolve(__dirname, '../../packages/foundation/platform/src/index.ts'),
      '@notrelix/contracts': path.resolve(__dirname, '../../packages/foundation/contracts/src/index.ts'),
      '@notrelix/query': path.resolve(__dirname, '../../packages/foundation/query/src/index.ts'),
      '@notrelix/wm-core': path.resolve(__dirname, '../../packages/product/work-management/core/src/index.ts'),
    },
  },
});
