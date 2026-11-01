import * as React from "react";
import {
  Bell,
  LayoutGrid,
  Palette,
  Shield,
  User,
  type LucideIcon,
} from "lucide-react";
import { cn } from "@notrelix/ui-web";
import type { AccountNavCopy } from "../nav/account-nav.copy";
import { Avatar, FOCUS } from "../shared/account-parts";

/**
 * The frame of the account pages (Final-Account): the head with the member's
 * avatar and email, the left menu of the five pages and the page beside it.
 * It is built like the frame of "Thiết lập workspace".
 */

export type AccountTab =
  "profile" | "appearance" | "security" | "notifications" | "workspaces";

export interface AccountFrameLink {
  readonly href: string;
  readonly onNavigate: () => void;
}

export interface AccountFrameProps {
  readonly copy: AccountNavCopy;
  readonly identity: {
    readonly name: string;
    readonly email: string;
    readonly avatarUrl: string | null;
  };
  readonly active: AccountTab;
  readonly links: Record<AccountTab, AccountFrameLink>;
  readonly children: React.ReactNode;
}

const ICONS: Record<AccountTab, LucideIcon> = {
  profile: User,
  appearance: Palette,
  security: Shield,
  notifications: Bell,
  workspaces: LayoutGrid,
};

const GROUPS: readonly (readonly AccountTab[])[] = [
  ["profile", "appearance", "security", "notifications"],
  ["workspaces"],
];

function isPlainClick(event: React.MouseEvent): boolean {
  return (
    event.button === 0 &&
    !event.metaKey &&
    !event.ctrlKey &&
    !event.shiftKey &&
    !event.altKey
  );
}

export function AccountFrame({
  copy,
  identity,
  active,
  links,
  children,
}: AccountFrameProps) {
  return (
    <div data-f="page" className="relative flex h-full min-h-0 flex-col">
      <header
        data-f="head"
        className="border-b border-line px-7 pt-[18px] pb-4"
      >
        <div className="flex items-center gap-3.5">
          <Avatar
            name={identity.name}
            url={identity.avatarUrl}
            className="size-10 text-nav"
          />
          <div className="flex min-w-0 flex-col">
            <small className="truncate text-crumb leading-[1.45] text-meta-foreground">
              {identity.name} · {identity.email}
            </small>
            <h1 className="m-0 text-[22px] leading-tight font-[650] tracking-[-0.015em] text-foreground">
              {copy.title}
            </h1>
          </div>
        </div>
      </header>
      <div
        data-f="body"
        className="flex min-h-0 flex-1 overflow-hidden max-md:flex-col"
      >
        <nav
          data-f="snav"
          aria-label={copy.label}
          className="box-border flex w-[212px] shrink-0 flex-col gap-0.5 border-r border-line px-3 py-4 max-md:w-full max-md:flex-row max-md:gap-1 max-md:flex-wrap max-md:border-r-0 max-md:border-b max-md:px-4 max-md:py-2"
        >
          {GROUPS.map((group, index) => (
            <React.Fragment key={index}>
              {index > 0 ? (
                <div
                  data-f="sn-sep"
                  className="mx-1.5 my-2 h-px bg-line max-md:hidden"
                  role="separator"
                />
              ) : null}
              {group.map((tab) => {
                const Icon = ICONS[tab];
                const on = tab === active;
                return (
                  <a
                    key={tab}
                    data-f="sn"
                    href={links[tab].href}
                    aria-current={on ? "page" : undefined}
                    onClick={(event) => {
                      if (!isPlainClick(event)) return;
                      event.preventDefault();
                      links[tab].onNavigate();
                    }}
                    className={cn(
                      "flex h-[34px] shrink-0 items-center gap-[9px] rounded-[7px] px-2.5 text-nav leading-[1.45] font-medium text-muted-foreground hover:bg-state-hover-strong hover:text-foreground",
                      on &&
                        "bg-popover font-semibold text-foreground ring-1 ring-line hover:bg-popover dark:bg-card dark:hover:bg-card",
                      FOCUS,
                    )}
                  >
                    <Icon
                      aria-hidden="true"
                      strokeWidth={1.8}
                      className="size-4 shrink-0 text-meta-foreground"
                    />
                    {copy[tab]}
                  </a>
                );
              })}
            </React.Fragment>
          ))}
        </nav>
        <div
          data-f="content"
          className="flex min-w-0 flex-1 flex-col gap-4 overflow-auto px-7 pt-[22px] pb-8 max-md:px-4"
        >
          {children}
        </div>
      </div>
    </div>
  );
}
