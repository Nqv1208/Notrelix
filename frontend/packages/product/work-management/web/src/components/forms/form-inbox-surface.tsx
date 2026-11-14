import { Fragment, useId, useState, type ReactElement } from "react";
import {
  ChevronLeft,
  CircleAlert,
  SquareCheck,
  Trash2,
  TriangleAlert,
} from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  Skeleton,
  cn,
} from "@notrelix/ui-web";
import type {
  FormDetail,
  FormSubmission,
  FormSubmissionStatus,
} from "@notrelix/work-management-core";
import type { FormWriteResult } from "@notrelix/work-management-core";
import {
  BUTTON_CLASS,
  HINT_CLASS,
  ICON_BUTTON_CLASS,
  TEXTAREA_CLASS,
} from "../board-settings/board-settings-parts";
import { DisabledControl } from "../item-page/item-page-parts";
import { Chip, FormAlert, FormStatusChip } from "./forms-parts";
import { fillFormsCopy, type FormsCopy } from "./forms.copy";
import { answerText, targetName, type FormBoardField } from "./form-model";

export const REJECT_REASON_MAX = 500;
const TABS: readonly FormSubmissionStatus[] = [
  "new",
  "processed",
  "rejected",
  "spam",
];

export interface FormInboxSurfaceProps {
  readonly copy: FormsCopy;
  readonly locale: "vi" | "en";
  readonly timeZone: string;
  /** The moment "today" and "yesterday" are counted from. */
  readonly now: Date;
  readonly status: "loading" | "error" | "missing" | "ready";
  readonly form: FormDetail | null;
  readonly fields: readonly FormBoardField[];
  readonly submissions: readonly FormSubmission[];
  /** More responses are on the server: the list shows "Xem thêm phản hồi" at its foot. */
  readonly hasMore?: boolean;
  readonly loadingMore?: boolean;
  readonly onLoadMore?: () => void;
  /** Names of the board's people by user id. */
  readonly people: Readonly<Record<string, string>>;
  /** Who decided: shown on the history lines. */
  readonly deciderName?: string;
  readonly tab: FormSubmissionStatus;
  readonly onTab: (tab: FormSubmissionStatus) => void;
  readonly selectedId: string | null;
  readonly onSelect: (id: string | null) => void;
  /** A response someone else already handled was found when acting on it; the list is fresh. */
  readonly conflict?: boolean;
  /** The last decision failed; the response stays where it was. */
  readonly failed?: { readonly retry: () => void } | null;
  /** The board is archived: the responses stay readable, every decision on one is off. */
  readonly archived?: boolean;
  readonly onCreateItem: (submission: FormSubmission) => void;
  readonly onReject: (
    submission: FormSubmission,
    reason: string,
  ) => Promise<FormWriteResult>;
  readonly onSpam: (submission: FormSubmission) => Promise<FormWriteResult>;
  readonly onRemove: (submission: FormSubmission) => Promise<FormWriteResult>;
  readonly onOpenItem?: (itemId: string) => void;
  readonly onReload: () => void;
  readonly onBack: () => void;
  /** Names where Back goes when it is not the forms list (an editor returns to the board). */
  readonly backLabel?: string;
}

/** "09:12" today, "Hôm qua" the day before, else "09/10". */
export function whenShort(
  iso: string,
  now: Date,
  locale: "vi" | "en",
  timeZone: string,
  yesterday: string,
): string {
  const at = new Date(iso);
  const day = (d: Date) =>
    new Intl.DateTimeFormat("en-CA", { timeZone }).format(d);
  if (day(at) === day(now))
    return new Intl.DateTimeFormat(locale === "vi" ? "vi-VN" : "en-GB", {
      timeZone,
      hour: "2-digit",
      minute: "2-digit",
    }).format(at);
  if (day(at) === day(new Date(now.getTime() - 86_400_000))) return yesterday;
  return new Intl.DateTimeFormat("en-GB", {
    timeZone,
    day: "2-digit",
    month: "2-digit",
  }).format(at);
}

