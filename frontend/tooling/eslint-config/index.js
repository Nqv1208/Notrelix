/**
 * @notrelix/eslint-config
 *
 * Shared ESLint configuration for Notrelix monorepo.
 * Phase 0 will set up base rules, Phase 1 will add architecture boundaries.
 */

import js from "@eslint/js";
import tseslint from "typescript-eslint";

export default tseslint.config(
  js.configs.recommended,
  ...tseslint.configs.recommended,
  {
    rules: {
      "no-unused-vars": "off",
      "@typescript-eslint/no-unused-vars": [
        "error",
        { argsIgnorePattern: "^_", varsIgnorePattern: "^_" },
      ],
    },
  },
);
