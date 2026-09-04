import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import {
  ownerCapabilities,
  taskDetailDefaultScenario,
  taskDetailLoadingScenario,
  taskDetailUnavailableScenario,
  viewerCapabilities,
} from "@notrelix/work-management-testing";
import type { CardDetailTab } from "@notrelix/work-management-core";
import { TaskDetailPanelSurface } from "../task-detail-panel-surface";

function renderTaskDetail(
  scenario = taskDetailDefaultScenario(),
  overrides: Partial<Parameters<typeof TaskDetailPanelSurface>[0]> = {},
) {
  const callbacks: Parameters<typeof TaskDetailPanelSurface>[0]["callbacks"] = {
    onClose: vi.fn(),
    onRenameTitle: vi.fn(),
    onToggleWatch: vi.fn(),
    onDuplicate: vi.fn(),
    onArchive: vi.fn(),
    onUpdateFieldValue: vi.fn(),
    onRefreshActivity: vi.fn(),
    onCreateUpdate: vi.fn((_input, options) => options?.onSuccess?.()),
    onUpdateUpdate: vi.fn(),
    onDeleteUpdate: vi.fn(),
    onSelectTab: vi.fn(),
  };
  const props: Parameters<typeof TaskDetailPanelSurface>[0] = {
    status: scenario.isLoading ? "loading" : scenario.error ? "error" : "ready",
    board: scenario.board,
    card: scenario.card,
    capabilities: ownerCapabilities,
    activeTab: "updates",
    detailData: scenario.card
      ? {
          updates: scenario.card.updates,
          updatesLoading: false,
          files: scenario.card.files,
          filesLoading: false,
          activity: scenario.card.activity,
          activityLoading: false,
          activityFetching: false,
        }
      : undefined,
    callbacks,
    ...overrides,
  };

  renderPureUi(<TaskDetailPanelSurface {...props} />);
  return { props, callbacks };
}

describe("TaskDetailPanelSurface interactions", () => {
  it("renders loading state without QueryClient", () => {
    renderTaskDetail(taskDetailLoadingScenario());
    expect(screen.getByLabelText("Loading task details")).toBeTruthy();
  });

  it("renders unavailable state without QueryClient", () => {
    renderTaskDetail(taskDetailUnavailableScenario());
    expect(screen.getByText("Task unavailable")).toBeTruthy();
  });

  it("renders ready state without QueryClient", () => {
    renderTaskDetail();
    expect(
      screen.getByRole("textbox", { name: "Edit task title" }),
    ).toBeTruthy();
    expect(
      screen.getByText("Updated the owner-local UI scenario."),
    ).toBeTruthy();
  });

  it("closes from Escape and the close button through injected callback", () => {
    const { callbacks } = renderTaskDetail();

    fireEvent.keyDown(screen.getByLabelText("Task detail panel"), {
      key: "Escape",
    });
    fireEvent.click(screen.getByRole("button", { name: "Close task details" }));

    expect(callbacks.onClose).toHaveBeenCalledTimes(2);
  });

  it("routes title edits and tab changes through local callbacks", () => {
    const { callbacks } = renderTaskDetail();
    const title = screen.getByRole("textbox", { name: "Edit task title" });

    title.textContent = "Renamed detail task";
    fireEvent.blur(title);
    fireEvent.click(screen.getByRole("tab", { name: /Files/i }));

    expect(callbacks.onRenameTitle).toHaveBeenCalledWith("card-test", {
      title: "Renamed detail task",
    });
    expect(callbacks.onSelectTab).toHaveBeenCalledWith("files");
  });

  it("submits updates through injected composer callback", () => {
    const { callbacks } = renderTaskDetail();

    fireEvent.change(screen.getByRole("textbox", { name: "Write an update" }), {
      target: { value: "New pure UI update" },
    });
    fireEvent.click(screen.getByRole("button", { name: /^Update$/i }));

    expect(callbacks.onCreateUpdate).toHaveBeenCalledWith(
      {
        cardId: "card-test",
        body: "New pure UI update",
        mentionUserIds: [],
        attachmentIds: [],
      },
      expect.objectContaining({ onSuccess: expect.any(Function) }),
    );
  });

  it("keeps read-only critical actions disabled", () => {
    renderTaskDetail(taskDetailDefaultScenario(), {
      capabilities: viewerCapabilities,
      activeTab: "updates" as CardDetailTab,
    });

    expect(
      (
        screen.getByRole("button", {
          name: "Unfollow task",
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);
    expect(
      (
        screen.getByRole("textbox", {
          name: "Write an update",
        }) as HTMLTextAreaElement
      ).disabled,
    ).toBe(true);
  });
});
