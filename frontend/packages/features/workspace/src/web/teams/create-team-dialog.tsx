import { useEffect, useMemo, useState, type FormEvent } from "react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
  cn,
} from "@notrelix/ui-web";
import { identityToneFor } from "../../core/model/navigation";
import { formatCopy } from "../shell/shell-models";
import {
  BTN,
  BTN_PRIMARY,
  FOCUS,
  Glyph,
  SpaceAvatar,
} from "../spaces/space-page-parts";
import {
  plainText,
  type ArchiveLoss,
  type TeamsExtData,
} from "./workspace-teams-models";
import type { WorkspaceTeamsCopy } from "./workspace-teams.copy";

/** The producer's Team name limit (`Team.cs`, 160). */
export const TEAM_NAME_MAX = 160;
/** The producer's Team description limit (`Team.cs`, 1024). */
export const TEAM_DESCRIPTION_MAX = 1024;

export interface CreateTeamValues {
  readonly name: string;
  /** `null` when the description is left empty. */
  readonly description: string | null;
  /** The person in charge; always in the Team. */
  readonly leadId: string;
  /** The others to add (the lead is not repeated). */
  readonly memberIds: readonly string[];
}

export type TeamCandidate = TeamsExtData["directory"][number];

export interface CreateTeamDialogProps {
  readonly copy: WorkspaceTeamsCopy;
  readonly workspaceName: string;
  readonly open: boolean;
  readonly pending: boolean;
  /** A failure message to show; the typed values are kept. */
  readonly error: string | null;
  /** Active people who can lead or join the Team. */
  readonly candidates: readonly TeamCandidate[];
  readonly onOpenChange: (open: boolean) => void;
  readonly onSubmit: (values: CreateTeamValues) => void;
}

const INPUT =
  "box-border h-9 w-full rounded-[8px] border border-line-strong bg-popover px-2.5 text-body leading-[1.45] text-foreground outline-none focus:border-primary focus:ring-2 focus:ring-state-selected dark:bg-card";

/**
 * Names a new Team (Final-Teams `tao-moi`): name, optional description, the
 * required Team lead and the people to add. The Team lead is in the Team
 * already. Writes are the host's: it creates the Team and adds the people.
 */
