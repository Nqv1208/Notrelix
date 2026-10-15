import {
  useMemo,
  useRef,
  useState,
  type KeyboardEvent,
  type ReactNode,
} from "react";
import {
  cn,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
} from "@notrelix/ui-web";
import { identityToneFor } from "../../core/model/navigation";
import type {
  SpaceMemberEntry,
  SpaceRole,
  SpaceTeam,
} from "../../core/model/spaces";
import type { SectionState } from "../home/home-models";
import { formatCopy, type ShellNavigation } from "../shell/shell-models";
import { ShellLink } from "../shell/shell-link";
import {
  isEmailLike,
  suggestTokens,
  teamRemoval,
  type AddSpaceMemberInput,
  type SpaceMemberCandidate,
  type TeamPersonView,
  type TypedToken,
} from "./space-members-models";
import { RoleTag } from "./space-members-view";
import { FOCUS, Glyph, SpaceAvatar } from "./space-page-parts";
import type { WorkspaceSpacesCopy } from "./workspace-spaces.copy";

const DIALOG =
  "top-[96px] box-border flex max-w-[calc(100%-2rem)] translate-y-0 flex-col gap-0 overflow-hidden rounded-[14px] border-0 bg-popover p-0 text-body leading-[1.45] font-normal shadow-dialog ring-0 sm:max-w-[calc(100%-2rem)] dark:bg-card";
const OVERLAY = "bg-modal-scrim backdrop-blur-none";
const BTN =
  "inline-flex h-8 items-center gap-1.5 rounded-[7px] border border-line-strong bg-popover px-3 text-compact leading-[1.45] font-medium whitespace-nowrap text-foreground hover:bg-state-hover-strong aria-disabled:cursor-default dark:bg-card";
const BTN_PRIMARY =
  "inline-flex h-8 items-center gap-1.5 rounded-[7px] border border-primary bg-primary px-3 text-compact leading-[1.45] font-semibold whitespace-nowrap text-primary-foreground hover:bg-primary/90 aria-disabled:cursor-default aria-disabled:opacity-60";

function CloseButton({
  label,
  onClick,
}: {
  label: string;
  onClick: () => void;
}) {
  return (
    <button
      data-f="ib"
      type="button"
      aria-label={label}
      onClick={onClick}
      className={cn(
        "ml-auto grid size-8 shrink-0 place-items-center rounded-[7px] border-0 bg-transparent text-meta-foreground hover:bg-state-hover-strong hover:text-foreground",
        FOCUS,
      )}
    >
      <Glyph name="xmark" />
    </button>
  );
}

export interface SpaceAddPreviewBase {
  readonly openBoards: number;
  readonly docs: number;
  readonly openChildren: readonly string[];
  readonly hidden: readonly string[];
}

export interface AddMembersDialogProps {
  readonly copy: WorkspaceSpacesCopy;
  readonly navigation: ShellNavigation;
  readonly open: boolean;
  readonly spaceName: string;
  readonly workspaceName: string;
  readonly users: SectionState<readonly SpaceMemberCandidate[]>;
  readonly teams: SectionState<readonly SpaceTeam[]>;
  /** `spaceMemberKey`s already in the department. */
  readonly taken: ReadonlySet<string>;
  readonly previewBase: SpaceAddPreviewBase | null;
  readonly adding: boolean;
  readonly progress: { readonly done: number; readonly total: number } | null;
  readonly error: string | null;
  readonly onClose: () => void;
  readonly onSubmit: (inputs: readonly AddSpaceMemberInput[]) => void;
  readonly initialTokens?: readonly TypedToken[];
  /** Board guests by email, so a typed guest is named and not just "not a member". */
  readonly guestsByEmail?: ReadonlyMap<
    string,
    { readonly name: string; readonly board: string }
  >;
}

function tokenKey(token: TypedToken): string {
  return token.kind === "person"
    ? `User:${token.candidate.id}`
    : token.kind === "team"
      ? `Team:${token.team.id}`
      : `email:${token.email}`;
}

