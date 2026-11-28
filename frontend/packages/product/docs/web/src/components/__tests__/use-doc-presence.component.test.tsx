import { describe, expect, it, vi } from "vitest";
import { act, renderPureUi, screen } from "@notrelix/testing";
import type {
  DocPresenceEvent,
  DocsRealtime,
} from "@notrelix/docs-collaboration";

import { DocPresenceRoster, useDocPresence } from "../use-doc-presence";

function fakeRealtime() {
  let listener: ((event: DocPresenceEvent) => void) | null = null;
  const stop = vi.fn();
  const realtime = {
    subscribePagePresence: vi.fn((_filter, next) => {
      listener = next;
      return stop;
    }),
    sendPresence: vi.fn(),
  } as unknown as DocsRealtime;
  return {
    realtime,
    stop,
    emit: (event: Omit<DocPresenceEvent, "pageId">) =>
      act(() => listener?.({ pageId: "page-1", ...event })),
  };
}

function Probe({ realtime }: { realtime: DocsRealtime | undefined }) {
  const people = useDocPresence({
    realtime,
    workspaceId: "ws-1",
    pageId: "page-1",
  });
  return <span role="status">{people.join(",")}</span>;
}

describe("DocPresenceRoster (UC-COL-E03)", () => {
  it("keeps online and idle people, drops offline at once and silent people after their time-to-live", () => {
    let now = 1_000;
    const roster = new DocPresenceRoster(() => now);
    roster.apply({
      pageId: "p",
      userId: "anna",
      state: "online",
      ttlMs: 75_000,
    });
    roster.apply({ pageId: "p", userId: "binh", state: "idle", ttlMs: 10_000 });
    roster.apply({
      pageId: "p",
      userId: "anna",
      state: "online",
      ttlMs: 75_000,
    });
    expect(roster.people()).toEqual(["anna", "binh"]);

    roster.apply({
      pageId: "p",
      userId: "anna",
      state: "offline",
      ttlMs: 75_000,
    });
    expect(roster.people()).toEqual(["binh"]);

    now += 10_000;
    expect(roster.people()).toEqual([]);
  });
});

describe("useDocPresence (UC-COL-E03)", () => {
  it("FUI[docs.page.presence] reports the caller online, lists who else has the page open and leaves the page on unmount", () => {
    const { realtime, stop, emit } = fakeRealtime();
    const { unmount } = renderPureUi(<Probe realtime={realtime} />);

    expect(realtime.subscribePagePresence).toHaveBeenCalledWith(
      { workspaceId: "ws-1", pageId: "page-1" },
      expect.any(Function),
    );
    expect(realtime.sendPresence).toHaveBeenCalledWith("online");

    emit({ userId: "anna", state: "online", ttlMs: 75_000 });
    expect(screen.getByRole("status").textContent).toBe("anna");
    emit({ userId: "anna", state: "offline", ttlMs: 75_000 });
    expect(screen.getByRole("status").textContent).toBe("");

    unmount();
    expect(stop).toHaveBeenCalled();
    expect(realtime.sendPresence).not.toHaveBeenCalledWith("offline");
  });

  it("FUI[docs.page.presence-off] lists nobody and sends nothing without a realtime session", () => {
    renderPureUi(<Probe realtime={undefined} />);
    expect(screen.getByRole("status").textContent).toBe("");
  });
});
