import type { FieldDefinition } from "@notrelix/work-management-core";
import { useBoardView } from "@notrelix/work-management-state";
import { FaceFieldsPopover } from "./face-fields-popover";

/**
 * The "Trường hiển thị trên thẻ" button of the popup's custom fields section. It reads and writes the
 * Kanban view's own `cardFieldIds` and `cardHideEmpty`, the same setting the board's cards draw from, so the
 * popup and the board never disagree. Writing needs edit rights on the board.
 */
export function FaceFieldsButton({
  boardId,
  workspaceId,
  fields,
  canEdit,
}: {
  boardId: string;
  workspaceId: string;
  fields: readonly FieldDefinition[];
  canEdit: boolean;
}) {
  const { viewConfig, updateViewConfig } = useBoardView(boardId, workspaceId);
  return (
    <FaceFieldsPopover
      fields={fields}
      value={{
        cardFieldIds: viewConfig.cardFieldIds ?? [],
        cardHideEmpty: viewConfig.cardHideEmpty ?? [],
      }}
      canEdit={canEdit}
      onChange={(next) => updateViewConfig(next)}
    />
  );
}
