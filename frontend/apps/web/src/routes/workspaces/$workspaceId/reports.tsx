import { useSearch } from "@tanstack/react-router";
import {
  FeatureUnavailableSurface,
  useFeatureAvailabilityCopy,
} from "@notrelix/features-workspace/web";
import { backendCapabilities } from "@/config/backend-capabilities";
import type { ReportsSearch } from "../../../router/schemas/reports-search-schema";
import { ReportListPage } from "./reports/report-list-page";
import { ReportPage } from "./reports/report-page";

const ROUTE = "/workspaces/$workspaceId/reports";

/**
 * Reports (Final-Reports): the reports a person sees, or one report when the
 * address names it. Both live at one path so the sidebar's "Báo cáo" stays
 * current while a report is open.
 */
export function ReportsPage() {
  return backendCapabilities.reports ? (
    <AvailableReportsPage />
  ) : (
    <UnavailableReportsPage />
  );
}

/** Reports on a backend that has no producer for them yet. */
function UnavailableReportsPage() {
  const copy = useFeatureAvailabilityCopy();
  return (
    <div className="p-7">
      <FeatureUnavailableSurface copy={copy} feature="reports" />
    </div>
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
