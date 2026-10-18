import { CheckSquare, FileText, Home, Inbox, Lock } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import type { OnboardingCopy } from "./onboarding.copy";
import {
  DEPARTMENT_TONE_CLASS,
  departmentInitials,
  type DepartmentColor,
} from "./onboarding-models";

/**
 * The small picture of the product beside the form (the Onboarding mockup):
 * a sidebar and a main area that change as the setup goes. Pure and static:
 * it draws what it is given, it does not read the page.
 */
export interface PreviewDepartment {
  readonly name: string;
  readonly color: DepartmentColor;
  readonly locked: boolean;
  readonly boards: readonly {
    readonly name: string;
    readonly isNew: boolean;
  }[];
}

export interface PreviewCompany {
  readonly name: string;
  readonly initials: string;
  readonly sub: string;
}

export interface PreviewUser {
  readonly name: string;
  readonly initials: string;
  readonly role: string;
  readonly toneClass: string;
}

export type PreviewMain =
  | { readonly kind: "stub" }
  | {
      readonly kind: "home";
      readonly greeting: string;
      readonly date: string;
      readonly startTitle: string;
      readonly items: readonly string[];
    }
  | {
      readonly kind: "board";
      readonly title: string;
      readonly lanes: readonly {
        readonly label: string;
        readonly card: string;
        readonly meta: string;
        readonly late?: string;
      }[];
    };

export interface OnboardingPreviewProps {
  readonly copy: OnboardingCopy;
  readonly title: string;
  readonly caption: string;
  readonly alt: string;
  readonly company: PreviewCompany;
  readonly departments: readonly PreviewDepartment[];
  readonly user: PreviewUser;
  readonly main: PreviewMain;
  /** Which board of the sidebar is the open one. */
  readonly activeBoard?: string | null;
}

const RAIL_ITEMS = [
  { key: "home", Icon: Home },
  { key: "myWork", Icon: CheckSquare },
  { key: "inbox", Icon: Inbox },
  { key: "docs", Icon: FileText },
] as const;

function Skeleton({ className }: { className: string }) {
  return (
    <span
      aria-hidden="true"
      className={cn("block rounded-full bg-state-hover-strong", className)}
    />
  );
}

