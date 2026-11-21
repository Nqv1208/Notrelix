import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";

import { ArchivedWorkspaceBanner } from "../archived-workspace-banner";
import { workspaceShellCopy } from "../workspace-shell.copy";

const copy = workspaceShellCopy("vi");

function renderBanner(
  props: Partial<Parameters<typeof ArchivedWorkspaceBanner>[0]> = {},
) {
  const onUnarchive = vi.fn();
  const onReload = vi.fn();
  renderPureUi(
    <ArchivedWorkspaceBanner
      copy={copy}
      canUnarchive
      state="idle"
      onUnarchive={onUnarchive}
      onReload={onReload}
      {...props}
    />,
  );
  return { onUnarchive, onReload };
}

describe("ArchivedWorkspaceBanner (D02, I05)", () => {
  it("FUI[workspace.archived:owner] tells the Owner the workspace is read-only and offers Unarchive", () => {
    const { onUnarchive } = renderBanner();
    const banner = screen.getByRole("status");
    expect(banner.textContent).toContain(copy.archived.title);
    expect(banner.textContent).toContain(copy.archived.ownerHint);
    fireEvent.click(
      screen.getByRole("button", { name: copy.archived.unarchive }),
    );
    expect(onUnarchive).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.archived:member] shows every other member the banner without the action", () => {
    renderBanner({ canUnarchive: false });
    expect(screen.getByRole("status").textContent).toContain(
      copy.archived.memberHint,
    );
    expect(screen.queryByRole("button")).toBeNull();
  });

  it("FUI[workspace.archived:pending] shows the action as running and cannot be sent twice", () => {
    const { onUnarchive } = renderBanner({ state: "pending" });
    const button = screen.getByRole("button", {
      name: copy.archived.unarchiving,
    }) as HTMLButtonElement;
    expect(button.disabled).toBe(true);
    fireEvent.click(button);
    expect(onUnarchive).not.toHaveBeenCalled();
  });

  it("FUI[workspace.archived:conflict] says the workspace changed and offers a reload, not a retry of the stale write", () => {
    const { onReload, onUnarchive } = renderBanner({ state: "conflict" });
    expect(screen.getByRole("alert").textContent).toContain(
      copy.archived.conflictTitle,
    );
    expect(
      screen.queryByRole("button", { name: copy.archived.unarchive }),
    ).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: copy.archived.reload }));
    expect(onReload).toHaveBeenCalledTimes(1);
    expect(onUnarchive).not.toHaveBeenCalled();
  });

  it("FUI[workspace.archived:denied-failed] names a refusal and a plain failure, and the action stays available", () => {
    renderBanner({ state: "denied" });
    expect(screen.getByRole("alert").textContent).toBe(
      copy.archived.deniedText,
    );
    expect(
      screen.getByRole("button", { name: copy.archived.unarchive }),
    ).toBeTruthy();
  });

  it("FUI[workspace.archived:failed] a failed write says nothing changed", () => {
    renderBanner({ state: "failed" });
    expect(screen.getByRole("alert").textContent).toBe(
      copy.archived.failedText,
    );
  });
});
