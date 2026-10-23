import * as React from "react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
  cn,
} from "@notrelix/ui-web";
import { formatCopy } from "../shell/shell-models";
import type { SectionState } from "../home/home-models";
import { FOCUS } from "../spaces/space-page-parts";
import {
  BILLING_BTN,
  BILLING_BTN_PRIMARY,
  BILLING_BTN_SM,
  BILLING_BTN_SM_PRIMARY,
  BillingGlyph,
  DIALOG_OVERLAY,
  DIALOG_PANEL,
  FIELD_LABEL,
  InfoNote,
  LNKB,
  NavAnchor,
} from "./billing-parts";
import {
  clockTime,
  monthYear,
  moneyLabel,
  shortDay,
  type BillingFormatters,
  type BillingLink,
  type BillingPurchaseDto,
  type BillingSeatQuoteDto,
  type BillingSeatSummaryDto,
} from "./billing-models";
import type { BillingCopy } from "./billing.copy";

function DialogHead({
  id,
  title,
  closeLabel,
  onClose,
}: {
  id: string;
  title: string;
  closeLabel: string;
  onClose: () => void;
}) {
  return (
    <div className="flex items-center gap-2.5 px-5 pt-[18px] pb-1.5">
      <DialogTitle
        id={id}
        className="m-0 text-dialog-title leading-[1.45] font-[650] text-foreground"
      >
        {title}
      </DialogTitle>
      <button
        type="button"
        aria-label={closeLabel}
        onClick={onClose}
        className={cn(
          "ml-auto grid size-8 shrink-0 cursor-pointer place-items-center rounded-[7px] border-0 bg-transparent text-meta-foreground hover:bg-state-hover-strong hover:text-foreground",
          FOCUS,
        )}
      >
        <BillingGlyph name="x" />
      </button>
    </div>
  );
}

const FOOT =
  "flex items-center gap-2 border-t border-line bg-background px-5 py-3.5";
const FOOT_NOTE =
  "text-caption tracking-normal leading-[1.45] text-meta-foreground";

function CalcRow({
  total,
  children,
  value,
}: {
  total?: boolean;
  children: React.ReactNode;
  value: string;
}) {
  return (
    <div
      role="row"
      className={cn(
        "flex items-baseline gap-3 border-t border-line px-3 py-[9px] text-compact leading-[1.45] first:border-t-0",
        total && "bg-background text-body leading-[1.45] font-[650]",
      )}
    >
      <span role="cell">{children}</span>
      <span
        role="cell"
        className="ml-auto whitespace-nowrap [font-variant-numeric:tabular-nums]"
      >
        {value}
      </span>
    </div>
  );
}

export function CalcSub({ children }: { children: React.ReactNode }) {
  return (
    <small className="mt-0.5 block text-caption tracking-normal leading-[1.45] font-normal text-meta-foreground">
      {children}
    </small>
  );
}

// ── Buy seats ───────────────────────────────────────────────────────────

export interface BillingBuyDialogProps {
  readonly copy: BillingCopy;
  readonly formatters: BillingFormatters;
  readonly open: boolean;
  readonly quantity: number;
  readonly quote: SectionState<BillingSeatQuoteDto>;
  /** The purchase waiting for the bank; `null` while the person is still choosing. */
  readonly pending: BillingPurchaseDto | null;
  readonly submitting: boolean;
  readonly checking: boolean;
  readonly failed: boolean;
  readonly onQuantity: (quantity: number) => void;
  readonly onConfirm: () => void;
  readonly onRecheck: () => void;
  readonly onChangeCard: () => void;
  readonly onClose: () => void;
}

export const SEAT_MIN = 1;
export const SEAT_MAX = 500;

