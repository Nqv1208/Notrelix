import { defineConfig } from "eslint/config";
import baseConfig from "@notrelix/eslint-config/library";

export default defineConfig([
  {
    ignores: [
      "dist/**",
      "node_modules/**",
      ".turbo/**",
      "src/__tests__/fixtures/**",
    ],
  },
  ...baseConfig,
]);
