export type MockFeature =
  | "docs"
  | "billing"
  | "search"
  | "governance"
  | "automation"
  | "integrations"
  | "work-management"

export type MockModeConfig = {
  nodeEnv?: string
  all?: string | boolean
  flags?: Partial<Record<MockFeature, string | boolean | undefined>>
}

function isEnabled(value: string | boolean | undefined): boolean {
  return value === true || value === "true"
}

export function createMockModeChecker(config: MockModeConfig = {}) {
  return function isMockModeEnabled(feature: MockFeature): boolean {
    if (config.nodeEnv === "production") {
      return false
    }

    if (isEnabled(config.all)) {
      return true
    }

    return isEnabled(config.flags?.[feature])
  }
}

export const isMockModeEnabled = createMockModeChecker()
