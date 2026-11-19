import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";

import { workspaceHomeCopy } from "../workspace-home.copy";
import { WorkspaceFiguresStrip } from "../workspace-figures";

const copy = workspaceHomeCopy("en");
const text = copy.home.figures;

describe("workspace figures", () => {
  it("FUI[workspace.home:figures-success] shows active members, Spaces and pending invitations", () => {
    renderPureUi(
      <WorkspaceFiguresStrip
        copy={copy}
        state={{
          status: "ready",
          data: {
            members: 86,
            spaces: 5,
            teams: 2,
            invitations: 7,
            archived: false,
          },
        }}
        onRetry={vi.fn()}
      />,
    );
    const figures = screen.getByRole("region", { name: text.label });
    for (const [label, value] of [
      [text.members, "86"],
      [text.spaces, "5"],
      [text.invitations, "7"],
    ] as const) {
      const tile = within(figures).getByText(label).parentElement!;
      expect(within(tile).getByText(value)).toBeTruthy();
    }
  });

  it("FUI[workspace.home:figures-loading] holds the place while the figures load", () => {
    renderPureUi(
      <WorkspaceFiguresStrip
        copy={copy}
        state={{ status: "loading" }}
        onRetry={vi.fn()}
      />,
    );
    expect(screen.getByRole("status", { name: text.loading })).toBeTruthy();
  });

  it("FUI[workspace.home:figures-error] says the figures did not load and retries", () => {
    const onRetry = vi.fn();
    renderPureUi(
      <WorkspaceFiguresStrip
        copy={copy}
        state={{ status: "error" }}
        onRetry={onRetry}
      />,
    );
    expect(screen.getByRole("alert").textContent).toContain(text.error);
    fireEvent.click(screen.getByRole("button", { name: text.retry }));
    expect(onRetry).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.home:figures-no-access] says only an Owner or Admin sees the figures when the read is refused", () => {
    renderPureUi(
      <WorkspaceFiguresStrip
        copy={copy}
        state={{ status: "error", reason: "forbidden" }}
        onRetry={vi.fn()}
      />,
    );
    expect(screen.getByRole("note").textContent).toBe(text.forbidden);
    expect(screen.queryByRole("button", { name: text.retry })).toBeNull();
  });
});
