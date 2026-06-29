// ── features/docs public API ─────────────────────────────────────────────────
// Only these exports may be imported by app/ or sibling features.
// Internal editor components, hooks, store, and mappers must NOT be exported.

// ── Screen and View components ───────────────────────────────────────────────
export { DocumentScreen } from "./pages/components/document-screen"
export { DocsWorkspaceView } from "./pages/components/docs-workspace-view"
export { DocumentHistoryScreen } from "./pages/components/document-history-screen"
export { DocsViewPickerDataProvider } from "./pages/components/docs-view-picker-data-provider"
export { DocsWorkspaceSummary } from "./pages/components/docs-workspace-summary"
export { DocsDashboardPreview } from "./pages/components/docs-dashboard-preview"
export { DocsViewToolbar } from "./pages/components/docs-view-toolbar"
export { usePageList } from "./tree/hooks/queries/use-page-tree"

// ── Domain types ─────────────────────────────────────────────────────────────
export type { Page } from "./pages/types/page.types"
export type { Block } from "./blocks/types/block.types"
