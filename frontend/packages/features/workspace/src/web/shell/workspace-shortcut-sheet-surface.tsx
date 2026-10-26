import * as React from "react";
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Kbd,
} from "@notrelix/ui-web";
import type { WorkspaceShellCopy } from "./workspace-shell.copy";

export interface WorkspaceShortcutSheetSurfaceProps {
  readonly copy: WorkspaceShellCopy;
  readonly open: boolean;
  readonly onOpenChange: (open: boolean) => void;
  /** Platform label of the palette chord, e.g. "⌘K" or "Ctrl K". */
  readonly commandShortcut: string;
}

interface ShortcutRow {
  readonly id: string;
  readonly label: string;
  /** Each inner array is one chord; several chords form a sequence. */
  readonly sequence: readonly string[];
}

function ShortcutKeys({
  sequence,
  then,
}: {
  sequence: readonly string[];
  then: string;
}) {
  return (
    <span className="inline-flex items-center gap-1">
      {sequence.map((key, index) => (
        <React.Fragment key={`${key}-${index}`}>
          {index > 0 ? (
            <span className="px-0.5 text-metadata text-muted-foreground">
              {then}
            </span>
          ) : null}
          <Kbd>{key}</Kbd>
        </React.Fragment>
      ))}
    </span>
  );
}

/** Discoverable list of the shell keyboard shortcuts (WX-SHELL-005). */
export function WorkspaceShortcutSheetSurface({
  copy,
  open,
  onOpenChange,
  commandShortcut,
}: WorkspaceShortcutSheetSurfaceProps) {
  const groups: { id: string; title: string; rows: ShortcutRow[] }[] = [
    {
      id: "navigation",
      title: copy.shortcuts.navigation,
      rows: [
        { id: "home", label: copy.shortcuts.goHome, sequence: ["G", "H"] },
        { id: "my-work", label: copy.shortcuts.goMyWork, sequence: ["G", "M"] },
        { id: "inbox", label: copy.shortcuts.goInbox, sequence: ["G", "I"] },
      ],
    },
    {
      id: "boards",
      title: copy.shortcuts.boards,
      rows: [
        { id: "add-item", label: copy.shortcuts.addItem, sequence: ["C"] },
        { id: "quick-add", label: copy.shortcuts.quickAdd, sequence: ["N"] },
        {
          id: "move-card",
          label: copy.shortcuts.moveCard,
          sequence: ["Alt →"],
        },
        { id: "assign-me", label: copy.shortcuts.assignMe, sequence: ["A"] },
        { id: "set-due", label: copy.shortcuts.setDue, sequence: ["D"] },
      ],
    },
    {
      id: "general",
      title: copy.shortcuts.general,
      rows: [
        {
          id: "palette",
          label: copy.shortcuts.openPalette,
          sequence: [commandShortcut],
        },
        {
          id: "shortcuts",
          label: copy.shortcuts.showShortcuts,
          sequence: ["?"],
        },
      ],
    },
  ];

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent showCloseButton={false} className="gap-5 sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="text-section-heading">
            {copy.shortcuts.title}
          </DialogTitle>
          <DialogDescription className="text-body text-muted-foreground">
            {copy.shortcuts.description}
          </DialogDescription>
        </DialogHeader>
        {groups.map((group) => (
          <section
            key={group.id}
            aria-labelledby={`shortcut-group-${group.id}`}
          >
            <h3
              id={`shortcut-group-${group.id}`}
              className="mb-1 text-metadata font-medium text-muted-foreground"
            >
              {group.title}
            </h3>
            <dl className="divide-y divide-border">
              {group.rows.map((row) => (
                <div
                  key={row.id}
                  className="flex items-center justify-between gap-4 py-2"
                >
                  <dt className="text-body text-foreground">{row.label}</dt>
                  <dd>
                    <ShortcutKeys
                      sequence={row.sequence}
                      then={copy.shortcuts.then}
                    />
                  </dd>
                </div>
              ))}
            </dl>
          </section>
        ))}
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            {copy.shortcuts.close}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
