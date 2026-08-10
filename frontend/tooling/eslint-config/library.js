/**
 * ESLint config for library packages
 */

import baseConfig from "./index.js";

export default [
  ...baseConfig,
  {
    rules: {
      // Library-specific rules will be added here
    },
  },
];
