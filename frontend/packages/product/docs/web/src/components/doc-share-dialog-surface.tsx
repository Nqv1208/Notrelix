import { useEffect, useRef, type KeyboardEvent, type ReactNode } from "react";
import {
  ChevronDown,
  CircleAlert,
  Link2,
  Lock,
  UserPlus,
  Users,
  X,
} from "lucide-react";
import { cn } from "@notrelix/ui-web";
import { fillDocCopy, type DocPageCopy } from "./doc-page.copy";
import type {
  DocPageCallbacks,
  DocShareMember,
  DocShareView,
} from "./doc-page-models";
import { DocAvatar, DocMark } from "./doc-page-ui";

export interface DocShareDialogSurfaceProps {
  readonly copy: DocPageCopy;
  readonly pageTitle: string;
  readonly share: DocShareView;
  readonly callbacks?: DocPageCallbacks;
  readonly onCopyLink?: () => void;
}

const OUTLINE =
  "inline-flex h-8 items-center gap-1.5 rounded-[7px] border border-line-strong bg-doc-panel px-3 text-compact leading-[1.45] font-medium text-foreground outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-default";

const LEVEL =
  "box-border inline-flex h-9.5 items-center gap-1.5 rounded-[9px] border border-line-strong bg-doc-panel px-2.5 text-compact leading-[1.45] font-medium whitespace-nowrap text-foreground outline-none aria-disabled:cursor-default";

const PLAIN_LEVEL =
  "inline-flex h-7.5 items-center gap-1.5 rounded-[9px] border border-transparent bg-doc-panel px-2.5 text-compact leading-[1.45] font-medium whitespace-nowrap text-foreground outline-none aria-disabled:cursor-default";

function Chevron() {
  return (
    <ChevronDown
      aria-hidden="true"
      className="size-[13px] stroke-[1.8] text-meta-foreground"
    />
  );
}

function levelLabel(copy: DocPageCopy, level: DocShareMember["level"]): string {
  const s = copy.share;
  return {
    manage: s.levelManage,
    edit: s.levelEdit,
    comment: s.levelComment,
    view: s.levelView,
  }[level];
}

/** Keeps Tab inside the dialog and closes it on Escape. */
function useModalFocus(
  dialog: React.RefObject<HTMLDivElement | null>,
  onClose: (() => void) | undefined,
) {
  useEffect(() => {
    const previous = document.activeElement as HTMLElement | null;
    const target =
      dialog.current?.querySelector<HTMLElement>("input:not([disabled])") ??
      dialog.current;
    target?.focus();
    // The page behind is inert until this unmounts; refocus after it clears.
    return () => {
      window.requestAnimationFrame(() => previous?.focus?.());
    };
  }, [dialog]);

  return (event: KeyboardEvent<HTMLDivElement>) => {
    if (event.key === "Escape") {
      event.stopPropagation();
      onClose?.();
      return;
    }
    if (event.key !== "Tab" || !dialog.current) return;
    const focusable = Array.from(
      dialog.current.querySelectorAll<HTMLElement>(
        'a[href],button:not([aria-disabled="true"]):not([disabled]),input:not([disabled])',
      ),
    );
    const first = focusable[0];
    const last = focusable[focusable.length - 1];
    if (!first || !last) return;
    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault();
      first.focus();
    }
  };
}

function PersonRow({
  member,
  copy,
}: {
  member: DocShareMember;
  copy: DocPageCopy;
}) {
  const s = copy.share;
  const level = levelLabel(copy, member.level);
  return (
    <div className="flex min-h-[50px] items-center gap-[11px]">
      {member.kind === "person" && member.person ? (
        <DocAvatar person={member.person} className="size-7.5 text-avatar-lg" />
      ) : (
        <span
          aria-hidden="true"
          className="grid size-7.5 shrink-0 place-items-center rounded-[9px] bg-state-hover-strong text-muted-foreground"
        >
          <Users className="size-4 stroke-[1.8]" />
        </span>
      )}
      <span className="min-w-0 flex-1">
        <b className="block truncate text-nav leading-[1.45] font-semibold">
          {member.name}
          {member.you ? (
            <span className="font-normal text-meta-foreground"> {s.you}</span>
          ) : null}
          {member.outside ? (
            <span className="ml-1.5 rounded-[4px] bg-danger-subtle px-1.5 py-px align-[1px] text-mark-md font-semibold text-danger-text">
              {s.outsideCompany}
            </span>
          ) : null}
        </b>
        <small className="block text-metadata leading-[1.45] text-meta-foreground tracking-normal">
          {[member.creator ? s.owner : null, member.detail]
            .filter(Boolean)
            .join(" · ")}
        </small>
      </span>
      {member.fixed ? (
        <span className="px-2.5 text-metadata whitespace-nowrap text-meta-foreground leading-[1.45] tracking-normal">
          {level}
        </span>
      ) : (
        <button
          type="button"
          aria-haspopup="menu"
          aria-disabled="true"
          title={s.unavailable}
          aria-label={fillDocCopy(s.personLevel, {
            name: member.name,
            level,
          })}
          className={PLAIN_LEVEL}
        >
          {level}
          <Chevron />
        </button>
      )}
    </div>
  );
}

