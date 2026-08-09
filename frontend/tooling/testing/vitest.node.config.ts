import { defineConfig } from "vitest/config";
import path from "node:path";

export default defineConfig({
  root: path.resolve(__dirname, "../../"),
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "../../apps/web/src"),
    },
  },
  test: {
    name: "node",
    globals: true,
    environment: "node",
    include: ["{apps,packages,tooling}/**/*.unit.test.{ts,tsx}"],
    exclude: ["**/node_modules/**", "**/dist/**", "**/.next/**"],
  },
});
