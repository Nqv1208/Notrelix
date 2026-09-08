export function unstableSummary(id: string) {
  return {
    id,
    token: `tok-${Math.random().toString(36).slice(2)}`,
    createdAt: new Date(),
  };
}
