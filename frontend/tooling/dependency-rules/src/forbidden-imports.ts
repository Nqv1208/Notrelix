/**
 * Forbidden imports per package — cross-runtime and architectural violations.
 * These rules prevent web/mobile cross-contamination, DOM leaks into core,
 * and business logic leaking into UI primitives.
 */
export const FORBIDDEN_IMPORTS: Record<string, string[]> = {
  // Foundation: no React, no DOM, no Next, no cross-layer UI
  "@notrelix/contracts": [
    "react", "react-dom", "react-native",
    "next", "next-themes", "@notrelix/ui-web", "@notrelix/ui-mobile"
  ],
  "@notrelix/kernel": [
    "react", "react-dom", "react-native",
    "next", "next-themes", "@notrelix/platform", "@notrelix/ui-web", "@notrelix/ui-mobile"
  ],
  "@notrelix/platform": [
    "next", "next-themes", "@notrelix/ui-web", "@notrelix/ui-mobile"
  ],
  "@notrelix/query": [
    "next", "next-themes", "@notrelix/ui-web", "@notrelix/ui-mobile"
  ],
  "@notrelix/realtime": [
    "next", "next-themes", "@notrelix/ui-web", "@notrelix/ui-mobile"
  ],
  "@notrelix/observability": [
    "next", "next-themes", "@notrelix/ui-web", "@notrelix/ui-mobile"
  ],
  "@notrelix/ui-tokens": [
    "react", "react-dom", "react-native", "next"
  ],

  // Product cores: no React, no UI
  "@notrelix/work-management-core": [
    "react", "react-dom",
    "@notrelix/ui-web", "@notrelix/ui-mobile",
  ],
  "@notrelix/work-management-state": [
    "next", "next-themes", "@notrelix/ui-web", "@notrelix/ui-mobile",
  ],
  "@notrelix/work-management-plugins": [
    "react", "react-dom",
    "@notrelix/ui-web", "@notrelix/ui-mobile",
  ],
  "@notrelix/docs-core": [
    "@notrelix/ui-web", "@notrelix/ui-mobile",
  ],
  "@notrelix/automation-core": [
    "react", "react-dom",
    "@notrelix/ui-web", "@notrelix/ui-mobile",
  ],

  // Cross-runtime: web must not import mobile, mobile must not import web
  "@notrelix/ui-web": ["@notrelix/ui-mobile", "react-native"],
  "@notrelix/ui-mobile": ["@notrelix/ui-web", "@radix-ui", "shadcn", "cmdk"],
  "@notrelix/work-management-web": [
    "@notrelix/ui-mobile", "@notrelix/runtime-mobile", "react-native",
  ],
  "@notrelix/work-management-mobile": [
    "@notrelix/ui-web", "@notrelix/runtime-web", "@radix-ui", "shadcn",
  ],
  "@notrelix/runtime-web": ["react-native"],
  "@notrelix/runtime-mobile": ["@radix-ui", "shadcn"],

  // Marketing: no product state, no realtime, no platform internals
  "@notrelix/app-marketing": [
    "@notrelix/work-management-state", "@notrelix/work-management-core",
    "@notrelix/realtime", "@notrelix/platform",
  ],

  // Web app: no Next.js
  "@notrelix/app-web": ["next", "next-themes"],

  // Mobile app: no web, no Next.js
  "@notrelix/app-mobile": ["next", "@notrelix/ui-web", "@notrelix/runtime-web"],
}