/**
 * The share dialog (Final-Docs `chia-se`): 640 px over a scrim. Invite and
 * permission controls are disabled until Governance exposes page access;
 * the page link copy works.
 */
export function DocShareDialogSurface({
  copy,
  pageTitle,
  share,
  callbacks,
  onCopyLink,
}: DocShareDialogSurfaceProps): ReactNode {
  const s = copy.share;
  const dialog = useRef<HTMLDivElement | null>(null);
  const onKeyDown = useModalFocus(dialog, callbacks?.onCloseOverlay);

  return (
    <div
      className="fixed inset-0 z-modal flex items-start justify-center bg-modal-scrim pt-14"
      onKeyDown={onKeyDown}
    >
      <div
        ref={dialog}
        role="dialog"
        aria-modal="true"
        tabIndex={-1}
        aria-labelledby="doc-share-title"
        className="relative flex w-[640px] outline-none max-w-[calc(100%-2rem)] flex-col rounded-[14px] bg-doc-panel shadow-dialog"
      >
        <div className="flex items-center gap-2.5 px-5 pt-[18px] pb-1">
          <h2 id="doc-share-title" className="text-dialog-title">
            {fillDocCopy(s.title, { name: pageTitle })}
          </h2>
          <button
            type="button"
            aria-label={s.close}
            onClick={callbacks?.onCloseOverlay}
            className="ml-auto grid size-8 shrink-0 place-items-center rounded-[7px] text-meta-foreground outline-none hover:bg-state-hover hover:text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring"
          >
            <X aria-hidden="true" className="size-4 stroke-[1.8]" />
          </button>
        </div>
        <p className="m-0 flex items-center gap-1.5 px-5 text-compact leading-[1.45] text-meta-foreground">
          <DocMark
            tone={share.space.tone}
            className="size-4 rounded-[4px] text-mark-2xs"
          >
            {share.space.mark}
          </DocMark>
          {fillDocCopy(s.subtitle, { space: share.space.name })}
        </p>
        <div className="flex flex-col gap-4 px-5 pt-4 pb-1.5">
          <div className="flex gap-2">
            <label
              htmlFor="doc-share-add"
              className="box-border flex h-9.5 flex-1 cursor-text items-center gap-2 rounded-[9px] border border-line-strong px-2.5 text-meta-foreground focus-within:border-primary focus-within:ring-2 focus-within:ring-state-selected"
            >
              <UserPlus
                aria-hidden="true"
                className="size-4 shrink-0 stroke-[1.8]"
              />
              <span className="sr-only">{s.addPlaceholder}</span>
              <input
                id="doc-share-add"
                type="text"
                autoComplete="off"
                disabled
                placeholder={s.addPlaceholder}
                className="h-full min-w-0 flex-1 border-0 bg-transparent p-0 text-nav text-foreground outline-none placeholder:text-meta-foreground leading-[1.45]"
              />
            </label>
            <button
              type="button"
              aria-haspopup="listbox"
              aria-disabled="true"
              title={s.unavailable}
              aria-label={fillDocCopy(s.level, { level: s.levelEdit })}
              className={LEVEL}
            >
              {s.levelEdit}
              <Chevron />
            </button>
            <button
              type="button"
              aria-disabled="true"
              title={s.unavailable}
              className={cn(
                LEVEL,
                "rounded-[7px] border-primary bg-brand-solid px-3 font-semibold text-brand-solid-foreground aria-disabled:text-brand-solid-foreground",
              )}
            >
              {s.invite}
            </button>
          </div>

          <div>
            <div
              id="doc-share-access"
              className="mb-1.5 text-metadata leading-[1.45] font-semibold text-meta-foreground tracking-normal"
            >
              {s.accessLabel}
            </div>
            <div
              role="radiogroup"
              aria-labelledby="doc-share-access"
              className="flex items-start gap-3 rounded-[10px] border border-primary p-3 ring-2 ring-state-selected"
            >
              <DocMark
                tone={share.space.tone}
                className="size-8.5 rounded-[9px] text-mark-md"
              >
                {share.space.mark}
              </DocMark>
              <span
                role="radio"
                aria-checked="true"
                className="min-w-0"
                tabIndex={0}
              >
                <b className="block text-label leading-[1.45] font-semibold">
                  {fillDocCopy(s.accessBySpace, { space: share.space.name })}
                </b>
                {share.spaceMemberCount !== null ? (
                  <small className="mt-px block text-crumb leading-[1.5] text-meta-foreground">
                    {fillDocCopy(s.accessBySpaceHint, {
                      count: share.spaceMemberCount,
                    })}
                  </small>
                ) : null}
                <span className="mt-2 flex flex-wrap gap-1.5">
                  {[s.mapAdmin, s.mapMember, s.mapViewer].map((label) => (
                    <span
                      key={label}
                      className="inline-flex h-5.5 items-center rounded-[5px] bg-state-hover-strong px-[7px] text-micro text-muted-foreground"
                    >
                      {label}
                    </span>
                  ))}
                </span>
              </span>
              <button
                type="button"
                aria-haspopup="listbox"
                aria-disabled="true"
                title={s.unavailable}
                aria-label={s.changeAccess}
                className={cn(PLAIN_LEVEL, "ml-auto")}
              >
                {s.change}
                <Chevron />
              </button>
            </div>
          </div>

          <div>
            <div className="mb-1.5 text-metadata leading-[1.45] font-semibold text-meta-foreground tracking-normal">
              {fillDocCopy(s.peopleLabel, { count: share.members.length })}
            </div>
            <div className="flex flex-col">
              {share.members.map((member) => (
                <PersonRow key={member.id} member={member} copy={copy} />
              ))}
            </div>
            <p className="mt-1.5 mb-0 flex items-start gap-[9px] rounded-[9px] bg-doc-notice px-3 py-2.5 text-crumb leading-[1.5] text-doc-body">
              <CircleAlert
                aria-hidden="true"
                className="mt-px size-[15px] shrink-0 stroke-[1.8] text-brand-solid"
              />
              <span>{s.note}</span>
            </p>
          </div>

          <div className="flex items-center gap-3 rounded-[10px] bg-background p-3">
            <span
              aria-hidden="true"
              className="box-content grid size-8.5 shrink-0 place-items-center rounded-[9px] border border-border bg-doc-panel text-muted-foreground"
            >
              <Link2 className="size-4 stroke-[1.8]" />
            </span>
            <span>
              <b className="block text-nav leading-[1.45] font-semibold">
                {s.linkTitle}
              </b>
              <small className="block text-metadata leading-[1.45] text-meta-foreground tracking-normal">
                {s.linkHint}
              </small>
            </span>
            <span className="ml-auto flex items-center gap-[5px] text-right text-metadata leading-[1.45] text-meta-foreground tracking-normal">
              <Lock
                aria-hidden="true"
                className="size-[13px] shrink-0 stroke-[1.8]"
              />
              {s.linkPublicOff}
            </span>
          </div>
        </div>
        <div className="mt-3.5 flex items-center gap-2 border-t border-border px-5 py-3.5">
          <a
            href="#"
            aria-disabled="true"
            title={s.unavailable}
            onClick={(event) => event.preventDefault()}
            className="text-crumb leading-[1.45] font-medium text-brand-subtle-foreground no-underline"
          >
            {s.auditLog}
          </a>
          <span className="flex-1" />
          <button type="button" onClick={onCopyLink} className={OUTLINE}>
            <Link2 aria-hidden="true" className="size-4 stroke-[1.8]" />
            {s.copyLink}
          </button>
          <button
            type="button"
            onClick={callbacks?.onCloseOverlay}
            className={cn(
              OUTLINE,
              "border-primary bg-brand-solid text-crumb font-semibold text-brand-solid-foreground hover:bg-brand-solid",
            )}
          >
            {s.done}
          </button>
        </div>
      </div>
    </div>
  );
}
