import { hasPermission } from "./ability"

export function useCan() {
  const can = (role: string | undefined, permission: string) => {
    return hasPermission(role, permission)
  }
  return { can }
}
