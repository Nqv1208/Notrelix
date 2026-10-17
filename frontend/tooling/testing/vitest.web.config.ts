import { defineConfig } from "vitest/config";
import path from "node:path";

// The product shows workspace times in Hanoi time, so the suites that assert
// formatted clock times must not depend on the machine or CI runner zone.
process.env.TZ = "Asia/Ho_Chi_Minh";

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
    include: ["{apps,packages,tooling}/**/*.component.test.{ts,tsx}"],
    exclude: ["**/node_modules/**", "**/dist/**", "**/.next/**"],
  },
});