/** "Thứ Hai 12/10/2026, 09:12". */
export function whenLong(
  iso: string,
  locale: "vi" | "en",
  timeZone: string,
): string {
  const at = new Date(iso);
  const tag = locale === "vi" ? "vi-VN" : "en-GB";
  const date = new Intl.DateTimeFormat(tag, {
    timeZone,
    weekday: "long",
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  }).format(at);
  const time = new Intl.DateTimeFormat(tag, {
    timeZone,
    hour: "2-digit",
    minute: "2-digit",
  }).format(at);
  return `${date.replace(",", "")}, ${time}`;
}

/**
 * The response inbox of a form: four tabs with their counts, the list of the
 * open tab and the detail of the chosen response with the board field each
 * answer goes into. Someone who can edit the board creates an item from a response, rejects it
 * (with an optional reason), marks it spam or deletes it. Pure: every decision
 * is a callback; "create item" hands the response to the host's dialog.
 */
export function FormInboxSurface(props: FormInboxSurfaceProps) {
  const { copy, status, form } = props;
  const t = copy.inbox;

  if (status === "loading" && !form)
    return (
      <Shell>
        <div
          role="status"
          aria-label={t.loading}
          className="flex flex-col gap-2"
        >
          {[0, 1, 2].map((n) => (
            <Skeleton key={n} className="h-11 rounded-[10px]" />
          ))}
        </div>
      </Shell>
    );
  if (status === "missing" || !form)
    return (
      <Shell>
        <p className="m-0 rounded-[12px] border border-line bg-popover p-4 text-item-ui text-muted-foreground">
          {status === "error" ? copy.common.loadError : t.gone}
        </p>
      </Shell>
    );

  const counts: Record<FormSubmissionStatus, number> = {
    new: 0,
    processed: 0,
    rejected: 0,
    spam: 0,
  };
  for (const s of props.submissions) counts[s.status] += 1;
  const inTab = props.submissions.filter((s) => s.status === props.tab);
  const selected = inTab.find((s) => s.id === props.selectedId) ?? null;

  return (
    <Shell>
      <section
        aria-label={t.aria}
        className="flex min-h-0 flex-1 flex-col overflow-hidden rounded-[12px] border border-line bg-popover"
      >
        <div className="flex flex-wrap items-center gap-3 border-b border-line px-4 py-3">
          <button
            type="button"
            onClick={props.onBack}
            className={cn(BUTTON_CLASS.ghost, "h-8 px-2 max-md:h-11")}
          >
            <ChevronLeft aria-hidden="true" />
            {props.backLabel ?? copy.common.back}
          </button>
          <h1
            className="m-0 flex-1 truncate text-[14px] font-bold"
            title={form.title}
          >
            {fillFormsCopy(t.title, { name: form.title })}
          </h1>
          <FormStatusChip status={form.status} copy={copy} />
        </div>
        <div
          role="tablist"
          aria-label={t.tabsAria}
          className="flex gap-0.5 border-b border-line px-4"
        >
          {TABS.map((tab) => (
            <button
              key={tab}
              type="button"
              role="tab"
              id={`inbox-tab-${tab}`}
              aria-selected={props.tab === tab}
              onClick={() => props.onTab(tab)}
              className={cn(
                "inline-flex h-[34px] max-md:h-11 cursor-pointer items-center gap-1.5 border-0 border-b-2 border-transparent bg-transparent px-3 text-item-small font-medium text-muted-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
                props.tab === tab &&
                  "border-primary font-semibold text-foreground",
              )}
            >
              {t.tabs[tab]}
              <small
                className={cn(
                  "text-micro font-semibold text-meta-foreground",
                  props.tab === tab && "text-brand-subtle-foreground",
                )}
              >
                {counts[tab]}
              </small>
            </button>
          ))}
        </div>

        {props.archived ? (
          <div
            role="status"
            className="border-b border-line bg-warning-subtle px-4 py-2.5 text-item-small"
          >
            <b>{t.archivedTitle}</b> {t.archivedBody}
          </div>
        ) : null}

        <div className="flex min-h-0 flex-1 flex-col md:flex-row">
          <div
            role="tabpanel"
            aria-labelledby={`inbox-tab-${props.tab}`}
            className="flex max-h-[46vh] min-w-0 flex-col overflow-auto border-b border-line md:max-h-none md:w-[340px] md:shrink-0 xl:w-[421px] md:border-r md:border-b-0"
          >
            {status === "loading" ? (
              <div
                role="status"
                aria-label={t.loading}
                className="flex flex-col gap-2 p-3"
              >
                {[0, 1, 2].map((n) => (
                  <Skeleton key={n} className="h-11 rounded-[10px]" />
                ))}
              </div>
            ) : status === "error" ? (
              <div className="m-3 rounded-[12px] border border-line bg-popover p-3.5">
                <FormAlert
                  message={t.loadError}
                  onRetry={props.onReload}
                  retryLabel={copy.common.retry}
                />
              </div>
            ) : inTab.length === 0 ? (
              <div className="m-3 rounded-[10px] border border-dashed border-line-strong bg-popover px-4 py-5 text-center text-[14px] leading-[1.45]">
                <b className="block text-item-ui font-semibold">
                  {t.empty[props.tab]}
                </b>
                <span className="text-crumb text-meta-foreground">
                  {t.empty[`${props.tab}Body` as `${FormSubmissionStatus}Body`]}
                </span>
              </div>
            ) : (
              <ResponseList {...props} items={inTab} />
            )}
            {status === "ready" && props.hasMore && props.onLoadMore ? (
              <div className="flex justify-center px-3 py-3">
                <button
                  type="button"
                  disabled={props.loadingMore}
                  onClick={props.onLoadMore}
                  className={cn(BUTTON_CLASS.outline, "h-8 max-md:h-11")}
                >
                  {props.loadingMore ? t.loadingMore : t.more}
                </button>
              </div>
            ) : null}
          </div>

          <div className="min-w-0 flex-1 overflow-auto p-4">
            {selected ? (
              <ResponseDetail {...props} form={form} response={selected} />
            ) : (
              <div className="flex flex-col gap-3.5">
                {props.conflict ? <ConflictNotice copy={copy} /> : null}
                <p className="m-0 text-item-small text-meta-foreground">
                  {t.pick}
                </p>
              </div>
            )}
          </div>
        </div>
      </section>
    </Shell>
  );
}

