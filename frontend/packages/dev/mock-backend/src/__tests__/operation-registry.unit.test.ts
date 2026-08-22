import { describe, it, expect } from "vitest";
import {
  MockOperationRegistry,
  MockDuplicateOperationIdError,
  MockDuplicateRouteError,
} from "../operations/operation-registry";
import { defineMockOperation } from "../operations/types";
import { ok } from "../transport/create-response";

describe("MFB-FZ-06: Operation Registry Uniqueness Hardening", () => {
  it("T-MFB-020: throws MockDuplicateOperationIdError on duplicate operation ID", () => {
    const registry = new MockOperationRegistry();

    const op1 = defineMockOperation({
      id: "test.duplicate.id",
      contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
      method: "GET",
      route: "/test/path/a",
      handle: async () => ok({}),
    });

    const op2 = defineMockOperation({
      id: "test.duplicate.id",
      contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
      method: "GET",
      route: "/test/path/b",
      handle: async () => ok({}),
    });

    registry.register(op1);
    expect(() => registry.register(op2)).toThrow(MockDuplicateOperationIdError);
  });

  it("T-MFB-021: throws MockDuplicateRouteError when different IDs share the same method and route", () => {
    const registry = new MockOperationRegistry();

    const op1 = defineMockOperation({
      id: "test.route.one",
      contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
      method: "GET",
      route: "/test/shared/route",
      handle: async () => ok({}),
    });

    const op2 = defineMockOperation({
      id: "test.route.two",
      contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
      method: "GET",
      route: "/test/shared/route",
      handle: async () => ok({}),
    });

    registry.register(op1);
    expect(() => registry.register(op2)).toThrow(MockDuplicateRouteError);
  });

  it("T-MFB-022: allows same route with different HTTP methods to coexist", () => {
    const registry = new MockOperationRegistry();

    const getOp = defineMockOperation({
      id: "resource.get",
      contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
      method: "GET",
      route: "/resource",
      handle: async () => ok({ id: "1" }),
    });

    const postOp = defineMockOperation({
      id: "resource.create",
      contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
      method: "POST",
      route: "/resource",
      handle: async () => ok({ id: "2" }),
    });

    registry.register(getOp);
    expect(() => registry.register(postOp)).not.toThrow();
    expect(registry.operationIds()).toContain("resource.get");
    expect(registry.operationIds()).toContain("resource.create");
  });

  it("T-MFB-023: throws MockDuplicateRouteError on parameter-equivalent route ambiguity", () => {
    const registry = new MockOperationRegistry();

    const op1 = defineMockOperation({
      id: "boards.detail.one",
      contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
      method: "GET",
      route: "/boards/:id",
      handle: async () => ok({}),
    });

    const op2 = defineMockOperation({
      id: "boards.detail.two",
      contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
      method: "GET",
      route: "/boards/:boardId",
      handle: async () => ok({}),
    });

    registry.register(op1);
    expect(() => registry.register(op2)).toThrow(MockDuplicateRouteError);
  });
});
