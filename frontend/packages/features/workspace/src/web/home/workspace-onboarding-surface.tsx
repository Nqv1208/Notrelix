import type { LucideIcon } from "lucide-react";
import { FileText, SquareKanban, UserPlus } from "lucide-react";
import { Button } from "@notrelix/ui-web";
import { formatCopy } from "../shell/shell-models";
import type { WorkspaceHomeCopy } from "./workspace-home.copy";

export interface WorkspaceOnboardingSurfaceProps {
  readonly copy: WorkspaceHomeCopy;
  readonly workspaceName: string;
  /** Members and admins create Boards and Docs; guests cannot. */
  readonly canCreate: boolean;
  /** Only admins and owners invite. */
  readonly canInvite: boolean;
  readonly onCreateBoard: () => void;
  readonly onCreateDoc: () => void;
  readonly onInvite: () => void;
}

interface Step {
  readonly id: string;
  readonly icon: LucideIcon;
  readonly title: string;
  readonly description: string;
  readonly action: string;
  readonly onAction: () => void;
  readonly primary: boolean;
}

/**
 * Teaching empty state for a new workspace (WX-HOME-002): the three first
 * steps, each a real action. Steps the member cannot take are explained
 * instead of shown as dead buttons (DESIGN.md §16.3, §33).
 */
export function WorkspaceOnboardingSurface({
  copy,
  workspaceName,
  canCreate,
  canInvite,
  onCreateBoard,
  onCreateDoc,
  onInvite,
}: WorkspaceOnboardingSurfaceProps) {
  const steps: Step[] = [];
  if (canCreate) {
    steps.push(
      {
        id: "board",
        icon: SquareKanban,
        title: copy.onboarding.boardTitle,
        description: copy.onboarding.boardDescription,
        action: copy.onboarding.boardAction,
        onAction: onCreateBoard,
        primary: true,
      },
      {
        id: "doc",
        icon: FileText,
        title: copy.onboarding.docTitle,
        description: copy.onboarding.docDescription,
        action: copy.onboarding.docAction,
        onAction: onCreateDoc,
        primary: false,
      },
    );
  }
  if (canInvite) {
    steps.push({
      id: "invite",
      icon: UserPlus,
      title: copy.onboarding.inviteTitle,
      description: copy.onboarding.inviteDescription,
      action: copy.onboarding.inviteAction,
      onAction: onInvite,
      primary: false,
    });
  }

  return (
    <section
      aria-labelledby="workspace-onboarding-title"
      className="flex flex-col gap-4"
    >
      <div className="flex flex-col gap-1">
        <h2
          id="workspace-onboarding-title"
          className="text-section-heading text-foreground"
        >
          {formatCopy(copy.onboarding.title, { workspace: workspaceName })}
        </h2>
        <p className="max-w-prose text-body text-muted-foreground">
          {copy.onboarding.description}
        </p>
      </div>

      {steps.length > 0 ? (
        <ol className="flex flex-col divide-y divide-border border-y border-border">
          {steps.map((step, index) => (
            <li
              key={step.id}
              className="flex flex-col gap-3 py-4 sm:flex-row sm:items-center sm:gap-4"
            >
              <span className="flex items-center gap-3 sm:flex-1">
                <span
                  aria-hidden="true"
                  className="flex size-9 shrink-0 items-center justify-center rounded-md bg-brand-subtle text-brand-subtle-foreground"
                >
                  <step.icon className="size-4" />
                </span>
                <span className="flex min-w-0 flex-col">
                  <span className="text-label text-foreground">
                    <span className="sr-only">{index + 1}. </span>
                    {step.title}
                  </span>
                  <span className="text-metadata text-muted-foreground">
                    {step.description}
                  </span>
                </span>
              </span>
              <Button
                variant={step.primary ? "default" : "outline"}
                size="sm"
                onClick={step.onAction}
                className="self-start sm:self-center"
              >
                {step.action}
              </Button>
            </li>
          ))}
        </ol>
      ) : null}

      {!canCreate ? (
        <p className="text-metadata text-muted-foreground">
          {copy.onboarding.createUnavailable}
        </p>
      ) : null}
      {!canInvite && canCreate ? (
        <p className="text-metadata text-muted-foreground">
          {copy.onboarding.inviteUnavailable}
        </p>
      ) : null}
    </section>
  );
}