export function OnboardingPreview({
  copy,
  title,
  caption,
  alt,
  company,
  departments,
  user,
  main,
  activeBoard,
}: OnboardingPreviewProps) {
  const t = copy.preview;
  return (
    <>
      <p className="m-0 text-item-ui font-semibold text-muted-foreground">
        {title}
      </p>
      <div
        role="img"
        aria-label={alt}
        className="flex h-119 overflow-hidden rounded-[14px] border border-line bg-popover shadow-md"
      >
        <div className="flex w-39 shrink-0 flex-col gap-1 border-r border-line bg-muted px-3 py-3">
          <div className="mb-2 flex items-start gap-2">
            <span
              aria-hidden="true"
              className="mt-0.5 grid size-5.5 shrink-0 place-items-center rounded-[6px] bg-primary text-mark-xs font-bold text-primary-foreground"
            >
              {company.initials}
            </span>
            <span className="min-w-0">
              <b className="block text-note-small leading-tight font-semibold">
                {company.name}
              </b>
              <small className="block text-note-small text-meta-foreground">
                {company.sub}
              </small>
            </span>
          </div>
          {RAIL_ITEMS.map(({ key, Icon }) => (
            <span
              key={key}
              className={cn(
                "flex items-center gap-2 rounded-[6px] px-1.5 py-1 text-micro",
                key === "home"
                  ? "bg-brand-subtle font-semibold text-brand-subtle-foreground"
                  : "text-foreground",
              )}
            >
              <Icon aria-hidden="true" className="size-3 shrink-0" />
              {t.rail[key]}
            </span>
          ))}
          <b className="mt-3 px-1.5 text-note-small font-semibold text-muted-foreground">
            {t.rail.departments}
          </b>
          {departments.length === 0 ? (
            <span className="rounded-[6px] border border-dashed border-line-strong px-2 py-1 text-note-small text-meta-foreground">
              {t.rail.departmentsEmpty}
            </span>
          ) : (
            departments.map((department) => (
              <div key={department.name} className="flex flex-col gap-0.5">
                <span className="flex items-center gap-2 px-1.5 py-0.5 text-micro font-medium">
                  <span
                    aria-hidden="true"
                    className={cn(
                      "grid size-3.5 shrink-0 place-items-center rounded-[4px] text-mark-2xs font-bold",
                      DEPARTMENT_TONE_CLASS[department.color],
                    )}
                  >
                    {departmentInitials(department.name)}
                  </span>
                  <span className="min-w-0 flex-1 truncate">
                    {department.name}
                  </span>
                  {department.locked ? (
                    <Lock
                      aria-hidden="true"
                      className="size-2.5 shrink-0 text-meta-foreground"
                    />
                  ) : null}
                </span>
                {department.boards.map((board) => (
                  <span
                    key={board.name}
                    className={cn(
                      "truncate rounded-[6px] py-0.5 pr-1.5 pl-6.5 text-micro",
                      board.isNew || board.name === activeBoard
                        ? "bg-brand-subtle font-semibold text-brand-subtle-foreground"
                        : "text-muted-foreground",
                    )}
                  >
                    {board.name}
                  </span>
                ))}
              </div>
            ))
          )}
          <span className="flex-1" />
          <div className="flex items-center gap-2">
            <span
              aria-hidden="true"
              className={cn(
                "grid size-5.5 shrink-0 place-items-center rounded-full text-mark-xs font-bold",
                user.toneClass,
              )}
            >
              {user.initials}
            </span>
            <span className="min-w-0">
              <b className="block truncate text-note-small font-semibold">
                {user.name}
              </b>
              <small className="block text-note-small text-meta-foreground">
                {user.role}
              </small>
            </span>
          </div>
        </div>
        <div className="flex min-w-0 flex-1 flex-col gap-2.5 p-4">
          {main.kind === "stub" ? (
            <>
              <Skeleton className="h-2.5 w-24" />
              <Skeleton className="h-2 w-16" />
              {[0, 1, 2].map((index) => (
                <div
                  key={index}
                  className="flex flex-col gap-1.5 rounded-[8px] border border-line p-2.5"
                >
                  <Skeleton className="h-1.75 w-4/5" />
                  <Skeleton className="h-1.75 w-3/5" />
                </div>
              ))}
            </>
          ) : main.kind === "home" ? (
            <>
              <b className="text-dialog-title font-semibold">{main.greeting}</b>
              <small className="text-note-small text-meta-foreground">
                {main.date}
              </small>
              <div className="flex flex-col gap-1.5 rounded-[8px] border border-line p-2.5">
                <span className="flex items-baseline justify-between text-micro font-semibold">
                  {main.startTitle}
                  <span className="font-normal text-meta-foreground">0/3</span>
                </span>
                <span className="h-0.75 rounded-full bg-state-hover-strong" />
                {main.items.map((item) => (
                  <span
                    key={item}
                    className="flex items-center gap-2 text-note-small"
                  >
                    <span
                      aria-hidden="true"
                      className="size-2.5 shrink-0 rounded-full border border-line-strong"
                    />
                    {item}
                  </span>
                ))}
              </div>
              <div className="flex flex-col gap-1.5 rounded-[8px] border border-line p-2.5">
                <Skeleton className="h-1.75 w-3/5" />
                <Skeleton className="h-1.75 w-2/5" />
              </div>
            </>
          ) : (
            <>
              <div className="flex items-center justify-between">
                <b className="text-dialog-title font-semibold">{main.title}</b>
                <span className="rounded-[6px] bg-primary px-2 py-0.5 text-micro font-semibold text-primary-foreground">
                  {t.join.add}
                </span>
              </div>
              <div className="flex gap-3 border-b border-line text-micro text-muted-foreground">
                <span className="pb-1">{t.join.views.table}</span>
                <span className="border-b-2 border-primary pb-1 font-semibold text-foreground">
                  {t.join.views.kanban}
                </span>
                <span className="pb-1">{t.join.views.calendar}</span>
              </div>
              {main.lanes.map((lane) => (
                <div key={lane.label} className="flex flex-col gap-1.5">
                  <span className="flex items-center gap-1.5 text-micro font-semibold">
                    <span
                      aria-hidden="true"
                      className="size-1.5 rounded-full bg-primary"
                    />
                    {lane.label}
                  </span>
                  <div className="flex flex-col gap-1 rounded-[8px] border border-line p-2">
                    <span className="text-micro leading-snug font-medium">
                      {lane.card}
                    </span>
                    <span className="flex items-center gap-2 text-note-small text-meta-foreground">
                      {lane.late ? (
                        <span className="rounded-[4px] bg-danger-subtle px-1.5 py-0.5 font-semibold text-danger-subtle-foreground">
                          {lane.late}
                        </span>
                      ) : null}
                      {lane.meta}
                    </span>
                  </div>
                </div>
              ))}
            </>
          )}
        </div>
      </div>
      <p className="m-0 text-item-prose leading-normal text-muted-foreground">
        {caption}
      </p>
    </>
  );
}
