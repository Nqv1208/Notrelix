import { CircleAlert, CircleCheck, Info, TriangleAlert } from "lucide-react";
import type { ReactNode } from "react";
import { Skeleton, cn } from "@notrelix/ui-web";
import type { BoardLifecycleCopy } from "./board-lifecycle.copy";
import { fillLifecycleCopy, pluralLifecycle } from "./board-lifecycle.copy";

/** What moving the board changes, as the server read it (`GetMoveImpact`). */
export interface BoardMoveImpact {
  readonly unchanged: boolean;
  readonly targetName: string | null;
  readonly losingCount: number | null;
  readonly everyoneGains: boolean;
  readonly everyoneLoses: boolean;
  readonly gainingCount: number;
  readonly stranded: readonly {
    readonly userId: string;
    readonly name: string;
    readonly items: number;
    readonly overdue: number;
  }[];
  readonly strandedCount: number;
  readonly strandedItems: number;
  readonly strandedOverdue: number;
  readonly keptNames: readonly string[];
  readonly keptCount: number;
}

const initials = (name: string) =>
  name
    .split(/\s+/)
    .filter(Boolean)
    .slice(-2)
    .map((part) => part[0]!.toUpperCase())
    .join("");

/**
 * The access impact of a move (WM-MoveBoard): who loses the board, who holds
 * unfinished items and would be left without it, who gains it, and who keeps
 * access on their own entry. Pure: the host reads the impact.
 */
