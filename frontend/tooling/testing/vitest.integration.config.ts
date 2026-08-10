import { defineConfig } from "vitest/config";
import path from "node:path";

export default defineConfig({
  root: path.resolve(__dirname, "../../"),
  test: {
    name: "integration",
    globals: true,
    environment: "node",
    passWithNoTests: true,
    include: ["{apps,packages}/**/*.integration.test.{ts,tsx}"],
    exclude: ["**/node_modules/**", "**/dist/**", "**/.next/**"],
  },
});
