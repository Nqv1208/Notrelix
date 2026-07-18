/**
 * ESLint config for dependency boundary enforcement.
 *
 * Enforces import rules between packages.
 */

export default {
  rules: {
    'no-restricted-imports': [
      'error',
      {
        patterns: [
          {
            group: ['@notrelix/*/ui-mobile', '@notrelix/runtime-mobile'],
            message: 'Web packages cannot import mobile packages.',
          },
          {
            group: ['@notrelix/*/ui-web', '@notrelix/runtime-web'],
            message: 'Mobile packages cannot import web packages.',
          },
          {
            group: ['next/*'],
            message: 'Packages cannot import next/*. Only apps/marketing can use Next.js.',
          },
          {
            group: ['../../*', '../../../*', '../../../../../*'],
            message: 'Deep relative imports (../../) are forbidden in packages. Use ~/ alias instead.',
          },
        ],
      },
    ],
  },
};
