import { defineConfig, globalIgnores } from "eslint/config";
import nextVitals from "eslint-config-next/core-web-vitals";
import nextTs from "eslint-config-next/typescript";

const eslintConfig = defineConfig([
  ...nextVitals,
  ...nextTs,
  // Override default ignores of eslint-config-next.
  globalIgnores([
    // Default ignores of eslint-config-next:
    ".next/**",
    "out/**",
    "build/**",
    "next-env.d.ts",
  ]),

  // ─────────────────────────────────────────────────────────
  // Architecture boundary enforcement via no-restricted-imports
  // ─────────────────────────────────────────────────────────

  // Rule: lib/ must not import from features/
  {
    files: ["lib/**/*.ts", "lib/**/*.tsx"],
    rules: {
      "no-restricted-imports": [
        "error",
        {
          patterns: [
            {
              group: ["@/features/*", "@/features/**"],
              message:
                "[ARCH] lib/ must not import from features/. Move shared logic to lib/ or use dependency injection.",
            },
          ],
        },
      ],
    },
  },

  // Rule: components/ui/ must not import from features/
  {
    files: ["components/ui/**/*.ts", "components/ui/**/*.tsx"],
    rules: {
      "no-restricted-imports": [
        "error",
        {
          patterns: [
            {
              group: ["@/features/*", "@/features/**"],
              message:
                "[ARCH] components/ui/ must not import from features/. UI components must be business-agnostic.",
            },
          ],
        },
      ],
    },
  },

  // Rule: features/ must not import from app/
  {
    files: ["features/**/*.ts", "features/**/*.tsx"],
    rules: {
      "no-restricted-imports": [
        "error",
        {
          patterns: [
            {
              group: ["@/app/*", "@/app/**"],
              message:
                "[ARCH] features/ must not import from app/. Extract shared components to features/ or lib/.",
            },
          ],
        },
      ],
    },
  },
]);

export default eslintConfig;
