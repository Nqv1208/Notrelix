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

function Skeleton({
  className,
  style,
}: {
  className?: string;
  style?: React.CSSProperties;
}) {
  return (
    <span
      aria-hidden="true"
      style={style}
      className={cn(
        "block h-[9px] rounded-[4px] bg-state-hover-strong",
        className,
      )}
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
      <p className="m-0 text-compact leading-[1.45] font-semibold text-muted-foreground">
        {title}
      </p>
      <div
        role="img"
        aria-label={alt}
        className="flex h-[478px] overflow-hidden rounded-[14px] border border-line-strong bg-background shadow-md"
      >
        <div className="flex w-39 shrink-0 flex-col gap-0.5 border-r border-line bg-muted px-2 py-2.5 text-micro">
          <div className="flex items-start gap-[7px] px-1 pt-1 pb-2">
            <span
              aria-hidden="true"
              className="grid size-5.5 shrink-0 place-items-center rounded-[6px] bg-primary text-avatar-sm font-bold text-primary-foreground"
            >
              {company.initials}
            </span>
            <span className="min-w-0">
              <b className="block text-micro leading-[1.3] font-semibold">
                {company.name}
              </b>
              <small className="block min-h-[1.45em] truncate text-rail-label leading-[1.45] font-normal text-meta-foreground">
                {company.sub}
              </small>
            </span>
          </div>
          {RAIL_ITEMS.map(({ key, Icon }) => (
            <span
              key={key}
              className={cn(
                "flex h-6 items-center gap-[7px] rounded-[5px] px-1.5 text-micro leading-[1.45]",
                key === "home"
                  ? "bg-brand-subtle font-semibold text-brand-subtle-foreground"
                  : "font-medium text-muted-foreground",
              )}
            >
              <Icon aria-hidden="true" className="size-3 shrink-0" />
              {t.rail[key]}
            </span>
          ))}
          <b className="mx-1.5 mt-2.5 mb-[3px] text-rail-label leading-[1.45] font-semibold text-meta-foreground">
            {t.rail.departments}
          </b>
          {departments.length === 0 ? (
            <span className="mx-1.5 my-1.5 flex min-h-6 items-center rounded-[5px] border border-dashed border-line-strong px-1.5 text-rail-label leading-[1.35] font-normal text-meta-foreground">
              {t.rail.departmentsEmpty}
            </span>
          ) : (
            departments.map((department) => (
              <div key={department.name} className="flex flex-col gap-0.5">
                <span className="flex h-6 items-center gap-[7px] rounded-[5px] px-1.5 text-micro font-medium">
                  <span
                    aria-hidden="true"
                    className={cn(
                      "grid size-3.75 shrink-0 place-items-center rounded-[4px] text-mark-4xs font-bold",
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
                      "h-[22px] truncate rounded-[5px] pr-1.5 pl-7 text-micro leading-[22px]",
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
          <div className="flex items-center gap-[7px] p-1">
            <span
              aria-hidden="true"
              className={cn(
                "grid size-5 shrink-0 place-items-center rounded-full text-avatar-xs font-bold",
                user.toneClass,
              )}
            >
              {user.initials}
            </span>
            <span className="min-w-0">
              <b className="block truncate text-mark-md leading-[1.45] font-semibold">
                {user.name}
              </b>
              <small className="block text-mark-sm leading-[1.45] font-normal text-meta-foreground">
                {user.role}
              </small>
            </span>
          </div>
        </div>
        <div className="flex min-w-0 flex-1 flex-col gap-2.5 px-3.5 py-4 text-micro">
          {main.kind === "stub" ? (
            <>
              <Skeleton className="h-[13px] w-[58%]" />
              <Skeleton className="w-[40%]" />
              {[
                ["80%", "55%"],
                ["70%", "45%"],
                ["76%", "38%"],
              ].map(([first, second], index) => (
                <div
                  key={index}
                  className={cn(
                    "flex flex-col gap-[7px] rounded-[8px] border border-line bg-popover p-2.5",
                    index === 0 && "mt-1.5",
                  )}
                >
                  <Skeleton style={{ width: first }} />
                  <Skeleton style={{ width: second }} />
                </div>
              ))}
            </>
          ) : main.kind === "home" ? (
            <>
              <b className="text-body leading-[1.45] font-[650]">
                {main.greeting}
              </b>
              <small className="-mt-1.5 text-rail-label leading-[1.45] font-normal text-meta-foreground">
                {main.date}
              </small>
              <div className="flex flex-col gap-1.5 rounded-[8px] border border-line bg-popover px-2.5 py-[9px]">
                <span className="flex items-center justify-between text-micro font-bold">
                  {main.startTitle}
                  <span className="text-rail-label font-normal text-meta-foreground">
                    0/3
                  </span>
                </span>
                <span className="h-1.25 rounded-[3px] bg-state-hover-strong" />
                {main.items.map((item) => (
                  <span
                    key={item}
                    className="flex items-center gap-[7px] text-micro"
                  >
                    <span
                      aria-hidden="true"
                      className="size-3 shrink-0 rounded-full border-[1.5px] border-line-strong"
                    />
                    {item}
                  </span>
                ))}
              </div>
              <div className="flex flex-col gap-[7px] rounded-[8px] border border-line bg-popover p-2.5">
                <Skeleton className="w-[70%]" />
                <Skeleton className="w-[45%]" />
              </div>
            </>
          ) : (
            <>
              <div className="flex items-center gap-2">
                <b className="text-item-small leading-[1.45] font-[650]">
                  {main.title}
                </b>
                <span className="ml-auto rounded-[6px] bg-primary px-[9px] py-1 text-rail-label leading-[1.45] font-semibold text-primary-foreground">
                  {t.join.add}
                </span>
              </div>
              <div className="flex gap-3 border-b border-line text-mark-md leading-[1.45] font-normal text-meta-foreground">
                <span className="pb-[5px]">{t.join.views.table}</span>
                <span className="pb-[5px] font-semibold text-foreground shadow-[inset_0_-2px_0_0_var(--color-primary)]">
                  {t.join.views.kanban}
                </span>
                <span className="pb-[5px]">{t.join.views.calendar}</span>
              </div>
              {main.lanes.map((lane, index) => (
                <div key={lane.label} className="contents">
                  <span className="flex items-center gap-1.5 text-rail-label leading-[1.45] font-semibold text-muted-foreground">
                    <span
                      aria-hidden="true"
                      className={cn(
                        "size-[7px] shrink-0 rounded-full",
                        index === 0
                          ? "bg-work-status-doing"
                          : "bg-work-status-review",
                      )}
                    />
                    {lane.label}
                  </span>
                  <div className="flex flex-col gap-1.5 rounded-[8px] border border-line bg-popover px-2.5 py-[9px]">
                    <b className="text-micro leading-[1.45] font-medium">
                      {lane.card}
                    </b>
                    <span className="flex items-center gap-[7px] text-mark-sm leading-[1.45] font-normal text-meta-foreground">
                      {lane.late ? (
                        <span className="inline-flex items-center gap-1 rounded-[4px] bg-danger-subtle px-1.5 py-px font-semibold text-danger-text">
                          {lane.late}
                        </span>
                      ) : null}
                      {lane.meta.split(" · ").map((part, at, parts) => (
                        <span
                          key={part}
                          className={
                            at === parts.length - 1 ? "font-mono" : undefined
                          }
                        >
                          {part}
                        </span>
                      ))}
                    </span>
                  </div>
                </div>
              ))}
            </>
          )}
        </div>
      </div>
      <p className="m-0 text-compact leading-[1.55] text-muted-foreground">
        {caption}
      </p>
    </>
  );
}
