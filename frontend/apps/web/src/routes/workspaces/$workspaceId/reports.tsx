import { useSearch } from "@tanstack/react-router";
import { backendCapabilities } from "@/config/backend-capabilities";
import type { ReportsSearch } from "../../../router/schemas/reports-search-schema";
import { ReportListPage } from "./reports/report-list-page";
import { ReportPage } from "./reports/report-page";
import { WorkReportPage } from "./reports/work-report-page";

const ROUTE = "/workspaces/$workspaceId/reports";

/**
 * Reports (Final-Reports): the reports a person sees, or one report when the
 * address names it. Both live at one path so the sidebar's "Báo cáo" stays
 * current while a report is open.
 */
export function ReportsPage() {
  const search: ReportsSearch = useSearch({ from: ROUTE });
  // The report builder needs the mock backend; the read-only work report is served by the real one
  // and is what the page shows there. `view=work` opens it on the mock backend too.
  return backendCapabilities.reports && search.view !== "work" ? (
    <AvailableReportsPage />
  ) : (
    <WorkReportPage />
  );
}

function AvailableReportsPage() {
  const search: ReportsSearch = useSearch({ from: ROUTE });
  return search.report ? (
    <ReportPage key={search.report} reportId={search.report} />
  ) : (
    <ReportListPage />
  );
}
