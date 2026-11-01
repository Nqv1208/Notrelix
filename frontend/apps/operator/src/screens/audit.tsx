import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@notrelix/ui-web";
import { operatorApi } from "../api";
import { ErrorNote, Loading, Page, dateTime, useLoad } from "../parts";

export function Audit() {
  const { data, error, loading } = useLoad(
    () => operatorApi.audit(null, 100),
    "all",
  );
  return (
    <Page
      title="Audit trail"
      lede="Every operator action: who, what, why, and when. Entries are append-only."
    >
      <ErrorNote message={error} />
      {loading && !data ? <Loading /> : null}
      {data ? (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>When</TableHead>
              <TableHead>Operator</TableHead>
              <TableHead>Action</TableHead>
              <TableHead>Target</TableHead>
              <TableHead>Reason</TableHead>
              <TableHead>Before</TableHead>
              <TableHead>After</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {data.map((e) => (
              <TableRow key={e.id}>
                <TableCell>{dateTime(e.occurredAt)}</TableCell>
                <TableCell>{e.operatorEmail}</TableCell>
                <TableCell>{e.action}</TableCell>
                <TableCell className="font-mono">
                  {e.targetKind} {e.targetId.slice(0, 8)}
                </TableCell>
                <TableCell>{e.reason}</TableCell>
                <TableCell className="max-w-[220px] truncate font-mono text-caption">
                  {e.beforeJson}
                </TableCell>
                <TableCell className="max-w-[220px] truncate font-mono text-caption">
                  {e.afterJson}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      ) : null}
    </Page>
  );
}
