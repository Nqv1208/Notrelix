import { act, useEffect, type ReactNode } from "react";
import { createRoot, type Root } from "react-dom/client";
import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
  authQueryKeys,
  createAuthProvider,
  useAuth,
} from "@notrelix/features-auth";
import { endpoints } from "@notrelix/contracts";
import { I18nProvider, type Locale } from "@notrelix/i18n";
import { AppError } from "@notrelix/kernel";
import { useAccountId } from "@notrelix/query";
import { AppRuntimeProvider } from "@notrelix/runtime-web";
import { ThemeProvider } from "@notrelix/ui-web";
import { createApplicationLifecycle } from "../composition/application-lifecycle";
import { ApplicationServicesProvider } from "../composition/application-services-context";
import { webCatalogs } from "../i18n/catalogs";
import { AccountScopeBridge } from "../providers/account-scope-bridge";

(
  globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }
).IS_REACT_ACT_ENVIRONMENT = true;

let root: Root | undefined;
let host: HTMLElement | undefined;

afterEach(() => {
  act(() => root?.unmount());
  host?.remove();
  root = undefined;
  host = undefined;
});

const userOne = { id: "user-1", email: "a@b.vn", name: "An", avatarUrl: null };
const userTwo = {
  id: "user-2",
  email: "c@d.vn",
  name: "Binh",
  avatarUrl: null,
};

const networkDown = () =>
  new AppError({ kind: "network", message: "Network error." });
const authRejected = () =>
  new AppError({ kind: "auth", status: 401, message: "Session expired." });

function deferred<T>() {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>((r) => {
    resolve = r;
  });
  return { promise, resolve };
}

interface Mount {
  readonly queryClient: QueryClient;
  readonly bootstrapCalls: () => number;
  readonly post: ReturnType<typeof vi.fn>;
  readonly transitions: Array<string | null>;
  readonly events: string[];
  readonly rendered: string[];
  readonly navigateToSignIn: ReturnType<typeof vi.fn>;
}

/** `bootstrap(userId, call)` answers GET /auth/bootstrap; `profile` answers GET /auth/me. */
async function mount({
  profile = () => Promise.resolve(userOne),
  bootstrap,
  locale = "en",
  lifecycleOverride,
}: {
  profile?: () => Promise<unknown>;
  bootstrap: (call: number) => Promise<unknown>;
  locale?: Locale;
  lifecycleOverride?: (queryClient: QueryClient) => {
    prepareAccountTransition(id: string | null): void;
  };
}): Promise<Mount> {
  let bootstrapCalls = 0;
  const get = vi.fn((url: string) => {
    if (url === endpoints.auth.profile) return profile();
    if (url === endpoints.auth.bootstrap) return bootstrap(++bootstrapCalls);
    return Promise.reject(new Error(`unexpected GET ${url}`));
  });
  const post = vi.fn(() => Promise.resolve(undefined));
  const api = { get, post } as never;
  const queryClient = new QueryClient();
  const transitions: Array<string | null> = [];
  const events: string[] = [];
  const rendered: string[] = [];
  const lifecycle =
    lifecycleOverride?.(queryClient) ??
    ({
      prepareAccountTransition(id: string | null) {
        transitions.push(id);
        events.push(`transition:${id}`);
      },
    } as const);
  const navigateToSignIn = vi.fn();
  const AuthProvider = createAuthProvider({ api, endpoints });

  function Scoped(): ReactNode {
    const accountId = useAccountId();
    rendered.push(accountId);
    useEffect(() => {
      events.push(`subscribe:${accountId}`);
    }, [accountId]);
    return <p data-testid="scoped">{`scoped:${accountId}`}</p>;
  }

  // Like the routes: account-scoped content only for a signed-in person.
  function Child(): ReactNode {
    const { user } = useAuth();
    return (
      <>
        {user ? <Scoped /> : null}
        <p>public</p>
      </>
    );
  }

  host = document.createElement("div");
  document.body.appendChild(host);
  root = createRoot(host);
  await act(async () => {
    root?.render(
      <QueryClientProvider client={queryClient}>
        <ApplicationServicesProvider services={{ lifecycle } as never}>
          <AppRuntimeProvider runtime={{ api: { api, endpoints } } as never}>
            <AuthProvider>
              <I18nProvider catalogs={webCatalogs} initialLocale={locale}>
                <ThemeProvider>
                  <AccountScopeBridge navigateToSignIn={navigateToSignIn}>
                    <Child />
                  </AccountScopeBridge>
                </ThemeProvider>
              </I18nProvider>
            </AuthProvider>
          </AppRuntimeProvider>
        </ApplicationServicesProvider>
      </QueryClientProvider>,
    );
  });
  return {
    queryClient,
    bootstrapCalls: () => bootstrapCalls,
    post,
    transitions,
    events,
    rendered,
    navigateToSignIn,
  };
}

