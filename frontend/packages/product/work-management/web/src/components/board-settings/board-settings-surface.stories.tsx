import { useState } from "react";
import type { Meta, StoryObj } from "@storybook/react";
import { BoardSettingsSurface } from "./board-settings-surface";
import { boardSettingsCopy } from "./board-settings.copy";
import type {
  BoardSettingsSection,
  BoardSettingsSurfaceProps,
} from "./board-settings-types";
import {
  ERP_SETTINGS_TIME_ZONE,
  emptyBoardSettings,
  erpBoardSettings,
  erpSettingsSpace,
  noopSettingsActions,
} from "../../verification/board-settings-fixtures";

const meta = {
  title: "Work Management/Board Settings/Surface",
  component: BoardSettingsSurface,
  parameters: { layout: "fullscreen" },
} satisfies Meta<typeof BoardSettingsSurface>;
export default meta;
type Story = StoryObj;

const copy = boardSettingsCopy("vi");

/**
 * Stateful story harness at the main-area size of the mockup (1108 x 900, the
 * rail and the sidebar pinned). Selection and sections apply locally so the
 * interactions are visible; writes answer ok and change nothing.
 */
function SettingsStory({
  status = "ready",
  initialSection = "fields",
  initialField = null,
  initialView = null,
  settings = erpBoardSettings(),
  ...rest
}: {
  status?: BoardSettingsSurfaceProps["status"];
  initialSection?: BoardSettingsSection;
  initialField?: string | null;
  initialView?: string | null;
  settings?: BoardSettingsSurfaceProps["settings"];
} & Partial<BoardSettingsSurfaceProps>) {
  const [section, setSection] = useState(initialSection);
  const [fieldId, setFieldId] = useState(initialField);
  const [viewId, setViewId] = useState(initialView);
  return (
    <div className="h-225 w-277 overflow-hidden">
      <BoardSettingsSurface
        status={status}
        settings={status === "ready" ? settings : null}
        copy={copy}
        boardTitle="Triển khai ERP Q4"
        space={erpSettingsSpace}
        askName="Lê Hoàng Phúc"
        timeZone={ERP_SETTINGS_TIME_ZONE}
        section={section}
        onSelectSection={setSection}
        fieldId={fieldId}
        onSelectField={setFieldId}
        viewId={viewId}
        onSelectView={setViewId}
        actions={noopSettingsActions()}
        onReload={() => undefined}
        onBack={() => undefined}
        onOpenAccess={() => undefined}
        {...rest}
      />
    </div>
  );
}

/** The fields of the board with the priority field open for editing (truong). */
export const Default: Story = {
  render: () => <SettingsStory initialField="sf-priority" />,
  tags: ["fui-surface--wm.board-settings", "fui-state--Default"],
};

/** The field type picker under "Thêm trường" (them-truong). */
export const FieldTypePicker: Story = {
  render: () => <SettingsStory initialField="sf-priority" defaultPickerOpen />,
  tags: ["fui-surface--wm.board-settings", "fui-state--Default"],
};

/** What archiving the priority field touches (luu-tru). */
export const ArchiveField: Story = {
  render: () => (
    <SettingsStory
      initialField="sf-priority"
      defaultArchiveFieldId="sf-priority"
    />
  ),
  tags: ["fui-surface--wm.board-settings", "fui-state--Default"],
};

/** The saved views with the Kanban view open (che-do-xem). */
export const Views: Story = {
  render: () => (
    <SettingsStory initialSection="views" initialView="sv-kanban" />
  ),
  tags: ["fui-surface--wm.board-settings", "fui-state--Default"],
};

/** A view whose date field was archived opens as "needs a fix" (xem-loi). */
export const ViewNeedsFix: Story = {
  render: () => (
    <SettingsStory initialSection="views" initialView="sv-handover" />
  ),
  tags: ["fui-surface--wm.board-settings", "fui-state--Default"],
};

/** Identity of the board, defaults and the danger zone (chung). */
export const General: Story = {
  render: () => <SettingsStory initialSection="general" />,
  tags: ["fui-surface--wm.board-settings", "fui-state--Default"],
};

/** The groups of the board (nhom-viec). */
export const Groups: Story = {
  render: () => <SettingsStory initialSection="groups" />,
  tags: ["fui-surface--wm.board-settings", "fui-state--Default"],
};

/** The rules of the board, running and paused (tu-dong). */
export const Automations: Story = {
  render: () => <SettingsStory initialSection="automations" />,
  tags: ["fui-surface--wm.board-settings", "fui-state--Default"],
};

export const Loading: Story = {
  render: () => <SettingsStory status="loading" />,
  tags: ["fui-surface--wm.board-settings", "fui-state--Loading"],
};

/** The settings could not be loaded. */
export const LoadError: Story = {
  render: () => <SettingsStory status="error" />,
  tags: ["fui-surface--wm.board-settings", "fui-state--Error"],
};

/** A board with no fields, one view, one group and no rule. */
export const EdgeData: Story = {
  render: () => <SettingsStory settings={emptyBoardSettings()} />,
  tags: ["fui-surface--wm.board-settings", "fui-state--EdgeData"],
};

/** A write the server refused as stale: the page asks to reload. */
export const Conflict: Story = {
  render: () => (
    <SettingsStory initialField="sf-priority" defaultFeedback="conflict" />
  ),
  tags: ["fui-surface--wm.board-settings", "fui-state--EdgeData"],
};

/** Not a manager of the board: the no-access card (khong-co-quyen). */
export const NoAccess: Story = {
  render: () => <SettingsStory status="forbidden" />,
  tags: ["fui-surface--wm.board-settings", "fui-state--ReadOnly"],
};
