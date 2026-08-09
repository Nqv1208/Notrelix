import { defineConfig } from "vitest/config";
import path from "node:path";

export default defineConfig({
  root: path.resolve(__dirname, "../../"),
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "../../apps/web/src"),
      "@notrelix/testing": path.resolve(__dirname, "./index.ts"),
    },
  },
  test: {
    name: "web",
    globals: true,
    environment: "jsdom",
    passWithNoTests: true,
    setupFiles: [path.resolve(__dirname, "./src/setup-web.ts")],
    include: ["{apps,packages}/**/*.component.test.{ts,tsx}"],
    exclude: ["**/node_modules/**", "**/dist/**", "**/.next/**"],
  },
});