/** The warning a decision on a response that someone else already handled leaves (the drawn warn `.alert`). */
function ConflictNotice({ copy }: { copy: FormsCopy }) {
  const t = copy.inbox;
  return (
    <div
      role="alert"
      className="flex items-start gap-2 text-crumb text-warning-text"
    >
      <CircleAlert aria-hidden="true" className="size-4 shrink-0" />
      <span className="flex-1">
        <b>{t.conflictTitle}</b> {t.conflictBody}
      </span>
    </div>
  );
}

function Shell({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-0 min-w-0 flex-1 flex-col bg-background p-4 text-foreground sm:p-6">
      <div className="mx-auto flex min-h-0 w-full max-w-[1400px] flex-1 flex-col">
        {children}
      </div>
    </div>
  );
}

function titleOf(response: FormSubmission, copy: FormsCopy): string {
  const title = response.answers.title;
  if (typeof title === "string" && title.trim() !== "") return title;
  const first = Object.values(response.answers).find(
    (value) => typeof value === "string" && value.trim() !== "",
  );
  return typeof first === "string" ? first : copy.inbox.untitled;
}

function personName(
  response: FormSubmission,
  props: FormInboxSurfaceProps,
): string {
  return (
    (response.submitterUserId
      ? props.people[response.submitterUserId]
      : undefined) ??
    response.submitterEmail ??
    props.copy.inbox.unknownPerson
  );
}

