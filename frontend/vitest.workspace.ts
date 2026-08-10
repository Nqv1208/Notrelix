import { defineWorkspace } from "vitest/config";

export default defineWorkspace([
  "./tooling/testing/vitest.node.config.ts",
  "./tooling/testing/vitest.web.config.ts",
]);
