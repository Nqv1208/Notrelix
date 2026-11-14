import { useEffect, useRef, useState, type ReactNode } from "react";
import {
  Building2,
  Check,
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  Crown,
  History,
  Info,
  Link2,
  Lock,
  Trash2,
  TriangleAlert,
  UserPlus,
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
import { fillBoardShareCopy as fill } from "./board-share.copy";
import type { BoardShareDialogSurfaceProps } from "./board-share-dialog-surface";
import type {
  ShareLevel,
  SharePersonView,
  ShareRowView,
} from "./board-share-models";
import { MARK_TONE, PERSON_TONE } from "./board-share-tones";

/**
 * The Board share dialog under 768 px (Final-M-Share): a full-height bottom sheet with the add field and its
 * "Permission when adding" row, who opens the board as a chevron card, the permissions given on the board in one
 * card, the board link and a stacked footer. A permission level, a switch to Private and the history open a view or
 * a second sheet of their own. The sheet has no "Invite" button: picking a person in the search adds them at once
 * with the level of the row, and a short toast with Undo takes the add back (the same write as the desktop add).
 */

const LEVELS: readonly ShareLevel[] = ["view", "comment", "edit", "manage"];
const FOCUS = "outline-none focus-visible:ring-2 focus-visible:ring-focus-ring";
const CARD =
  "rounded-[14px] border border-state-hover-strong dark:border-line bg-popover dark:bg-card";
const LABEL =
  "mx-1 mb-2 text-[13px] leading-[1.45] font-semibold text-meta-foreground";
const LV = cn(
  "inline-flex min-h-11 shrink-0 items-center gap-0.5 rounded-[10px] border-0 bg-transparent pr-1.5 pl-2 text-[14px] leading-[1.45] font-semibold whitespace-nowrap text-link active:bg-state-selected aria-disabled:cursor-default aria-disabled:text-meta-foreground",
  FOCUS,
);
const BTN =
  "inline-flex min-h-11 items-center justify-center gap-1.5 rounded-[12px] border px-4 text-[15px] leading-[1.45] font-medium whitespace-nowrap aria-disabled:cursor-default aria-disabled:opacity-60";
const BTN_PLAIN = cn(
  BTN,
  "border-line-strong bg-popover text-foreground dark:bg-card",
  FOCUS,
);
const BTN_PRIMARY = cn(
  BTN,
  "min-h-[50px] w-full border-primary bg-primary font-semibold text-primary-foreground",
  FOCUS,
);
const SHEET =
  "top-[88px] bottom-0 left-0 box-border flex w-full max-w-none translate-x-0 translate-y-0 flex-col gap-0 overflow-hidden rounded-t-[14px] rounded-b-none border-0 bg-background p-0 text-[15px] leading-[1.45] font-normal text-foreground shadow-dialog ring-0 sm:max-w-none data-open:zoom-in-100 data-open:slide-in-from-bottom-10";

type Screen = "share" | "private" | "log";
type LevelTarget =
  { readonly kind: "add" } | { readonly kind: "row"; id: string };

function Avatar({ person }: { person: SharePersonView }) {
  return (
    <span
      data-f="av"
      aria-hidden="true"
      className={cn(
        "grid size-9 shrink-0 place-items-center rounded-full text-[12px] font-bold",
        PERSON_TONE[person.tone],
      )}
    >
      {person.initials}
    </span>
  );
}

function TeamTile({ round = false }: { round?: boolean }) {
  return (
    <span
      data-f="tm"
      aria-hidden="true"
      className={cn(
        "grid size-9 shrink-0 place-items-center bg-state-hover-strong text-[12px] text-muted-foreground",
        round ? "rounded-full" : "rounded-[10px]",
      )}
    >
      <Users className="size-5" strokeWidth={1.8} />
    </span>
  );
}

function Strong({ text }: { text: string }) {
  return (
    <>
      {text.split("**").map((part, index) =>
        index % 2 === 1 ? (
          <b key={index} className="font-semibold text-foreground">
            {part}
          </b>
        ) : (
          <span key={index}>{part}</span>
        ),
      )}
    </>
  );
}

export function BoardShareSheet(props: BoardShareDialogSurfaceProps) {
  const { copy, view, open, onClose } = props;
  const [screen, setScreen] = useState<Screen>(props.initialView ?? "share");
  const [target, setTarget] = useState<LevelTarget | null>(
    props.initialLevelMenu ? { kind: "row", id: props.initialLevelMenu } : null,
  );
  const [addLevel, setAddLevel] = useState<ShareLevel>("edit");
  const [toast, setToast] = useState<{ text: string; undoId?: string } | null>(
    null,
  );
  const field = useRef<HTMLInputElement>(null);
  // The undo toast calls the write the host has by then, not the one of the render that drew it.
  const latest = useRef(props);
  useEffect(() => {
    latest.current = props;
  });
  useEffect(() => {
    if (!toast) return;
    const timer = setTimeout(() => setToast(null), 6000);
    return () => clearTimeout(timer);
  }, [toast]);

  const title =
    screen === "private"
      ? copy.privateConfirm.title
      : screen === "log"
        ? copy.log.title
        : fill(copy.title, { name: view.boardTitle });
  const sub = view.space
    ? fill(copy.subtitle, { space: view.space.name })
    : fill(copy.subtitleWorkspace, { workspace: view.workspaceName });
  const managers = view.rows.filter((row) => row.level === "manage");
  const lockedId =
    props.onChangeLevel && managers.length === 1 ? managers[0]!.id : null;
  const targetRow =
    target?.kind === "row"
      ? (view.rows.find((row) => row.id === target.id) ?? null)
      : null;

  const copyLink = async () => {
    const done = await props.onCopyLink({ silent: true });
    if (done === true) setToast({ text: copy.sheet.linkCopied });
    if (done === false) setToast({ text: copy.footer.copyFailed });
  };

  return (
    <Dialog open={open} onOpenChange={(next) => (next ? undefined : onClose())}>
      <DialogContent
        showCloseButton={false}
        overlayClassName="bg-modal-scrim backdrop-blur-none"
        data-f="dlg"
        className={SHEET}
        onOpenAutoFocus={(event) => {
          event.preventDefault();
          (event.currentTarget as HTMLElement)
            ?.querySelector<HTMLElement>("[data-f='ib']")
            ?.focus();
        }}
      >
        <div
          data-f="grab"
          aria-hidden="true"
          className="mx-auto mt-1.5 mb-0.5 h-[5px] w-9 shrink-0 rounded-[3px] bg-line-strong"
        />
        <div
          data-f="dh"
          className={cn(
            "flex min-h-[52px] shrink-0 items-center gap-1 pr-2",
            screen === "share" ? "pl-5" : "pl-1.5",
          )}
        >
          {screen === "share" ? null : (
            <button
              type="button"
              aria-label={copy.sheet.back}
              onClick={() => setScreen("share")}
              className={cn(
                "grid size-11 shrink-0 place-items-center rounded-[12px] border-0 bg-transparent text-foreground active:bg-state-hover-strong",
                FOCUS,
              )}
            >
              <ChevronLeft
                aria-hidden="true"
                className="size-6"
                strokeWidth={1.8}
              />
            </button>
          )}
          <DialogTitle
            data-f="dt"
            className="m-0 min-w-0 flex-1 truncate text-[17px] leading-[1.45] font-[650] text-foreground"
          >
            {title}
          </DialogTitle>
          <button
            data-f="ib"
            type="button"
            aria-label={copy.close}
            onClick={onClose}
            className={cn(
              "grid size-11 shrink-0 place-items-center rounded-[12px] border-0 bg-transparent text-foreground active:bg-state-hover-strong",
              FOCUS,
            )}
          >
            <X aria-hidden="true" className="size-5" strokeWidth={1.8} />
          </button>
        </div>
        <DialogDescription className="sr-only">{sub}</DialogDescription>

        {screen === "share" ? (
          <ShareScreen
            {...props}
            field={field}
            addLevel={addLevel}
            toasted={toast !== null}
            openLevel={setTarget}
            openScreen={setScreen}
            addManager={() => {
              setTarget(null);
              setAddLevel("manage");
              field.current?.focus();
            }}
            onToast={setToast}
            latest={latest}
            copyLink={() => void copyLink()}
          />
        ) : screen === "log" ? (
          <LogScreen {...props} />
        ) : (
          <PrivateScreen {...props} openScreen={setScreen} />
        )}

        {toast ? (
          <div
            data-f="toast"
            role="status"
            className="absolute right-4 bottom-40 left-4 z-10 box-border flex min-h-12 items-center gap-2.5 rounded-[12px] bg-foreground px-3.5 py-3 text-[14px] leading-[1.35] font-medium text-background shadow-popover"
          >
            <Check
              aria-hidden="true"
              className="size-5 shrink-0 text-inverse-success dark:text-success"
              strokeWidth={1.8}
            />
            <span className="min-w-0 flex-1">{toast.text}</span>
            {toast.undoId && props.onRemove ? (
              <button
                data-f="undo"
                type="button"
                onClick={() => {
                  const id = toast.undoId!;
                  setToast(null);
                  latest.current.onRemove?.(id);
                }}
                className={cn(
                  "-my-2 -mr-2.5 min-h-11 shrink-0 rounded-[10px] border-0 bg-transparent px-3 text-[14px] font-semibold text-background underline",
                  FOCUS,
                )}
              >
                {copy.sheet.undo}
              </button>
            ) : null}
          </div>
        ) : null}

        <LevelSheet
          {...props}
          target={target}
          row={targetRow}
          current={
            target?.kind === "add" ? addLevel : (targetRow?.level ?? null)
          }
          locked={targetRow !== null && targetRow.id === lockedId}
          onClose={() => setTarget(null)}
          onPick={(level) => {
            if (target?.kind === "add") setAddLevel(level);
            else if (targetRow && level !== targetRow.level)
              props.onChangeLevel?.(targetRow.id, level);
            setTarget(null);
          }}
          onAddManager={() => {
            setTarget(null);
            setAddLevel("manage");
            field.current?.focus();
          }}
        />
      </DialogContent>
    </Dialog>
  );
}

function ShareScreen({
  copy,
  view,
  field,
  addLevel,
  toasted,
  openLevel,
  openScreen,
  addManager,
  onToast,
  latest,
  copyLink,
  candidates,
  onInvite,
  onChangeGeneral,
  onChangeLevel,
  onClaimOwner,
  notice,
  peopleState,
  onRetryPeople,
  log,
}: BoardShareDialogSurfaceProps & {
  field: React.RefObject<HTMLInputElement | null>;
  addLevel: ShareLevel;
  toasted: boolean;
  openLevel: (target: LevelTarget) => void;
  openScreen: (screen: Screen) => void;
  addManager: () => void;
  onToast: (toast: { text: string; undoId?: string }) => void;
  latest: React.RefObject<BoardShareDialogSurfaceProps>;
  copyLink: () => void;
}) {
  const g = copy.general;
  const p = copy.people;
  const general = view.general;
  const [query, setQuery] = useState("");
  const [listOpen, setListOpen] = useState(false);
  const [adding, setAdding] = useState(false);
  const live = Boolean(candidates && onInvite);
  const needle = query.trim().toLowerCase();
  const matches = (candidates ?? [])
    .filter((candidate) => candidate.name.toLowerCase().includes(needle))
    .slice(0, 6);
  const memberRows = view.rows.filter((row) => row.viewer !== true);
  const viewerRows = view.rows.filter((row) => row.viewer === true);

  const add = async (id: string, name: string) => {
    if (adding) return;
    setAdding(true);
    setQuery("");
    setListOpen(false);
    try {
      const done = await latest.current.onInvite?.(id, addLevel);
      if (done !== false)
        onToast({
          text: fill(copy.sheet.added, { name, level: copy.levels[addLevel] }),
          undoId: id,
        });
    } finally {
      setAdding(false);
    }
  };

  const personRow = (row: ShareRowView) => (
    <li
      key={row.id}
      data-f="pr"
      className="box-content flex min-h-[60px] items-center gap-3 border-t border-state-hover-strong dark:border-line py-2 pr-1 pl-3.5 first:border-t-0"
    >
      {row.kind === "team" || !row.person ? (
        <TeamTile round={row.kind !== "team"} />
      ) : (
        <Avatar person={row.person} />
      )}
      <span data-f="who" className="min-w-0 flex-1">
        <b
          data-f="wname"
          className="block text-[15px] leading-[1.35] font-semibold text-foreground"
        >
          {row.name}
          {row.isYou ? (
            <span className="font-normal text-meta-foreground"> {p.you}</span>
          ) : null}
        </b>
        {row.detail || row.badge ? (
          <small
            data-f="wsub"
            className="mt-px block text-[13px] leading-[1.38] font-normal text-meta-foreground"
          >
            {row.badge ? (
              <span
                data-f="badge"
                className={cn(
                  "mr-0.5 inline-block rounded-[5px] px-1.5 align-[1px] text-[12px] leading-[1.45] font-semibold",
                  row.badge === "outside"
                    ? "bg-danger-subtle text-danger-text"
                    : "bg-warning-subtle text-warning-text",
                )}
              >
                {row.badge === "outside" ? p.outside : p.guest}
              </span>
            ) : null}{" "}
            {row.detail}
          </small>
        ) : null}
      </span>
      {row.level === null ? null : row.fixed ? (
        <span
          data-f="from"
          className="shrink-0 pr-3.5 pl-2 text-[14px] whitespace-nowrap text-meta-foreground"
        >
          {copy.sheet.levelShort[row.level]}
        </span>
      ) : (
        <button
          data-f="lvl-plain"
          type="button"
          aria-haspopup="dialog"
          aria-disabled={onChangeLevel ? undefined : true}
          title={onChangeLevel ? undefined : p.levelUnavailable}
          aria-label={fill(p.levelOf, {
            name: row.name,
            level: copy.levels[row.level],
          })}
          onClick={
            onChangeLevel
              ? () => openLevel({ kind: "row", id: row.id })
              : undefined
          }
          className={LV}
        >
          {copy.sheet.levelShort[row.level]}
          <ChevronDown
            aria-hidden="true"
            className="size-4"
            strokeWidth={1.8}
          />
        </button>
      )}
    </li>
  );

  return (
    <>
      <div
        data-f="db"
        className="min-h-0 flex-1 overflow-y-auto overscroll-contain"
      >
        <div
          className={cn(
            "flex flex-col gap-[22px] px-4 pt-1.5",
            toasted ? "pb-24" : "pb-6",
          )}
        >
          {notice ? (
            <div
              data-f="bn"
              role={notice.kind === "refused" ? "alert" : "status"}
              className={cn(
                "flex items-start gap-2.5 rounded-[12px] px-3.5 py-3 text-[14px] leading-[1.45]",
                notice.kind === "refused"
                  ? "bg-danger-subtle text-danger-text"
                  : "bg-state-selected text-link",
              )}
            >
              {notice.kind === "refused" ? (
                <TriangleAlert
                  aria-hidden="true"
                  className="mt-px size-5 shrink-0"
                  strokeWidth={1.8}
                />
              ) : (
                <Info
                  aria-hidden="true"
                  className="mt-px size-5 shrink-0"
                  strokeWidth={1.8}
                />
              )}
              <span className="flex min-w-0 flex-1 flex-col gap-px">
                <b className="font-semibold text-foreground">{notice.title}</b>
                <span>{notice.body}</span>
                {notice.addManager ? (
                  <button
                    type="button"
                    onClick={addManager}
                    className={cn(BTN_PLAIN, "mt-2 self-start text-[14px]")}
                  >
                    {copy.menu.addManager}
                  </button>
                ) : null}
              </span>
            </div>
          ) : null}

          <div className="flex flex-col" data-f="add">
            <div className="relative">
              <UserPlus
                aria-hidden="true"
                className="pointer-events-none absolute top-[15px] left-[13px] size-5 text-meta-foreground"
                strokeWidth={1.8}
              />
              <input
                ref={field}
                data-f="in"
                role="combobox"
                aria-expanded={listOpen && matches.length > 0}
                aria-controls="board-share-candidates"
                aria-autocomplete="list"
                aria-disabled={live ? undefined : true}
                readOnly={!live}
                title={live ? undefined : copy.add.unavailable}
                value={query}
                placeholder={copy.add.placeholder}
                aria-label={copy.add.placeholder}
                autoComplete="off"
                onChange={(event) => {
                  setQuery(event.target.value);
                  setListOpen(true);
                }}
                onFocus={() => setListOpen(true)}
                className={cn(
                  "box-border block h-[50px] w-full rounded-[12px] border border-line-strong bg-popover pr-3 pl-[42px] dark:bg-card text-[16px] text-foreground placeholder:text-meta-foreground",
                  FOCUS,
                )}
              />
            </div>
            <div className="mt-1 flex items-center gap-2 pl-1 text-[13px] text-meta-foreground">
              <span id="board-share-add-level">{copy.sheet.addLevelLabel}</span>
              <button
                data-f="lvl"
                type="button"
                aria-haspopup="dialog"
                aria-disabled={live ? undefined : true}
                aria-labelledby="board-share-add-level board-share-add-level-value"
                title={live ? undefined : copy.add.unavailable}
                onClick={live ? () => openLevel({ kind: "add" }) : undefined}
                className={LV}
              >
                <span id="board-share-add-level-value">
                  {copy.levels[addLevel]}
                </span>
                <ChevronDown
                  aria-hidden="true"
                  className="size-4"
                  strokeWidth={1.8}
                />
              </button>
            </div>
            {live && listOpen && needle ? (
              matches.length > 0 ? (
                <ul
                  id="board-share-candidates"
                  data-f="cands"
                  role="listbox"
                  className={cn(CARD, "m-0 mt-1 list-none overflow-hidden p-0")}
                >
                  {matches.map((candidate) => (
                    <li
                      key={candidate.id}
                      role="option"
                      aria-selected="false"
                      className="border-t border-state-hover-strong dark:border-line first:border-t-0"
                    >
                      <button
                        type="button"
                        disabled={adding}
                        onClick={() => void add(candidate.id, candidate.name)}
                        className={cn(
                          "flex min-h-[60px] w-full items-center gap-3 border-0 bg-transparent py-2 pr-3.5 pl-3.5 text-left text-[15px] font-semibold text-foreground active:bg-state-hover",
                          FOCUS,
                        )}
                      >
                        <Avatar person={candidate.person} />
                        <span className="min-w-0 flex-1 truncate">
                          {candidate.name}
                        </span>
                      </button>
                    </li>
                  ))}
                </ul>
              ) : (
                <p
                  role="status"
                  className="m-0 mt-1 px-1 text-[14px] text-meta-foreground"
                >
                  {copy.sheet.searchEmpty}
                </p>
              )
            ) : null}
          </div>

          <section className="flex flex-col" aria-labelledby="board-share-acc">
            <h3 id="board-share-acc" data-f="lbl" className={LABEL}>
              {g.label}
            </h3>
            {(() => {
              const change =
                Boolean(onChangeGeneral) && general.kind !== "private";
              const name =
                general.kind === "space"
                  ? fill(g.bySpace, { name: general.name })
                  : general.kind === "workspace"
                    ? g.byWorkspace
                    : g.private;
              return (
                <button
                  data-f="ga"
                  type="button"
                  aria-haspopup={change ? "dialog" : undefined}
                  aria-disabled={change ? undefined : true}
                  title={onChangeGeneral ? undefined : g.changeUnavailable}
                  onClick={
                    change
                      ? () => {
                          onChangeGeneral?.("private");
                          openScreen("private");
                        }
                      : undefined
                  }
                  className={cn(
                    CARD,
                    "box-border flex min-h-[72px] w-full items-center gap-3 py-3 pr-2.5 pl-3.5 text-left text-foreground active:bg-state-hover aria-disabled:cursor-default",
                    FOCUS,
                  )}
                >
                  <span
                    data-f="gaic"
                    aria-hidden="true"
                    className={cn(
                      "grid size-9 shrink-0 place-items-center rounded-[10px] text-[12px] font-bold",
                      general.kind === "space"
                        ? MARK_TONE[general.mark.tone]
                        : "bg-state-hover-strong text-muted-foreground",
                    )}
                  >
                    {general.kind === "space" ? (
                      general.mark.initials
                    ) : general.kind === "workspace" ? (
                      <Building2 className="size-5" strokeWidth={1.8} />
                    ) : (
                      <Lock className="size-5" strokeWidth={1.8} />
                    )}
                  </span>
                  <span className="min-w-0 flex-1">
                    <b
                      data-f="gat"
                      className="block text-[15px] leading-[1.45] font-semibold"
                    >
                      {name}
                      {general.kind === "space" && general.count !== null
                        ? ` · ${fill(copy.sheet.peopleCount, { count: general.count })}`
                        : ""}
                    </b>
                    {general.kind === "space" ? (
                      <>
                        <span className="sr-only">
                          {copy.sheet.accessRoles}
                        </span>
                        <span
                          data-f="map"
                          className="mt-1.5 flex flex-wrap gap-x-1.5 gap-y-1"
                        >
                          {[g.mapAdmin, g.mapMember, g.mapViewer].map(
                            (text) => (
                              <span
                                key={text}
                                data-f="mapc"
                                className="inline-flex h-6 items-center rounded-[6px] bg-state-hover-strong px-2 text-[12.5px] whitespace-nowrap text-muted-foreground"
                              >
                                {text}
                              </span>
                            ),
                          )}
                        </span>
                        {change ? (
                          <span className="sr-only">
                            {copy.sheet.accessChange}
                          </span>
                        ) : null}
                      </>
                    ) : (
                      <small className="mt-0.5 block text-[13px] leading-[1.42] text-meta-foreground">
                        {general.kind === "workspace"
                          ? g.byWorkspaceHint
                          : g.privateHint}
                      </small>
                    )}
                  </span>
                  {change ? (
                    <ChevronRight
                      aria-hidden="true"
                      className="size-5 shrink-0 text-meta-foreground"
                      strokeWidth={1.8}
                    />
                  ) : null}
                </button>
              );
            })()}
          </section>

          {peopleState === "loading" ? (
            <div
              role="status"
              aria-label={p.loading}
              aria-busy="true"
              className={cn(CARD, "flex flex-col gap-3 p-3.5")}
            >
              {[0, 1, 2].map((row) => (
                <div key={row} className="flex items-center gap-3">
                  <Skeleton className="size-9 shrink-0 rounded-full" />
                  <span className="flex flex-1 flex-col gap-1.5">
                    <Skeleton className="h-3.5 w-1/3" />
                    <Skeleton className="h-3 w-1/2" />
                  </span>
                </div>
              ))}
              <span className="sr-only">{p.loading}</span>
            </div>
          ) : peopleState === "error" ? (
            <div
              data-f="perror"
              className={cn(
                CARD,
                "flex flex-col gap-3 p-3.5 text-[14px] text-muted-foreground",
              )}
            >
              <p role="alert" className="m-0">
                {p.loadError}
              </p>
              <button
                type="button"
                onClick={onRetryPeople}
                className={cn(BTN_PLAIN, "self-start")}
              >
                {p.retry}
              </button>
            </div>
          ) : view.hiddenCounts ? (
            <p
              data-f="pcounts"
              className="m-0 px-1 text-[14px] leading-[1.45] text-meta-foreground"
            >
              {fill(p.counts, {
                people: view.hiddenCounts.people,
                guests: view.hiddenCounts.guests,
              })}
            </p>
          ) : (
            <section
              className="flex flex-col"
              aria-labelledby="board-share-people"
            >
              <h3 id="board-share-people" data-f="lbl" className={LABEL}>
                {fill(p.label, { count: memberRows.length })}
              </h3>
              {memberRows.length === 0 ? (
                viewerRows.length === 0 ? (
                  <p className="m-0 px-1 text-[14px] text-meta-foreground">
                    {p.empty}
                  </p>
                ) : null
              ) : (
                <ul
                  data-f="pl"
                  className={cn(CARD, "m-0 list-none overflow-hidden p-0")}
                >
                  {memberRows.map((row) => personRow(row))}
                </ul>
              )}
              {view.canManage &&
              !view.rows.some((row) => row.owner === true) ? (
                <div
                  data-f="noowner"
                  role="status"
                  className="mt-2 flex flex-col items-start gap-2 rounded-[12px] border border-warning-text/40 bg-warning-subtle px-3.5 py-3 text-[14px] leading-[1.45] text-warning-text"
                >
                  <span>{p.noOwner}</span>
                  {onClaimOwner && !view.rows.some((row) => row.isYou) ? (
                    <button
                      type="button"
                      onClick={onClaimOwner}
                      className={BTN_PLAIN}
                    >
                      {p.claimOwner}
                    </button>
                  ) : null}
                </div>
              ) : null}
              {view.canManage && memberRows.some((row) => row.ownable) ? (
                <p
                  data-f="ownerhint"
                  className="m-0 mt-2 px-1 text-[13px] leading-[1.45] text-meta-foreground"
                >
                  {p.ownerHint}
                </p>
              ) : null}
              {viewerRows.length > 0 ? (
                <div className="mt-[22px] flex flex-col" data-f="viewers">
                  <h3 data-f="vlbl" className={LABEL}>
                    {fill(p.viewersLabel, { count: viewerRows.length })}
                  </h3>
                  <p className="m-0 mx-1 mb-2 text-[13px] leading-[1.45] text-meta-foreground">
                    {p.viewersHint}
                  </p>
                  <ul className={cn(CARD, "m-0 list-none overflow-hidden p-0")}>
                    {viewerRows.map((row) => personRow(row))}
                  </ul>
                </div>
              ) : null}
            </section>
          )}

          <section
            data-f="lk"
            aria-labelledby="board-share-link"
            className={cn(CARD, "flex items-start gap-3 p-3.5")}
          >
            <span
              data-f="lkic"
              aria-hidden="true"
              className="grid size-9 shrink-0 place-items-center rounded-[10px] border border-state-hover-strong dark:border-line bg-background text-muted-foreground"
            >
              <Link2 className="size-5" strokeWidth={1.8} />
            </span>
            <div>
              <h3
                id="board-share-link"
                data-f="lkt"
                className="m-0 text-[15px] leading-[1.45] font-semibold"
              >
                {copy.link.title}
              </h3>
              <p
                data-f="lks"
                className="m-0 mt-0.5 text-[13px] leading-[1.42] text-meta-foreground"
              >
                {copy.link.hint}
              </p>
              {view.policy === "unknown" ? null : (
                <p
                  data-f="off"
                  className="m-0 mt-2 flex items-center gap-1.5 text-[13px] leading-[1.42] font-medium text-muted-foreground"
                >
                  <Lock
                    aria-hidden="true"
                    className="size-3.5 shrink-0"
                    strokeWidth={1.8}
                  />
                  {view.policy === "off"
                    ? copy.link.publicOff
                    : copy.link.publicOn}
                </p>
              )}
            </div>
          </section>
        </div>
      </div>
      <div
        data-f="df"
        className="flex shrink-0 flex-col gap-2 border-t border-state-hover-strong dark:border-line bg-background px-4 pt-3 pb-[34px]"
      >
        <button
          data-f="btn-pri"
          type="button"
          onClick={copyLink}
          className={BTN_PRIMARY}
        >
          <Link2 aria-hidden="true" className="size-5" strokeWidth={1.8} />
          {copy.footer.copy}
        </button>
        <button
          data-f="logl"
          type="button"
          aria-disabled={log ? undefined : true}
          title={log ? undefined : copy.footer.logUnavailable}
          onClick={log ? () => openScreen("log") : undefined}
          className={cn(
            BTN,
            "w-full border-transparent bg-transparent font-semibold text-link",
            FOCUS,
          )}
        >
          <History aria-hidden="true" className="size-5" strokeWidth={1.8} />
          {copy.footer.log}
        </button>
      </div>
    </>
  );
}

function PrivateScreen({
  copy,
  view,
  impact,
  onClose,
  onConfirmPrivate,
  openScreen,
}: BoardShareDialogSurfaceProps & { openScreen: (screen: Screen) => void }) {
  const c = copy.privateConfirm;
  const s = copy.sheet;
  const [chosen, setChosen] = useState<ReadonlySet<string>>(
    () => new Set((impact?.people ?? []).map((person) => person.id)),
  );
  // A Private board keeps an Owner: with none, becoming it is the caller's own explicit choice.
  const needsOwner =
    !view.hiddenCounts &&
    !view.rows.some((row) => row.owner === true || row.isYou);
  const [addMe, setAddMe] = useState(false);
  const blocked = needsOwner && !addMe;
  const ready = Boolean(onConfirmPrivate) && !blocked;
  const general = view.general;
  const spaceName =
    general.kind === "space" ? general.name : view.workspaceName;
  const confirm = (ids: readonly string[]) => {
    if (!ready) return;
    onConfirmPrivate?.(ids, needsOwner && addMe);
    openScreen("share");
    onClose();
  };
  return (
    <>
      <div
        data-f="db"
        className="min-h-0 flex-1 overflow-y-auto overscroll-contain"
      >
        <div className="flex flex-col gap-3.5 px-4 pt-0 pb-6">
          <p
            data-f="pdesc"
            className="m-0 mx-1 mt-1.5 text-[14px] leading-[1.45] text-muted-foreground"
          >
            {s.privateDesc}
          </p>
          {impact ? (
            <>
              <div
                data-f="warn"
                role="note"
                className="flex items-start gap-2.5 rounded-[12px] border border-warning-text/40 bg-warning-subtle px-3.5 py-3 text-[14px] leading-[1.45] text-warning-text"
              >
                <TriangleAlert
                  aria-hidden="true"
                  className="mt-px size-5 shrink-0"
                  strokeWidth={1.8}
                />
                <span>
                  <b className="font-[650]">
                    {fill(c.warn, { count: impact.lose, name: spaceName })}
                  </b>
                  {impact.people.length > 0
                    ? fill(s.warnWork, { work: impact.people.length })
                    : null}
                </span>
              </div>
              {impact.people.length > 0 ? (
                <fieldset className="m-0 flex min-w-0 flex-col border-0 p-0">
                  <legend data-f="plegend" className={cn(LABEL, "p-0")}>
                    {s.addBefore}
                  </legend>
                  <div
                    data-f="imp-list"
                    className={cn(CARD, "overflow-hidden")}
                  >
                    {impact.people.map((person) => (
                      <label
                        key={person.id}
                        data-f="ck"
                        className="box-content flex min-h-[52px] cursor-pointer items-center gap-2.5 border-t border-state-hover-strong dark:border-line py-0.5 pr-3.5 pl-1 first:border-t-0"
                      >
                        <span
                          data-f="hit"
                          className="grid size-11 shrink-0 place-items-center"
                        >
                          <input
                            type="checkbox"
                            checked={chosen.has(person.id)}
                            onChange={(event) =>
                              setChosen((current) => {
                                const next = new Set(current);
                                if (event.target.checked) next.add(person.id);
                                else next.delete(person.id);
                                return next;
                              })
                            }
                            className="m-0 size-[22px] cursor-pointer accent-primary"
                          />
                        </span>
                        <Avatar person={person} />
                        <span className="min-w-0 flex-1">
                          <b className="block text-[15px] leading-[1.45] font-semibold">
                            {person.name}
                          </b>
                          <small
                            className={cn(
                              "block text-[13px] leading-[1.45]",
                              person.overdue > 0
                                ? "text-danger-text"
                                : "text-meta-foreground",
                            )}
                          >
                            {person.overdue > 0
                              ? fill(s.itemsOverdue, {
                                  count: person.items,
                                  overdue: person.overdue,
                                })
                              : fill(s.itemsOpen, { count: person.items })}
                          </small>
                        </span>
                      </label>
                    ))}
                  </div>
                </fieldset>
              ) : null}
              <p
                data-f="pkeep"
                className="m-0 mx-1 text-[14px] leading-[1.5] text-muted-foreground"
              >
                {c.keep} <Strong text={impact.keepText} />
              </p>
            </>
          ) : null}
          <p
            data-f="pnote"
            className="m-0 mx-1 -mt-1.5 text-[13px] leading-[1.5] text-meta-foreground"
          >
            {s.privateNote}
          </p>
          {needsOwner ? (
            <label
              data-f="ownchoice"
              className={cn(
                CARD,
                "flex cursor-pointer items-start gap-2.5 p-3.5 text-[14px] leading-[1.45]",
              )}
            >
              <input
                type="checkbox"
                checked={addMe}
                onChange={(event) => setAddMe(event.target.checked)}
                className="m-0 mt-0.5 size-[22px] shrink-0 accent-primary"
              />
              <span>
                <b className="block font-semibold text-foreground">
                  {c.addMeOwner}
                </b>
                <small className="block text-[13px] text-meta-foreground">
                  {c.addMeOwnerHint}
                </small>
              </span>
            </label>
          ) : null}
        </div>
      </div>
      <div
        data-f="df"
        className="flex shrink-0 flex-col gap-2 border-t border-state-hover-strong dark:border-line bg-background px-4 pt-3 pb-[34px]"
      >
        <button
          data-f="btn-pri"
          type="button"
          aria-disabled={ready && chosen.size > 0 ? undefined : true}
          title={
            blocked
              ? c.addMeOwnerHint
              : onConfirmPrivate
                ? undefined
                : copy.general.changeUnavailable
          }
          onClick={() => confirm([...chosen])}
          className={BTN_PRIMARY}
        >
          {fill(c.switchAdd, { count: chosen.size })}
        </button>
        <div className="flex gap-2.5">
          <button
            data-f="btn-cancel"
            type="button"
            onClick={() => {
              openScreen("share");
              onClose();
            }}
            className={cn(BTN_PLAIN, "shrink-0 basis-24")}
          >
            {c.cancel}
          </button>
          <button
            data-f="btn-switch"
            type="button"
            aria-disabled={ready ? undefined : true}
            onClick={() => confirm([])}
            className={cn(BTN_PLAIN, "flex-1")}
          >
            {c.switchOnly}
          </button>
        </div>
      </div>
    </>
  );
}

function LogScreen({ copy, log }: BoardShareDialogSurfaceProps) {
  const l = copy.log;
  return (
    <div
      data-f="db"
      className="min-h-0 flex-1 overflow-y-auto overscroll-contain"
    >
      <div className="flex flex-col gap-[22px] px-4 pt-1.5 pb-6">
        <p
          data-f="lsub"
          className="m-0 mx-1 -mb-2 text-[13px] leading-[1.45] text-meta-foreground"
        >
          {l.onlyManagers}
        </p>
        {log && log.length > 0 ? (
          <ul
            data-f="log"
            aria-label={l.title}
            className={cn(CARD, "m-0 list-none px-3.5 py-0")}
          >
            {log.map((entry) => (
              <li
                key={entry.id}
                data-f="le"
                className="flex items-start gap-3 border-t border-state-hover-strong dark:border-line py-3 text-[14px] leading-[1.48] first:border-t-0"
              >
                {entry.actor ? (
                  <span
                    data-f="av"
                    aria-hidden="true"
                    className={cn(
                      "grid size-8 shrink-0 place-items-center rounded-full text-[11.5px] font-bold",
                      PERSON_TONE[entry.actor.tone],
                    )}
                  >
                    {entry.actor.initials}
                  </span>
                ) : (
                  <span
                    data-f="tm"
                    aria-hidden="true"
                    className="grid size-8 shrink-0 place-items-center rounded-[9px] bg-state-hover-strong text-muted-foreground"
                  >
                    <History className="size-5" strokeWidth={1.8} />
                  </span>
                )}
                <span>
                  <Strong text={entry.text} />
                  <small className="mt-0.5 block text-[13px] leading-[1.48] text-meta-foreground">
                    {[entry.when, entry.note].filter(Boolean).join(" · ")}
                  </small>
                </span>
              </li>
            ))}
          </ul>
        ) : (
          <p className="m-0 text-[14px] text-meta-foreground">{l.empty}</p>
        )}
      </div>
    </div>
  );
}

function SheetRow({
  icon,
  danger = false,
  disabled = false,
  onClick,
  children,
  hint,
}: {
  icon: ReactNode;
  danger?: boolean;
  disabled?: boolean;
  onClick?: () => void;
  children: ReactNode;
  hint?: string;
}) {
  return (
    <button
      data-f="lo"
      type="button"
      aria-disabled={disabled ? true : undefined}
      onClick={disabled ? undefined : onClick}
      className={cn(
        "flex min-h-[60px] w-full items-center gap-3 border-0 bg-transparent px-5 text-left active:bg-state-hover aria-disabled:cursor-not-allowed aria-disabled:opacity-60",
        FOCUS,
      )}
    >
      <span
        className={cn(
          "grid size-9 shrink-0 place-items-center rounded-[10px] border",
          danger
            ? "border-transparent bg-danger-subtle text-danger-text"
            : "border-state-hover-strong dark:border-line bg-background text-muted-foreground",
        )}
      >
        {icon}
      </span>
      <span className="min-w-0 flex-1">
        <b
          className={cn(
            "block text-[15px] leading-[1.45] font-semibold",
            danger ? "text-danger-text" : "text-foreground",
          )}
        >
          {children}
        </b>
        {hint ? (
          <small className="block text-[13px] leading-[1.45] text-meta-foreground">
            {hint}
          </small>
        ) : null}
      </span>
    </button>
  );
}

/** The second sheet over the first: the four levels, and for a person the Owner and removal rows. */
function LevelSheet({
  copy,
  target,
  row,
  current,
  locked,
  onClose,
  onPick,
  onAddManager,
  onRemove,
  onMakeOwner,
}: BoardShareDialogSurfaceProps & {
  target: LevelTarget | null;
  row: ShareRowView | null;
  current: ShareLevel | null;
  locked: boolean;
  onClose: () => void;
  onPick: (level: ShareLevel) => void;
  onAddManager: () => void;
}) {
  const guest = row?.badge != null;
  const name = row?.shortName ?? row?.name ?? "";
  const heading = row
    ? fill(copy.menu.label, { name: row.name })
    : copy.sheet.addLevelLabel;
  return (
    <Dialog
      open={target !== null}
      onOpenChange={(next) => (next ? undefined : onClose())}
    >
      <DialogContent
        showCloseButton={false}
        overlayClassName="bg-modal-scrim backdrop-blur-none"
        data-f="lsheet"
        className="top-auto bottom-0 left-0 box-border flex w-full max-w-none translate-x-0 translate-y-0 flex-col gap-0 overflow-y-auto rounded-t-[14px] rounded-b-none border-0 bg-popover p-0 pt-1.5 dark:bg-card pb-[34px] text-[15px] leading-[1.45] text-foreground shadow-dialog ring-0 sm:max-w-none data-open:zoom-in-100 data-open:slide-in-from-bottom-10"
      >
        <div
          aria-hidden="true"
          className="mx-auto mb-0.5 h-[5px] w-9 shrink-0 rounded-[3px] bg-line-strong"
        />
        <div className="flex min-h-[52px] items-center gap-2 pr-2 pl-5">
          <span className="min-w-0 flex-1">
            <DialogTitle
              data-f="lt"
              className="m-0 text-[17px] leading-[1.45] font-[650]"
            >
              {heading}
            </DialogTitle>
            <DialogDescription className="m-0 mt-px truncate text-[13px] leading-[1.45] text-meta-foreground">
              {row?.detail ?? ""}
            </DialogDescription>
          </span>
          <button
            data-f="ib"
            type="button"
            aria-label={copy.close}
            onClick={onClose}
            className={cn(
              "grid size-11 shrink-0 place-items-center rounded-[12px] border-0 bg-transparent active:bg-state-hover-strong",
              FOCUS,
            )}
          >
            <X aria-hidden="true" className="size-5" strokeWidth={1.8} />
          </button>
        </div>
        <div role="radiogroup" aria-label={heading} className="flex flex-col">
          {LEVELS.map((level) => {
            const disabled =
              (level !== "view" && guest) || (locked && level !== "manage");
            const checked = current === level;
            return (
              <button
                key={level}
                data-f="lo"
                type="button"
                role="radio"
                aria-checked={checked}
                aria-disabled={disabled ? true : undefined}
                onClick={disabled ? undefined : () => onPick(level)}
                className={cn(
                  "flex min-h-[62px] w-full items-center gap-3 border-0 bg-transparent px-5 text-left active:bg-state-hover aria-disabled:cursor-not-allowed aria-disabled:active:bg-transparent",
                  FOCUS,
                )}
              >
                <span className="min-w-0 flex-1">
                  <b
                    data-f="lot"
                    className={cn(
                      "block text-[15px] leading-[1.45] font-semibold",
                      disabled
                        ? "text-muted-foreground"
                        : checked
                          ? "text-link"
                          : "text-foreground",
                    )}
                  >
                    {copy.levels[level]}
                  </b>
                  <small
                    data-f="loh"
                    className="block text-[13px] leading-[1.38] text-meta-foreground"
                  >
                    {locked
                      ? checked
                        ? copy.menu.keepCurrent
                        : copy.menu.needManager
                      : disabled
                        ? copy.levelHints.guestViewOnly
                        : copy.levelHints[level]}
                  </small>
                </span>
                <span className="grid w-6 shrink-0 place-items-center text-link">
                  {checked ? (
                    <Check
                      aria-hidden="true"
                      className="size-5"
                      strokeWidth={1.8}
                    />
                  ) : disabled ? (
                    <Lock
                      aria-hidden="true"
                      className="size-5 text-meta-foreground"
                      strokeWidth={1.8}
                    />
                  ) : null}
                </span>
              </button>
            );
          })}
        </div>
        {row ? (
          <>
            <div
              role="separator"
              className="mx-5 my-1.5 h-px bg-state-hover-strong dark:bg-line"
            />
            {onMakeOwner && row.ownable ? (
              <SheetRow
                icon={<Crown className="size-5" strokeWidth={1.8} />}
                hint={fill(copy.menu.makeOwnerHint, { name })}
                onClick={() => {
                  onClose();
                  onMakeOwner(row.id);
                }}
              >
                {copy.menu.makeOwner}
              </SheetRow>
            ) : null}
            <SheetRow
              danger
              disabled={!onRemove || locked}
              icon={<Trash2 className="size-5" strokeWidth={1.8} />}
              hint={
                locked
                  ? copy.menu.removeLast
                  : fill(copy.menu.removeHint, { name })
              }
              onClick={() => {
                onClose();
                onRemove?.(row.id);
              }}
            >
              {copy.menu.remove}
            </SheetRow>
            {locked ? (
              <div
                data-f="lnote"
                className="flex flex-col items-start gap-2 px-5 pt-2 text-[13px] leading-[1.45] text-muted-foreground"
              >
                <span>{copy.menu.lastNote}</span>
                <button
                  type="button"
                  onClick={onAddManager}
                  className={BTN_PLAIN}
                >
                  {copy.menu.addManager}
                </button>
              </div>
            ) : null}
          </>
        ) : null}
      </DialogContent>
    </Dialog>
  );
}
