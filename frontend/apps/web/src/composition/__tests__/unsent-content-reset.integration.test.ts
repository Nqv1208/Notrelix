import { afterEach, describe, expect, it, vi } from "vitest";
import { createQueryClient, onSessionReset } from "@notrelix/query";
import { createSessionEventBus } from "@notrelix/runtime-web";
import {
  clearApplicationCache,
  createApplicationLifecycle,
} from "../application-lifecycle";

const unregister: Array<() => void> = [];
afterEach(() => unregister.splice(0).forEach((off) => off()));

function store() {
  const unsent = new Map<string, string>([["card-1", "chưa gửi"]]);
  unregister.push(onSessionReset(() => unsent.clear()));
  return unsent;
}

function lifecycleWith(queryClient = createQueryClient()) {
  const sessionEvents = createSessionEventBus();
  const lifecycle = createApplicationLifecycle({
    queryClient,
    realtime: { disconnect: vi.fn() } as never,
    sessionEvents,
    navigateToSignedOut: vi.fn(),
  });
  return { lifecycle, sessionEvents };
}

describe("unsent content kept in memory is cleared with the session", () => {
  it("empties the store when the session ends", () => {
    const unsent = store();
    clearApplicationCache(createQueryClient(), { sessionEnded: true });
    expect(unsent.size).toBe(0);
  });

  it("keeps the store when only the workspace changes", () => {
    const unsent = store();
    clearApplicationCache(createQueryClient(), { sessionEnded: false });
    expect(unsent.size).toBe(1);
  });

  it("empties the store when another account signs in", () => {
    const unsent = store();
    const { lifecycle } = lifecycleWith();
    lifecycle.prepareAccountTransition("acc-a");
    expect(unsent.size).toBe(1);
    lifecycle.prepareAccountTransition("acc-b");
    expect(unsent.size).toBe(0);
    lifecycle.dispose();
  });
});
