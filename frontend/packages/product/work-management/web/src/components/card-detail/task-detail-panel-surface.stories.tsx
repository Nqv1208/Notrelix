import { useMemo, useReducer } from "react";
import type { Meta, StoryObj } from "@storybook/react";
import {
  createTaskDetailUiController,
  ownerCapabilities,
  taskDetailDefaultScenario,
  taskDetailEdgeScenario,
  taskDetailLoadingScenario,
  taskDetailReadOnlyScenario,
  taskDetailUnavailableScenario,
  viewerCapabilities,
} from "@notrelix/work-management-testing";
import type { TaskDetailScenarioData } from "@notrelix/work-management-testing";
import type { CardDetailTab } from "@notrelix/work-management-core";
import { TaskDetailPanelSurface } from "./task-detail-panel-surface";
import type { TaskDetailCapabilities } from "./task-detail-types";

const meta = {
  title: "Work Management/Task Detail/Panel",
  component: TaskDetailPanelSurface,
  parameters: {
    a11y: {
      disable: true,
    },
  },
} satisfies Meta<typeof TaskDetailPanelSurface>;
export default meta;
type Story = StoryObj;

function TaskDetailStory({
  scenario,
  capabilities = ownerCapabilities,
}: {
  scenario: TaskDetailScenarioData;
  capabilities?: TaskDetailCapabilities;
}) {
  const [, rerender] = useReducer((count: number) => count + 1, 0);
  const controller = useMemo(
    () => createTaskDetailUiController(scenario),
    [scenario],
  );
  const state = controller.state;
  const card = state.card;

  function apply(update: () => void) {
    if (!capabilities.canEditFields && !capabilities.canDelete) return;
    update();
    rerender();
  }

  return (
    <div className="h-[760px] w-[720px] overflow-hidden rounded-lg border bg-popover">
      <TaskDetailPanelSurface
        status={state.isLoading ? "loading" : state.error ? "error" : "ready"}
        board={state.board}
        card={card}
        capabilities={capabilities}
        activeTab={state.activeTab}
        detailData={
          card
            ? {
                updates: card.updates,
                updatesLoading: false,
                files: card.files,
                filesLoading: false,
                activity: card.activity,
                activityLoading: false,
                activityFetching: false,
              }
            : undefined
        }
        callbacks={{
          onClose: () => undefined,
          onRenameTitle: (_cardId, patch) =>
            apply(() => {
              if (patch.title) controller.renameTitle(patch.title);
            }),
          onToggleWatch: (watched) =>
            apply(() => controller.setWatched(watched)),
          onDuplicate: () => undefined,
          onArchive: () => undefined,
          onUpdateFieldValue: ({ fieldDefinitionId, value }) =>
            apply(() => controller.editField(fieldDefinitionId, value)),
          onRefreshActivity: () => undefined,
          onCreateUpdate: (input, options) =>
            apply(() => {
              controller.addUpdate(input.body);
              options?.onSuccess?.();
            }),
          onUpdateUpdate: (updateId, body) =>
            apply(() => controller.editUpdate(updateId, body)),
          onDeleteUpdate: (updateId) =>
            apply(() => controller.deleteUpdate(updateId)),
          onSelectTab: (tab: CardDetailTab) => {
            controller.selectTab(tab);
            rerender();
          },
        }}
      />
    </div>
  );
}

export const Default: Story = {
  render: () => <TaskDetailStory scenario={taskDetailDefaultScenario()} />,
  tags: ["fui-surface--wm.task-detail.panel", "fui-state--Default"],
};

export const Loading: Story = {
  render: () => <TaskDetailStory scenario={taskDetailLoadingScenario()} />,
  tags: ["fui-surface--wm.task-detail.panel", "fui-state--Loading"],
};

export const Unavailable: Story = {
  render: () => <TaskDetailStory scenario={taskDetailUnavailableScenario()} />,
  tags: ["fui-surface--wm.task-detail.panel", "fui-state--Unavailable"],
};

export const EdgeData: Story = {
  render: () => <TaskDetailStory scenario={taskDetailEdgeScenario()} />,
  tags: ["fui-surface--wm.task-detail.panel", "fui-state--EdgeData"],
};

export const ReadOnly: Story = {
  render: () => (
    <TaskDetailStory
      scenario={taskDetailReadOnlyScenario().data}
      capabilities={viewerCapabilities}
    />
  ),
  tags: ["fui-surface--wm.task-detail.panel", "fui-state--ReadOnly"],
};
