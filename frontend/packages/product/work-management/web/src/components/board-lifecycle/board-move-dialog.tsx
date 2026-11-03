import { useState, type ReactNode } from "react";
import {
  Check,
  CircleAlert,
  CircleCheck,
  Home,
  Lock,
  RotateCw,
  TriangleAlert,
  Users,
  X,
} from "lucide-react";
import {
  cn,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
  Skeleton,
} from "@notrelix/ui-web";
import type { BoardLifecycleCopy } from "./board-lifecycle.copy";
import {
  fillLifecycleCopy as fill,
  pluralLifecycle,
} from "./board-lifecycle.copy";
import { MARK_TONE } from "../board-share/board-share-tones";

/** What moving the board changes, as the server read it (`GetMoveImpact`). */
export interface BoardMoveImpact {
  readonly unchanged: boolean;
  readonly targetName: string | null;
  /** People in the target Space, `null` when the read does not say. */
  readonly targetPeople: number | null;
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

/** One place the board can go: a Space, or the top of the workspace (`id` null). */
export interface MovePlaceView {
  readonly id: string | null;
  readonly name: string;
  readonly initials: string;
  /** A key of the Space mark colours; ignored for the workspace root. */
  readonly markTone: string;
  /** `current`: the board is here; `blocked`: the member may not create boards there. */
  readonly state: "open" | "current" | "blocked";
}

export interface BoardMoveDialogProps {
  readonly copy: BoardLifecycleCopy["move"];
  readonly open: boolean;
  readonly boardName: string;
  readonly fromName: string;
  readonly workspaceName: string;
  readonly places: readonly MovePlaceView[];
  /** `undefined` while nothing is picked; `null` is the workspace root. */
  readonly picked: string | null | undefined;
  readonly onPick: (id: string | null) => void;
  readonly impactState: "loading" | "error" | "ready";
  readonly impact: BoardMoveImpact | null;
  readonly onRetryImpact: () => void;
  readonly pending: boolean;
  /** The move was refused or did not reach the server: nothing changed. */
  readonly error: string | null;
  readonly onClose: () => void;
  readonly onSubmit: (
    target: string | null,
    addUserIds: readonly string[],
  ) => void;
}

const FOCUS = "outline-none focus-visible:ring-2 focus-visible:ring-focus-ring";
const BOX = "overflow-hidden rounded-[10px] border border-line bg-popover";
const ROW =
  "flex min-h-[48px] items-center gap-3 border-t border-line px-3.5 py-2 first:border-t-0";
const BTN =
  "inline-flex h-8 flex-none items-center justify-center gap-1.5 rounded-[7px] border border-line-strong bg-popover px-3 text-[13px] leading-[1.45] font-medium whitespace-nowrap text-foreground aria-disabled:cursor-not-allowed max-md:h-11 max-md:min-w-[120px] max-md:flex-1";
const BTN_PRIMARY =
  "border-primary bg-primary font-semibold text-primary-foreground aria-disabled:border-line-strong aria-disabled:bg-state-hover-strong aria-disabled:font-medium aria-disabled:text-meta-foreground";

const initialsOf = (name: string) =>
  name
    .split(/\s+/)
    .filter(Boolean)
    .slice(-2)
    .map((part) => part[0]!.toUpperCase())
    .join("");

function Title({ children, sub }: { children: ReactNode; sub?: ReactNode }) {
  return (
    <span className="flex min-w-0 flex-1 flex-col gap-0.5">
      <b className="text-[13.5px] leading-[1.45] font-semibold text-foreground">
        {children}
      </b>
      {sub ? (
        <small className="text-[12.5px] leading-[1.4] font-normal text-meta-foreground">
          {sub}
        </small>
      ) : null}
    </span>
  );
}

/**
 * Move a board to another department or to the top of the workspace (WM-MoveBoard): the places as one card list,
 * and under it who loses the board, who holds unfinished work and would be left without it (each can be added
 * first), who gains it and who keeps access on their own entry. Pure: the host reads the impact and writes the move.
 */
export function BoardMoveDialog(props: BoardMoveDialogProps) {
  const { copy, picked, impact, impactState, pending, error } = props;
  // `null`: nobody has been unchecked yet, so everyone who would be left behind is added.
  const [unchecked, setUnchecked] = useState<ReadonlySet<string>>(new Set());
  const stranded = impact?.stranded ?? [];
  const chosen = stranded
    .map((person) => person.userId)
    .filter((id) => !unchecked.has(id));
  const pickedPlace =
    picked === undefined
      ? null
      : (props.places.find((place) => place.id === picked) ?? null);
  const target = pickedPlace?.name ?? "";
  const ready = picked !== undefined && impactState === "ready" && !pending;
  const send = (ids: readonly string[]) => {
    if (picked === undefined || pending) return;
    props.onSubmit(picked, ids);
  };
  const strandedHere = ready && stranded.length > 0;

  return (
    <Dialog
      open={props.open}
      onOpenChange={(next) => (next || pending ? undefined : props.onClose())}
    >
      <DialogContent
        showCloseButton={false}
        overlayClassName="bg-modal-scrim backdrop-blur-none"
        data-f="mdlg"
        className="box-border flex max-h-[calc(100vh-48px)] w-[600px] max-w-[calc(100%-2rem)] flex-col gap-0 overflow-hidden rounded-[14px] border border-line bg-popover p-0 text-[14px] leading-[1.45] font-normal text-foreground shadow-dialog ring-0 sm:max-w-[600px] max-md:top-auto max-md:bottom-0 max-md:left-0 max-md:max-h-[calc(100vh-48px)] max-md:w-full max-md:max-w-none max-md:translate-x-0 max-md:translate-y-0 max-md:rounded-b-none max-md:rounded-t-[16px] max-md:border-x-0 max-md:border-b-0 max-md:data-open:zoom-in-100 max-md:data-open:slide-in-from-bottom-10 max-md:sm:max-w-none"
      >
        <div
          data-f="mh"
          className="flex shrink-0 items-center gap-2.5 px-5 pt-4 pb-2 max-md:pt-3.5 max-md:pr-2 max-md:pl-4"
        >
          <span className="min-w-0 flex-1">
            <DialogTitle
              data-f="mt"
              className="m-0 text-[17px] leading-[1.45] font-[650]"
            >
              {copy.title}
            </DialogTitle>
            <DialogDescription
              data-f="msub"
              className="m-0 mt-0.5 text-[13px] leading-[1.45] text-muted-foreground"
            >
              {fill(copy.intro, {
                name: props.boardName,
                from: props.fromName,
              })}
            </DialogDescription>
          </span>
          <button
            data-f="mclose"
            type="button"
            aria-label={copy.close}
            onClick={props.onClose}
            disabled={pending}
            className={cn(
              "grid size-8 shrink-0 place-items-center rounded-[7px] border border-transparent bg-transparent text-muted-foreground hover:bg-state-hover-strong max-md:size-11",
              FOCUS,
            )}
          >
            <X aria-hidden="true" className="size-4" strokeWidth={1.8} />
          </button>
        </div>

        <div
          data-f="mb"
          className="flex min-h-0 flex-1 flex-col gap-3.5 overflow-y-auto px-5 pt-2 pb-4 text-[13.5px] max-md:px-4 max-md:text-[14.5px] [&>*]:shrink-0"
        >
          {error ? (
            <div
              data-f="merr"
              role="alert"
              className="flex items-start gap-2.5 rounded-[10px] bg-danger-subtle px-3.5 py-2.5 text-[13px] leading-[1.4] text-danger-text"
            >
              <TriangleAlert
                aria-hidden="true"
                className="mt-0.5 size-4 shrink-0"
                strokeWidth={1.8}
              />
              <span className="flex min-w-0 flex-1 flex-col gap-px">
                <b className="font-semibold">{copy.failedTitle}</b>
                <span>{error}</span>
              </span>
            </div>
          ) : null}

          <div
            data-f="places"
            role="radiogroup"
            aria-label={copy.placeLabel}
            className={BOX}
          >
            {props.places.map((place) => {
              const checked = picked === place.id;
              const disabled = place.state !== "open" || pending;
              const sub =
                place.id === null
                  ? fill(copy.rootSub, { workspace: props.workspaceName })
                  : place.state === "current"
                    ? copy.currentSub
                    : place.state === "blocked"
                      ? copy.blockedSub
                      : checked && impact?.targetPeople != null
                        ? fill(copy.peopleIn, { count: impact.targetPeople })
                        : null;
              return (
                <button
                  key={place.id ?? "root"}
                  data-f="place"
                  type="button"
                  role="radio"
                  aria-checked={checked}
                  aria-disabled={place.state !== "open" ? true : undefined}
                  onClick={
                    disabled
                      ? undefined
                      : () => {
                          setUnchecked(new Set());
                          props.onPick(place.id);
                        }
                  }
                  className={cn(
                    ROW,
                    "w-full border-0 border-t bg-transparent text-left first:border-t-0 max-md:min-h-[52px]",
                    checked && "bg-state-selected",
                    place.state !== "open" && "cursor-not-allowed opacity-70",
                    FOCUS,
                  )}
                >
                  <span
                    aria-hidden="true"
                    className={cn(
                      "grid size-[18px] shrink-0 place-items-center rounded-full",
                      checked
                        ? "shadow-[inset_0_0_0_2px_var(--primary)]"
                        : "shadow-[inset_0_0_0_1.5px_var(--line-strong)]",
                      place.state !== "open" && "opacity-50",
                    )}
                  >
                    {checked ? (
                      <span className="size-2 rounded-full bg-primary" />
                    ) : null}
                  </span>
                  {place.id === null ? (
                    <span
                      aria-hidden="true"
                      className="grid size-7 shrink-0 place-items-center rounded-[7px] bg-state-hover-strong text-muted-foreground"
                    >
                      <Home className="size-4" strokeWidth={1.8} />
                    </span>
                  ) : (
                    <span
                      data-f="mark"
                      aria-hidden="true"
                      className={cn(
                        "grid size-7 shrink-0 place-items-center rounded-[6px] text-[11px] font-bold",
                        MARK_TONE[place.markTone],
                      )}
                    >
                      {place.initials}
                    </span>
                  )}
                  <span className="flex min-w-0 flex-1 flex-col gap-0.5">
                    <b
                      className={cn(
                        "text-[13.5px] leading-[1.45]",
                        checked ? "font-semibold" : "font-medium",
                      )}
                    >
                      {place.id === null ? copy.rootTitle : place.name}
                    </b>
                    {sub ? (
                      <small className="text-[12.5px] leading-[1.4] text-meta-foreground">
                        {sub}
                      </small>
                    ) : null}
                  </span>
                  {place.state === "current" ? (
                    <span className="inline-flex h-[22px] items-center rounded-[6px] bg-state-hover-strong px-2 text-[12px] font-semibold whitespace-nowrap text-muted-foreground">
                      {copy.currentChip}
                    </span>
                  ) : null}
                </button>
              );
            })}
          </div>

          {picked !== undefined ? (
            <div data-f="impact" className="flex flex-col gap-3">
              {impactState === "loading" ? (
                <div
                  role="status"
                  aria-label={copy.loading}
                  className="flex flex-col gap-2 motion-safe:animate-in motion-safe:fade-in-0"
                >
                  <Skeleton className="h-[74px] rounded-[10px]" />
                  <Skeleton className="h-[74px] rounded-[10px]" />
                  <span className="text-[12.5px] text-meta-foreground">
                    {copy.loading}
                  </span>
                </div>
              ) : impactState === "error" || !impact ? (
                <div
                  role="alert"
                  className="flex items-center gap-2 rounded-[10px] bg-danger-subtle px-3.5 py-2.5 text-[13px] text-danger-text"
                >
                  <CircleAlert aria-hidden="true" className="size-4 shrink-0" />
                  <span className="flex-1">{copy.failed}</span>
                  <button
                    type="button"
                    onClick={props.onRetryImpact}
                    className={cn(BTN, "h-7 max-md:h-11")}
                  >
                    {copy.retry}
                  </button>
                </div>
              ) : (
                <ImpactSections
                  copy={copy}
                  impact={impact}
                  fromName={props.fromName}
                  target={target}
                  unchecked={unchecked}
                  setUnchecked={setUnchecked}
                />
              )}
            </div>
          ) : null}
        </div>

        <div
          data-f="mf"
          className="flex shrink-0 flex-wrap items-center justify-end gap-2 border-t border-line bg-popover px-5 py-3 max-md:px-4 max-md:pb-4"
        >
          {picked === undefined && !error ? (
            <span className="min-w-0 flex-1 text-[12.5px] leading-[1.4] text-meta-foreground max-md:basis-full">
              {copy.pickHint}
            </span>
          ) : null}
          {pending ? null : (
            <button
              data-f="mcancel"
              type="button"
              onClick={props.onClose}
              className={cn(BTN, FOCUS)}
            >
              {copy.cancel}
            </button>
          )}
          {strandedHere ? (
            <button
              data-f="mwithout"
              type="button"
              onClick={() => send([])}
              className={cn(BTN, FOCUS)}
            >
              {copy.moveWithout}
            </button>
          ) : null}
          <button
            data-f="mgo"
            type="button"
            aria-disabled={
              error
                ? undefined
                : ready && (!strandedHere || chosen.length > 0)
                  ? undefined
                  : true
            }
            onClick={
              error
                ? () => send(chosen)
                : ready && (!strandedHere || chosen.length > 0)
                  ? () => send(strandedHere ? chosen : [])
                  : undefined
            }
            className={cn(BTN, BTN_PRIMARY, FOCUS)}
          >
            {error ? (
              <RotateCw
                aria-hidden="true"
                className="size-4"
                strokeWidth={1.8}
              />
            ) : null}
            {pending
              ? copy.moving
              : error
                ? copy.retry
                : strandedHere
                  ? pluralLifecycle(copy, "moveAdding", chosen.length)
                  : picked === undefined
                    ? copy.confirm
                    : fill(copy.confirmTo, { name: target })}
          </button>
        </div>
      </DialogContent>
    </Dialog>
  );
}

function Section({
  tone,
  icon,
  title,
  body,
}: {
  tone: "danger" | "ok" | "neutral";
  icon: ReactNode;
  title: string;
  body?: string;
}) {
  return (
    <div className={BOX}>
      <div
        className={cn(
          "flex items-start gap-3 px-3.5 py-2",
          tone === "danger" && "bg-danger-subtle text-danger-text",
          tone === "ok" && "bg-success-subtle text-success-text",
          tone === "neutral" && "text-muted-foreground",
        )}
      >
        <span className="mt-0.5 shrink-0">{icon}</span>
        <Title sub={body}>{title}</Title>
      </div>
    </div>
  );
}

/** A banner of the dialog: amber for unfinished work left behind, green when nobody is. */
function Banner({
  dataF,
  tone,
  title,
  body,
}: {
  dataF: string;
  tone: "warn" | "ok";
  title: string;
  body: string;
}) {
  return (
    <div
      data-f={dataF}
      role={tone === "warn" ? "alert" : "status"}
      className={cn(
        "flex items-start gap-2.5 rounded-[10px] px-3.5 py-2.5 text-[13px] leading-[1.4]",
        tone === "warn"
          ? "bg-warning-subtle text-warning-text"
          : "bg-success-subtle text-success-text",
      )}
    >
      {tone === "warn" ? (
        <TriangleAlert
          aria-hidden="true"
          className="mt-0.5 size-4 shrink-0"
          strokeWidth={1.8}
        />
      ) : (
        <Check
          aria-hidden="true"
          className="mt-0.5 size-4 shrink-0"
          strokeWidth={1.8}
        />
      )}
      <span className="flex min-w-0 flex-1 flex-col gap-px">
        <b className="text-[13.5px] font-semibold text-foreground">{title}</b>
        <span>{body}</span>
      </span>
    </div>
  );
}

function ImpactSections({
  copy,
  impact,
  fromName,
  target,
  unchecked,
  setUnchecked,
}: {
  copy: BoardLifecycleCopy["move"];
  impact: BoardMoveImpact;
  fromName: string;
  target: string;
  unchecked: ReadonlySet<string>;
  setUnchecked: (next: ReadonlySet<string>) => void;
}) {
  const loses = impact.everyoneLoses || (impact.losingCount ?? 0) > 0;
  const gains = impact.everyoneGains || impact.gainingCount > 0;
  const showRows = loses || gains;
  const stranded = impact.strandedCount > 0;
  const kept =
    impact.keptNames.length === 0
      ? null
      : impact.keptCount > impact.keptNames.length
        ? fill(copy.keptMore, {
            names: impact.keptNames.join(", "),
            count: impact.keptCount - impact.keptNames.length,
          })
        : impact.keptNames.join(", ");
  return (
    <div
      data-f="sections"
      className="flex flex-col gap-3 motion-safe:animate-in motion-safe:fade-in-0 motion-safe:slide-in-from-top-1 motion-safe:duration-150"
    >
      {!showRows ? (
        <Section
          tone="ok"
          icon={<CircleCheck className="size-4" strokeWidth={1.8} />}
          title={copy.noChange}
        />
      ) : null}
      {loses ? (
        <Section
          tone="danger"
          icon={<Users className="size-4" strokeWidth={1.8} />}
          title={
            impact.everyoneLoses
              ? fill(copy.losingEveryone, { target })
              : pluralLifecycle(copy, "losing", impact.losingCount ?? 0)
          }
          body={fill(
            impact.everyoneLoses ? copy.losingBodyRoot : copy.losingBody,
            { from: fromName },
          )}
        />
      ) : null}
      {showRows && stranded ? (
        <>
          <Banner
            dataF="warn"
            tone="warn"
            title={pluralLifecycle(copy, "strandedTitle", impact.strandedCount)}
            body={fill(copy.strandedBody, {
              items: impact.strandedItems,
              overdue: impact.strandedOverdue,
            })}
          />
          <ul
            data-f="stranded"
            aria-label={pluralLifecycle(
              copy,
              "strandedTitle",
              impact.strandedCount,
            )}
            className={cn(BOX, "m-0 list-none p-0")}
          >
            {impact.stranded.map((person) => {
              const on = !unchecked.has(person.userId);
              return (
                <li key={person.userId} data-f="srow" className={ROW}>
                  <button
                    data-f="cbx"
                    type="button"
                    role="checkbox"
                    aria-checked={on}
                    aria-label={fill(copy.addPerson, { name: person.name })}
                    onClick={() => {
                      const next = new Set(unchecked);
                      if (on) next.add(person.userId);
                      else next.delete(person.userId);
                      setUnchecked(next);
                    }}
                    className={cn(
                      "relative grid size-[18px] shrink-0 place-items-center rounded-[5px] border-[1.5px] p-0 after:absolute after:-inset-[13px] after:content-['']",
                      on
                        ? "border-primary bg-primary text-primary-foreground"
                        : "border-line-strong bg-popover",
                      FOCUS,
                    )}
                  >
                    {on ? (
                      <Check
                        aria-hidden="true"
                        className="size-3"
                        strokeWidth={2.4}
                      />
                    ) : null}
                  </button>
                  <span
                    aria-hidden="true"
                    className="grid size-7 shrink-0 place-items-center rounded-full bg-identity-blue text-[11px] font-bold text-identity-blue-foreground"
                  >
                    {initialsOf(person.name)}
                  </span>
                  <Title
                    sub={
                      pluralLifecycle(copy, "stranded", person.items) +
                      (person.overdue > 0
                        ? `, ${fill(copy.strandedOverdue, { count: person.overdue })}`
                        : "")
                    }
                  >
                    {person.name}
                  </Title>
                  <span className="inline-flex h-[22px] shrink-0 items-center rounded-[6px] bg-warning-subtle px-2 text-[12px] font-semibold whitespace-nowrap text-warning-text">
                    {copy.noOwnAccess}
                  </span>
                </li>
              );
            })}
            {impact.strandedCount > impact.stranded.length ? (
              <li className="border-t border-line px-3.5 py-2 text-[12.5px] text-meta-foreground">
                {fill(copy.moreStranded, {
                  count: impact.strandedCount - impact.stranded.length,
                })}
              </li>
            ) : null}
          </ul>
        </>
      ) : showRows && loses ? (
        <Banner
          dataF="ok"
          tone="ok"
          title={copy.noneStranded}
          body={copy.noneStrandedBody}
        />
      ) : null}
      {gains ? (
        <Section
          tone="ok"
          icon={<Users className="size-4" strokeWidth={1.8} />}
          title={
            impact.everyoneGains
              ? copy.gainingEveryone
              : pluralLifecycle(copy, "gaining", impact.gainingCount)
          }
          body={
            impact.everyoneGains
              ? undefined
              : fill(copy.gainingBody, { target })
          }
        />
      ) : null}
      {kept ? (
        <Section
          tone="neutral"
          icon={<Lock className="size-4" strokeWidth={1.8} />}
          title={copy.kept}
          body={fill(copy.keptBody, { names: kept })}
        />
      ) : null}
    </div>
  );
}