function ResponseList(
  props: FormInboxSurfaceProps & { items: readonly FormSubmission[] },
) {
  const { copy, items } = props;
  const unread = props.tab === "new";
  function onKeyDown(event: React.KeyboardEvent) {
    if (event.key !== "ArrowDown" && event.key !== "ArrowUp") return;
    event.preventDefault();
    // The move starts from the row that has the focus, else from the selected one.
    const focused = (event.target as HTMLElement)
      .closest("[role=option]")
      ?.id.replace("inbox-row-", "");
    const index = items.findIndex(
      (s) => s.id === (focused ?? props.selectedId),
    );
    const next =
      items[
        Math.min(
          items.length - 1,
          Math.max(0, index + (event.key === "ArrowDown" ? 1 : -1)),
        )
      ];
    if (next) {
      props.onSelect(next.id);
      document.getElementById(`inbox-row-${next.id}`)?.focus();
    }
  }
  return (
    <ul
      role="listbox"
      aria-label={copy.inbox.listAria}
      onKeyDown={onKeyDown}
      className="m-0 flex list-none flex-col p-0"
    >
      {items.map((s, index) => {
        const on = s.id === props.selectedId;
        return (
          <li key={s.id} role="presentation">
            <button
              type="button"
              role="option"
              id={`inbox-row-${s.id}`}
              aria-selected={on}
              tabIndex={
                on || (props.selectedId === null && index === 0) ? 0 : -1
              }
              onClick={() => props.onSelect(s.id)}
              className={cn(
                "grid min-h-[58px] w-full cursor-pointer grid-cols-[20px_minmax(0,1fr)_auto] items-center gap-x-2.5 border-0 border-b border-line px-3 py-2.5 text-left outline-none focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:ring-inset",
                on ? "bg-row-selected" : "bg-transparent",
              )}
            >
              <span
                aria-hidden="true"
                className={cn(
                  "row-span-2 size-2 shrink-0 rounded-full",
                  unread && "bg-primary",
                )}
              />
              <b
                title={titleOf(s, copy)}
                className={cn(
                  "min-w-0 truncate text-item-ui",
                  unread ? "font-[650]" : "font-medium",
                )}
              >
                {titleOf(s, copy)}
              </b>
              <span className="row-span-2 text-metadata text-meta-foreground">
                {whenShort(
                  s.submittedAt,
                  props.now,
                  props.locale,
                  props.timeZone,
                  copy.inbox.yesterday,
                )}
              </span>
              <span className="min-w-0 truncate text-metadata text-meta-foreground">
                {personName(s, props)}
              </span>
            </button>
          </li>
        );
      })}
    </ul>
  );
}