export function CreateTeamDialog({
  copy,
  workspaceName,
  open,
  pending,
  error,
  candidates,
  onOpenChange,
  onSubmit,
}: CreateTeamDialogProps) {
  const d = copy.createDialog;
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [leadId, setLeadId] = useState("");
  const [memberIds, setMemberIds] = useState<readonly string[]>([]);
  const [query, setQuery] = useState("");
  const [touched, setTouched] = useState(false);

  useEffect(() => {
    if (!open) return;
    setName("");
    setDescription("");
    setLeadId("");
    setMemberIds([]);
    setQuery("");
    setTouched(false);
  }, [open]);

  const trimmed = name.trim();
  const nameError =
    touched && trimmed === ""
      ? copy.create.nameRequired
      : trimmed.length > TEAM_NAME_MAX
        ? copy.create.nameTooLong
        : null;
  const leadError = touched && leadId === "" ? d.leadRequired : null;
  const byId = useMemo(
    () => new Map(candidates.map((candidate) => [candidate.id, candidate])),
    [candidates],
  );
  const suggestions = useMemo(() => {
    const needle = plainText(query.trim());
    if (!needle) return [];
    return candidates
      .filter(
        (candidate) =>
          candidate.id !== leadId &&
          !memberIds.includes(candidate.id) &&
          plainText(`${candidate.name} ${candidate.email ?? ""}`).includes(
            needle,
          ),
      )
      .slice(0, 6);
  }, [candidates, leadId, memberIds, query]);

  const lead = leadId ? byId.get(leadId) : undefined;
  const people = (leadId ? 1 : 0) + memberIds.length;

  const submit = (event: FormEvent) => {
    event.preventDefault();
    setTouched(true);
    if (trimmed === "" || trimmed.length > TEAM_NAME_MAX || leadId === "")
      return;
    if (description.length > TEAM_DESCRIPTION_MAX) return;
    onSubmit({
      name: trimmed,
      description: description.trim() === "" ? null : description.trim(),
      leadId,
      memberIds,
    });
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        showCloseButton={false}
        overlayClassName="bg-modal-scrim backdrop-blur-none"
        data-f="dlg"
        className="top-[110px] box-border flex w-[560px] max-w-[calc(100%-2rem)] translate-y-0 flex-col gap-0 overflow-hidden rounded-[14px] border-0 bg-popover p-0 text-body leading-[1.45] font-normal shadow-dialog ring-0 sm:max-w-[calc(100%-2rem)] dark:bg-card"
      >
        <form onSubmit={submit} noValidate className="contents">
          <div className="flex items-center gap-2.5 px-5 pt-[18px] pb-1.5">
            <DialogTitle className="m-0 text-dialog-title leading-[1.45] font-[650] text-foreground">
              {d.title}
            </DialogTitle>
            <button
              type="button"
              aria-label={copy.create.cancel}
              onClick={() => onOpenChange(false)}
              className={cn(
                "ml-auto grid size-8 place-items-center rounded-[7px] text-meta-foreground hover:bg-state-hover-strong hover:text-foreground",
                FOCUS,
              )}
            >
              <Glyph name="x" />
            </button>
          </div>
          <DialogDescription className="sr-only">{d.info}</DialogDescription>
          <div className="flex flex-col gap-3.5 px-5 pt-3.5 pb-4">
            <div className="flex flex-col gap-1.5">
              <label
                htmlFor="team-new-name"
                className="text-crumb leading-[1.45] font-semibold text-muted-foreground"
              >
                {d.nameLabel}
              </label>
              <input
                id="team-new-name"
                value={name}
                maxLength={TEAM_NAME_MAX + 20}
                onChange={(event) => setName(event.target.value)}
                aria-invalid={nameError ? true : undefined}
                className={INPUT}
              />
              {nameError ? (
                <p role="alert" className="m-0 text-crumb text-danger-text">
                  {nameError}
                </p>
              ) : null}
            </div>
            <div className="flex flex-col gap-1.5">
              <label
                htmlFor="team-new-desc"
                className="text-crumb leading-[1.45] font-semibold text-muted-foreground"
              >
                {d.descriptionLabel}{" "}
                <span className="font-medium text-meta-foreground">
                  {d.optional}
                </span>
              </label>
              <textarea
                id="team-new-desc"
                value={description}
                onChange={(event) => setDescription(event.target.value)}
                className={cn(INPUT, "h-16 resize-none py-2")}
              />
            </div>
            <div className="flex flex-col gap-1.5">
              <label
                htmlFor="team-new-lead"
                className="text-crumb leading-[1.45] font-semibold text-muted-foreground"
              >
                {d.leadLabel}{" "}
                <span className="font-medium text-meta-foreground">
                  {d.required}
                </span>
              </label>
              <select
                id="team-new-lead"
                required
                aria-required="true"
                value={leadId}
                onChange={(event) => setLeadId(event.target.value)}
                aria-invalid={leadError ? true : undefined}
                className={INPUT}
              >
                <option value="">{d.leadPlaceholder}</option>
                {candidates.map((candidate) => (
                  <option key={candidate.id} value={candidate.id}>
                    {candidate.name}
                    {candidate.email ? ` · ${candidate.email}` : ""}
                  </option>
                ))}
              </select>
              {leadError ? (
                <p role="alert" className="m-0 text-crumb text-danger-text">
                  {leadError}
                </p>
              ) : null}
            </div>
            <div className="flex flex-col gap-1.5">
              <label
                htmlFor="team-new-members"
                className="text-crumb leading-[1.45] font-semibold text-muted-foreground"
              >
                {d.membersLabel}
              </label>
              <div className="relative flex min-h-[42px] flex-wrap items-center gap-1.5 rounded-[8px] border border-line-strong px-2 py-1.5 focus-within:border-primary focus-within:ring-2 focus-within:ring-state-selected">
                {memberIds.map((id) => {
                  const person = byId.get(id);
                  if (!person) return null;
                  return (
                    <span
                      key={id}
                      className="inline-flex h-7 items-center gap-1.5 rounded-[7px] bg-state-hover-strong pr-1 pl-1 text-compact font-medium"
                    >
                      <SpaceAvatar
                        person={{
                          id,
                          name: person.name,
                          tone: identityToneFor(id),
                        }}
                        className="size-5 text-mark-2xs"
                      />
                      {person.name}
                      <button
                        type="button"
                        aria-label={formatCopy(d.removeMember, {
                          name: person.name,
                        })}
                        onClick={() =>
                          setMemberIds((current) =>
                            current.filter((value) => value !== id),
                          )
                        }
                        className={cn(
                          "grid size-[18px] place-items-center rounded-[4px] text-meta-foreground hover:bg-line-strong",
                          FOCUS,
                        )}
                      >
                        <Glyph name="x" className="size-3" />
                      </button>
                    </span>
                  );
                })}
                <input
                  id="team-new-members"
                  role="combobox"
                  aria-expanded={suggestions.length > 0}
                  aria-controls="team-new-suggest"
                  aria-describedby="team-new-members-hint"
                  autoComplete="off"
                  value={query}
                  placeholder={d.membersPlaceholder}
                  onChange={(event) => setQuery(event.target.value)}
                  onKeyDown={(event) => {
                    if (event.key === "Enter" && suggestions[0]) {
                      event.preventDefault();
                      setMemberIds((current) => [
                        ...current,
                        suggestions[0]!.id,
                      ]);
                      setQuery("");
                    } else if (
                      event.key === "Backspace" &&
                      query === "" &&
                      memberIds.length > 0
                    ) {
                      setMemberIds((current) => current.slice(0, -1));
                    }
                  }}
                  className="h-[26px] min-w-[120px] flex-1 border-0 bg-transparent text-body text-foreground outline-none placeholder:text-meta-foreground"
                />
                {suggestions.length > 0 ? (
                  <ul
                    id="team-new-suggest"
                    role="listbox"
                    className="absolute top-full right-0 left-0 z-10 m-0 mt-1 list-none rounded-[8px] border border-line-strong bg-popover p-1 shadow-dialog dark:bg-card"
                  >
                    {suggestions.map((person, index) => (
                      <li
                        key={person.id}
                        role="option"
                        aria-selected={index === 0}
                        onMouseDown={(event) => {
                          event.preventDefault();
                          setMemberIds((current) => [...current, person.id]);
                          setQuery("");
                        }}
                        className={cn(
                          "flex cursor-pointer items-center gap-2 rounded-[6px] px-2 py-1.5 text-compact",
                          index === 0 && "bg-state-hover-strong",
                        )}
                      >
                        <SpaceAvatar
                          person={{
                            id: person.id,
                            name: person.name,
                            tone: identityToneFor(person.id),
                          }}
                          className="size-5 text-mark-2xs"
                        />
                        {person.name}
                        {person.email ? (
                          <small className="text-xs text-meta-foreground">
                            {person.email}
                          </small>
                        ) : null}
                      </li>
                    ))}
                  </ul>
                ) : null}
              </div>
              <span
                id="team-new-members-hint"
                className="text-xs leading-normal text-meta-foreground"
              >
                {formatCopy(d.membersHint, { workspace: workspaceName })}
              </span>
            </div>
            <div className="flex items-start gap-2.5 rounded-[8px] border border-line bg-background px-3 py-2.5 text-crumb leading-normal text-muted-foreground">
              <Glyph name="infoCircle" className="mt-0.5 size-3.5" />
              <span>{d.info}</span>
            </div>
            {error ? (
              <p role="alert" className="m-0 text-crumb text-danger-text">
                {error}
              </p>
            ) : null}
          </div>
          <div className="flex items-center gap-2 border-t border-line bg-background px-5 py-3.5">
            <small className="text-xs text-meta-foreground">
              {lead
                ? formatCopy(d.footer, { count: people, lead: lead.name })
                : formatCopy(d.footerNoLead, { count: people })}
            </small>
            <span className="flex-1" />
            <button
              type="button"
              onClick={() => onOpenChange(false)}
              className={cn(BTN, FOCUS)}
            >
              {d.cancel}
            </button>
            <button
              type="submit"
              disabled={pending}
              className={cn(BTN_PRIMARY, FOCUS)}
            >
              {pending ? d.submitting : d.submit}
            </button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}

export interface ArchiveTeamDialogProps {
  readonly copy: WorkspaceTeamsCopy;
  readonly open: boolean;
  readonly teamName: string;
  readonly loss: ArchiveLoss | null;
  readonly pending: boolean;
  readonly onClose: () => void;
  readonly onConfirm: () => void;
}

/** Archive confirmation (Final-Teams `luu-tru`): who loses access and who stays. */
export function ArchiveTeamDialog({
  copy,
  open,
  teamName,
  loss,
  pending,
  onClose,
  onConfirm,
}: ArchiveTeamDialogProps) {
  const a = copy.archiveDialog;
  const where = loss?.where.join(", ") ?? "";
  const kept = loss?.keep[0] ?? null;
  return (
    <Dialog open={open} onOpenChange={(next) => (next ? undefined : onClose())}>
      <DialogContent
        showCloseButton={false}
        overlayClassName="bg-modal-scrim backdrop-blur-none"
        data-f="dlg"
        className="top-[110px] box-border flex w-[470px] max-w-[calc(100%-2rem)] translate-y-0 flex-col gap-0 overflow-hidden rounded-[14px] border-0 bg-popover p-0 text-body leading-[1.45] font-normal shadow-dialog ring-0 sm:max-w-[calc(100%-2rem)] dark:bg-card"
      >
        <div className="flex items-center gap-2.5 px-5 pt-[18px] pb-1.5">
          <DialogTitle className="m-0 text-dialog-title leading-[1.45] font-[650] text-foreground">
            {formatCopy(a.title, { team: teamName })}
          </DialogTitle>
          <button
            type="button"
            aria-label={a.cancel}
            onClick={onClose}
            className={cn(
              "ml-auto grid size-8 place-items-center rounded-[7px] text-meta-foreground hover:bg-state-hover-strong hover:text-foreground",
              FOCUS,
            )}
          >
            <Glyph name="x" />
          </button>
        </div>
        <div className="flex flex-col gap-3.5 px-5 pt-3.5 pb-4">
          <DialogDescription className="m-0 text-body leading-[1.55] text-foreground">
            {loss && loss.lose.length > 0
              ? formatCopy(a.lose, {
                  count: loss.lose.length,
                  where,
                  keep: kept ? formatCopy(a.keep, { name: kept.name }) : "",
                })
              : a.loseNone}
          </DialogDescription>
          {loss && (loss.lose.length > 0 || loss.keep.length > 0) ? (
            <ul className="m-0 flex list-none flex-col gap-1.5 p-0 text-compact">
              {loss.lose.map((person) => (
                <li key={person.id} className="flex items-center gap-2">
                  <SpaceAvatar
                    person={person}
                    className="size-[22px] text-mark-xs"
                  />
                  {person.name}
                  <small className="text-xs text-meta-foreground">
                    · {formatCopy(a.lostLine, { where })}
                  </small>
                </li>
              ))}
              {loss.keep.map((person) => (
                <li key={person.id} className="flex items-center gap-2">
                  <SpaceAvatar
                    person={person}
                    className="size-[22px] text-mark-xs"
                  />
                  {person.name}
                  <small className="text-xs text-meta-foreground">
                    ·{" "}
                    {formatCopy(a.keptLine, {
                      role: copy.page.spaceRoles[person.role],
                    })}
                  </small>
                </li>
              ))}
            </ul>
          ) : null}
          <p className="m-0 text-crumb leading-normal text-meta-foreground">
            {a.footnote}
          </p>
        </div>
        <div className="flex items-center gap-2 border-t border-line bg-background px-5 py-3.5">
          <span className="flex-1" />
          <button type="button" onClick={onClose} className={cn(BTN, FOCUS)}>
            {a.cancel}
          </button>
          <button
            type="button"
            disabled={pending}
            onClick={onConfirm}
            className={cn(
              "inline-flex h-8 items-center gap-1.5 rounded-[7px] border border-danger-text bg-danger-text px-3 text-compact font-semibold whitespace-nowrap text-background",
              FOCUS,
            )}
          >
            {pending ? a.confirming : a.confirm}
          </button>
        </div>
      </DialogContent>
    </Dialog>
  );
}