const text = () => host?.textContent ?? "";
const scoped = () => host?.querySelector('[data-testid="scoped"]') ?? null;
const button = (name: string) =>
  [...(host?.querySelectorAll("button") ?? [])].find(
    (candidate) => candidate.textContent === name,
  );

async function until(check: () => boolean) {
  await vi.waitFor(() => expect(check()).toBe(true));
}

async function click(name: string) {
  const target = button(name);
  expect(target, `button "${name}"`).toBeDefined();
  await act(async () => target!.click());
}

describe("AccountScopeBridge", () => {
  it("provides the Account of the bootstrap, which is not the User id", async () => {
    const m = await mount({
      bootstrap: () => Promise.resolve({ accountId: "acc-real" }),
    });
    await until(() => scoped() !== null);
    expect(text()).toContain("scoped:acc-real");
    expect(text()).not.toContain(userOne.id);
    expect(m.rendered.every((id) => id === "acc-real")).toBe(true);
    expect(m.transitions.filter((id) => id !== null)).toEqual(["acc-real"]);
    expect(m.transitions).not.toContain(userOne.id);
  });

  it("renders no account-scoped child while the bootstrap is pending", async () => {
    const pending = deferred<unknown>();
    const m = await mount({ bootstrap: () => pending.promise });
    await until(() => m.bootstrapCalls() === 1);
    const status = host!.querySelector('[role="status"]');
    expect(status?.textContent).toContain("Opening your account...");
    expect(scoped()).toBeNull();
    expect(text()).not.toContain("public");
    expect(m.rendered).toEqual([]);
    expect(m.transitions.filter((id) => id !== null)).toEqual([]);

    await act(async () => pending.resolve({ accountId: "acc-real" }));
    await until(() => scoped() !== null);
    expect(host!.querySelector('[role="status"]')).toBeNull();
  });

  it("says so in Vietnamese while it loads", async () => {
    const pending = deferred<unknown>();
    const m = await mount({ bootstrap: () => pending.promise, locale: "vi" });
    await until(() => m.bootstrapCalls() === 1);
    expect(host!.querySelector('[role="status"]')?.textContent).toContain(
      "Đang mở tài khoản của bạn...",
    );
  });

  it("shows a failure with Retry that recovers once the read works", async () => {
    const m = await mount({
      bootstrap: (call) =>
        call === 1
          ? Promise.reject(networkDown())
          : Promise.resolve({ accountId: "acc-real" }),
    });
    await until(() => host!.querySelector('[role="alert"]') !== null);
    expect(text()).toContain("Could not open your account");
    expect(scoped()).toBeNull();
    expect(m.rendered).toEqual([]);
    expect(button("Sign out")).toBeUndefined();

    await click("Try again");
    await until(() => scoped() !== null);
    expect(text()).toContain("scoped:acc-real");
    expect(m.bootstrapCalls()).toBe(2);
    expect(host!.querySelector('[role="alert"]')).toBeNull();
  });

  it("says so in Vietnamese when the read failed", async () => {
    await mount({
      bootstrap: () => Promise.reject(networkDown()),
      locale: "vi",
    });
    await until(() => host!.querySelector('[role="alert"]') !== null);
    expect(text()).toContain("Chưa mở được tài khoản của bạn");
    expect(button("Thử lại")).toBeDefined();
  });

  it("offers Retry and Sign out when the member belongs to no Account", async () => {
    const m = await mount({
      bootstrap: (call) =>
        Promise.resolve({ accountId: call === 1 ? null : "acc-late" }),
    });
    await until(() => host!.querySelector('[role="alert"]') !== null);
    expect(text()).toContain("No account to open");
    expect(scoped()).toBeNull();
    expect(m.rendered).toEqual([]);
    expect(m.transitions.filter((id) => id !== null)).toEqual([]);
    expect(button("Try again")).toBeDefined();

    await click("Sign out");
    await until(() => m.navigateToSignIn.mock.calls.length === 1);
    expect(m.post).toHaveBeenCalledWith(endpoints.auth.logout, {});

    await click("Try again");
    await until(() => scoped() !== null);
    expect(text()).toContain("scoped:acc-late");
  });

  it("does not gate a signed-out visitor and reads no bootstrap", async () => {
    const m = await mount({
      profile: () => Promise.reject(authRejected()),
      bootstrap: () => Promise.reject(new Error("must not be read")),
    });
    await until(() => text().includes("public"));
    expect(host!.querySelector('[role="status"]')).toBeNull();
    expect(host!.querySelector('[role="alert"]')).toBeNull();
    expect(m.bootstrapCalls()).toBe(0);
    expect(m.transitions.every((id) => id === null)).toBe(true);
  });

  it("calls prepareAccountTransition before the children subscribe", async () => {
    const m = await mount({
      bootstrap: () => Promise.resolve({ accountId: "acc-real" }),
    });
    await until(() => scoped() !== null);
    expect(m.events.indexOf("transition:acc-real")).toBeGreaterThanOrEqual(0);
    expect(m.events.indexOf("transition:acc-real")).toBeLessThan(
      m.events.indexOf("subscribe:acc-real"),
    );
  });

  it("re-keys on a change of person: a new read, the previous Account never leaks, the cache is cleared", async () => {
    const second = deferred<unknown>();
    const realtime = { disconnect: vi.fn() };
    const m = await mount({
      bootstrap: (call) =>
        call === 1 ? Promise.resolve({ accountId: "acc-one" }) : second.promise,
      lifecycleOverride: (queryClient) =>
        createApplicationLifecycle({
          queryClient,
          realtime: realtime as never,
          sessionEvents: { subscribe: () => () => undefined } as never,
          navigateToSignedOut: vi.fn(),
        }),
    });
    await until(() => scoped() !== null);
    expect(text()).toContain("scoped:acc-one");
    m.queryClient.setQueryData(["marker"], "left by the first person");
    const renderedBefore = m.rendered.length;

    await act(async () =>
      m.queryClient.setQueryData(authQueryKeys.profile, userTwo),
    );
    await until(() => m.bootstrapCalls() === 2);
    // Pending for the second person: the first one's Account is not reused.
    expect(scoped()).toBeNull();
    expect(host!.querySelector('[role="status"]')).not.toBeNull();
    expect(m.rendered.slice(renderedBefore)).toEqual([]);
    expect(
      m.queryClient.getQueryData(authQueryKeys.bootstrapAccount(userTwo.id)),
    ).toBeUndefined();

    await act(async () => second.resolve({ accountId: "acc-two" }));
    await until(() => scoped() !== null);
    expect(text()).toContain("scoped:acc-two");
    expect(m.rendered.slice(renderedBefore)).not.toContain("acc-one");
    expect(
      m.rendered.slice(renderedBefore).every((id) => id === "acc-two"),
    ).toBe(true);
    // The Account changed: the first person's data is gone, nothing was read twice.
    expect(m.queryClient.getQueryData(["marker"])).toBeUndefined();
    expect(
      m.queryClient.getQueryData(authQueryKeys.bootstrapAccount(userOne.id)),
    ).toBeUndefined();
    expect(
      m.queryClient.getQueryData(authQueryKeys.bootstrapAccount(userTwo.id)),
    ).toEqual({ accountId: "acc-two" });
    expect(m.bootstrapCalls()).toBe(2);
    expect(realtime.disconnect).toHaveBeenCalledWith("account-switch");
  });

  it("passes the real id of each person to prepareAccountTransition", async () => {
    const m = await mount({
      bootstrap: (call) =>
        Promise.resolve({ accountId: call === 1 ? "acc-one" : "acc-two" }),
    });
    await until(() => scoped() !== null);
    await act(async () =>
      m.queryClient.setQueryData(authQueryKeys.profile, userTwo),
    );
    await until(() => text().includes("scoped:acc-two"));
    expect(m.transitions.filter((id) => id !== null)).toEqual([
      "acc-one",
      "acc-two",
    ]);
  });
});