export function BoardMoveImpactPanel({
  copy,
  state,
  impact,
  fromName,
  targetName,
  onRetry,
}: {
  copy: BoardLifecycleCopy["move"];
  state: "loading" | "error" | "ready";
  impact: BoardMoveImpact | null;
  fromName: string;
  targetName: string;
  onRetry: () => void;
}) {
  if (state === "loading")
    return (
      <div
        role="status"
        aria-label={copy.loading}
        className="flex flex-col gap-2 motion-safe:animate-in motion-safe:fade-in-0"
      >
        <Skeleton className="h-14 rounded-[10px]" />
        <Skeleton className="h-14 rounded-[10px]" />
        <span className="text-crumb text-meta-foreground">{copy.loading}</span>
      </div>
    );
  if (state === "error" || !impact)
    return (
      <div
        role="alert"
        className="flex items-center gap-2 rounded-[10px] bg-danger-subtle px-3 py-2.5 text-crumb text-danger-text"
      >
        <CircleAlert aria-hidden="true" className="size-4 shrink-0" />
        <span className="flex-1">{copy.failed}</span>
        <button
          type="button"
          onClick={onRetry}
          className="cursor-pointer rounded-md border border-line-strong bg-popover px-2.5 py-1 text-crumb font-medium"
        >
          {copy.retry}
        </button>
      </div>
    );
  const target = impact.targetName ?? targetName;
  const stranded = impact.strandedCount > 0;
  const showRows =
    impact.everyoneLoses ||
    (impact.losingCount ?? 0) > 0 ||
    impact.everyoneGains ||
    impact.gainingCount > 0;
  const kept =
    impact.keptNames.length === 0
      ? null
      : impact.keptCount > impact.keptNames.length
        ? fillLifecycleCopy(copy.keptMore, {
            names: impact.keptNames.join(", "),
            count: impact.keptCount - impact.keptNames.length,
          })
        : impact.keptNames.join(", ");
  return (
    <div className="flex flex-col gap-2 motion-safe:animate-in motion-safe:fade-in-0 motion-safe:slide-in-from-top-1 motion-safe:duration-150">
      {!showRows ? (
        <Row tone="ok" icon={CircleCheck} title={copy.noChange} />
      ) : null}
      {impact.everyoneLoses || (impact.losingCount ?? 0) > 0 ? (
        <Row
          tone="danger"
          icon={TriangleAlert}
          title={
            impact.everyoneLoses
              ? fillLifecycleCopy(copy.losingEveryone, { target })
              : pluralLifecycle(copy, "losing", impact.losingCount ?? 0)
          }
          body={fillLifecycleCopy(
            impact.everyoneLoses ? copy.losingBodyRoot : copy.losingBody,
            { from: fromName },
          )}
        />
      ) : null}
      {showRows ? (
        stranded ? (
          <Row
            tone="warning"
            icon={TriangleAlert}
            title={pluralLifecycle(copy, "strandedTitle", impact.strandedCount)}
            body={fillLifecycleCopy(copy.strandedBody, {
              items: impact.strandedItems,
              overdue: impact.strandedOverdue,
            })}
          >
            <ul className="m-0 mt-2 flex list-none flex-col gap-1 p-0">
              {impact.stranded.map((person) => (
                <li
                  key={person.userId}
                  className="flex items-center gap-2 rounded-lg bg-popover px-2.5 py-1.5"
                >
                  <span
                    aria-hidden="true"
                    className="grid size-6 shrink-0 place-items-center rounded-full bg-state-hover-strong text-[10px] font-bold text-muted-foreground"
                  >
                    {initials(person.name)}
                  </span>
                  <span className="min-w-0 flex-1">
                    <b className="block truncate text-item-small font-semibold">
                      {person.name}
                    </b>
                    <small className="text-metadata text-meta-foreground">
                      {pluralLifecycle(copy, "stranded", person.items)}
                      {person.overdue > 0
                        ? `, ${pluralLifecycle(copy, "strandedOverdue", person.overdue)}`
                        : ""}
                    </small>
                  </span>
                  <small className="shrink-0 text-metadata text-meta-foreground">
                    {copy.noOwnAccess}
                  </small>
                </li>
              ))}
              {impact.strandedCount > impact.stranded.length ? (
                <li className="px-2.5 text-metadata text-meta-foreground">
                  {fillLifecycleCopy(copy.moreStranded, {
                    count: impact.strandedCount - impact.stranded.length,
                  })}
                </li>
              ) : null}
            </ul>
          </Row>
        ) : impact.everyoneLoses || (impact.losingCount ?? 0) > 0 ? (
          <Row
            tone="ok"
            icon={CircleCheck}
            title={copy.noneStranded}
            body={copy.noneStrandedBody}
          />
        ) : null
      ) : null}
      {impact.everyoneGains || impact.gainingCount > 0 ? (
        <Row
          tone="info"
          icon={Info}
          title={
            impact.everyoneGains
              ? copy.gainingEveryone
              : pluralLifecycle(copy, "gaining", impact.gainingCount)
          }
          body={
            impact.everyoneGains
              ? undefined
              : fillLifecycleCopy(copy.gainingBody, { target })
          }
        />
      ) : null}
      {kept ? (
        <Row
          tone="neutral"
          icon={Info}
          title={copy.kept}
          body={fillLifecycleCopy(copy.keptBody, { names: kept })}
        />
      ) : null}
    </div>
  );
}

const TONE = {
  danger: "bg-danger-subtle text-danger-text",
  warning: "bg-warning-subtle text-warning-text",
  ok: "bg-success-subtle text-success-text",
  info: "bg-brand-subtle text-brand-subtle-foreground",
  neutral: "border border-line bg-background text-muted-foreground",
} as const;

function Row({
  tone,
  icon: Icon,
  title,
  body,
  children,
}: {
  tone: keyof typeof TONE;
  icon: typeof Info;
  title: string;
  body?: string;
  children?: ReactNode;
}) {
  return (
    <div
      className={cn(
        "flex items-start gap-2.5 rounded-[10px] px-3 py-2.5 text-[13px] leading-[1.4]",
        TONE[tone],
      )}
    >
      <Icon aria-hidden="true" className="mt-0.5 size-4 shrink-0" />
      <div className="min-w-0 flex-1">
        <b className="block font-semibold text-foreground">{title}</b>
        {body ? <span className="block">{body}</span> : null}
        {children}
      </div>
    </div>
  );
}
