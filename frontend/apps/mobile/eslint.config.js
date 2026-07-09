import { defineConfig } from "eslint/config";
import mobileConfig from "@notrelix/eslint-config/mobile";

export default defineConfig([
  {
    ignores: [
      ".expo/**",
      "node_modules/**",
      ".turbo/**",
      "dist/**",
      "build/**",
    ],
  },
  ...mobileConfig,
]);
