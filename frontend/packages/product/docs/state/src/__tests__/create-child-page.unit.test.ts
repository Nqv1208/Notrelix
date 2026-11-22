import { QueryClient } from "@tanstack/react-query";
import type { CreatePagePayload, Page } from "@notrelix/docs-core";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { createPageApi } from "../api/page.api";
import { docsQueryKeys } from "../query/keys";
import {
  PENDING_PAGE_ID_PREFIX,
  createUseCreatePage,
} from "../query/hooks/use-create-page";
import { testEndpoints } from "./test-endpoints";

// `useMutation` is replaced by its options, so the optimistic patch, rollback
// and settle callbacks can be driven without a React tree.
const harness = vi.hoisted(() => ({
  client: undefined as unknown as QueryClient,
}));

vi.mock("@tanstack/react-query", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@tanstack/react-query")>();
  return {
    ...actual,
    useMutation: (options: unknown) => options,
    useQueryClient: () => harness.client,
  };
});

interface Options {
  onMutate: (payload: CreatePagePayload) => Promise<{ previous?: Page[] }>;
  onError: (
    error: unknown,
    payload: CreatePagePayload,
    context?: { previous?: Page[] },
  ) => void;
  onSettled: () => Promise<void>;
}

const WORKSPACE = "ws-1";
const key = docsQueryKeys.list(WORKSPACE);

function page(id: string, parentId: string | null, spaceId: string | null) {
  return {
    id,
    workspaceId: WORKSPACE,
    title: id,
    parentId,
    spaceId,
    position: 1,
    version: 3,
  } as unknown as Page;
}

function api() {
  return { get: vi.fn(), post: vi.fn(), patch: vi.fn(), delete: vi.fn() };
}

const child: CreatePagePayload = {
  title: "Untitled",
  workspaceId: WORKSPACE,
  parentId: "parent",
  idempotencyKey: "k1",
};

beforeEach(() => {
  harness.client = new QueryClient();
  harness.client.setQueryData(key, [
    page("parent", null, "space-1"),
    page("other", null, null),
  ]);
});

function options(client = api()) {
  return createUseCreatePage(
    client,
    testEndpoints,
  )(WORKSPACE) as unknown as Options;
}

describe("creating a page under a parent", () => {
  it("shows the child under its parent in the parent's Space at once", async () => {
    await options().onMutate(child);

    const list = harness.client.getQueryData<Page[]>(key)!;
    const pending = list.at(-1)!;
    expect(list).toHaveLength(3);
    expect(pending.id).toBe(`${PENDING_PAGE_ID_PREFIX}k1`);
    expect(pending.parentId).toBe("parent");
    expect(pending.spaceId).toBe("space-1");
    expect(pending.title).toBe("Untitled");
  });

  it("puts the list back exactly as it was when the producer refuses", async () => {
    const before = harness.client.getQueryData<Page[]>(key);
    const mutation = options();

    const context = await mutation.onMutate(child);
    expect(harness.client.getQueryData<Page[]>(key)).toHaveLength(3);
    mutation.onError(new Error("403"), child, context);

    expect(harness.client.getQueryData<Page[]>(key)).toEqual(before);
  });

  it("adds nothing for a page at the root or a parent the list does not hold", async () => {
    const mutation = options();

    await mutation.onMutate({ ...child, parentId: null });
    await mutation.onMutate({ ...child, parentId: "missing" });

    expect(harness.client.getQueryData<Page[]>(key)).toHaveLength(2);
  });

  it("refreshes the list after every outcome", async () => {
    const spy = vi.spyOn(harness.client, "invalidateQueries");

    await options().onSettled();

    expect(spy).toHaveBeenCalledWith({ queryKey: key });
  });
});

describe("the create request", () => {
  it("never sends a Space together with a parent (the server refuses both)", async () => {
    const client = api();
    client.post.mockResolvedValue("new-id");
    client.get.mockResolvedValue({});
    await createPageApi(client, testEndpoints)
      .create({ ...child, spaceId: "space-1" })
      .catch(() => undefined);

    const body = client.post.mock.calls[0]![1] as Record<string, unknown>;
    expect(body.parentId).toBe("parent");
    expect(body).not.toHaveProperty("spaceId");
    expect(client.post.mock.calls[0]![2]).toEqual({ idempotencyKey: "k1" });
  });

  it("still sends the Space of a root page", async () => {
    const client = api();
    client.post.mockResolvedValue("new-id");
    client.get.mockResolvedValue({});
    await createPageApi(client, testEndpoints)
      .create({ ...child, parentId: null, spaceId: "space-1" })
      .catch(() => undefined);

    expect(client.post.mock.calls[0]![1]).toMatchObject({ spaceId: "space-1" });
  });
});