export function AddMembersDialog(props: AddMembersDialogProps) {
  const { copy, open } = props;
  const a = copy.members.add;
  const [tokens, setTokens] = useState<readonly TypedToken[]>(
    props.initialTokens ?? [],
  );
  const [text, setText] = useState("");
  const [role, setRole] = useState<SpaceRole>("Member");
  const [roleOpen, setRoleOpen] = useState(false);
  const [active, setActive] = useState(0);
  const input = useRef<HTMLInputElement>(null);

  const users = props.users.status === "ready" ? props.users.data : [];
  const teams = props.teams.status === "ready" ? props.teams.data : [];
  const chosen = useMemo(
    () => new Set([...props.taken, ...tokens.map(tokenKey)]),
    [props.taken, tokens],
  );
  const suggestions = suggestTokens(text, users, teams, chosen);
  const valid = tokens.filter((token) => token.kind !== "outsider");
  const outsiders = tokens.filter(
    (token): token is Extract<TypedToken, { kind: "outsider" }> =>
      token.kind === "outsider",
  );

  const add = (token: TypedToken) => {
    if (tokens.some((existing) => tokenKey(existing) === tokenKey(token)))
      return;
    setTokens((current) => [...current, token]);
    setText("");
    setActive(0);
  };
  const commit = () => {
    const pick = suggestions[active] ?? suggestions[0];
    if (pick) return add(pick);
    if (isEmailLike(text)) add({ kind: "outsider", email: text.trim() });
  };
  const onKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === "Enter" || event.key === ",") {
      if (text.trim() === "") return;
      event.preventDefault();
      commit();
    } else if (event.key === "Backspace" && text === "" && tokens.length > 0) {
      setTokens((current) => current.slice(0, -1));
    } else if (event.key === "ArrowDown" && suggestions.length > 0) {
      event.preventDefault();
      setActive((index) => (index + 1) % suggestions.length);
    } else if (event.key === "ArrowUp" && suggestions.length > 0) {
      event.preventDefault();
      setActive(
        (index) => (index - 1 + suggestions.length) % suggestions.length,
      );
    }
  };

  const people =
    valid.length === 0
      ? 0
      : valid.reduce(
          (sum, token) =>
            sum + (token.kind === "team" ? token.team.memberCount : 1),
          0,
        );
  const base = props.previewBase;
  const submit = () => {
    if (valid.length === 0 || props.adding) return;
    props.onSubmit(
      valid.map((token) =>
        token.kind === "person"
          ? { subjectType: "User", subjectId: token.candidate.id, role }
          : {
              subjectType: "Team",
              subjectId: (token as Extract<TypedToken, { kind: "team" }>).team
                .id,
              role,
            },
      ),
    );
  };
  const submitLabel = props.adding
    ? props.progress
      ? formatCopy(a.progress, props.progress)
      : a.submitting
    : `${
        people === 1 ? a.submitOne : formatCopy(a.submit, { count: people })
      }${outsiders.length > 0 ? formatCopy(a.submitSkip, { count: outsiders.length }) : ""}`;

  const chip = (token: TypedToken) => {
    const bad = token.kind === "outsider";
    const name =
      token.kind === "person"
        ? token.candidate.name
        : token.kind === "team"
          ? token.team.name
          : token.email;
    return (
      <span
        key={tokenKey(token)}
        data-f="tk"
        className={cn(
          "inline-flex h-7 items-center gap-1.5 rounded-[7px] pr-1.5 pl-1 text-compact leading-[1.45] font-medium",
          bad
            ? "bg-warning-subtle text-warning-text ring-1 ring-warning-text/40 ring-inset"
            : "bg-state-hover-strong text-foreground",
        )}
      >
        {token.kind === "person" ? (
          <SpaceAvatar
            person={{
              id: token.candidate.id,
              name: token.candidate.name,
              tone: identityToneFor(token.candidate.id),
            }}
            className="size-5 text-mark-3xs"
          />
        ) : token.kind === "team" ? (
          <span
            aria-hidden="true"
            className="grid size-5 place-items-center rounded-[6px] bg-state-hover-strong text-muted-foreground"
          >
            <Glyph name="team" className="size-3" />
          </span>
        ) : (
          <Glyph name="alert" className="size-[13px]" />
        )}
        {name}
        {token.kind === "team" ? ` · ${token.team.memberCount}` : null}
        <button
          data-f="x"
          type="button"
          aria-label={formatCopy(a.remove, { name })}
          onClick={() =>
            setTokens((current) =>
              current.filter(
                (existing) => tokenKey(existing) !== tokenKey(token),
              ),
            )
          }
          className={cn(
            "grid size-[18px] place-items-center rounded-[4px] border-0 bg-transparent p-0 font-normal text-meta-foreground hover:bg-line-strong",
            FOCUS,
          )}
        >
          <Glyph name="xmark" className="size-3" />
        </button>
      </span>
    );
  };

  const roleLabel = copy.members.roleMenu[role];
  return (
    <Dialog
      open={open}
      onOpenChange={(next) => (next ? undefined : props.onClose())}
    >
      <DialogContent
        showCloseButton={false}
        overlayClassName={OVERLAY}
        data-f="dlg"
        onOpenAutoFocus={(event) => {
          event.preventDefault();
          input.current?.focus();
        }}
        className={cn(DIALOG, "w-[600px]")}
      >
        <div
          data-f="dh"
          className="flex items-center gap-2.5 px-5 pt-[18px] pb-1.5"
        >
          <DialogTitle
            data-f="dt"
            className="m-0 text-dialog-title leading-[1.45] font-[650] text-foreground"
          >
            {formatCopy(a.title, { name: props.spaceName })}
          </DialogTitle>
          <CloseButton label={a.close} onClick={props.onClose} />
        </div>
        <DialogDescription
          data-f="dsub"
          className="m-0 px-5 text-compact leading-[1.45] text-meta-foreground"
        >
          {formatCopy(a.subtitle, { workspace: props.workspaceName })}
        </DialogDescription>
        <div data-f="db" className="flex flex-col gap-3.5 px-5 py-4">
          <div className="relative">
            <div
              data-f="tok"
              className="box-border flex min-h-11 flex-wrap items-center gap-1.5 rounded-[10px] border border-primary px-2 py-1.5 ring-2 ring-state-selected"
              onClick={() => input.current?.focus()}
            >
              {tokens.map(chip)}
              <input
                ref={input}
                data-f="typing"
                type="text"
                role="combobox"
                aria-label={a.inputLabel}
                aria-expanded={suggestions.length > 0}
                aria-controls="space-add-suggestions"
                aria-activedescendant={
                  suggestions.length > 0 ? `space-add-opt-${active}` : undefined
                }
                value={text}
                placeholder={tokens.length === 0 ? a.placeholder : undefined}
                onChange={(event) => {
                  setText(event.target.value);
                  setActive(0);
                }}
                onKeyDown={onKeyDown}
                className="h-[19.5px] min-w-24 flex-1 border-0 bg-transparent pl-1 text-nav leading-[19.5px] font-normal text-foreground outline-none placeholder:text-meta-foreground"
              />
            </div>
            {suggestions.length > 0 ? (
              <ul
                id="space-add-suggestions"
                role="listbox"
                aria-label={a.suggestions}
                className="absolute top-full right-0 left-0 z-dropdown m-0 mt-1 list-none rounded-[10px] border border-line-strong bg-popover p-1 shadow-popover dark:bg-card"
              >
                {suggestions.map((token, index) => (
                  <li
                    key={tokenKey(token)}
                    id={`space-add-opt-${index}`}
                    role="option"
                    aria-selected={index === active}
                    onMouseDown={(event) => {
                      event.preventDefault();
                      add(token);
                    }}
                    className={cn(
                      "flex cursor-pointer items-center gap-2 rounded-[7px] px-2 py-1.5 text-compact leading-[1.45] text-foreground",
                      index === active && "bg-state-hover-strong",
                    )}
                  >
                    {token.kind === "person" ? (
                      <SpaceAvatar
                        person={{
                          id: token.candidate.id,
                          name: token.candidate.name,
                          tone: identityToneFor(token.candidate.id),
                        }}
                        className="size-5 text-mark-3xs"
                      />
                    ) : (
                      <span
                        aria-hidden="true"
                        className="grid size-5 place-items-center rounded-[6px] bg-state-hover-strong text-muted-foreground"
                      >
                        <Glyph name="team" className="size-3" />
                      </span>
                    )}
                    {token.kind === "person"
                      ? token.candidate.name
                      : token.kind === "team"
                        ? token.team.name
                        : token.email}
                    {token.kind === "team" ? (
                      <small className="ml-auto text-xs text-meta-foreground">
                        {formatCopy(a.teamPeople, {
                          count: token.team.memberCount,
                        })}
                      </small>
                    ) : null}
                  </li>
                ))}
              </ul>
            ) : null}
          </div>
          {outsiders.length > 0 ? (
            <div
              data-f="warn"
              role="alert"
              className="flex items-start gap-2.5 rounded-[9px] border border-warning-text/40 bg-warning-subtle px-3 py-2.5 text-crumb leading-normal font-normal text-warning-text"
            >
              <Glyph name="alert" className="mt-px size-[15px]" />
              <span>
                <b className="font-semibold">
                  {outsiders.every((item) =>
                    props.guestsByEmail?.has(item.email.toLowerCase()),
                  )
                    ? outsiders
                        .map((item) => {
                          const guest = props.guestsByEmail?.get(
                            item.email.toLowerCase(),
                          );
                          return formatCopy(a.outsiderGuest, {
                            name: guest?.name ?? item.email,
                            board: guest?.board ?? "",
                          });
                        })
                        .join(" ")
                    : formatCopy(a.outsider, {
                        email: outsiders.map((item) => item.email).join(", "),
                      })}
                </b>{" "}
                <ShellLink
                  destination={{ section: "members", tab: "invitations" }}
                  navigation={props.navigation}
                  className="font-semibold underline underline-offset-2"
                >
                  {a.outsiderLink}
                </ShellLink>
              </span>
            </div>
          ) : null}
          <div
            data-f="rowf"
            className="relative flex items-center gap-2.5 text-compact leading-[1.45]"
          >
            <span data-f="rowl" className="w-[120px] text-meta-foreground">
              {a.roleLabel}
            </span>
            <button
              data-f="rsel"
              type="button"
              aria-haspopup="listbox"
              aria-expanded={roleOpen}
              onClick={() => setRoleOpen((value) => !value)}
              onKeyDown={(event) => {
                if (event.key === "Escape" && roleOpen) {
                  event.stopPropagation();
                  setRoleOpen(false);
                }
              }}
              className={cn(
                "inline-flex h-[30px] items-center gap-1.5 rounded-[7px] border border-primary bg-popover px-[9px] text-compact leading-[1.45] font-medium text-foreground ring-2 ring-state-selected dark:bg-card",
                FOCUS,
              )}
            >
              <RoleTag role={role} />
              {roleLabel}
              <Glyph
                name="chevronDown"
                className="size-[13px] text-meta-foreground"
              />
            </button>
            {roleOpen ? (
              <ul
                role="listbox"
                aria-label={a.roleLabel}
                className="absolute top-9 left-[130px] z-dropdown m-0 w-[200px] list-none rounded-[10px] border border-line-strong bg-popover p-1 shadow-popover dark:bg-card"
              >
                {(["Admin", "Member", "Viewer"] as const).map((option) => (
                  <li
                    key={option}
                    role="option"
                    aria-selected={option === role}
                    tabIndex={0}
                    onClick={() => {
                      setRole(option);
                      setRoleOpen(false);
                    }}
                    onKeyDown={(event) => {
                      if (event.key === "Enter" || event.key === " ") {
                        event.preventDefault();
                        setRole(option);
                        setRoleOpen(false);
                      }
                    }}
                    className="flex cursor-pointer items-center gap-2 rounded-[7px] px-2 py-1.5 text-compact leading-[1.45] text-foreground outline-none hover:bg-state-hover-strong focus-visible:bg-state-hover-strong"
                  >
                    <RoleTag role={option} />
                    {copy.members.roleMenu[option]}
                  </li>
                ))}
              </ul>
            ) : null}
          </div>
          {base && valid.length > 0 ? (
            <div
              data-f="prev"
              className="rounded-[10px] border border-line bg-background px-3.5 py-3"
            >
              <b
                data-f="prevh"
                className="mb-1.5 block text-compact leading-[1.45] font-semibold text-foreground"
              >
                {people === 1
                  ? a.previewOne
                  : formatCopy(a.preview, { count: people })}
              </b>
              <ul className="m-0 flex list-none flex-col gap-[5px] p-0 text-compact leading-[1.45] text-foreground">
                <PreviewLine ok>
                  {formatCopy(a.previewOpen, {
                    boards: base.openBoards,
                    docs: base.docs,
                    name: props.spaceName,
                  })}
                </PreviewLine>
                {base.openChildren.length > 0 ? (
                  <PreviewLine ok>
                    {formatCopy(a.previewChildren, {
                      names: base.openChildren.join(", "),
                    })}
                  </PreviewLine>
                ) : null}
                {base.hidden.length > 0 ? (
                  <PreviewLine ok={false}>
                    {formatCopy(a.previewHidden, {
                      names:
                        base.hidden.length > 1
                          ? `${base.hidden.slice(0, -1).join(", ")}${a.hiddenAnd}${base.hidden[base.hidden.length - 1]}`
                          : base.hidden.join(", "),
                    })}
                  </PreviewLine>
                ) : null}
              </ul>
            </div>
          ) : null}
          {props.error ? (
            <p
              role="alert"
              className="m-0 text-crumb leading-[1.45] text-danger-text"
            >
              {props.error}
            </p>
          ) : null}
        </div>
        <div
          data-f="df"
          className="flex items-center gap-2 border-t border-line bg-background px-5 py-3.5"
        >
          <small
            data-f="dfs"
            className="text-xs leading-[1.45] font-normal text-meta-foreground"
          >
            {a.foot}
          </small>
          <span className="flex-1" />
          <button
            data-f="btn"
            type="button"
            onClick={props.onClose}
            className={cn(BTN, FOCUS)}
          >
            {a.cancel}
          </button>
          <button
            data-f="btn-pri"
            type="button"
            aria-disabled={
              valid.length === 0 || props.adding ? true : undefined
            }
            onClick={submit}
            className={cn(BTN_PRIMARY, FOCUS)}
          >
            {submitLabel}
          </button>
        </div>
      </DialogContent>
    </Dialog>
  );
}