function ResponseDetail(
  props: FormInboxSurfaceProps & { form: FormDetail; response: FormSubmission },
) {
  const { copy, response, form } = props;
  const t = copy.inbox;
  const id = useId();
  const [rejecting, setRejecting] = useState(false);
  const [reason, setReason] = useState("");
  const [confirmRemove, setConfirmRemove] = useState(false);
  const decider = props.deciderName ?? "";
  // An archived board refuses every decision: the control stays, says why and never acts.
  const gate = (control: ReactElement<{ onClick?: () => void }>) =>
    props.archived ? (
      <DisabledControl reason={t.archivedReason}>{control}</DisabledControl>
    ) : (
      control
    );
  const by = personName(response, props);
  const sentAt = whenLong(response.submittedAt, props.locale, props.timeZone);
  const decidedAt = response.processedAt
    ? whenLong(response.processedAt, props.locale, props.timeZone)
    : "";

  async function reject() {
    const result = await props.onReject(response, reason);
    if (result.ok) {
      setRejecting(false);
      setReason("");
    }
  }

  return (
    <article
      aria-label={t.detailAria}
      className="flex flex-col gap-7 rounded-[12px] border border-line bg-popover p-5"
    >
      <header className="flex items-start gap-3">
        <div className="flex min-w-0 flex-1 flex-col gap-0.5">
          <h2 className="m-0 text-[16px] leading-[1.45] font-[650]">
            {titleOf(response, copy)}
          </h2>
          <span className="text-metadata leading-[1.45] text-meta-foreground">
            {by} · {sentAt}
          </span>
        </div>
        <Chip tone={response.status === "new" ? "brand" : "neutral"}>
          {t.status[response.status]}
        </Chip>
      </header>

      <dl className="m-0 grid grid-cols-1 gap-x-3 gap-y-2 text-item-small md:grid-cols-[130px_minmax(0,1fr)]">
        {form.questions.map((q) => {
          const target = targetName(q, props.fields, copy);
          const text = answerText(q, response.answers[q.key], props.fields);
          return (
            <Fragment key={q.id}>
              <dt className="text-meta-foreground">{q.label}</dt>
              <dd className="m-0 flex flex-wrap items-center gap-x-2 gap-y-1 break-words whitespace-pre-wrap">
                {text === "" ? (
                  <span className="text-meta-foreground">{t.noAnswer}</span>
                ) : (
                  text
                )}
                {target ? (
                  <Chip tone="brand">
                    {fillFormsCopy(t.mapsTo, { field: target })}
                  </Chip>
                ) : (
                  <Chip tone="neutral">{t.notMapped}</Chip>
                )}
              </dd>
            </Fragment>
          );
        })}
      </dl>

      {response.status === "processed" ? (
        <div className="flex flex-wrap items-center gap-3 rounded-[10px] bg-success-subtle px-3.5 py-2.5 text-item-small">
          <span className="min-w-0 flex-1">
            {fillFormsCopy(t.processedBannerNoItem, {
              name: decider,
              when: decidedAt,
            })}
          </span>
          {response.createdItemId && props.onOpenItem ? (
            <button
              type="button"
              onClick={() => props.onOpenItem?.(response.createdItemId!)}
              className={cn(BUTTON_CLASS.outline, "h-7")}
            >
              {t.openItem}
            </button>
          ) : null}
        </div>
      ) : null}
      {response.status === "rejected" ? (
        <p className="m-0 rounded-[10px] bg-state-hover-strong px-3.5 py-2.5 text-item-small">
          {response.reason ? (
            <>
              <b>{t.rejectedReason}</b> {response.reason} ·{" "}
              {fillFormsCopy(t.rejectedBy, { name: decider, when: decidedAt })}
            </>
          ) : (
            fillFormsCopy(t.rejectedNoReason, {
              name: decider,
              when: decidedAt,
            })
          )}
        </p>
      ) : null}
      {response.status === "spam" ? (
        <p className="m-0 rounded-[10px] bg-state-hover-strong px-3.5 py-2.5 text-item-small">
          {fillFormsCopy(t.spamBanner, { name: decider, when: decidedAt })}
        </p>
      ) : null}

      {props.conflict ? (
        <div className="-mb-3.5">
          <ConflictNotice copy={copy} />
        </div>
      ) : null}

      {props.failed ? (
        <FormAlert
          message={t.actionFailed}
          onRetry={props.failed.retry}
          retryLabel={copy.common.retry}
        />
      ) : null}

      {confirmRemove ? (
        <div
          role="alertdialog"
          aria-label={t.removeAria}
          className="flex flex-col gap-2.5 rounded-[12px] border border-line bg-popover p-3.5"
        >
          <div className="flex items-start gap-2 text-crumb text-danger-text">
            <CircleAlert aria-hidden="true" className="size-4 shrink-0" />
            <span className="flex-1">
              {t.removeAsk.split("{{name}}")[0]}
              <b>{by}</b>
              {t.removeAsk.split("{{name}}")[1]}
            </span>
          </div>
          <div className="flex gap-2">
            <button
              type="button"
              autoFocus
              onClick={() => setConfirmRemove(false)}
              className={cn(BUTTON_CLASS.outline, "h-7 px-2.5 text-crumb")}
            >
              {copy.common.keep}
            </button>
            <button
              type="button"
              onClick={() => void props.onRemove(response)}
              className={cn(BUTTON_CLASS.danger, "h-7 px-2.5 text-crumb")}
            >
              {t.removeConfirm}
            </button>
          </div>
        </div>
      ) : null}

      {response.status === "new" ? (
        <div className="flex flex-wrap items-center gap-2">
          {gate(
            <button
              type="button"
              onClick={() => props.onCreateItem(response)}
              className={BUTTON_CLASS.primary}
            >
              <SquareCheck aria-hidden="true" />
              {t.createItem}
            </button>,
          )}
          {gate(
            <button
              type="button"
              onClick={() => setRejecting(true)}
              className={BUTTON_CLASS.outline}
            >
              {t.reject}
            </button>,
          )}
          {gate(
            <button
              type="button"
              onClick={() => void props.onSpam(response)}
              className={BUTTON_CLASS.outline}
            >
              <TriangleAlert aria-hidden="true" />
              {t.spamAction}
            </button>,
          )}
          <span className="flex-1" />
          {gate(
            <button
              type="button"
              aria-label={t.removeAria}
              onClick={() => setConfirmRemove(true)}
              className={ICON_BUTTON_CLASS}
            >
              <Trash2 aria-hidden="true" />
            </button>,
          )}
        </div>
      ) : (
        <div className="flex justify-end">
          {gate(
            <button
              type="button"
              aria-label={t.removeAria}
              onClick={() => setConfirmRemove(true)}
              className={ICON_BUTTON_CLASS}
            >
              <Trash2 aria-hidden="true" />
            </button>,
          )}
        </div>
      )}

      <Dialog open={rejecting} onOpenChange={setRejecting}>
        {/* WM-Forms-Inbox (and WM-Forms-Dem): a 480 px card, 21 px padding, a 16 px title, a 70 px reason box, the hint
            under it and two 32 px buttons, Từ chối in the danger colour; Escape or Giữ lại closes it. */}
        <DialogContent
          showCloseButton={false}
          className="gap-0 rounded-[14px] bg-popover p-[21px] sm:max-w-[480px]"
        >
          <DialogHeader className="gap-0">
            <DialogTitle className="text-[16px] leading-[1.45] font-[650]">
              {t.rejectTitle}
            </DialogTitle>
            <DialogDescription className="sr-only">
              {t.reasonHint}
            </DialogDescription>
          </DialogHeader>
          <div className="mt-[14px] flex flex-col gap-1.5">
            <label
              htmlFor={`${id}-r`}
              className="text-crumb font-semibold text-muted-foreground"
            >
              {t.reasonLabel}
            </label>
            <textarea
              id={`${id}-r`}
              value={reason}
              maxLength={REJECT_REASON_MAX}
              onChange={(e) => setReason(e.target.value)}
              className={cn(TEXTAREA_CLASS, "h-[70px] max-md:h-[70px]")}
            />
            <span className={HINT_CLASS}>
              {fillFormsCopy(t.reasonHint, { max: REJECT_REASON_MAX })}
            </span>
          </div>
          <div className="mt-[14px] flex justify-end gap-2">
            <button
              type="button"
              className={BUTTON_CLASS.outline}
              onClick={() => setRejecting(false)}
            >
              {copy.common.keep}
            </button>
            <button
              type="button"
              // The forms list's danger fill (`bg-danger-text`), as the artboards draw the destructive button.
              className={cn(
                BUTTON_CLASS.danger,
                "border-danger-text bg-danger-text text-primary-foreground",
              )}
              onClick={() => void reject()}
            >
              {t.rejectConfirm}
            </button>
          </div>
        </DialogContent>
      </Dialog>
    </article>
  );
}
