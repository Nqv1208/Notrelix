/**
 * @notrelix/ui-web — Web UI primitives barrel export
 *
 * This package contains the shadcn/ui component library, feedback state components,
 * theme infrastructure, and responsive hooks for the Notrelix web client.
 *
 * Extracted from apps/app/components/ui, apps/app/components/feedback, and apps/app/lib/theme.
 */

// ── Design Tokens & Utilities ──────────────────────────────────────
export { cn } from "./lib/cn"

// ── Responsive Hooks ───────────────────────────────────────────────
export { useIsMobile } from "./hooks/use-mobile"
export { useLocalStorage } from "./hooks/use-local-storage"
export { useMediaQuery } from "./hooks/use-media-query"

// ── Theme ──────────────────────────────────────────────────────────
export { ThemeProvider, useTheme } from "./theme/theme-provider"
export { useColorTheme, COLOR_THEMES } from "./theme/use-color-theme"
export type { ColorTheme, ColorThemeMeta } from "./theme/use-color-theme"
export { colorThemeScript } from "./theme/color-theme-script"

// ── UI Primitives (shadcn) ─────────────────────────────────────────
export * from "./components/ui/accordion"
export * from "./components/ui/alert"
export * from "./components/ui/alert-dialog"
export * from "./components/ui/aspect-ratio"
export * from "./components/ui/avatar"
export * from "./components/ui/badge"
export * from "./components/ui/breadcrumb"
export * from "./components/ui/button"
export * from "./components/ui/button-group"
export * from "./components/ui/calendar"
export * from "./components/ui/card"
export * from "./components/ui/carousel"
export * from "./components/ui/chart"
export * from "./components/ui/checkbox"
export * from "./components/ui/collapsible"
export * from "./components/ui/command"
export * from "./components/ui/context-menu"
export * from "./components/ui/dialog"
export * from "./components/ui/drawer"
export * from "./components/ui/dropdown-menu"
export * from "./components/ui/empty"
export * from "./components/ui/field"
export * from "./components/ui/hover-card"
export * from "./components/ui/input"
export * from "./components/ui/input-group"
export * from "./components/ui/input-otp"
export * from "./components/ui/item"
export * from "./components/ui/kbd"
export * from "./components/ui/label"
export * from "./components/ui/menubar"
export * from "./components/ui/navigation-menu"
export * from "./components/ui/pagination"
export * from "./components/ui/popover"
export * from "./components/ui/progress"
export * from "./components/ui/radio-group"
export * from "./components/ui/resizable"
export * from "./components/ui/scroll-area"
export * from "./components/ui/select"
export * from "./components/ui/separator"
export * from "./components/ui/sheet"
export * from "./components/ui/sidebar"
export * from "./components/ui/skeleton"
export * from "./components/ui/slider"
export * from "./components/ui/sonner"
export * from "./components/ui/spinner"
export * from "./components/ui/switch"
export * from "./components/ui/table"
export * from "./components/ui/tabs"
export * from "./components/ui/textarea"
export * from "./components/ui/toggle"
export * from "./components/ui/toggle-group"
export * from "./components/ui/tooltip"

// ── Form Conventions ──────────────────────────────────────────────
export * from "./components/forms/submit-state"
export * from "./components/forms/server-validation-mapper"

// ── Feedback Components ────────────────────────────────────────────
export * from "./components/feedback/access-denied-state"
export * from "./components/feedback/empty-state"
export * from "./components/feedback/error-state"
export * from "./components/feedback/forbidden-state"
export * from "./components/feedback/loading-state"
export * from "./components/feedback/mock-disabled-state"
export * from "./components/feedback/not-found-state"
export * from "./components/feedback/upgrade-required-state"
