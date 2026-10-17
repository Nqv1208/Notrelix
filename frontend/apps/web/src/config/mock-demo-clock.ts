/**
 * The demo clock of the mock world (dev only, `VITE_MOCK_API=true`).
 *
 * The mock data is anchored to Monday 2026-10-12 10:40 (Hanoi), the "today" of
 * the design mockups. A plain browser has the real day, so the pages would show
 * every date wrong. This shim makes `Date` start at the demo instant and then
 * advance in real time. `VITE_MOCK_NOW` overrides the instant (ISO, e.g.
 * `2026-10-12T10:40:00+07:00`); `VITE_MOCK_NOW=real` keeps the real clock.
 *
 * A test runner that already fakes the clock (Playwright `page.clock`) is left
 * alone: its `Date.now` is not native code.
 */
export const DEMO_NOW = "2026-10-12T10:40:00+07:00";

export function installMockDemoClock(
  setting: string | undefined,
  scope: typeof globalThis = globalThis,
): void {
  if (setting === "real") return;
  const NativeDate = scope.Date;
  if (!/\[native code\]/.test(Function.prototype.toString.call(NativeDate.now)))
    return;
  const target = NativeDate.parse(setting || DEMO_NOW);
  if (Number.isNaN(target)) return;
  const offset = target - NativeDate.now();

  class DemoDate extends NativeDate {
    constructor(...args: unknown[]) {
      if (args.length === 0) super(NativeDate.now() + offset);
      else super(...(args as [number]));
    }
    static override now(): number {
      return NativeDate.now() + offset;
    }
  }
  scope.Date = DemoDate as unknown as DateConstructor;
}