function PreviewLine({ ok, children }: { ok: boolean; children: ReactNode }) {
  return (
    <li
      data-f="pl"
      className={cn("flex items-center gap-2", !ok && "text-meta-foreground")}
    >
      <Glyph
        name={ok ? "check" : "lock"}
        className={cn(
          "size-[14px]",
          ok ? "text-work-status-done" : "text-meta-foreground",
        )}
      />
      {children}
    </li>
  );
}

export interface RemoveMemberDialogProps {
  readonly copy: WorkspaceSpacesCopy;
  readonly entry: SpaceMemberEntry | null;
  readonly spaceName: string;
  readonly entries: readonly SpaceMemberEntry[];
  readonly teamPeople: SectionState<readonly TeamPersonView[]> | null;
  readonly removing: boolean;
  readonly onClose: () => void;
  readonly onConfirm: (entry: SpaceMemberEntry) => void;
}

export function RemoveMemberDialog(props: RemoveMemberDialogProps) {
  const { copy, entry } = props;
  const r = copy.members.remove;
  const isTeam = entry?.subjectType === "Team";
  const people =
    isTeam && props.teamPeople?.status === "ready"
      ? props.teamPeople.data
      : null;
  const impact = people ? teamRemoval(people, props.entries) : null;
  const title = entry
    ? isTeam
      ? formatCopy(r.teamTitle, { name: entry.displayName })
      : formatCopy(r.userTitle, {
          name: entry.displayName,
          space: props.spaceName,
        })
    : "";
  const text = !entry
    ? ""
    : !isTeam
      ? r.userText
      : impact
        ? impact.keep.length > 0
          ? formatCopy(r.teamTextKeep, {
              lose: impact.lose.length,
              keep: impact.keep.map((item) => item.person.name).join(", "),
            })
          : formatCopy(r.teamText, { lose: impact.lose.length })
        : r.teamNoCount;
  return (
    <Dialog
      open={entry !== null}
      onOpenChange={(next) => (next ? undefined : props.onClose())}
    >
      <DialogContent
        showCloseButton={false}
        overlayClassName={OVERLAY}
        data-f="dlg"
        className={cn(DIALOG, "w-[460px]")}
      >
        <div
          data-f="dh"
          className="flex items-center gap-2.5 px-5 pt-[18px] pb-1.5"
        >
          <DialogTitle
            data-f="dt"
            className="m-0 text-dialog-title leading-[1.45] font-[650] text-foreground"
          >
            {title}
          </DialogTitle>
          <CloseButton label={r.close} onClick={props.onClose} />
        </div>
        <div data-f="db" className="flex flex-col gap-3.5 px-5 py-4">
          <DialogDescription
            data-f="rtext"
            className="m-0 text-nav leading-[1.55] font-normal text-foreground"
          >
            {text}
          </DialogDescription>
          {impact ? (
            <ul
              data-f="loss"
              className="m-0 flex list-none flex-col gap-1.5 p-0 text-compact leading-[1.45]"
            >
              {impact.lose.map((person) => (
                <li key={person.id} className="flex items-center gap-2">
                  <SpaceAvatar
                    person={person}
                    className="size-[22px] text-avatar-sm"
                  />
                  {person.name}
                  <small className="text-xs text-meta-foreground">
                    {r.loses}
                  </small>
                </li>
              ))}
              {impact.keep.map(({ person, role }) => (
                <li key={person.id} className="flex items-center gap-2">
                  <SpaceAvatar
                    person={person}
                    className="size-[22px] text-avatar-sm"
                  />
                  {person.name}
                  <small className="text-xs text-meta-foreground">
                    {formatCopy(r.keeps, { role: copy.members.roleMenu[role] })}
                  </small>
                </li>
              ))}
            </ul>
          ) : null}
          <p
            data-f="rnote2"
            className="m-0 text-crumb leading-normal text-meta-foreground"
          >
            {r.note}
          </p>
        </div>
        <div
          data-f="df"
          className="flex items-center gap-2 border-t border-line bg-background px-5 py-3.5"
        >
          <span className="flex-1" />
          <button
            data-f="btn"
            type="button"
            onClick={props.onClose}
            className={cn(BTN, FOCUS)}
          >
            {r.cancel}
          </button>
          <button
            data-f="btn-danger"
            type="button"
            aria-disabled={props.removing ? true : undefined}
            onClick={() => entry && !props.removing && props.onConfirm(entry)}
            className={cn(
              "inline-flex h-8 items-center rounded-[7px] border border-danger-text bg-danger-text px-3 text-compact leading-[1.45] font-semibold whitespace-nowrap text-danger-foreground aria-disabled:cursor-default aria-disabled:opacity-60",
              FOCUS,
            )}
          >
            {isTeam ? r.confirmTeam : r.confirmUser}
          </button>
        </div>
      </DialogContent>
    </Dialog>
  );
}
