import { useState } from "react";
import {
  Button,
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Input,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@notrelix/ui-web";
import { FEATURE_CODES, operatorApi, type PlanDto } from "../api";
import {
  ErrorNote,
  Field,
  Loading,
  Page,
  ReasonDialog,
  SELECT_CLASS,
  StatusBadge,
  messageOf,
  useLoad,
} from "../parts";

const TIERS = ["Free", "Pro", "Enterprise"] as const;
const PERIODS = ["Monthly", "Yearly"] as const;

type Retiring = { plan: PlanDto; kind: "deprecate" | "archive" };

export function Plans() {
  const { data, error, loading, reload } = useLoad(
    () => operatorApi.plans(),
    "plans",
  );
  const [creating, setCreating] = useState(false);
  const [retiring, setRetiring] = useState<Retiring | null>(null);

  const rows = [...(data ?? [])].sort(
    (a, b) => a.familyKey.localeCompare(b.familyKey) || b.revision - a.revision,
  );

  return (
    <Page
      title="Plan catalog"
      lede="Plans are published as numbered revisions and never edited in place. A tenant stays on the revision it has until an operator moves it."
      actions={<Button onClick={() => setCreating(true)}>New revision</Button>}
    >
      <ErrorNote message={error} />
      {loading && !data ? <Loading /> : null}
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Plan</TableHead>
            <TableHead>Revision</TableHead>
            <TableHead>Tier</TableHead>
            <TableHead>Price</TableHead>
            <TableHead>Status</TableHead>
            <TableHead>Limits</TableHead>
            <TableHead />
          </TableRow>
        </TableHeader>
        <TableBody>
          {rows.map((p) => (
            <TableRow key={p.id}>
              <TableCell>
                <strong>{p.name}</strong>{" "}
                <span className="font-mono text-caption text-muted-foreground">
                  {p.familyKey}
                </span>
              </TableCell>
              <TableCell>{p.revision}</TableCell>
              <TableCell>{p.tier}</TableCell>
              <TableCell>
                {p.priceAmount} {p.currency} / {p.period}
              </TableCell>
              <TableCell>
                <StatusBadge status={p.status} />
              </TableCell>
              <TableCell className="font-mono text-caption">
                {p.limits
                  .map(
                    (l) =>
                      `${l.featureCode} ${l.isUnlimited ? "unlimited" : l.limit}`,
                  )
                  .join(", ")}
              </TableCell>
              <TableCell className="whitespace-nowrap">
                {p.status === "Active" ? (
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setRetiring({ plan: p, kind: "deprecate" })}
                  >
                    Deprecate
                  </Button>
                ) : null}{" "}
                {p.status !== "Archived" ? (
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setRetiring({ plan: p, kind: "archive" })}
                  >
                    Archive
                  </Button>
                ) : null}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>

      <NewRevision
        open={creating}
        existing={rows}
        onClose={() => setCreating(false)}
        onCreated={reload}
      />
      <ReasonDialog
        open={retiring !== null}
        title={
          retiring
            ? `${retiring.kind === "archive" ? "Archive" : "Deprecate"} ${retiring.plan.name} revision ${retiring.plan.revision}`
            : ""
        }
        description={
          retiring?.kind === "archive"
            ? "The revision is no longer offered. Tenants still on it keep their limits."
            : "The revision stops being sold. Tenants still on it keep their limits."
        }
        confirmLabel={retiring?.kind === "archive" ? "Archive" : "Deprecate"}
        destructive
        onClose={() => setRetiring(null)}
        onConfirm={async (reason) => {
          if (!retiring) return;
          const { plan, kind } = retiring;
          await (kind === "archive"
            ? operatorApi.archivePlan(plan.id, reason, plan.version)
            : operatorApi.deprecatePlan(plan.id, reason, plan.version));
          await reload();
        }}
      />
    </Page>
  );
}

type LimitDraft = { on: boolean; unlimited: boolean; value: string };

function NewRevision({
  open,
  existing,
  onClose,
  onCreated,
}: {
  open: boolean;
  existing: PlanDto[];
  onClose: () => void;
  onCreated: () => Promise<void>;
}) {
  const [familyKey, setFamilyKey] = useState("");
  const [name, setName] = useState("");
  const [tier, setTier] = useState<string>(TIERS[0]);
  const [price, setPrice] = useState("0");
  const [currency, setCurrency] = useState("VND");
  const [period, setPeriod] = useState<string>(PERIODS[0]);
  const [limits, setLimits] = useState<Record<string, LimitDraft>>(
    Object.fromEntries(
      FEATURE_CODES.map((c) => [
        c,
        { on: false, unlimited: false, value: "0" },
      ]),
    ),
  );
  const [reason, setReason] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const family = familyKey.trim().toLowerCase();
  const nextRevision =
    existing
      .filter((p) => p.familyKey === family)
      .reduce((max, p) => Math.max(max, p.revision), 0) + 1;

  const patch = (code: string, change: Partial<LimitDraft>) =>
    setLimits({ ...limits, [code]: { ...limits[code], ...change } });

  const submit = async () => {
    setBusy(true);
    setError(null);
    try {
      await operatorApi.createRevision({
        familyKey: family,
        revision: nextRevision,
        name: name.trim(),
        tier,
        priceAmount: Number(price),
        currency: currency.trim().toUpperCase(),
        period,
        limits: FEATURE_CODES.filter((c) => limits[c].on).map((c) => ({
          featureCode: c,
          isUnlimited: limits[c].unlimited,
          limit: limits[c].unlimited ? 0 : Number(limits[c].value),
        })),
        reason: reason.trim(),
      });
      await onCreated();
      onClose();
    } catch (e) {
      setError(messageOf(e));
    } finally {
      setBusy(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={(next) => (next ? undefined : onClose())}>
      <DialogContent className="max-h-[90svh] overflow-y-auto sm:max-w-xl">
        <DialogHeader>
          <DialogTitle>Publish a plan revision</DialogTitle>
        </DialogHeader>
        <form
          className="grid grid-cols-1 gap-3 sm:grid-cols-2"
          onSubmit={(e) => {
            e.preventDefault();
            void submit();
          }}
        >
          <Field label="Plan family key" htmlFor="rev-family">
            <Input
              id="rev-family"
              value={familyKey}
              placeholder="pro"
              onChange={(e) => setFamilyKey(e.target.value)}
            />
          </Field>
          <Field label="Revision (next in the family)" htmlFor="rev-number">
            <Input id="rev-number" value={nextRevision} readOnly />
          </Field>
          <Field label="Name" htmlFor="rev-name">
            <Input
              id="rev-name"
              value={name}
              onChange={(e) => setName(e.target.value)}
            />
          </Field>
          <Field label="Tier" htmlFor="rev-tier">
            <select
              id="rev-tier"
              className={SELECT_CLASS}
              value={tier}
              onChange={(e) => setTier(e.target.value)}
            >
              {TIERS.map((t) => (
                <option key={t}>{t}</option>
              ))}
            </select>
          </Field>
          <Field label="Price" htmlFor="rev-price">
            <Input
              id="rev-price"
              type="number"
              min={0}
              value={price}
              onChange={(e) => setPrice(e.target.value)}
            />
          </Field>
          <Field label="Currency" htmlFor="rev-currency">
            <Input
              id="rev-currency"
              value={currency}
              onChange={(e) => setCurrency(e.target.value)}
            />
          </Field>
          <Field label="Billing period" htmlFor="rev-period">
            <select
              id="rev-period"
              className={SELECT_CLASS}
              value={period}
              onChange={(e) => setPeriod(e.target.value)}
            >
              {PERIODS.map((p) => (
                <option key={p}>{p}</option>
              ))}
            </select>
          </Field>
          <fieldset className="flex flex-col gap-2 sm:col-span-2">
            <legend className="mb-1 text-label">Limits</legend>
            {FEATURE_CODES.map((code) => (
              <div
                key={code}
                className="flex flex-wrap items-center gap-3 text-compact"
              >
                <label className="flex w-48 items-center gap-2 font-mono">
                  <input
                    type="checkbox"
                    checked={limits[code].on}
                    onChange={(e) => patch(code, { on: e.target.checked })}
                  />
                  {code}
                </label>
                <Input
                  aria-label={`${code} limit`}
                  className="w-28"
                  type="number"
                  min={0}
                  disabled={!limits[code].on || limits[code].unlimited}
                  value={limits[code].value}
                  onChange={(e) => patch(code, { value: e.target.value })}
                />
                <label className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    disabled={!limits[code].on}
                    checked={limits[code].unlimited}
                    onChange={(e) =>
                      patch(code, { unlimited: e.target.checked })
                    }
                  />
                  Unlimited
                </label>
              </div>
            ))}
          </fieldset>
          <div className="sm:col-span-2">
            <Field
              label="Reason (kept in the audit trail)"
              htmlFor="rev-reason"
            >
              <Input
                id="rev-reason"
                maxLength={500}
                value={reason}
                onChange={(e) => setReason(e.target.value)}
              />
            </Field>
          </div>
          <div className="sm:col-span-2">
            <ErrorNote message={error} />
          </div>
          <DialogFooter className="sm:col-span-2">
            <Button
              type="button"
              variant="outline"
              onClick={onClose}
              disabled={busy}
            >
              Cancel
            </Button>
            <Button
              type="submit"
              disabled={
                busy || !familyKey.trim() || !name.trim() || !reason.trim()
              }
            >
              {busy ? "Publishing" : "Publish revision"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
