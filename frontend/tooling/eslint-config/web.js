/**
 * ESLint config for web apps (Vite + React).
 */

import baseConfig from './index.js';
import boundariesConfig from './boundaries.js';

export default [
  ...baseConfig,
  boundariesConfig,
  {
    rules: {
      'no-restricted-imports': [
        'error',
        {
          patterns: [
            {
              group: ['next/*'],
              message: 'Web app cannot import next/*. Use @tanstack/react-router instead.',
            },
            {
              group: ['@notrelix/ui-mobile', '@notrelix/runtime-mobile'],
              message: 'Web app cannot import mobile packages.',
            },
          ],
        },
      ],
    },
  },
];
