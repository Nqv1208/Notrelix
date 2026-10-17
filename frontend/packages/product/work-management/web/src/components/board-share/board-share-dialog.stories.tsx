import type { ReactNode } from "react";
import type { Meta, StoryObj } from "@storybook/react";

import {
  shareDefaultScenario,
  shareEmptyScenario,
  shareLevelScenario,
  shareLogScenario,
  sharePrivateScenario,
  shareProducedOnlyScenario,
  shareWorkspaceScenario,
  type BoardShareScenario,
} from "../../verification/board-share-fixtures";
import { BoardShareDialogSurface } from "./board-share-dialog-surface";

const meta: Meta = {
  title: "Work Management/Board share",
  parameters: { layout: "fullscreen" },
};

export default meta;
type Story = StoryObj;

const noop = () => undefined;

/** The dialog sits over the page; the story gives it the mockup's 1440x900. */
function Stage({ children }: { children: ReactNode }) {
  return <div className="h-[900px] w-full">{children}</div>;
}

function render(scenario: BoardShareScenario, withWrites = false) {
  return (
    <Stage>
      <BoardShareDialogSurface
        {...scenario}
        onClose={noop}
        onCopyLink={noop}
        onChangeLevel={withWrites ? noop : undefined}
        onRemove={withWrites ? noop : undefined}
        onChangeGeneral={withWrites ? noop : undefined}
      />
    </Stage>
  );
}

/** Final-Share `chia-se`. */
export const ShareDefault: Story = {
  render: () => render(shareDefaultScenario(), true),
  tags: ["fui-surface--wm.board-share", "fui-state--Default"],
};

/** Final-Share `doi-quyen`. */
export const ShareLevelMenu: Story = {
  render: () => render(shareLevelScenario(), true),
  tags: ["fui-surface--wm.board-share", "fui-state--Default"],
};

/** Final-Share `rieng-tu`. */
export const SharePrivate: Story = {
  render: () => render(sharePrivateScenario(), true),
  tags: ["fui-surface--wm.board-share", "fui-state--Default"],
};

/** Final-Share `nhat-ky`. */
export const ShareLog: Story = {
  render: () => render(shareLogScenario(), true),
  tags: ["fui-surface--wm.board-share", "fui-state--Default"],
};

/** What the mock world fills today: levels, expiry and history are missing. */
export const ShareProducedOnly: Story = {
  render: () => render(shareProducedOnlyScenario()),
  tags: ["fui-surface--wm.board-share", "fui-state--EdgeData"],
};

export const ShareWorkspaceBoard: Story = {
  render: () => render(shareWorkspaceScenario()),
  tags: ["fui-surface--wm.board-share", "fui-state--EdgeData"],
};

export const ShareEmpty: Story = {
  render: () => render(shareEmptyScenario()),
  tags: ["fui-surface--wm.board-share", "fui-state--Empty"],
};
