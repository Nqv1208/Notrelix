import { defineConfig } from "eslint/config";
import baseConfig from "@notrelix/eslint-config/mobile";

export default defineConfig([
  {
    ignores: ["dist/**", "node_modules/**", ".turbo/**"],
  },
  ...baseConfig,
]);
