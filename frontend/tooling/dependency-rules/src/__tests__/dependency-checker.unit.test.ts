import { describe, it, expect } from "vitest";
import {
  isForbiddenClientCall,
  isForbiddenWebSocketInstantiation,
  isForbiddenQueryClientInstantiation,
  isDeepSrcImport,
} from "../forbidden-source-patterns";
import { classifyLayer } from "../layer-classifier";

describe("AST Architecture Checker Rules", () => {
  it("guards createNotrelixClient invocation sites", () => {
    expect(isForbiddenClientCall("/packages/features/auth/src/index.ts")).toBe(
      true,
    );
    expect(
      isForbiddenClientCall(
        "/packages/runtimes/web/src/runtime/app-runtime.tsx",
      ),
    ).toBe(false);
    expect(
      isForbiddenClientCall(
        "/packages/foundation/contracts/src/client/api-client.ts",
      ),
    ).toBe(false);
  });

  it("guards WebSocket instantiation sites", () => {
    expect(
      isForbiddenWebSocketInstantiation(
        "/packages/features/workspace/src/index.ts",
      ),
    ).toBe(true);
    expect(
      isForbiddenWebSocketInstantiation(
        "/packages/foundation/realtime/src/transport/realtime-client.ts",
      ),
    ).toBe(true);
    expect(
      isForbiddenWebSocketInstantiation(
        "/packages/runtimes/web/src/realtime/browser-websocket-factory.ts",
      ),
    ).toBe(false);
  });

  it("guards QueryClient instantiation sites", () => {
    expect(
      isForbiddenQueryClientInstantiation(
        "/packages/features/auth/src/index.ts",
      ),
    ).toBe(true);
    expect(
      isForbiddenQueryClientInstantiation(
        "/packages/foundation/query/src/query-client.ts",
      ),
    ).toBe(false);
    expect(
      isForbiddenQueryClientInstantiation(
        "/packages/runtimes/web/src/runtime/app-runtime.tsx",
      ),
    ).toBe(false);
  });

  it("prohibits deep src imports", () => {
    expect(isDeepSrcImport("@notrelix/kernel/src/errors")).toBe(true);
    expect(isDeepSrcImport("@notrelix/kernel")).toBe(false);
  });

  it("classifies package-core layout as core", () => {
    expect(
      classifyLayer(
        "/packages/product/docs/core/src/query/hooks/use-page.ts",
        "@notrelix/docs-core",
      ),
    ).toBe("core");
  });
});
