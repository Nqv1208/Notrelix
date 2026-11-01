import { defineConfig } from "eslint/config";
import webConfig from "@notrelix/eslint-config/web";

export default defineConfig([
  {
    ignores: [
      "dist/**",
      "node_modules/**",
      ".turbo/**",
      "build/**",
      ".tanstack/**",
    ],
  },
  ...webConfig,
]);
