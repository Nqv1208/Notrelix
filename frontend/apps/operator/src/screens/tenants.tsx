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
import { operatorApi } from "../api";
import {
  ErrorNote,
  Loading,
  Page,
  StatusBadge,
  dateTime,
  useLoad,
} from "../parts";

const PAGE = 25;

export function Tenants() {
  const [draft, setDraft] = useState("");
  const [search, setSearch] = useState("");
  const [skip, setSkip] = useState(0);

  const { data, error, loading } = useLoad(async () => {
    const page = await operatorApi.accounts(search, skip, PAGE);
    const plans = page.items.length
      ? await operatorApi.accountPlans(page.items.map((a) => a.id))
      : [];
    return { page, plans: new Map(plans.map((p) => [p.accountId, p])) };
  }, `${search}|${skip}`);

  return (
    <Page
      title="Tenants"
      lede="Accounts with their status and plan. The console never shows what is inside a tenant."
    >
      <form
        role="search"
        className="flex max-w-[460px] gap-2"
        onSubmit={(e) => {
          e.preventDefault();
          setSkip(0);
          setSearch(draft.trim());
        }}
      >
        <Input
          aria-label="Search tenants by name or slug"
          placeholder="Search by name or slug"
          value={draft}
          onChange={(e) => setDraft(e.target.value)}
        />
        <Button type="submit" variant="outline">
          Search
        </Button>
      </form>
      <ErrorNote message={error} />
      {loading && !data ? <Loading /> : null}
      {data ? (
        <>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Tenant</TableHead>
                <TableHead>Slug</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Plan</TableHead>
                <TableHead>Created</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data.page.items.map((account) => {
                const plan = data.plans.get(account.id);
                return (
                  <TableRow key={account.id}>
                    <TableCell>
                      <a
                        href={`#/tenants/${account.id}`}
                        className="font-medium text-link underline-offset-4 hover:underline"
                      >
                        {account.name}
                      </a>
                    </TableCell>
                    <TableCell className="font-mono">{account.slug}</TableCell>
                    <TableCell>
                      <StatusBadge status={account.status} />
                    </TableCell>
                    <TableCell>
                      {plan ? `${plan.name} r${plan.revision}` : ""}
                    </TableCell>
                    <TableCell>{dateTime(account.createdAt)}</TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
          {data.page.items.length === 0 ? (
            <p className="m-0 text-compact text-muted-foreground">
              No tenant matches.
            </p>
          ) : null}
          <div className="flex items-center gap-3 text-compact text-muted-foreground">
            <Button
              variant="outline"
              size="sm"
              disabled={skip === 0}
              onClick={() => setSkip(Math.max(0, skip - PAGE))}
            >
              Previous
            </Button>
            <span>
              {data.page.total === 0 ? 0 : skip + 1} to{" "}
              {Math.min(skip + PAGE, data.page.total)} of {data.page.total}
            </span>
            <Button
              variant="outline"
              size="sm"
              disabled={skip + PAGE >= data.page.total}
              onClick={() => setSkip(skip + PAGE)}
            >
              Next
            </Button>
          </div>
        </>
      ) : null}
    </Page>
  );
}
