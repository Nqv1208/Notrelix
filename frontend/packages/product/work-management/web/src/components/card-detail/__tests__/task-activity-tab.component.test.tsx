import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import type { CardActivity } from "@notrelix/work-management-core";
import { en, vi as viCopy } from "../item-sheet.copy";
import { TaskActivityTab } from "../task-activity-tab";

const NOW = new Date("2026-10-27T09:00:00Z");

const status: CardActivity = {
  id: "a1",
  cardId: "c1",
  actor: "An",
  action: "changed the status from To do to Doing",
  type: "system",
  createdAt: "2026-10-27T08:30:00Z",
  code: "work.item-status-changed",
  change: { fieldName: "Status", from: "Cần làm", to: "Đang làm" },
};

function renderTab(
  props: Partial<Parameters<typeof TaskActivityTab>[0]> = {},
  copy: typeof en | typeof viCopy = en,
) {
  return renderPureUi(
    <TaskActivityTab
      activity={[]}
      isLoading={false}
      copy={copy}
      now={NOW}
      {...props}
    />,
  );
}

describe("TaskActivityTab", () => {
  it("shows placeholders while the history loads", () => {
    const { container } = renderTab({ isLoading: true });

    expect(container.querySelector('[aria-busy="true"]')).not.toBeNull();
  });

  it("says there is no history yet when none was recorded", () => {
    renderTab();

    expect(screen.getByText(en.sections.noHistory)).toBeTruthy();
  });

  it("says the history could not be read and retries on request", () => {
    const onRefresh = vi.fn();
    renderTab({ isError: true, onRefresh });

    expect(screen.getByRole("alert").textContent).toContain(
      en.activityEvents.loadError,
    );
    fireEvent.click(
      screen.getByRole("button", { name: en.activityEvents.retry }),
    );
    expect(onRefresh).toHaveBeenCalledTimes(1);
  });

  it("words a recorded change in the viewer's language from its type", () => {
    renderTab({ activity: [status] }, viCopy);

    expect(
      screen.getByText(/đổi trạng thái từ Cần làm sang Đang làm/),
    ).toBeTruthy();
    expect(screen.getByText("An")).toBeTruthy();
  });

  it("reads an older line from its own recorded action", () => {
    renderTab({
      activity: [
        {
          id: "a2",
          cardId: "c1",
          actor: "Hà",
          action: "tạo việc",
          createdAt: "2026-10-26T08:30:00Z",
        },
      ],
    });

    expect(screen.getByText(/tạo việc/)).toBeTruthy();
  });
});
