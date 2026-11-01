import { useState } from "react";
import {
  Button,
  Input,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@notrelix/ui-web";
import {
  FEATURE_CODES,
  operatorApi,
  type AccountBilling,
  type OverrideDto,
} from "../api";
import {
  ErrorNote,
  Field,
  Loading,
  Page,
  Panel,
  ReasonDialog,
  SELECT_CLASS,
  StatusBadge,
  dateTime,
  messageOf,
  useLoad,
} from "../parts";

type Action = "suspend" | "lift" | "plan" | "terminate" | null;

export function TenantDetail({ accountId }: { accountId: string }) {
  const account = useLoad(() => operatorApi.account(accountId), accountId);
  const billing = useLoad(() => operatorApi.billing(accountId), accountId);
  const plans = useLoad(() => operatorApi.plans(), "plans");
  const audit = useLoad(() => operatorApi.audit(accountId, 20), accountId);
  const [action, setAction] = useState<Action>(null);
  const [revoking, setRevoking] = useState<OverrideDto | null>(null);
  const [targetPlan, setTargetPlan] = useState("");

  const refresh = async () => {
    await Promise.all([account.reload(), billing.reload(), audit.reload()]);
  };

  if (account.error) {
    return (
      <Page title="Tenant">
        <ErrorNote message={account.error} />
      </Page>
    );
  }
  if (!account.data || !billing.data) {
    return (
      <Page title="Tenant">
        <Loading />
      </Page>
    );
  }

  const { account: a, activeMembers, workspaces } = account.data;
  const b = billing.data;
  const sellable = (plans.data ?? []).filter((p) => p.status === "Active");
  const chosen = targetPlan || b.currentPlan.planId;
  const suspended = a.status === "Suspended";

  return (
    <Page
      title={a.name}
      lede={`Slug ${a.slug}. Created ${dateTime(a.createdAt)}.`}
      actions={
        <div className="flex items-center gap-3">
          <StatusBadge status={a.status} />
          {a.status === "Closed" ? null : suspended ? (
            <Button variant="outline" onClick={() => setAction("lift")}>
              Lift suspension
            </Button>
          ) : (
            <Button variant="destructive" onClick={() => setAction("suspend")}>
              Suspend
            </Button>
          )}
        </div>
      }
    >
      <Panel title="Impact">
        <p className="m-0 text-body">
          {activeMembers} active members in {workspaces} workspaces. Suspending
          makes all of it read-only for them; nothing is deleted.
        </p>
      </Panel>

      <Panel title="Plan">
        <p className="m-0 text-body">
          Current plan:{" "}
          <strong>
            {b.currentPlan.name} (revision {b.currentPlan.revision})
          </strong>
          {b.subscription
            ? `, subscription ${b.subscription.status}, period ends ${dateTime(b.subscription.currentPeriodEnd)}`
            : ", no subscription"}
        </p>
        <div className="flex max-w-[520px] items-end gap-2">
          <div className="flex-1">
            <Field label="Move to plan revision" htmlFor="operator-plan">
              <select
                id="operator-plan"
                className={SELECT_CLASS}
                value={chosen}
                onChange={(e) => setTargetPlan(e.target.value)}
              >
                {!sellable.some((p) => p.id === b.currentPlan.planId) ? (
                  <option value={b.currentPlan.planId}>
                    {b.currentPlan.name} r{b.currentPlan.revision} (current)
                  </option>
                ) : null}
                {sellable.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.name} r{p.revision}
                  </option>
                ))}
              </select>
            </Field>
          </div>
          <Button
            disabled={chosen === b.currentPlan.planId}
            onClick={() => setAction("plan")}
          >
            Change plan
          </Button>
        </div>
        {b.subscription &&
        b.subscription.status !== "Canceled" &&
        b.subscription.status !== "Expired" ? (
          <div>
            <Button
              variant="destructive"
              onClick={() => setAction("terminate")}
            >
              Terminate subscription now
            </Button>
          </div>
        ) : null}
      </Panel>

      <Overrides
        billing={b}
        onChanged={refresh}
        onRevoke={setRevoking}
        accountId={accountId}
      />

      <Panel title="Recent operator actions on this tenant">
        <ErrorNote message={audit.error} />
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>When</TableHead>
              <TableHead>Operator</TableHead>
              <TableHead>Action</TableHead>
              <TableHead>Reason</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {(audit.data ?? []).map((entry) => (
              <TableRow key={entry.id}>
                <TableCell>{dateTime(entry.occurredAt)}</TableCell>
                <TableCell>{entry.operatorEmail}</TableCell>
                <TableCell>{entry.action}</TableCell>
                <TableCell>{entry.reason}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Panel>

      <ReasonDialog
        open={action === "suspend"}
        title={`Suspend ${a.name}`}
        description={`Every member of this tenant becomes read-only (${activeMembers} members, ${workspaces} workspaces). Nothing is deleted and the suspension can be lifted.`}
        confirmLabel="Suspend"
        destructive
        onClose={() => setAction(null)}
        onConfirm={async (reason) => {
          await operatorApi.suspend(accountId, reason, a.version);
          await refresh();
        }}
      />
      <ReasonDialog
        open={action === "lift"}
        title={`Lift the suspension of ${a.name}`}
        description="Members can write again straight away."
        confirmLabel="Lift suspension"
        onClose={() => setAction(null)}
        onConfirm={async (reason) => {
          await operatorApi.liftSuspension(accountId, reason, a.version);
          await refresh();
        }}
      />
      <ReasonDialog
        open={action === "plan"}
        title="Change the plan"
        description="The tenant sees the new limits at once. Plan revisions it was on stay valid for other tenants."
        confirmLabel="Change plan"
        onClose={() => setAction(null)}
        onConfirm={async (reason) => {
          await operatorApi.changePlan(
            accountId,
            chosen,
            reason,
            b.subscription?.version ?? null,
          );
          setTargetPlan("");
          await refresh();
        }}
      />
      <ReasonDialog
        open={action === "terminate"}
        title="Terminate the subscription now"
        description="The subscription ends immediately and the tenant falls back to the default plan limits."
        confirmLabel="Terminate"
        destructive
        onClose={() => setAction(null)}
        onConfirm={async (reason) => {
          if (!b.subscription) return;
          await operatorApi.terminate(
            accountId,
            reason,
            b.subscription.version,
          );
          await refresh();
        }}
      />
      <ReasonDialog
        open={revoking !== null}
        title="Revoke the override"
        description={
          revoking
            ? `The ${revoking.featureCode} override ends and the plan limit applies again.`
            : ""
        }
        confirmLabel="Revoke"
        destructive
        onClose={() => setRevoking(null)}
        onConfirm={async (reason) => {
          if (!revoking) return;
          await operatorApi.revokeOverride(
            revoking.id,
            reason,
            revoking.version,
          );
          await refresh();
        }}
      />
    </Page>
  );
}

function Overrides({
  billing,
  accountId,
  onChanged,
  onRevoke,
}: {
  billing: AccountBilling;
  accountId: string;
  onChanged: () => Promise<void>;
  onRevoke: (o: OverrideDto) => void;
}) {
  const [feature, setFeature] = useState<string>(FEATURE_CODES[0]);
  const [unlimited, setUnlimited] = useState(false);
  const [limit, setLimit] = useState("0");
  const [expires, setExpires] = useState("");
  const [reason, setReason] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const grant = async () => {
    setBusy(true);
    setError(null);
    try {
      await operatorApi.grantOverride(accountId, {
        featureCode: feature,
        isUnlimited: unlimited,
        limit: unlimited ? 0 : Number(limit),
        expiresAt: expires ? new Date(expires).toISOString() : null,
        reason: reason.trim(),
      });
      setReason("");
      await onChanged();
    } catch (e) {
      setError(messageOf(e));
    } finally {
      setBusy(false);
    }
  };

  return (
    <Panel title="Entitlement overrides">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Feature</TableHead>
            <TableHead>Limit</TableHead>
            <TableHead>Source</TableHead>
            <TableHead>Status</TableHead>
            <TableHead>Expires</TableHead>
            <TableHead />
          </TableRow>
        </TableHeader>
        <TableBody>
          {billing.overrides.map((o) => (
            <TableRow key={o.id}>
              <TableCell className="font-mono">{o.featureCode}</TableCell>
              <TableCell>{o.isUnlimited ? "Unlimited" : o.limit}</TableCell>
              <TableCell>{o.source}</TableCell>
              <TableCell>
                <StatusBadge status={o.status} />
              </TableCell>
              <TableCell>{dateTime(o.expiresAt)}</TableCell>
              <TableCell>
                {o.source === "Manual" && o.status === "Active" ? (
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => onRevoke(o)}
                  >
                    Revoke
                  </Button>
                ) : null}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
      {billing.overrides.length === 0 ? (
        <p className="m-0 text-compact text-muted-foreground">
          No override. The plan limits apply.
        </p>
      ) : null}
      <form
        className="grid max-w-[720px] grid-cols-1 gap-3 sm:grid-cols-2"
        onSubmit={(e) => {
          e.preventDefault();
          void grant();
        }}
      >
        <Field label="Feature" htmlFor="override-feature">
          <select
            id="override-feature"
            className={SELECT_CLASS}
            value={feature}
            onChange={(e) => setFeature(e.target.value)}
          >
            {FEATURE_CODES.map((code) => (
              <option key={code} value={code}>
                {code}
              </option>
            ))}
          </select>
        </Field>
        <Field label="Limit" htmlFor="override-limit">
          <Input
            id="override-limit"
            type="number"
            min={0}
            disabled={unlimited}
            value={limit}
            onChange={(e) => setLimit(e.target.value)}
          />
        </Field>
        <label className="flex items-center gap-2 text-compact">
          <input
            type="checkbox"
            checked={unlimited}
            onChange={(e) => setUnlimited(e.target.checked)}
          />
          Unlimited
        </label>
        <Field label="Expires (optional)" htmlFor="override-expires">
          <Input
            id="override-expires"
            type="datetime-local"
            value={expires}
            onChange={(e) => setExpires(e.target.value)}
          />
        </Field>
        <div className="sm:col-span-2">
          <Field
            label="Reason (kept in the audit trail)"
            htmlFor="override-reason"
          >
            <Input
              id="override-reason"
              maxLength={500}
              value={reason}
              onChange={(e) => setReason(e.target.value)}
            />
          </Field>
        </div>
        <ErrorNote message={error} />
        <div className="sm:col-span-2">
          <Button type="submit" disabled={busy || reason.trim().length === 0}>
            Grant override
          </Button>
        </div>
      </form>
    </Panel>
  );
}
