import { useEffect, useRef, useState, type KeyboardEvent, type ReactNode } from "react";
import {
  Building2,
  Check,
  ChevronDown,
  CircleAlert,
  Info,
  Plus,
  TriangleAlert,
  Link2,
  Lock,
  Users,
  UserPlus,
  X,
  ArrowLeft,
} from "lucide-react";
import {
  cn,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
  Popover,
  PopoverAnchor,
  PopoverContent,
} from "@notrelix/ui-web";
import {
  fillBoardShareCopy as fill,
  type BoardShareCopy,
} from "./board-share.copy";
import type {
  BoardShareView,
  ShareImpactPerson,
  ShareLevel,
  ShareLogEntry,
  SharePersonView,
  ShareRowView,
} from "./board-share-models";

/**
 * The Board share dialog (Final-Share): who can open the Board through its
 * department, the permissions given on the Board itself, the Board link, the
 * history. The Board access read, the Board's members and the workspace policy
 * are what exist; inviting, changing a permission, changing who can open the
 * Board and the history have no write or read behind them yet, so those
 * controls are drawn aria-disabled with the reason, and the views that need
 * them (permission menu, switch to Private, history) are drawn only when a
 * host gives the handler.
 */

const PERSON_TONE: Record<SharePersonView["tone"], string> = {
  blue: "bg-identity-blue text-identity-blue-foreground",
  coral: "bg-identity-coral text-identity-coral-foreground",
  gold: "bg-identity-gold text-identity-gold-foreground",
  sage: "bg-identity-sage text-identity-sage-foreground",
  ember: "bg-identity-ember text-identity-ember-foreground",
};

const MARK_TONE: Record<string, string> = {
  blue: "bg-mark-blue text-mark-blue-foreground",
  gold: "bg-mark-gold text-mark-gold-foreground",
  coral: "bg-mark-coral text-mark-coral-foreground",
  sage: "bg-mark-sage text-mark-sage-foreground",
  crimson: "bg-mark-crimson text-mark-crimson-foreground",
  violet: "bg-mark-violet text-mark-violet-foreground",
};

const FOCUS = "outline-none focus-visible:ring-2 focus-visible:ring-focus-ring";
const BTN =
  "inline-flex h-8 items-center gap-1.5 rounded-[7px] border border-line-strong bg-popover px-3 text-compact leading-[1.45] font-medium whitespace-nowrap text-foreground hover:bg-state-hover-strong aria-disabled:cursor-default dark:bg-card";
const BTN_PRIMARY =
  "inline-flex h-8 items-center gap-1.5 rounded-[7px] border border-primary bg-primary px-3 text-compact leading-[1.45] font-semibold whitespace-nowrap text-primary-foreground hover:bg-primary/90 aria-disabled:cursor-default aria-disabled:opacity-60";
const LEVEL =
  "box-border inline-flex h-[38px] items-center gap-1.5 rounded-[9px] border border-line-strong bg-popover px-2.5 text-compact leading-[1.45] font-medium whitespace-nowrap text-foreground aria-disabled:cursor-default dark:bg-card";

/** Rows shown before "Show n more": the mockup list is 248 px, five rows. */
const VISIBLE_ROWS = 5;

const LEVELS: readonly ShareLevel[] = ["view", "comment", "edit", "manage"];

function Avatar({
  person,
  className,
}: {
  person: SharePersonView;
  className?: string;
}) {
  return (
    <span
      data-f="av"
      aria-hidden="true"
      className={cn(
        "grid size-[30px] shrink-0 place-items-center rounded-full text-avatar-lg font-bold",
        PERSON_TONE[person.tone],
        className,
      )}
    >
      {person.initials}
    </span>
  );
}

/** A person the Board can be shared with. */
export interface ShareCandidate {
  readonly id: string;
  readonly name: string;
  readonly person: SharePersonView;
}

export interface BoardShareDialogSurfaceProps {
  readonly copy: BoardShareCopy;
  readonly open: boolean;
  readonly view: BoardShareView;
  readonly onClose: () => void;
  readonly onCopyLink: () => void;
  /** Change a person's permission; absent: the level buttons are inert. */
  readonly onChangeLevel?: (rowId: string, level: ShareLevel) => void;
  readonly onRemove?: (rowId: string) => void;
  /** Switch who can open the Board; absent: "Đổi" is inert. */
  readonly onChangeGeneral?: (target: "private") => void;
  /** Confirms the switch to Private, adding the chosen people first; absent: inert. */
  readonly onConfirmPrivate?: (addIds: readonly string[]) => void;
  /** Workspace members the Board is not shared with yet; with `onInvite` the add field is live. */
  readonly candidates?: readonly ShareCandidate[];
  /** Gives the person the chosen permission on the Board; absent: the add field is inert. */
  readonly onInvite?: (
    userId: string,
    level: ShareLevel,
  ) => Promise<void> | void;
  /** The history; absent: its link is inert. */
  readonly log?: readonly ShareLogEntry[] | null;
  /** People the switch to Private would cut off (stories). */
  readonly impact?: {
    readonly lose: number;
    readonly people: readonly ShareImpactPerson[];
    /** The kept permissions as a sentence, strong parts between `**`. */
    readonly keepText: string;
  } | null;
  /**
   * A refusal or a standing fact the host puts at the head of the dialog (the last
   * Manager refused, a workspace admin managing through Governance).
   */
  readonly notice?: {
    readonly kind: "refused" | "info";
    readonly title: string;
    readonly body: string;
    /** The refusal offers "Add a Manager": it focuses the add field at Manager level. */
    readonly addManager?: boolean;
  } | null;
  readonly initialView?: "share" | "private" | "log";
  readonly initialLevelMenu?: string | null;
}

