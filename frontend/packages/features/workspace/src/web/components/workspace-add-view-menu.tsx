import { useMemo, useState } from "react";
import { Plus } from "lucide-react";
import {
  Badge,
  Button,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@notrelix/ui-web";
import { workspaceViewTemplates } from "../../core/constants/view-templates";
import type {
  WorkspaceView,
  WorkspaceViewType,
} from "../../core/types/workspace";
import { createUseCreateWorkspaceView } from "../hooks/mutations/use-create-workspace-view";
import { getWorkspaceViewCreationState } from "./workspace-view-creation";

import type { WorkspaceApiClient } from "../../core";

export function WorkspaceAddViewMenu({
  workspaceId,
  boards = [],
  pages = [],
  api,
  onViewCreated,
}: {
  workspaceId: string;
  boards?: Array<{ id: string; title?: string }>;
  pages?: Array<{ id: string; title: string }>;
  api: WorkspaceApiClient;
  onViewCreated?: (view: WorkspaceView) => void;
}) {
  const [docPickerOpen, setDocPickerOpen] = useState(false);

  const createViewHook = useMemo(
    () => createUseCreateWorkspaceView({ api }),
    [api],
  );
  const createView = createViewHook(workspaceId);

  async function handleCreate(
    type: WorkspaceViewType,
    label: string,
    target: {
      boardId?: string;
      pageId?: string;
      calendarId?: string;
      dashboardId?: string;
    } = {},
  ) {
    if (createView.isPending) return;

    const view = await createView.mutateAsync({
      workspaceId,
      name: label,
      type,
      target,
    });
    onViewCreated?.(view);
  }

  return (
    <>
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button variant="ghost" size="sm" className="h-9 rounded-full px-2.5">
            <Plus className="size-4" />
            <span className="sr-only sm:not-sr-only">Add view</span>
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" className="w-[330px]">
          <DropdownMenuLabel>Add workspace view</DropdownMenuLabel>
          <DropdownMenuSeparator />
          {workspaceViewTemplates.map((template) =>
            (() => {
              const creationState = getWorkspaceViewCreationState(
                template.type,
                {
                  boards,
                  pages,
                },
              );
              return (
                <DropdownMenuItem
                  key={template.type}
                  disabled={
                    Boolean(template.badge) ||
                    (template.type === "doc"
                      ? pages.length === 0
                      : creationState.disabled)
                  }
                  onClick={() => {
                    if (template.type === "doc") {
                      setDocPickerOpen(true);
                      return;
                    }
                    void handleCreate(
                      template.type,
                      template.label,
                      creationState.target,
                    );
                  }}
                  className="items-start gap-3 py-3"
                >
                  <span className="mt-0.5 flex size-8 shrink-0 items-center justify-center rounded-lg bg-muted text-sm text-foreground">
                    {template.icon}
                  </span>
                  <span className="min-w-0 flex-1">
                    <span className="flex items-center gap-2 text-sm font-medium text-foreground">
                      {template.label}
                      {template.badge ? (
                        <Badge variant="secondary" className="rounded-full">
                          {template.badge}
                        </Badge>
                      ) : null}
                    </span>
                    <span className="mt-0.5 block text-xs leading-5 text-muted-foreground">
                      {template.type === "doc" && pages.length === 0
                        ? creationState.reason
                        : template.type === "doc"
                          ? template.description
                          : (creationState.reason ?? template.description)}
                    </span>
                  </span>
                </DropdownMenuItem>
              );
            })(),
          )}
        </DropdownMenuContent>
      </DropdownMenu>
      <Dialog open={docPickerOpen} onOpenChange={setDocPickerOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Select a document</DialogTitle>
            <DialogDescription>
              A Doc view opens an existing document in this workspace.
            </DialogDescription>
          </DialogHeader>
          <div className="grid gap-2">
            {pages.map((page) => (
              <Button
                key={page.id}
                type="button"
                variant="outline"
                className="justify-start"
                onClick={() => {
                  setDocPickerOpen(false);
                  void handleCreate("doc", page.title, { pageId: page.id });
                }}
              >
                {page.title}
              </Button>
            ))}
          </div>
        </DialogContent>
      </Dialog>
    </>
  );
}
