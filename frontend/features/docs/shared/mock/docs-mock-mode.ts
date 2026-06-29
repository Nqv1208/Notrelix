import { isMockModeEnabled } from "@/lib/config/mock-mode"

export function isDocsMockModeEnabled(): boolean {
  return isMockModeEnabled("docs")
}
