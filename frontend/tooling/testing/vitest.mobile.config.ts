import { defineConfig } from "vitest/config";
import path from "node:path";

export default defineConfig({
  root: path.resolve(__dirname, "../../"),
  test: {
    name: "mobile",
    globals: true,
    environment: "node",
    include: [
      "apps/mobile/**/*.mobile.test.{ts,tsx}",
      "packages/runtimes/mobile/**/*.mobile.test.{ts,tsx}",
      "packages/ui/mobile/**/*.mobile.test.{ts,tsx}",
      "packages/product/work-management/mobile/**/*.mobile.test.{ts,tsx}",
      "packages/product/docs/mobile/**/*.mobile.test.{ts,tsx}",
      "packages/product/automation/mobile/**/*.mobile.test.{ts,tsx}",
    ],
    exclude: ["**/node_modules/**", "**/dist/**", "**/.next/**"],
  },
});
