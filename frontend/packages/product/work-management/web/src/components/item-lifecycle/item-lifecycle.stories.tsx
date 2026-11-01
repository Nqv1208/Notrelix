import type { Meta, StoryObj } from "@storybook/react";
import {
  ITEM_LIFECYCLE_BOARDS,
  ITEM_LIFECYCLE_ITEM_TITLE,
  ITEM_LIFECYCLE_TRASH_EDGE_ROWS,
  ITEM_LIFECYCLE_TRASH_ROWS,
  itemLifecycleCopyFixture,
} from "../../verification/item-lifecycle-fixtures";
import { BoardTrashSheet } from "./board-trash-sheet";
import { MoveBoardDialog } from "../item-actions/move-board-dialog";
import { en as actionsCopy } from "../item-actions/item-actions.copy";
import { DeleteItemDialog } from "./item-lifecycle-dialogs";

const noop = () => undefined;
const copy = itemLifecycleCopyFixture();
const trash = {
  copy,
  open: true,
  loading: false,
  failed: false,
  onRestore: noop,
  onClose: noop,
};

const meta: Meta = {
  title: "Work Management/Item lifecycle",
  parameters: { layout: "fullscreen" },
};
export default meta;
type Story = StoryObj;

/** Asks before an item is deleted; the way back is the board's trash. */
export const DeleteConfirm: Story = {
  render: () => (
    <DeleteItemDialog
      copy={copy}
      title={ITEM_LIFECYCLE_ITEM_TITLE}
      open
      onCancel={noop}
      onConfirm={noop}
    />
  ),
  tags: ["fui-surface--wm.item-lifecycle", "fui-state--Default"],
};

/** Picks the board the item moves to. */
export const MoveBoard: Story = {
  render: () => (
    <MoveBoardDialog
      copy={actionsCopy}
      title={ITEM_LIFECYCLE_ITEM_TITLE}
      itemKey="ERP-142"
      boards={ITEM_LIFECYCLE_BOARDS}
      open
      preview={null}
      status="idle"
      onPick={noop}
      onCancel={noop}
      onConfirm={noop}
    />
  ),
  tags: ["fui-surface--wm.item-lifecycle", "fui-state--Default"],
};

/** No other board accepts items: the dialog says so. */
export const MoveNoBoards: Story = {
  render: () => (
    <MoveBoardDialog
      copy={actionsCopy}
      title={ITEM_LIFECYCLE_ITEM_TITLE}
      itemKey="ERP-142"
      boards={[]}
      open
      preview={null}
      status="idle"
      onPick={noop}
      onCancel={noop}
      onConfirm={noop}
    />
  ),
  tags: ["fui-surface--wm.item-lifecycle", "fui-state--Empty"],
};

/** The board's deleted items with the last day each can be restored. */
export const TrashList: Story = {
  render: () => <BoardTrashSheet {...trash} rows={ITEM_LIFECYCLE_TRASH_ROWS} />,
  tags: ["fui-surface--wm.item-lifecycle", "fui-state--Default"],
};

/** Nothing has been deleted. */
export const TrashEmpty: Story = {
  render: () => <BoardTrashSheet {...trash} rows={[]} />,
  tags: ["fui-surface--wm.item-lifecycle", "fui-state--Empty"],
};

/** The trash is being read. */
export const TrashLoading: Story = {
  render: () => <BoardTrashSheet {...trash} rows={[]} loading />,
  tags: ["fui-surface--wm.item-lifecycle", "fui-state--Loading"],
};

/** The trash could not be read. */
export const TrashFailed: Story = {
  render: () => <BoardTrashSheet {...trash} rows={[]} failed />,
  tags: ["fui-surface--wm.item-lifecycle", "fui-state--Error"],
};

/** Long names truncate and an item without a key still lines up. */
export const TrashEdgeData: Story = {
  render: () => (
    <BoardTrashSheet {...trash} rows={ITEM_LIFECYCLE_TRASH_EDGE_ROWS} />
  ),
  tags: ["fui-surface--wm.item-lifecycle", "fui-state--EdgeData"],
};
