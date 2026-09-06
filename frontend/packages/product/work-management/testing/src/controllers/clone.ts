export function cloneScenario<T>(value: T): T {
  return structuredClone(value);
}
