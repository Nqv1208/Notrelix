import type { WorkspaceSummary } from "@notrelix/features-workspace/core";

export type HomeResourceKind = "board" | "doc";

export interface HomeResourceItem {
  readonly id: string;
  readonly kind: HomeResourceKind;
  readonly title: string;
  readonly workspaceId: string;
  readonly workspaceName: string;
  readonly subtitle: string;
  readonly updatedLabel: string;
}

export interface HomeTaskItem {
  readonly id: string;
  readonly title: string;
  readonly workspaceId: string;
  readonly workspaceName: string;
  readonly dueLabel: string;
  readonly priority: "low" | "medium" | "high";
  readonly completed: boolean;
}

export interface HomeActivityItem {
  readonly id: string;
  readonly actor: string;
  readonly initials: string;
  readonly action: string;
  readonly target: string;
  readonly workspaceName: string;
  readonly timeLabel: string;
}

export interface HomeScenario {
  readonly userName: string;
  readonly workspaces: readonly WorkspaceSummary[];
  readonly favoriteDocs: readonly HomeResourceItem[];
  readonly continueItems: readonly HomeResourceItem[];
  readonly tasks: readonly HomeTaskItem[];
  readonly activity: readonly HomeActivityItem[];
}