function Strong({ text }: { text: string }) {
  const parts = text.split("**");
  return (
    <>
      {parts.map((part, index) =>
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

export function BoardShareDialogSurface(props: BoardShareDialogSurfaceProps) {
  const { copy, view, open } = props;
  const [screen, setScreen] = useState(props.initialView ?? "share");
  const [menuFor, setMenuFor] = useState<string | null>(
    props.initialLevelMenu ?? null,
  );
  const [managerToken, setManagerToken] = useState(0);
  const title = fill(copy.title, { name: view.boardTitle });
  const closeButton = (label: string) => (
    <button
      data-f="ib"
      type="button"
      aria-label={label}
      onClick={props.onClose}
      className={cn(
        "ml-auto grid size-8 shrink-0 place-items-center rounded-[7px] border-0 bg-transparent text-meta-foreground hover:bg-state-hover-strong hover:text-foreground",
        FOCUS,
      )}
    >
      <X aria-hidden="true" className="size-4" strokeWidth={1.8} />
    </button>
  );

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => (next ? undefined : props.onClose())}
    >
      <DialogContent
        showCloseButton={false}
        overlayClassName="bg-modal-scrim backdrop-blur-none"
        data-f="dlg"
        onOpenAutoFocus={(event) => {
          event.preventDefault();
          (event.currentTarget as HTMLElement)
            ?.querySelector<HTMLElement>("[data-f='ib']")
            ?.focus();
        }}
        className="top-[56px] box-border flex w-[640px] max-w-[calc(100%-2rem)] translate-y-0 flex-col gap-0 overflow-visible rounded-[14px] border-0 bg-popover p-0 text-body leading-[1.45] font-normal shadow-dialog ring-0 sm:max-w-[calc(100%-2rem)] dark:bg-card"
      >
        <div
          data-f="dh"
          className="flex items-center gap-2.5 px-5 pt-[18px] pb-1"
        >
          <DialogTitle
            data-f="dt"
            className="m-0 text-dialog-title leading-[1.45] font-[650] text-foreground"
          >
            {screen === "private" ? copy.privateConfirm.title : title}
          </DialogTitle>
          {closeButton(copy.close)}
        </div>
        <DialogDescription
          data-f="dsub"
          className="m-0 flex items-center gap-1.5 px-5 text-compact leading-[1.45] text-meta-foreground"
        >
          {view.space ? (
            <span
              aria-hidden="true"
              className={cn(
                "grid size-4 shrink-0 place-items-center rounded-[4px] text-mark-2xs font-bold",
                MARK_TONE[view.space.markTone],
              )}
            >
              {view.space.initials}
            </span>
          ) : null}
          {view.space
            ? fill(copy.subtitle, { space: view.space.name })
            : fill(copy.subtitleWorkspace, { workspace: view.workspaceName })}
        </DialogDescription>

        {screen === "share" ? (
          <ShareBody
            {...props}
            menuFor={menuFor}
            setMenuFor={setMenuFor}
            setScreen={setScreen}
            managerToken={managerToken}
            addManager={() => {
              setMenuFor(null);
              setManagerToken((token) => token + 1);
            }}
          />
        ) : screen === "log" ? (
          <LogBody {...props} setScreen={setScreen} />
        ) : (
          <PrivateBody {...props} setScreen={setScreen} />
        )}
      </DialogContent>
    </Dialog>
  );
}

type BodyProps = BoardShareDialogSurfaceProps & {
  setScreen: (screen: "share" | "private" | "log") => void;
};

function InertHint({
  reason,
  children,
}: {
  reason: string;
  children: ReactNode;
}) {
  return <span title={reason}>{children}</span>;
}

function ShareBody({
  copy,
  view,
  onCopyLink,
  onClose,
  onChangeLevel,
  onRemove,
  onChangeGeneral,
  candidates,
  onInvite,
  log,
  menuFor,
  setMenuFor,
  setScreen,
  notice,
  managerToken,
  addManager,
}: BodyProps & {
  menuFor: string | null;
  setMenuFor: (id: string | null) => void;
  managerToken: number;
  addManager: () => void;
}) {
  const g = copy.general;
  // The only person or team at Manager level cannot be lowered or removed: lock it from the start.
  const managers = view.rows.filter((row) => row.level === "manage");
  const lockedId = onChangeLevel && managers.length === 1 ? managers[0]!.id : null;
  const general = view.general;
  const [expandedRows, setExpandedRows] = useState(false);
  return (
    <>
      <div data-f="db" className="flex flex-col gap-4 px-5 pt-4 pb-1.5">
        {notice ? (
          <ShareNotice notice={notice} copy={copy} onAddManager={addManager} />
        ) : null}
        {candidates && onInvite ? (
          <AddPeople
            copy={copy}
            candidates={candidates}
            onInvite={onInvite}
            managerToken={managerToken}
          />
        ) : (
          <div data-f="add" className="flex gap-2">
            <label
              data-f="in"
              title={copy.add.unavailable}
              className="box-content flex h-[38px] flex-1 items-center gap-2 rounded-[9px] border border-line-strong px-2.5 text-nav leading-[1.45] font-normal text-meta-foreground"
            >
              <UserPlus
                aria-hidden="true"
                className="size-4 shrink-0"
                strokeWidth={1.8}
              />
              <input
                aria-disabled="true"
                readOnly
                placeholder={copy.add.placeholder}
                aria-label={copy.add.placeholder}
                className="h-full min-w-0 flex-1 cursor-default border-0 bg-transparent text-nav text-foreground outline-none placeholder:text-meta-foreground"
              />
            </label>
            <button
              data-f="lvl"
              type="button"
              aria-disabled="true"
              title={copy.add.unavailable}
              aria-label={fill(copy.add.level, { level: copy.levels.edit })}
              className={LEVEL}
            >
              {copy.levels.edit}
              <ChevronDown
                aria-hidden="true"
                className="size-[13px] text-meta-foreground"
                strokeWidth={1.8}
              />
            </button>
            <button
              data-f="invite"
              type="button"
              aria-disabled="true"
              title={copy.add.unavailable}
              className={cn(BTN_PRIMARY, "h-[38px] aria-disabled:opacity-100")}
            >
              {copy.add.invite}
            </button>
          </div>
        )}

        <div>
          <div
            data-f="lbl"
            className="mb-1.5 text-xs leading-[1.45] font-semibold text-meta-foreground"
          >
            {g.label}
          </div>
          <div
            data-f="ga"
            role="radiogroup"
            aria-label={g.group}
            className="flex items-start gap-3 rounded-[10px] border border-primary p-3 ring-2 ring-state-selected"
          >
            <span
              data-f="gaic"
              aria-hidden="true"
              className={cn(
                "grid size-[34px] shrink-0 place-items-center rounded-[9px] text-mark-md font-bold",
                general.kind === "space"
                  ? MARK_TONE[general.mark.tone]
                  : "bg-state-hover-strong text-muted-foreground",
              )}
            >
              {general.kind === "space" ? (
                general.mark.initials
              ) : general.kind === "workspace" ? (
                <Building2 className="size-4" strokeWidth={1.8} />
              ) : (
                <Lock className="size-4" strokeWidth={1.8} />
              )}
            </span>
            <span className="min-w-0" role="radio" aria-checked="true">
              <b
                data-f="gat"
                className="block text-body leading-[1.45] font-semibold text-foreground"
              >
                {general.kind === "space"
                  ? fill(g.bySpace, { name: general.name })
                  : general.kind === "workspace"
                    ? g.byWorkspace
                    : g.private}
              </b>
              <small
                data-f="gas"
                className="mt-px block text-crumb leading-normal font-normal text-meta-foreground"
              >
                {general.kind === "space"
                  ? general.count === null
                    ? g.bySpaceHintUnknown
                    : fill(g.bySpaceHint, { count: general.count })
                  : general.kind === "workspace"
                    ? g.byWorkspaceHint
                    : g.privateHint}
              </small>
              {general.kind === "space" ? (
                <span data-f="map" className="mt-2 flex flex-wrap gap-1.5">
                  {[g.mapAdmin, g.mapMember, g.mapViewer].map((text) => (
                    <span
                      key={text}
                      data-f="mapc"
                      className="inline-flex h-[22px] items-center rounded-[5px] bg-state-hover-strong px-[7px] text-micro leading-[1.45] text-muted-foreground"
                    >
                      {text}
                    </span>
                  ))}
                </span>
              ) : null}
            </span>
            <button
              data-f="lvl-plain"
              type="button"
              aria-haspopup="listbox"
              aria-expanded="false"
              aria-disabled={
                onChangeGeneral && general.kind !== "private" ? undefined : true
              }
              title={onChangeGeneral ? undefined : g.changeUnavailable}
              onClick={
                onChangeGeneral && general.kind !== "private"
                  ? () => {
                      onChangeGeneral("private");
                      setScreen("private");
                    }
                  : undefined
              }
              className={cn(
                "ml-auto box-border inline-flex h-[30px] items-center gap-1.5 rounded-[9px] border border-transparent bg-popover px-2.5 text-compact leading-[1.45] font-medium whitespace-nowrap text-foreground hover:border-line-strong aria-disabled:cursor-default dark:bg-card",
                FOCUS,
              )}
            >
              {g.change}
              <ChevronDown
                aria-hidden="true"
                className="size-[13px] text-meta-foreground"
                strokeWidth={1.8}
              />
            </button>
          </div>
        </div>

        {view.hiddenCounts ? (
          <p
            data-f="pcounts"
            className="m-0 text-compact leading-[1.45] text-meta-foreground"
          >
            {fill(copy.people.counts, {
              people: view.hiddenCounts.people,
              guests: view.hiddenCounts.guests,
            })}
          </p>
        ) : (
          <div>
            <div
              data-f="lbl"
              className="mb-1.5 text-xs leading-[1.45] font-semibold text-meta-foreground"
            >
              {fill(copy.people.label, { count: view.rows.length })}
            </div>
            {view.rows.length === 0 ? (
              <p className="m-0 text-compact leading-[1.45] text-meta-foreground">
                {copy.people.empty}
              </p>
            ) : (
              <div
                data-f="pl"
                role="group"
                tabIndex={0}
                aria-label={fill(copy.people.label, {
                  count: view.rows.length,
                })}
                className={cn(
                  "-mx-2 flex flex-col px-2 outline-none focus-visible:ring-2 focus-visible:ring-focus-ring [@media(max-height:820px)]:overflow-y-auto",
                  expandedRows || view.rows.length <= VISIBLE_ROWS
                    ? "max-h-[min(248px,max(120px,calc(100vh-573px)))]"
                    : "max-h-[min(250px,max(120px,calc(100vh-573px)))]",
                )}
              >
                {(expandedRows
                  ? view.rows
                  : view.rows.slice(0, VISIBLE_ROWS)
                ).map((row) => (
                  <PersonRow
                    key={row.id}
                    copy={copy}
                    row={row}
                    menuOpen={menuFor === row.id}
                    setMenu={(open) => setMenuFor(open ? row.id : null)}
                    onChangeLevel={onChangeLevel}
                    onRemove={onRemove}
                    locked={row.id === lockedId}
                    onAddManager={addManager}
                  />
                ))}
                {!expandedRows && view.rows.length > VISIBLE_ROWS ? (
                  <button
                    data-f="plmore"
                    type="button"
                    onClick={() => setExpandedRows(true)}
                    className={cn(
                      "mt-1 h-8 rounded-[7px] border-0 bg-transparent text-compact leading-[1.45] font-medium text-state-selected-foreground hover:bg-state-hover-strong",
                      FOCUS,
                    )}
                  >
                    {fill(copy.people.more, {
                      count: view.rows.length - VISIBLE_ROWS,
                    })}
                  </button>
                ) : null}
              </div>
            )}
          </div>
        )}

        <div
          data-f="lk"
          className="flex items-center gap-3 rounded-[10px] bg-background p-3"
        >
          <span
            data-f="lkic"
            aria-hidden="true"
            className="box-content grid size-[34px] shrink-0 place-items-center rounded-[9px] border border-line bg-popover text-muted-foreground dark:bg-card"
          >
            <Link2 className="size-4" strokeWidth={1.8} />
          </span>
          <span>
            <b
              data-f="lkt"
              className="block text-nav leading-[1.45] font-semibold text-foreground"
            >
              {copy.link.title}
            </b>
            <small
              data-f="lks"
              className="block text-xs leading-[1.45] font-normal text-meta-foreground"
            >
              {copy.link.hint}
            </small>
          </span>
          <span
            data-f="off"
            className="ml-auto flex items-center gap-[5px] text-right text-xs leading-[1.45] text-meta-foreground"
          >
            {view.policy === "unknown" ? null : (
              <>
                <Lock
                  aria-hidden="true"
                  className="size-3.5 shrink-0"
                  strokeWidth={1.8}
                />
                {view.policy === "off"
                  ? copy.link.publicOff
                  : copy.link.publicOn}
              </>
            )}
          </span>
        </div>
      </div>
      <div
        data-f="df"
        className="mt-3.5 flex items-center gap-2 border-t border-line px-5 py-3.5"
      >
        {log ? (
          <button
            data-f="logl"
            type="button"
            onClick={() => setScreen("log")}
            className={cn(
              "rounded-sm border-0 bg-transparent p-0 text-crumb leading-[1.45] font-medium text-state-selected-foreground hover:underline",
              FOCUS,
            )}
          >
            {copy.footer.log}
          </button>
        ) : (
          <InertHint reason={copy.footer.logUnavailable}>
            <button
              data-f="logl"
              type="button"
              aria-disabled="true"
              className="cursor-default rounded-sm border-0 bg-transparent p-0 text-crumb leading-[1.45] font-medium text-state-selected-foreground"
            >
              {copy.footer.log}
            </button>
          </InertHint>
        )}
        <span className="flex-1" />
        <button
          data-f="btn"
          type="button"
          onClick={onCopyLink}
          className={cn(BTN, FOCUS)}
        >
          <Link2 aria-hidden="true" className="size-4" strokeWidth={1.8} />
          {copy.footer.copy}
        </button>
        <button
          data-f="btn-pri"
          type="button"
          onClick={onClose}
          className={cn(BTN_PRIMARY, "text-crumb", FOCUS)}
        >
          {copy.footer.done}
        </button>
      </div>
    </>
  );
}

/** The add field: pick a workspace member, choose the permission, give it. */
function AddPeople({
  copy,
  candidates,
  onInvite,
  managerToken,
}: {
  copy: BoardShareCopy;
  candidates: readonly ShareCandidate[];
  onInvite: (userId: string, level: ShareLevel) => Promise<void> | void;
  /** Bumped by "Add a Manager": the field takes the Manager level and focus. */
  managerToken: number;
}) {
  const field = useRef<HTMLInputElement>(null);
  useEffect(() => {
    if (managerToken === 0) return;
    setLevel("manage");
    field.current?.focus();
  }, [managerToken]);
  const [query, setQuery] = useState("");
  const [picked, setPicked] = useState<ShareCandidate | null>(null);
  const [level, setLevel] = useState<ShareLevel>("edit");
  const [listOpen, setListOpen] = useState(false);
  const [levelOpen, setLevelOpen] = useState(false);
  const [busy, setBusy] = useState(false);
  const needle = query.trim().toLowerCase();
  const matches = candidates
    .filter((candidate) => candidate.name.toLowerCase().includes(needle))
    .slice(0, 6);
  const invite = async () => {
    if (!picked || busy) return;
    setBusy(true);
    try {
      await onInvite(picked.id, level);
      setPicked(null);
      setQuery("");
    } finally {
      setBusy(false);
    }
  };
  return (
    <div data-f="add" className="relative flex gap-2">
      <label
        data-f="in"
        className="box-content flex h-[38px] flex-1 items-center gap-2 rounded-[9px] border border-line-strong px-2.5 text-nav leading-[1.45] font-normal text-meta-foreground focus-within:ring-2 focus-within:ring-focus-ring"
      >
        <UserPlus
          aria-hidden="true"
          className="size-4 shrink-0"
          strokeWidth={1.8}
        />
        <input
          ref={field}
          role="combobox"
          aria-expanded={listOpen && matches.length > 0}
          aria-controls="board-share-candidates"
          aria-autocomplete="list"
          value={picked ? picked.name : query}
          placeholder={copy.add.placeholder}
          aria-label={copy.add.placeholder}
          onChange={(event) => {
            setPicked(null);
            setQuery(event.target.value);
            setListOpen(true);
          }}
          onFocus={() => setListOpen(true)}
          onBlur={() => setTimeout(() => setListOpen(false), 120)}
          onKeyDown={(event: KeyboardEvent<HTMLInputElement>) => {
            if (event.key === "Enter") {
              event.preventDefault();
              if (picked) void invite();
              else if (matches[0]) {
                setPicked(matches[0]);
                setListOpen(false);
              }
            }
          }}
          className="h-full min-w-0 flex-1 border-0 bg-transparent text-nav text-foreground outline-none placeholder:text-meta-foreground"
        />
      </label>
      {listOpen && !picked && matches.length > 0 ? (
        <ul
          id="board-share-candidates"
          role="listbox"
          className="absolute top-[44px] left-0 z-10 m-0 flex max-h-[220px] w-[60%] list-none flex-col overflow-auto rounded-[9px] border border-line-strong bg-popover p-1 shadow-dialog dark:bg-card"
        >
          {matches.map((candidate) => (
            <li key={candidate.id} role="option" aria-selected="false">
              <button
                type="button"
                onMouseDown={(event) => event.preventDefault()}
                onClick={() => {
                  setPicked(candidate);
                  setListOpen(false);
                }}
                className={cn(
                  "flex w-full items-center gap-2 rounded-[7px] border-0 bg-transparent px-2 py-1.5 text-left text-nav text-foreground hover:bg-state-hover-strong",
                  FOCUS,
                )}
              >
                <Avatar person={candidate.person} className="size-6" />
                <span className="truncate">{candidate.name}</span>
              </button>
            </li>
          ))}
        </ul>
      ) : null}
      <button
        data-f="lvl"
        type="button"
        aria-haspopup="listbox"
        aria-expanded={levelOpen}
        aria-label={fill(copy.add.level, { level: copy.levels[level] })}
        onClick={() => setLevelOpen((open) => !open)}
        className={cn(LEVEL, FOCUS)}
      >
        {copy.levels[level]}
        <ChevronDown
          aria-hidden="true"
          className="size-[13px] text-meta-foreground"
          strokeWidth={1.8}
        />
      </button>
      {levelOpen ? (
        <ul
          role="listbox"
          aria-label={copy.add.levelList}
          className="absolute top-[44px] right-[92px] z-10 m-0 flex w-[180px] list-none flex-col rounded-[9px] border border-line-strong bg-popover p-1 shadow-dialog dark:bg-card"
        >
          {LEVELS.map((option) => (
            <li key={option} role="option" aria-selected={option === level}>
              <button
                type="button"
                onClick={() => {
                  setLevel(option);
                  setLevelOpen(false);
                }}
                className={cn(
                  "flex w-full items-center gap-2 rounded-[7px] border-0 bg-transparent px-2 py-1.5 text-left text-nav text-foreground hover:bg-state-hover-strong",
                  FOCUS,
                )}
              >
                {copy.levels[option]}
                {option === level ? (
                  <Check
                    aria-hidden="true"
                    className="ml-auto size-3.5"
                    strokeWidth={1.8}
                  />
                ) : null}
              </button>
            </li>
          ))}
        </ul>
      ) : null}
      <button
        data-f="invite"
        type="button"
        disabled={!picked || busy}
        onClick={() => void invite()}
        className={cn(
          BTN_PRIMARY,
          "h-[38px] disabled:cursor-default disabled:opacity-60",
          FOCUS,
        )}
      >
        {copy.add.invite}
      </button>
    </div>
  );
}

function ShareNotice({
  notice,
  copy,
  onAddManager,
}: {
  notice: NonNullable<BoardShareDialogSurfaceProps["notice"]>;
  copy: BoardShareCopy;
  onAddManager: () => void;
}) {
  const refused = notice.kind === "refused";
  return (
    <div
      data-f="bn"
      role={refused ? "alert" : "status"}
      className={cn(
        "flex items-start gap-2.5 rounded-[10px] px-3.5 py-2.5 text-compact leading-[1.4]",
        refused
          ? "bg-danger-subtle text-danger-text"
          : "bg-state-selected text-state-selected-foreground",
      )}
    >
      {refused ? (
        <TriangleAlert
          aria-hidden="true"
          className="mt-0.5 size-4 shrink-0"
          strokeWidth={1.8}
        />
      ) : (
        <Info
          aria-hidden="true"
          className="mt-0.5 size-4 shrink-0"
          strokeWidth={1.8}
        />
      )}
      <span className="flex min-w-0 flex-1 flex-col gap-px">
        <b className="font-semibold text-foreground">{notice.title}</b>
        <span>{notice.body}</span>
      </span>
      {notice.addManager ? (
        <button
          type="button"
          onClick={onAddManager}
          className={cn(BTN, "h-7 shrink-0 bg-popover px-2.5 text-xs", FOCUS)}
        >
          {copy.menu.addManager}
        </button>
      ) : null}
    </div>
  );
}

function PersonRow({
  copy,
  row,
  menuOpen,
  setMenu,
  onChangeLevel,
  onRemove,
  locked,
  onAddManager,
}: {
  copy: BoardShareCopy;
  row: ShareRowView;
  menuOpen: boolean;
  setMenu: (open: boolean) => void;
  onChangeLevel?: (rowId: string, level: ShareLevel) => void;
  onRemove?: (rowId: string) => void;
  /** The only Manager of the Board: lower levels and removal are locked with the reason. */
  locked: boolean;
  onAddManager: () => void;
}) {
  const p = copy.people;
  const trigger = useRef<HTMLButtonElement>(null);
  const level = row.level ? copy.levels[row.level] : null;
  const detail = row.detail ?? "";
  const editable = Boolean(onChangeLevel) && !row.fixed && row.level !== null;
  return (
    <div
      data-f="pr"
      className="relative flex min-h-[50px] items-center gap-[11px]"
    >
      {row.kind === "team" ? (
        <span
          data-f="tm"
          aria-hidden="true"
          className="grid size-[30px] shrink-0 place-items-center rounded-[9px] bg-state-hover-strong text-muted-foreground"
        >
          <Users className="size-4" strokeWidth={1.8} />
        </span>
      ) : row.person ? (
        <Avatar person={row.person} />
      ) : (
        <span
          data-f="tm"
          aria-hidden="true"
          className="grid size-[30px] shrink-0 place-items-center rounded-full bg-state-hover-strong text-muted-foreground"
        >
          <Users className="size-4" strokeWidth={1.8} />
        </span>
      )}
      <span data-f="who" className="min-w-0 flex-1">
        <b
          data-f="wname"
          className="block truncate text-nav leading-[1.45] font-semibold text-foreground"
        >
          {row.name}
          {row.isYou ? (
            <span className="font-normal text-meta-foreground"> {p.you}</span>
          ) : null}
          {row.badge ? (
            <span
              data-f="badge"
              className={cn(
                "ml-1.5 rounded-[4px] px-1.5 py-px align-[1px] text-key leading-[1.45] font-semibold",
                row.badge === "outside"
                  ? "bg-danger-subtle text-danger-text"
                  : "bg-warning-subtle text-warning-text",
              )}
            >
              {row.badge === "outside" ? p.outside : p.guest}
            </span>
          ) : null}
        </b>
        {detail ? (
          <small
            data-f="wsub"
            className="block text-xs leading-[1.45] font-normal text-meta-foreground"
          >
            {detail}
          </small>
        ) : null}
      </span>
      {row.level === null ? null : row.fixed ? (
        <span
          data-f="from"
          className="px-2.5 text-xs leading-[1.45] whitespace-nowrap text-meta-foreground"
        >
          {level}
        </span>
      ) : (
        <Popover
          open={menuOpen && Boolean(onChangeLevel) && Boolean(row.level)}
          onOpenChange={(open) => {
            if (!open) {
              setMenu(false);
              trigger.current?.focus();
            }
          }}
        >
          <PopoverAnchor asChild>
            <button
              ref={trigger}
              data-f="lvl-plain"
              type="button"
              aria-haspopup="menu"
              aria-expanded={menuOpen}
              aria-disabled={editable ? undefined : true}
              title={editable ? undefined : p.levelUnavailable}
              aria-label={fill(p.levelOf, {
                name: row.name,
                level: level ?? "",
              })}
              onClick={editable ? () => setMenu(!menuOpen) : undefined}
              className={cn(
                "box-border inline-flex h-[30px] items-center gap-1.5 rounded-[9px] border border-transparent bg-popover px-2.5 text-compact leading-[1.45] font-medium whitespace-nowrap text-foreground hover:border-line-strong aria-disabled:cursor-default dark:bg-card",
                menuOpen && "border-primary ring-2 ring-state-selected",
                FOCUS,
              )}
            >
              {level}
              <ChevronDown
                aria-hidden="true"
                className="size-[13px] text-meta-foreground"
                strokeWidth={1.8}
              />
            </button>
          </PopoverAnchor>
          <PopoverContent
            side="bottom"
            align="end"
            sideOffset={6}
            collisionPadding={12}
            aria-label={fill(p.levelOf, {
              name: row.name,
              level: level ?? "",
            })}
            onOpenAutoFocus={(event) => {
              // Focus the checked choice once the popover owns focus, so the
              // dialog's focus trap does not take it back.
              event.preventDefault();
              const list = Array.from(
                (
                  event.currentTarget as HTMLElement
                ).querySelectorAll<HTMLElement>(
                  '[role="menuitemradio"]:not([aria-disabled="true"]),[role="menuitem"]:not([aria-disabled="true"])',
                ),
              );
              (
                list.find(
                  (item) => item.getAttribute("aria-checked") === "true",
                ) ?? list[0]
              )?.focus({ preventScroll: true });
            }}
            onFocusOutside={(event) => event.preventDefault()}
            className="w-[300px] gap-0 rounded-none border-0 bg-transparent p-0 shadow-none ring-0"
          >
            {onChangeLevel && row.level ? (
              <LevelMenu
                copy={copy}
                row={row}
                locked={locked}
                onAddManager={onAddManager}
                onPick={(next) => {
                  setMenu(false);
                  if (next !== row.level) onChangeLevel(row.id, next);
                  trigger.current?.focus();
                }}
                onRemove={
                  onRemove
                    ? () => {
                        setMenu(false);
                        onRemove(row.id);
                      }
                    : null
                }
                onClose={() => {
                  setMenu(false);
                  trigger.current?.focus();
                }}
              />
            ) : null}
          </PopoverContent>
        </Popover>
      )}
    </div>
  );
}

function LevelMenu({
  copy,
  row,
  locked,
  onAddManager,
  onPick,
  onRemove,
  onClose,
}: {
  copy: BoardShareCopy;
  row: ShareRowView;
  locked: boolean;
  onAddManager: () => void;
  onPick: (level: ShareLevel) => void;
  onRemove: (() => void) | null;
  onClose: () => void;
}) {
  const menu = useRef<HTMLDivElement>(null);
  const guest = row.badge !== null;
  const items = () =>
    Array.from(
      menu.current?.querySelectorAll<HTMLElement>(
        '[role="menuitemradio"]:not([aria-disabled="true"]),[role="menuitem"]:not([aria-disabled="true"])',
      ) ?? [],
    );
  const onKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    if (event.key === "Escape") {
      event.stopPropagation();
      onClose();
      return;
    }
    const list = items();
    const index = list.indexOf(document.activeElement as HTMLElement);
    if (event.key === "ArrowDown" || event.key === "ArrowUp") {
      event.preventDefault();
      const step = event.key === "ArrowDown" ? 1 : -1;
      list[(index + step + list.length) % list.length]?.focus();
    }
  };
  const itemClass =
    "mx-1 flex items-start gap-2.5 rounded-[7px] px-3 py-2 outline-none hover:bg-state-hover-strong focus-visible:bg-state-hover-strong";
  return (
    <div
      ref={menu}
      data-f="lmenu"
      role="menu"
      aria-label={fill(copy.menu.label, { name: row.name })}
      onKeyDown={onKeyDown}
      className="box-content w-[300px] rounded-[10px] border border-line-strong bg-popover py-1 text-nav font-normal shadow-popover dark:bg-card"
    >
      {LEVELS.map((level) => {
        const disabled = (level === "manage" && guest) || (locked && level !== "manage");
        const checked = row.level === level;
        return (
          <div
            key={level}
            data-f="lo"
            role="menuitemradio"
            aria-checked={checked}
            aria-disabled={disabled ? true : undefined}
            tabIndex={-1}
            onClick={disabled ? undefined : () => onPick(level)}
            onKeyDown={(event) => {
              if (event.key === "Enter" || event.key === " ") {
                event.preventDefault();
                event.stopPropagation();
                if (!disabled) onPick(level);
              }
            }}
            className={cn(
              itemClass,
              disabled && "cursor-not-allowed opacity-55 hover:bg-transparent",
            )}
          >
            <span>
              <b
                data-f="lot"
                className="block text-nav leading-[1.45] font-semibold text-foreground"
              >
                {copy.levels[level]}
              </b>
              <small
                data-f="loh"
                className="block text-xs leading-[1.45] font-normal text-meta-foreground"
              >
                {locked
                  ? checked
                    ? copy.menu.keepCurrent
                    : copy.menu.needManager
                  : disabled
                    ? copy.levelHints.guestManage
                    : copy.levelHints[level]}
              </small>
            </span>
            {checked ? (
              <Check
                aria-hidden="true"
                className="mt-0.5 ml-auto size-4 text-state-selected-foreground"
                strokeWidth={1.8}
              />
            ) : null}
          </div>
        );
      })}
      <div data-f="lsep" role="separator" className="my-1 h-px bg-line" />
      <div
        data-f="lo"
        role="menuitem"
        aria-disabled={onRemove && !locked ? undefined : true}
        tabIndex={-1}
        onClick={locked ? undefined : (onRemove ?? undefined)}
        onKeyDown={(event) => {
          if ((event.key === "Enter" || event.key === " ") && onRemove && !locked) {
            event.preventDefault();
            event.stopPropagation();
            onRemove();
          }
        }}
        className={cn(
          itemClass,
          (!onRemove || locked) &&
            "cursor-not-allowed opacity-55 hover:bg-transparent",
        )}
      >
        <span>
          <b
            data-f="lot"
            className="block text-nav leading-[1.45] font-semibold text-danger-text"
          >
            {copy.menu.remove}
          </b>
          <small
            data-f="loh"
            className="block text-xs leading-[1.45] font-normal text-meta-foreground"
          >
            {locked
              ? copy.menu.removeLast
              : fill(copy.menu.removeHint, {
                  name: row.shortName ?? row.name,
                })}
          </small>
        </span>
      </div>
      {locked ? (
        <>
          <div data-f="lsep" role="separator" className="my-1 h-px bg-line" />
          <div
            data-f="lnote"
            className="flex flex-col gap-2 px-3 pt-2 pb-1.5 text-xs leading-[1.45] text-muted-foreground"
          >
            <span>{copy.menu.lastNote}</span>
            <button
              type="button"
              onClick={onAddManager}
              className={cn(BTN, "h-7 self-start px-2.5 text-xs", FOCUS)}
            >
              <Plus aria-hidden="true" className="size-3.5" strokeWidth={1.8} />
              {copy.menu.addManager}
            </button>
          </div>
        </>
      ) : null}
    </div>
  );
}

function PrivateBody({
  copy,
  view,
  impact,
  onClose,
  onConfirmPrivate,
  setScreen,
}: BodyProps) {
  const c = copy.privateConfirm;
  // Everyone with work on the Board starts chosen to be added.
  const [chosen, setChosen] = useState<ReadonlySet<string>>(
    () => new Set((impact?.people ?? []).map((person) => person.id)),
  );
  const confirm = (ids: readonly string[]) => {
    onConfirmPrivate?.(ids);
    setScreen("share");
    onClose();
  };
  const general = view.general;
  const spaceName =
    general.kind === "space" ? general.name : view.workspaceName;
  return (
    <>
      <div data-f="db" className="flex flex-col gap-4 px-5 pt-4 pb-1.5">
        <div
          data-f="lbl"
          id="share-ga"
          className="m-0 text-xs leading-[1.45] font-semibold text-meta-foreground"
        >
          {copy.general.label}
        </div>
        <div
          role="radiogroup"
          aria-labelledby="share-ga"
          className="flex flex-col gap-4"
        >
          <div
            data-f="ga"
            role="radio"
            aria-checked="false"
            tabIndex={-1}
            className="flex items-start gap-3 rounded-[10px] border border-line p-3"
          >
            <span
              data-f="gaic"
              aria-hidden="true"
              className={cn(
                "grid size-[34px] shrink-0 place-items-center rounded-[9px] text-mark-md font-bold",
                general.kind === "space"
                  ? MARK_TONE[general.mark.tone]
                  : "bg-state-hover-strong",
              )}
            >
              {general.kind === "space" ? (
                general.mark.initials
              ) : (
                <Building2 className="size-4" />
              )}
            </span>
            <span>
              <b
                data-f="gat"
                className="block text-body leading-[1.45] font-semibold text-foreground"
              >
                {fill(c.current, { name: spaceName })}
              </b>
              <small
                data-f="gas"
                className="mt-px block text-crumb leading-normal text-meta-foreground"
              >
                {c.currentHint}
              </small>
            </span>
          </div>
          <div
            data-f="ga"
            role="radio"
            aria-checked="true"
            tabIndex={0}
            className="flex items-start gap-3 rounded-[10px] border border-primary p-3 ring-2 ring-state-selected"
          >
            <span
              data-f="gaic"
              aria-hidden="true"
              className="grid size-[34px] shrink-0 place-items-center rounded-[9px] bg-state-hover-strong text-muted-foreground"
            >
              <Lock className="size-4" strokeWidth={1.8} />
            </span>
            <span>
              <b
                data-f="gat"
                className="block text-body leading-[1.45] font-semibold text-foreground"
              >
                {c.privateTitle}
              </b>
              <small
                data-f="gas"
                className="mt-px block text-crumb leading-normal text-meta-foreground"
              >
                {c.privateHint}
              </small>
            </span>
          </div>
        </div>
        {impact ? (
          <div data-f="imp" className="flex flex-col gap-3">
            <div
              data-f="warn"
              className="flex items-start gap-2.5 rounded-[10px] border border-warning-text/40 bg-warning-subtle p-3 text-compact leading-normal font-normal text-warning-text"
            >
              <CircleAlert
                aria-hidden="true"
                className="mt-0.5 size-4 shrink-0"
                strokeWidth={1.8}
              />
              <span>
                <b className="font-semibold">
                  {fill(c.warn, { count: impact.lose, name: spaceName })}
                </b>
                {impact.people.length > 0
                  ? fill(c.warnWork, { work: impact.people.length })
                  : null}
              </span>
            </div>
            {impact.people.length > 0 ? (
              <ul
                data-f="imp-list"
                aria-label={c.workLabel}
                className="m-0 list-none overflow-hidden rounded-[10px] border border-line p-0"
              >
                {impact.people.map((person) => (
                  <li
                    key={person.id}
                    className="flex items-center gap-2.5 border-t border-line px-3 py-2.5 text-nav leading-[1.45] first:border-t-0"
                  >
                    <label className="flex min-w-0 flex-1 cursor-pointer items-center gap-2.5">
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
                        className="m-0 size-4 shrink-0 accent-primary"
                      />
                      <Avatar
                        person={person}
                        className="size-[26px] text-mark-xs"
                      />
                      <span>
                        <b className="font-semibold">{person.name}</b>
                        <small className="block text-xs leading-[1.45] text-meta-foreground">
                          {fill(c.workItems, { count: person.items })}
                        </small>
                      </span>
                    </label>
                    <span className="ml-auto text-xs text-meta-foreground">
                      {c.noPermission}
                    </span>
                  </li>
                ))}
              </ul>
            ) : null}
            <p className="m-0 text-crumb leading-[1.55] text-meta-foreground">
              {c.keep} <Strong text={impact.keepText} />
              {c.keepEnd}
            </p>
          </div>
        ) : null}
      </div>
      <div
        data-f="df"
        className="mt-3.5 flex items-center gap-2 border-t border-line px-5 py-3.5"
      >
        <span className="flex-1" />
        <button
          data-f="btn"
          type="button"
          onClick={() => {
            setScreen("share");
            onClose();
          }}
          className={cn(BTN, FOCUS)}
        >
          {c.cancel}
        </button>
        <button
          data-f="btn"
          type="button"
          aria-disabled={onConfirmPrivate ? undefined : true}
          title={onConfirmPrivate ? undefined : copy.general.changeUnavailable}
          onClick={onConfirmPrivate ? () => confirm([]) : undefined}
          className={cn(BTN, FOCUS)}
        >
          {c.switchOnly}
        </button>
        <button
          data-f="btn-pri"
          type="button"
          aria-disabled={onConfirmPrivate ? undefined : true}
          title={onConfirmPrivate ? undefined : copy.general.changeUnavailable}
          onClick={onConfirmPrivate ? () => confirm([...chosen]) : undefined}
          className={cn(BTN_PRIMARY, FOCUS)}
        >
          {fill(c.switchAdd, { count: chosen.size })}
        </button>
      </div>
    </>
  );
}

function LogBody({ copy, log, onClose, setScreen }: BodyProps) {
  const l = copy.log;
  return (
    <>
      <div data-f="db" className="flex flex-col gap-4 px-5 pt-4 pb-1.5">
        <div data-f="lh2" className="flex items-center gap-2">
          <button
            data-f="ib"
            type="button"
            aria-label={l.back}
            onClick={() => setScreen("share")}
            className={cn(
              "grid size-8 shrink-0 place-items-center rounded-[7px] border-0 bg-transparent text-meta-foreground hover:bg-state-hover-strong hover:text-foreground",
              FOCUS,
            )}
          >
            <ArrowLeft
              aria-hidden="true"
              className="size-4"
              strokeWidth={1.8}
            />
          </button>
          <h3
            id="share-log-title"
            className="m-0 text-item-heading leading-[1.45] text-foreground"
          >
            {l.title}
          </h3>
          <span className="flex-1" />
          <span className="text-xs leading-[1.45] text-meta-foreground">
            {l.onlyManagers}
          </span>
        </div>
        {log && log.length > 0 ? (
          <ul
            data-f="log"
            aria-labelledby="share-log-title"
            className="m-0 flex list-none flex-col p-0"
          >
            {log.map((entry) => (
              <li
                key={entry.id}
                className="flex items-start gap-[11px] border-t border-line py-[11px] text-compact leading-normal first:border-t-0"
              >
                {entry.actor ? (
                  <Avatar
                    person={entry.actor}
                    className="mt-px size-[26px] text-[9.5px]"
                  />
                ) : (
                  <span
                    data-f="av"
                    aria-hidden="true"
                    className="mt-px grid size-[26px] shrink-0 place-items-center rounded-[8px] bg-state-hover-strong text-muted-foreground"
                  >
                    <Users className="size-3.5" strokeWidth={1.8} />
                  </span>
                )}
                <span>
                  <Strong text={entry.text} />
                  <small className="block text-xs leading-normal text-meta-foreground">
                    {[entry.when, entry.note].filter(Boolean).join(" · ")}
                  </small>
                </span>
              </li>
            ))}
          </ul>
        ) : (
          <p className="m-0 text-compact text-meta-foreground">{l.empty}</p>
        )}
      </div>
      <div
        data-f="df"
        className="mt-3.5 flex items-center gap-2 border-t border-line px-5 py-3.5"
      >
        <button
          data-f="btn"
          type="button"
          onClick={() => setScreen("share")}
          className={cn(BTN, FOCUS)}
        >
          {l.backShort}
        </button>
        <span className="flex-1" />
        <button
          data-f="btn-pri"
          type="button"
          onClick={onClose}
          className={cn(BTN_PRIMARY, "text-crumb", FOCUS)}
        >
          {copy.footer.done}
        </button>
      </div>
    </>
  );
}
