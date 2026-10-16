import * as React from "react";
import { cn } from "@notrelix/ui-web";
import { markToneFor, type MarkToneId } from "../../core/model/navigation";
import { IdentityMark } from "../shell/shell-link";
import type { IdentityTone } from "../../core/model/navigation";
import { IDENTITY_MARK_CLASS } from "../shell/shell-models";
import { teamInitials } from "./workspace-teams-models";
import { isPlainClick } from "../shell/shell-models";
import { FOCUS, Glyph, type GlyphName } from "../spaces/space-page-parts";
import type { WorkspaceTeamsCopy } from "./workspace-teams.copy";

/**
 * The shared frame of "Thiết lập workspace" (Final-Teams): the head with the
 * workspace mark, the left menu of the setup sections and the content beside
 * it. Teams and Sharing and security use it today; the pages of General,
 * Members, Plans and billing and Integrations take it over as they are built.
 */

export type SetupSection =
  | "general"
  | "members"
  | "teams"
  | "departments"
  | "sharing"
  | "integrations"
  | "billing";

export interface SetupFrameLink {
  readonly href: string;
  readonly onNavigate: () => void;
}

export interface SetupFrameProps {
  readonly workspaceName: string;
  readonly workspaceId: string;
  readonly active: SetupSection;
  /** Counts beside the sections; `null` leaves a count out. */
  readonly counts: Partial<Record<SetupSection, number | null>>;
  /** Where each section lives; `null` = no page yet, drawn inert. */
  readonly links: Record<SetupSection, SetupFrameLink | null>;
}

const ICONS: Record<SetupSection, GlyphName> = {
  general: "gear",
  members: "person",
  teams: "team",
  departments: "grid",
  sharing: "shield",
  integrations: "plug",
  billing: "card",
};

const GROUPS: readonly (readonly SetupSection[])[] = [
  ["general", "members", "teams", "departments"],
  ["sharing", "integrations", "billing"],
];

export function WorkspaceSetupFrame({
  copy,
  frame,
  hideNav = false,
  children,
}: {
  copy: WorkspaceTeamsCopy;
  frame: SetupFrameProps;
  /** A member who cannot manage the workspace sees the page without the menu. */
  hideNav?: boolean;
  children: React.ReactNode;
}) {
  const markTone: MarkToneId = markToneFor(frame.workspaceId);
  return (
    <div data-f="page" className="flex h-full min-h-0 flex-col">
      <header
        data-f="head"
        className="border-b border-line px-7 pt-[18px] pb-4"
      >
        <div className="flex items-center gap-3.5">
          <IdentityMark
            name={frame.workspaceName}
            markTone={markTone}
            className="size-10 rounded-[11px] text-body"
          />
          <div className="flex flex-col">
            <small className="text-crumb leading-[1.45] text-meta-foreground">
              {frame.workspaceName}
            </small>
            <h1 className="m-0 text-list-title text-foreground">
              {copy.setup.title}
            </h1>
          </div>
        </div>
      </header>
      <div data-f="body" className="flex min-h-0 flex-1 overflow-hidden">
        {hideNav ? null : (
          <nav
            data-f="snav"
            aria-label={copy.setup.label}
            className="box-border flex w-[212px] shrink-0 flex-col gap-0.5 border-r border-line px-3 py-4"
          >
            {GROUPS.map((group, index) => (
              <React.Fragment key={index}>
                {index > 0 ? (
                  <div
                    data-f="sn-sep"
                    className="mx-1.5 my-2 h-px bg-line"
                    role="separator"
                  />
                ) : null}
                {group.map((section) => {
                  const link = frame.links[section];
                  const on = section === frame.active;
                  const count = frame.counts[section];
                  const className = cn(
                    "flex h-[34px] items-center gap-[9px] rounded-[7px] px-2.5 text-nav leading-[1.45] font-medium text-muted-foreground hover:bg-state-hover-strong hover:text-foreground",
                    on &&
                      "bg-popover font-semibold text-foreground ring-1 ring-line hover:bg-popover dark:bg-card dark:hover:bg-card",
                    FOCUS,
                  );
                  const body = (
                    <>
                      <Glyph
                        name={ICONS[section]}
                        className="size-[15px] text-meta-foreground"
                      />
                      {copy.setup.items[section]}
                      {count !== undefined && count !== null ? (
                        <span className="ml-auto text-xs font-medium text-meta-foreground">
                          {count}
                        </span>
                      ) : null}
                    </>
                  );
                  return link ? (
                    <a
                      key={section}
                      data-f="sn"
                      href={link.href}
                      aria-current={on ? "page" : undefined}
                      onClick={(event) => {
                        if (!isPlainClick(event)) return;
                        event.preventDefault();
                        link.onNavigate();
                      }}
                      className={className}
                    >
                      {body}
                    </a>
                  ) : (
                    <span
                      key={section}
                      data-f="sn"
                      aria-disabled="true"
                      className={cn(className, "cursor-default opacity-70")}
                    >
                      {body}
                    </span>
                  );
                })}
              </React.Fragment>
            ))}
          </nav>
        )}
        <div
          data-f="content"
          className="relative flex min-w-0 flex-1 flex-col gap-4 overflow-auto px-7 pt-[22px] pb-8"
        >
          {children}
        </div>
      </div>
    </div>
  );
}

/** A Team's tile: the first letters of its name on an identity colour. */
export function TeamTile({
  name,
  tone,
  className,
}: {
  name: string;
  tone: IdentityTone;
  className?: string;
}) {
  return (
    <span
      aria-hidden="true"
      className={cn(
        "grid shrink-0 place-items-center font-bold tracking-[0.02em]",
        IDENTITY_MARK_CLASS[tone],
        className,
      )}
    >
      {teamInitials(name)}
    </span>
  );
}