/** Buy more seats (Final-Billing `mua-cho`, `dang-xu-ly`). */
export function BillingBuyDialog(props: BillingBuyDialogProps) {
  const { copy, formatters: f, quote, pending } = props;
  const c = copy.buy;
  const isPending = pending !== null;
  const data = quote.status === "ready" ? quote.data : null;
  const money = (amount: number) => moneyLabel(f, copy.money, amount);
  const step = (delta: number) =>
    props.onQuantity(
      Math.min(SEAT_MAX, Math.max(SEAT_MIN, props.quantity + delta)),
    );
  return (
    <Dialog
      open={props.open}
      onOpenChange={(next) => (next ? undefined : props.onClose())}
    >
      <DialogContent
        showCloseButton={false}
        overlayClassName={DIALOG_OVERLAY}
        aria-labelledby="buy-t"
        aria-busy={isPending}
        className={DIALOG_PANEL}
      >
        <DialogHead
          id="buy-t"
          title={c.title}
          closeLabel={c.close}
          onClose={props.onClose}
        />
        <DialogDescription className="sr-only">{c.info}</DialogDescription>
        <div className="flex flex-col gap-3.5 px-5 pt-3 pb-4">
          <div className="flex flex-col gap-1.5">
            <span id="qty-l" className={FIELD_LABEL}>
              {c.qty}
            </span>
            <div
              role="group"
              aria-labelledby="qty-l"
              className="flex items-center"
            >
              <button
                type="button"
                aria-label={c.less}
                disabled={isPending || props.quantity <= SEAT_MIN}
                onClick={() => step(-1)}
                className={cn(STEP_BTN, "rounded-l-[8px]")}
              >
                −
              </button>
              <input
                type="text"
                inputMode="numeric"
                aria-label={c.qty}
                value={String(props.quantity)}
                disabled={isPending}
                onChange={(event) => {
                  const next = Number(event.target.value.replace(/\D/g, ""));
                  if (Number.isFinite(next)) {
                    props.onQuantity(Math.min(SEAT_MAX, Math.max(0, next)));
                  }
                }}
                className="m-0 box-border h-9 w-16 border-y border-line-strong bg-popover text-center text-item-heading leading-[1.45] font-[650] text-foreground outline-none focus:ring-2 focus:ring-focus-ring disabled:bg-state-hover-strong disabled:text-muted-foreground dark:bg-card"
              />
              <button
                type="button"
                aria-label={c.more}
                disabled={isPending || props.quantity >= SEAT_MAX}
                onClick={() => step(1)}
                className={cn(STEP_BTN, "rounded-r-[8px]")}
              >
                +
              </button>
              {data ? (
                <span className="ml-3 text-crumb leading-[1.5] text-meta-foreground">
                  {formatCopy(c.after, {
                    total: f.number(data.seatsAfter),
                    used: f.number(data.seatsUsed),
                  })}
                </span>
              ) : null}
            </div>
          </div>
          {quote.status === "error" ? (
            <p role="alert" className="m-0 text-compact text-danger-text">
              {c.quoteFailed}
            </p>
          ) : null}
          {data ? (
            <>
              <div
                role="table"
                aria-label={c.calcLabel}
                className="overflow-hidden rounded-[10px] border border-line"
              >
                <CalcRow value={money(data.amount)}>
                  {formatCopy(c.formula, {
                    count: f.number(data.quantity),
                    price: money(data.unitPrice),
                    days: data.days,
                    year: data.yearDays,
                  })}
                  <CalcSub>
                    {formatCopy(c.formulaNote, {
                      from: shortDay(f, data.from),
                      to: `${shortDay(f, data.to)}/${data.to.slice(0, 4)}`,
                      days: data.days,
                    })}
                  </CalcSub>
                </CalcRow>
                <CalcRow value={money(data.vat)}>
                  {formatCopy(c.vat, { percent: data.vatPercent })}
                </CalcRow>
                <CalcRow total value={money(data.total)}>
                  {c.total}
                </CalcRow>
              </div>
              <div className="flex flex-col gap-1.5">
                <span className={FIELD_LABEL}>{c.payWith}</span>
                <div className="flex items-center gap-2.5 rounded-[10px] border border-line px-3 py-2.5">
                  <span className="grid h-[26px] w-9 shrink-0 place-items-center rounded-[5px] border border-line-strong bg-background text-mark-sm leading-none font-bold text-muted-foreground">
                    {data.card.brand}
                  </span>
                  <span>
                    <b className="block text-nav leading-[1.45] font-semibold text-foreground">
                      {formatCopy(copy.payment.card, {
                        brand: data.card.brand,
                        last4: data.card.last4,
                      })}
                    </b>
                    <small className="block text-caption tracking-normal leading-[1.45] text-meta-foreground">
                      {formatCopy(c.cardMeta, {
                        expires: monthYear(data.card.expires),
                      })}
                    </small>
                  </span>
                  {isPending ? null : (
                    <button
                      type="button"
                      onClick={props.onChangeCard}
                      className={cn(LNKB, "ml-auto")}
                    >
                      {c.changeCard}
                    </button>
                  )}
                </div>
              </div>
              {isPending ? null : (
                <InfoNote>
                  <b className="font-semibold text-foreground">{c.info}</b>{" "}
                  {formatCopy(c.infoDetail, { email: data.email })}
                </InfoNote>
              )}
            </>
          ) : null}
          {pending ? (
            <div
              role="status"
              aria-live="polite"
              className="flex items-start gap-2.5 rounded-[10px] bg-state-selected p-3 text-compact leading-[1.45]"
            >
              <Spinner className="mt-0.5" />
              <div>
                <b className="block font-semibold text-foreground">
                  {c.pendingTitle}
                </b>
                <small className="mt-[3px] block text-caption tracking-normal leading-[1.45] text-muted-foreground">
                  {formatCopy(c.pendingSent, {
                    sent: clockTime(f, pending.submittedAt),
                    checked: clockTime(f, pending.lastCheckedAt),
                  })}
                  {" · "}
                  <button
                    type="button"
                    onClick={props.onRecheck}
                    aria-disabled={props.checking || undefined}
                    className={cn(
                      LNKB,
                      "text-caption tracking-normal leading-[1.45]",
                    )}
                  >
                    {c.recheck}
                  </button>
                </small>
                <small className="mt-[3px] block text-caption tracking-normal leading-[1.45] text-muted-foreground">
                  {c.pendingNote}
                </small>
              </div>
            </div>
          ) : null}
          {props.failed ? (
            <p role="alert" className="m-0 text-compact text-danger-text">
              {c.failed}
            </p>
          ) : null}
        </div>
        <div className={FOOT}>
          {data ? (
            <small className={FOOT_NOTE}>
              {formatCopy(c.footTotal, { total: money(data.total) })}
            </small>
          ) : null}
          <span className="grow" />
          {isPending ? (
            <>
              <button
                type="button"
                onClick={props.onClose}
                className={BILLING_BTN}
              >
                {c.close2}
              </button>
              <button type="button" disabled className={BILLING_BTN_PRIMARY}>
                <Spinner className="mt-0" />
                {c.processing}
              </button>
            </>
          ) : (
            <>
              <button
                type="button"
                onClick={props.onClose}
                className={BILLING_BTN}
              >
                {c.cancel}
              </button>
              <button
                type="button"
                disabled={!data || props.submitting}
                onClick={props.onConfirm}
                className={BILLING_BTN_PRIMARY}
              >
                {c.confirm}
              </button>
            </>
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
}

const STEP_BTN =
  "m-0 grid size-9 cursor-pointer place-items-center border border-line-strong bg-popover p-0 text-calendar-title leading-none text-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring disabled:cursor-not-allowed disabled:bg-state-hover-strong disabled:text-meta-foreground dark:bg-card";

function Spinner({ className }: { className?: string }) {
  return (
    <span
      aria-hidden="true"
      className={cn(
        "size-[18px] shrink-0 animate-spin rounded-full border-2 border-line-strong border-t-primary motion-reduce:animate-none",
        className,
      )}
    />
  );
}

// ── Invite beyond the seats (Final-Billing `het-cho`) ───────────────────

export interface BillingSeatInviteDialogProps {
  readonly copy: BillingCopy;
  readonly open: boolean;
  readonly workspaceName: string;
  readonly seats: BillingSeatSummaryDto | null;
  readonly emails: readonly string[];
  readonly role: "member" | "guest";
  readonly sending: boolean;
  readonly error: string | null;
  readonly buyLink: BillingLink | null;
  readonly onEmails: (emails: readonly string[]) => void;
  readonly onRole: (role: "member" | "guest") => void;
  readonly onSend: () => void;
  readonly onClose: () => void;
}

const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/** The invitation dialog that stops at the seats the account owns. */
export function BillingSeatInviteDialog(props: BillingSeatInviteDialogProps) {
  const { copy, seats, emails } = props;
  const c = copy.invite;
  const [draft, setDraft] = React.useState("");
  const [invalid, setInvalid] = React.useState<string | null>(null);
  const left = seats ? seats.total - seats.used : 0;
  const over = seats !== null && emails.length > left;
  const commit = (raw: string) => {
    const parts = raw
      .split(/[\s,;]+/)
      .map((part) => part.trim())
      .filter(Boolean);
    if (parts.length === 0) return;
    const bad = parts.find((part) => !EMAIL.test(part));
    if (bad) {
      setInvalid(bad);
      return;
    }
    setInvalid(null);
    setDraft("");
    const next = [...emails];
    for (const part of parts) if (!next.includes(part)) next.push(part);
    props.onEmails(next);
  };
  return (
    <Dialog
      open={props.open}
      onOpenChange={(next) => (next ? undefined : props.onClose())}
    >
      <DialogContent
        showCloseButton={false}
        overlayClassName={DIALOG_OVERLAY}
        aria-labelledby="inv-dlg-t"
        className={DIALOG_PANEL}
      >
        <DialogHead
          id="inv-dlg-t"
          title={formatCopy(c.title, { workspace: props.workspaceName })}
          closeLabel={c.close}
          onClose={props.onClose}
        />
        <DialogDescription className="sr-only">
          {c.limitDetail}
        </DialogDescription>
        <div className="flex flex-col gap-3.5 px-5 pt-3 pb-4">
          <div className="flex flex-col gap-1.5">
            <label htmlFor="inv-mail" className={FIELD_LABEL}>
              {formatCopy(c.email, { count: emails.length })}
            </label>
            <div className="box-border flex min-h-[42px] flex-wrap items-center gap-1.5 rounded-[8px] border border-line-strong px-2 py-1.5">
              {emails.map((email) => (
                <span
                  key={email}
                  className="inline-flex h-7 items-center gap-1.5 rounded-[7px] bg-state-hover-strong pr-1 pl-2 text-compact leading-[1.45] font-medium text-foreground"
                >
                  {email}
                  <button
                    type="button"
                    aria-label={formatCopy(c.remove, { email })}
                    onClick={() =>
                      props.onEmails(emails.filter((entry) => entry !== email))
                    }
                    className={cn(
                      "grid size-[18px] cursor-pointer place-items-center rounded-[4px] border-0 bg-transparent p-0 text-meta-foreground",
                      FOCUS,
                    )}
                  >
                    <BillingGlyph name="x" className="size-3" />
                  </button>
                </span>
              ))}
              <input
                id="inv-mail"
                type="text"
                value={draft}
                placeholder={c.add}
                aria-describedby="seat-msg"
                onChange={(event) => setDraft(event.target.value)}
                onKeyDown={(event) => {
                  if (event.key === "Enter" || event.key === ",") {
                    event.preventDefault();
                    commit(draft);
                  }
                }}
                onBlur={() => commit(draft)}
                className="h-[26px] min-w-[100px] flex-1 border-0 bg-transparent text-nav leading-[1.45] text-foreground outline-none placeholder:text-meta-foreground"
              />
            </div>
            {invalid ? (
              <p role="alert" className="m-0 text-crumb text-danger-text">
                {formatCopy(c.invalid, { email: invalid })}
              </p>
            ) : null}
          </div>
          <div className="flex flex-col gap-1.5">
            <label htmlFor="inv-role" className={FIELD_LABEL}>
              {c.role}
            </label>
            <select
              id="inv-role"
              value={props.role}
              onChange={(event) =>
                props.onRole(
                  event.target.value === "guest" ? "guest" : "member",
                )
              }
              className="box-border h-9 w-full rounded-[8px] border border-line-strong bg-popover px-2.5 text-nav leading-[1.45] text-foreground dark:bg-card"
            >
              <option value="member">{c.roles.member}</option>
              <option value="guest">{c.roles.guest}</option>
            </select>
          </div>
          {over && seats ? (
            <div
              role="alert"
              id="seat-msg"
              className="flex items-start gap-2.5 rounded-[10px] border border-warning-text/40 bg-warning-subtle p-3 text-compact leading-[1.45] text-foreground"
            >
              <BillingGlyph name="warn" className="mt-0.5 text-warning-text" />
              <div>
                <b className="block font-[650]">
                  {formatCopy(c.limit, { left: Math.max(0, left) })}
                </b>
                <small className="mt-[3px] block text-caption tracking-normal leading-[1.5] text-muted-foreground">
                  {formatCopy(c.limitDetail, {
                    others: seats.sharedWith.join(", "),
                    used: seats.used,
                    total: seats.total,
                  })}
                </small>
                <div className="mt-2.5 flex gap-2">
                  {props.buyLink && seats.canBuy ? (
                    <NavAnchor
                      link={props.buyLink}
                      className={BILLING_BTN_SM_PRIMARY}
                    >
                      {c.buy}
                    </NavAnchor>
                  ) : null}
                  <button
                    type="button"
                    onClick={() =>
                      props.onEmails(emails.slice(0, Math.max(0, left)))
                    }
                    className={BILLING_BTN_SM}
                  >
                    {c.trim}
                  </button>
                </div>
              </div>
            </div>
          ) : null}
          {props.error ? (
            <p role="alert" className="m-0 text-compact text-danger-text">
              {props.error}
            </p>
          ) : null}
        </div>
        <div className={FOOT}>
          <small className={FOOT_NOTE}>
            {formatCopy(c.foot, { count: emails.length })}
          </small>
          <span className="grow" />
          <button type="button" onClick={props.onClose} className={BILLING_BTN}>
            {c.cancel}
          </button>
          <button
            type="button"
            disabled={emails.length === 0 || over || props.sending}
            aria-describedby={over ? "seat-msg" : undefined}
            onClick={props.onSend}
            className={BILLING_BTN_PRIMARY}
          >
            {formatCopy(c.send, { count: emails.length })}
          </button>
        </div>
      </DialogContent>
    </Dialog>
  );
}
