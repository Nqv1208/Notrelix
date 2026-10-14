import { useMemo } from "react";
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@notrelix/ui-web";
import { useBoardView } from "@notrelix/work-management-state";
import { resolveKanbanSettings } from "@notrelix/work-management-core";
import { KanbanSettingsPanel } from "./kanban-settings-panel";
import { useKanbanLaneCopy } from "./use-kanban-lane-copy";

export interface KanbanSettingsDialogProps {
  boardId: string;
  workspaceId: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

/**
 * The board header's "Thiết lập" on the Kanban view: the Kanban settings,
 * stored on the view's config through the same update as the table's column
 * widths (only the changed keys; the rest of the config is kept).
 */
export function KanbanSettingsDialog({
  boardId,
  workspaceId,
  open,
  onOpenChange,
}: KanbanSettingsDialogProps) {
  const copy = useKanbanLaneCopy();
  const { viewConfig, updateViewConfig } = useBoardView(boardId, workspaceId);
  const settings = useMemo(
    () => resolveKanbanSettings(viewConfig),
    [viewConfig],
  );
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent showCloseButton={false}>
        <DialogHeader>
          <DialogTitle>{copy.settingsTitle}</DialogTitle>
          <DialogDescription>{copy.settingsDescription}</DialogDescription>
        </DialogHeader>
        <KanbanSettingsPanel
          copy={copy}
          settings={settings}
          onChange={({ statusManageRoles, ...rest }) =>
            updateViewConfig(
              statusManageRoles
                ? { ...rest, statusManageRoles: [...statusManageRoles] }
                : rest,
            )
          }
        />
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            {copy.settingsClose}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
