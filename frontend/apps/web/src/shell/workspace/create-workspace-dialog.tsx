import { useId, useMemo, useState, type FormEvent } from "react";
import {
  createUseCreateWorkspace,
  type WorkspaceShellCopy,
} from "@notrelix/features-workspace/web";
import type { WorkspaceSummary } from "@notrelix/features-workspace/core";
import { useAppRuntime } from "@notrelix/runtime-web";
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Input,
  Label,
} from "@notrelix/ui-web";

function slugFor(name: string): string {
  return name
    .toLowerCase()
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .replace(/đ/g, "d")
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/(^-|-$)/g, "");
}

/** The one create-workspace dialog (replaces the two switcher copies). */
export function CreateWorkspaceDialog({
  copy,
  open,
  onOpenChange,
  onCreated,
}: {
  copy: WorkspaceShellCopy;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onCreated: (workspace: WorkspaceSummary) => void;
}) {
  const { api: runtimeClient } = useAppRuntime();
  const useCreateWorkspace = useMemo(
    () =>
      createUseCreateWorkspace({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
      }),
    [runtimeClient],
  );
  const createWorkspace = useCreateWorkspace();
  const [name, setName] = useState("");
  const inputId = useId();
  const errorId = useId();
  const trimmed = name.trim();

  const submit = (event: FormEvent) => {
    event.preventDefault();
    if (!trimmed || createWorkspace.isPending) return;
    createWorkspace.mutate(
      {
        name: trimmed,
        slug: slugFor(trimmed) || "workspace",
        isPersonal: false,
      },
      {
        onSuccess: (workspace) => {
          setName("");
          onOpenChange(false);
          onCreated(workspace);
        },
      },
    );
  };

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        if (!next) createWorkspace.reset();
        onOpenChange(next);
      }}
    >
      <DialogContent showCloseButton={false} className="sm:max-w-md">
        <form onSubmit={submit} className="grid gap-5">
          <DialogHeader>
            <DialogTitle className="text-section-heading">
              {copy.createWorkspace.title}
            </DialogTitle>
            <DialogDescription className="text-body">
              {copy.createWorkspace.description}
            </DialogDescription>
          </DialogHeader>
          <div className="grid gap-2">
            <Label htmlFor={inputId}>{copy.createWorkspace.nameLabel}</Label>
            <Input
              id={inputId}
              autoFocus
              value={name}
              onChange={(event) => setName(event.target.value)}
              placeholder={copy.createWorkspace.namePlaceholder}
              aria-invalid={createWorkspace.isError || undefined}
              aria-describedby={createWorkspace.isError ? errorId : undefined}
              maxLength={100}
            />
            {createWorkspace.isError ? (
              <p
                id={errorId}
                role="alert"
                className="text-metadata text-danger-text"
              >
                {copy.createWorkspace.error}
              </p>
            ) : null}
          </div>
          <DialogFooter>
            <Button
              type="button"
              variant="ghost"
              onClick={() => onOpenChange(false)}
            >
              {copy.createWorkspace.cancel}
            </Button>
            <Button
              type="submit"
              disabled={!trimmed || createWorkspace.isPending}
            >
              {createWorkspace.isPending
                ? copy.createWorkspace.submitting
                : copy.createWorkspace.submit}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
