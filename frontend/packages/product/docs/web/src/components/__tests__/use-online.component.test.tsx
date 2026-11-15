import { describe, expect, it } from "vitest";
import { act, renderPureUi, screen } from "@notrelix/testing";

import { useOnline } from "../use-online";

function Probe() {
  return <span role="status">{useOnline() ? "online" : "offline"}</span>;
}

describe("useOnline", () => {
  it("FUI[docs.page.online] follows the offline and online events of the window", () => {
    renderPureUi(<Probe />);
    expect(screen.getByRole("status").textContent).toBe("online");

    act(() => {
      window.dispatchEvent(new Event("offline"));
    });
    expect(screen.getByRole("status").textContent).toBe("offline");

    act(() => {
      window.dispatchEvent(new Event("online"));
    });
    expect(screen.getByRole("status").textContent).toBe("online");
  });
});
