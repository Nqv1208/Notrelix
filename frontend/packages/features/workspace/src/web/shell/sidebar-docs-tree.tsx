import * as React from "react";
import { ChevronDown } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import { markInitialsFor } from "../../core/model/navigation";
import { ShellLink } from "./shell-link";
import {
  MARK_TONE_CLASS,
  formatCopy,
  type ShellNavigation,
  type SidebarDocPage,
  type SidebarDocsTree,
} from "./shell-models";
import type { WorkspaceShellCopy } from "./workspace-shell.copy";

export interface SidebarDocsTreeNavProps {
  readonly copy: WorkspaceShellCopy;
  readonly navigation: ShellNavigation;
  readonly tree: SidebarDocsTree;
  readonly onNavigated?: () => void;
}

/**
 * Start padding (px) of a row, from Final-Docs `.side .tree`: departments
 * 28, a page with children 46 and 18 per further level, a leaf page 71 (51
 * directly under a department) and 20 per further level.
 */
function rowInset(depth: number, leaf: boolean): number {
  if (depth === 1) return 28;
  return leaf ? 51 + (depth - 2) * 20 : 28 + (depth - 1) * 18;
}

const ROW =
  "flex h-7.5 shrink-0 items-center gap-[7px] rounded-[7px] pr-2 text-compact leading-[1.45] text-muted-foreground hover:bg-state-hover hover:text-foreground";

function Toggle({
  label,
  expanded,
  onClick,
}: {
  label: string;
  expanded: boolean;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      aria-expanded={expanded}
      aria-label={label}
      onClick={onClick}
      className="-ml-0.5 grid size-[18px] shrink-0 place-items-center rounded-[4px] p-0 text-inherit outline-none hover:bg-line-strong focus-visible:ring-2 focus-visible:ring-focus-ring"
    >
      <ChevronDown
        aria-hidden="true"
        className={cn(
          "size-3.5 stroke-[1.8] transition-transform",
          !expanded && "-rotate-90",
        )}
      />
    </button>
  );
}

function PageRows({
  pages,
  depth,
  props,
}: {
  pages: readonly SidebarDocPage[];
  depth: number;
  props: SidebarDocsTreeNavProps;
}): React.ReactNode {
  const { copy, navigation, tree, onNavigated } = props;
  return pages.map((page) => {
    const leaf = page.children.length === 0;
    const open = tree.expandedIds.has(page.id);
    const active = tree.activeDocId === page.id;
    const destination = { section: "doc", docId: page.id } as const;
    return (
      <React.Fragment key={page.id}>
        <div
          className={cn(
            ROW,
            "pl-[var(--inset)]",
            active &&
              "bg-surface-panel font-semibold text-foreground shadow-[0_0_0_1px_var(--line)] hover:bg-surface-panel",
          )}
          style={
            { "--inset": `${rowInset(depth, leaf)}px` } as React.CSSProperties
          }
        >
          {leaf ? null : (
            <Toggle
              label={formatCopy(
                open ? copy.sidebar.docsCollapse : copy.sidebar.docsExpand,
                { name: page.title },
              )}
              expanded={open}
              onClick={() => tree.onExpandedChange(page.id, !open)}
            />
          )}
          <ShellLink
            destination={destination}
            navigation={navigation}
            onNavigated={onNavigated}
            aria-current={active ? "page" : undefined}
            className="flex h-full min-w-0 flex-1 items-center truncate text-inherit no-underline outline-none focus-visible:ring-2 focus-visible:ring-focus-ring"
          >
            <span className="truncate">{page.title}</span>
          </ShellLink>
        </div>
        {!leaf && open ? (
          <PageRows pages={page.children} depth={depth + 1} props={props} />
        ) : null}
      </React.Fragment>
    );
  });
}

/** The document tree under the Docs row: departments, folders and pages. */
export function SidebarDocsTreeNav(props: SidebarDocsTreeNavProps) {
  const { copy, navigation, tree, onNavigated } = props;
  return (
    <div
      role="group"
      aria-label={copy.sidebar.docsTree}
      className="flex flex-col gap-0.5"
    >
      {tree.departments.map((department) => {
        const open = tree.expandedIds.has(department.id);
        return (
          <React.Fragment key={department.id}>
            <div className={cn(ROW, "pl-7 font-medium text-foreground")}>
              <Toggle
                label={formatCopy(
                  open ? copy.sidebar.docsCollapse : copy.sidebar.docsExpand,
                  { name: department.name },
                )}
                expanded={open}
                onClick={() => tree.onExpandedChange(department.id, !open)}
              />
              <ShellLink
                destination={{ section: "space", spaceId: department.id }}
                navigation={navigation}
                onNavigated={onNavigated}
                className="flex h-full min-w-0 flex-1 items-center gap-[7px] truncate text-inherit no-underline outline-none focus-visible:ring-2 focus-visible:ring-focus-ring"
              >
                <span
                  aria-hidden="true"
                  className={cn(
                    "grid size-4 shrink-0 place-items-center rounded-[5px] text-mark-2xs font-bold",
                    MARK_TONE_CLASS[department.markTone],
                  )}
                >
                  {markInitialsFor(department.name)}
                </span>
                <span className="truncate">{department.name}</span>
              </ShellLink>
            </div>
            {open ? (
              <PageRows pages={department.pages} depth={2} props={props} />
            ) : null}
          </React.Fragment>
        );
      })}
    </div>
  );
}
