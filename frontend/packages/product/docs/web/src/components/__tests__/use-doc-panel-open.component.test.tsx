import { describe, expect, it } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";

import {
  docPanelStorageKey,
  useDocPanelOpen,
  type PanelStorage,
} from "../use-doc-panel-open";

function fakeStorage(initial: Record<string, string> = {}) {
  const data = new Map(Object.entries(initial));
  return {
    data,
    getItem: (key: string) => data.get(key) ?? null,
    setItem: (key: string, value: string) => void data.set(key, value),
  };
}

function Probe({ viewer, storage }: { viewer: string; storage: PanelStorage }) {
  const [open, setOpen] = useDocPanelOpen(viewer, storage);
  return (
    <button type="button" onClick={() => setOpen(!open)}>
      {open ? "open" : "closed"}
    </button>
  );
}

describe("useDocPanelOpen", () => {
  it("is open by default and remembers a close per viewer", () => {
    const storage = fakeStorage();
    const first = renderPureUi(<Probe viewer="u1" storage={storage} />);
    fireEvent.click(screen.getByRole("button", { name: "open" }));
    expect(screen.getByRole("button", { name: "closed" })).toBeTruthy();
    expect(storage.data.get(docPanelStorageKey("u1"))).toBe("0");
    first.unmount();

    const again = renderPureUi(<Probe viewer="u1" storage={storage} />);
    expect(screen.getByRole("button", { name: "closed" })).toBeTruthy();
    again.unmount();

    renderPureUi(<Probe viewer="u2" storage={storage} />);
    expect(screen.getByRole("button", { name: "open" })).toBeTruthy();
  });

  it("keeps working when storage throws", () => {
    const broken: PanelStorage = {
      getItem: () => {
        throw new Error("blocked");
      },
      setItem: () => {
        throw new Error("blocked");
      },
    };
    renderPureUi(<Probe viewer="u1" storage={broken} />);
    fireEvent.click(screen.getByRole("button", { name: "open" }));
    expect(screen.getByRole("button", { name: "closed" })).toBeTruthy();
  });
});
