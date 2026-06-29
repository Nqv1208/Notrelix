export type MockFeature =
  | "docs"
  | "billing"
  | "search"
  | "governance"
  | "automation"
  | "integrations"
  | "work-management"

const mockFlags = {
  docs: process.env.NEXT_PUBLIC_USE_MOCK_DOCS,
  billing: process.env.NEXT_PUBLIC_USE_MOCK_BILLING,
  search: process.env.NEXT_PUBLIC_USE_MOCK_SEARCH,
  governance: process.env.NEXT_PUBLIC_USE_MOCK_GOVERNANCE,
  automation: process.env.NEXT_PUBLIC_USE_MOCK_AUTOMATION,
  integrations: process.env.NEXT_PUBLIC_USE_MOCK_INTEGRATIONS,
  "work-management": process.env.NEXT_PUBLIC_USE_MOCK_WORK_MANAGEMENT,
} as const

export function isMockModeEnabled(feature: MockFeature): boolean {
  if (process.env.NODE_ENV === "production") {
    return false
  }

  if (process.env.NEXT_PUBLIC_USE_MOCK_API === "true") {
    return true
  }

  return mockFlags[feature] === "true"
}
