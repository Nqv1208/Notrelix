import { randomUUID } from "node:crypto";
import { expect, request, test } from "@playwright/test";
import { apiRead, apiWrite, realUsers, signIn } from "./support/real-session";

const apiUrl = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";

const ownFacts = ["id", "email", "name", "avatarUrl", "emailConfirmed"];

test("a signed-in person reads only their own facts, and a revoked session ends at sign-in with the page remembered", async ({
  page,
  browser,
}, testInfo) => {
  await signIn(page, realUsers.admin);
  const here = new URL(page.url());
  const hereAddress = here.pathname + here.search;

  // Main flow: the answer holds the caller's identity facts and no workspace role or entitlement.
  const me = await apiRead<Record<string, unknown>>(page, "/auth/me");
  expect(me.email).toBe(realUsers.admin.email);
  expect(me.id).toEqual(expect.any(String));
  expect(Object.keys(me).filter((key) => !ownFacts.includes(key))).toEqual([]);

  // Rejection: with no session the answer is 401.
  const stranger = await request.newContext();
  try {
    expect((await stranger.get(`${apiUrl}/auth/me`)).status()).toBe(401);
  } finally {
    await stranger.dispose();
  }

  // Rejection: a session revoked from another device is refused at once.
  const sessions = await apiRead<{ id: string; isCurrent: boolean }[]>(
    page,
    "/auth/sessions",
  );
  const thisDevice = sessions.find((session) => session.isCurrent);
  expect(thisDevice, "the session list marks this device").toBeDefined();

  const otherContext = await browser.newContext({
    baseURL: testInfo.project.use.baseURL,
  });
  try {
    const otherDevice = await otherContext.newPage();
    await signIn(otherDevice, realUsers.admin);
    const revoked = await apiWrite(
      otherDevice,
      "POST",
      `/auth/sessions/${thisDevice!.id}/revoke`,
    );
    expect(revoked.status).toBe(200);
  } finally {
    await otherContext.close();
  }
  expect((await page.request.get(`${apiUrl}/auth/me`)).status()).toBe(401);

  // Opening the app on the revoked device shows sign-in and remembers the page.
  await page.reload();
  await expect(page).toHaveURL(/\/sign-in\?/);
  expect(new URL(page.url()).searchParams.get("redirect")).toBe(hereAddress);
  await expect(page.getByLabel("Email")).toBeVisible();
  // The page says why: the session ended, calmly, in words.
  await expect(
    page.getByRole("status").filter({ hasText: "Your session ended" }),
  ).toBeVisible();

  // Signing in again returns to the page the person was on.
  await page.getByLabel("Email").fill(realUsers.admin.email);
  const identified = page.waitForResponse((response) =>
    response.url().includes("/auth/identify"),
  );
  await page.getByRole("button", { name: "Continue", exact: true }).click();
  await identified;
  await page.locator("#auth-password").fill(realUsers.admin.password);
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  await expect(page).toHaveURL(here.origin + hereAddress);
  expect((await page.request.get(`${apiUrl}/auth/me`)).status()).toBe(200);
});

test("the device row signs another device out at once; another person's or an unknown session id reads as not found and changes nothing", async ({
  page,
  browser,
}, testInfo) => {
  await signIn(page, realUsers.workspaceAdmin);
  const workspaceId = /\/workspaces\/([^/]+)/.exec(page.url())![1];

  const otherContext = await browser.newContext({
    baseURL: testInfo.project.use.baseURL,
  });
  try {
    const otherDevice = await otherContext.newPage();
    await signIn(otherDevice, realUsers.workspaceAdmin);
    expect((await otherDevice.request.get(`${apiUrl}/auth/me`)).status()).toBe(
      200,
    );

    // Main flow: the person chooses Sign out on the other device's row.
    await page.goto(`/workspaces/${workspaceId}/account/security`);
    const otherRows = page.locator(
      'button[aria-label^="Sign out "]:not([aria-label*="(this device)"])',
    );
    await expect(otherRows.first()).toBeVisible();
    const before = await otherRows.count();
    await otherRows.first().click();
    await expect(page.getByText("Signed out.")).toBeVisible();
    await expect(otherRows).toHaveCount(before - 1);

    // The ended device's next call is refused.
    await expect
      .poll(async () =>
        (await otherDevice.request.get(`${apiUrl}/auth/me`)).status(),
      )
      .toBe(401);
    // This device stays signed in.
    expect((await page.request.get(`${apiUrl}/auth/me`)).status()).toBe(200);
  } finally {
    await otherContext.close();
  }

  // Rejection: another person's session id answers the same 404 as an id that never existed,
  // and the other person's session stays active.
  const mine = await apiRead<{ id: string; isCurrent: boolean }[]>(
    page,
    "/auth/sessions",
  );
  const foreignContext = await browser.newContext({
    baseURL: testInfo.project.use.baseURL,
  });
  try {
    const foreign = await foreignContext.newPage();
    await signIn(foreign, realUsers.admin);
    const foreignSessions = await apiRead<{ id: string; isCurrent: boolean }[]>(
      foreign,
      "/auth/sessions",
    );
    const foreignId = foreignSessions.find((entry) => entry.isCurrent)!.id;
    expect(mine.some((entry) => entry.id === foreignId)).toBe(false);

    const otherPersons = await apiWrite(
      page,
      "POST",
      `/auth/sessions/${foreignId}/revoke`,
    );
    const unknown = await apiWrite(
      page,
      "POST",
      `/auth/sessions/${randomUUID()}/revoke`,
    );
    expect(otherPersons.status).toBe(404);
    expect(unknown.status).toBe(404);
    expect(otherPersons.data).toMatchObject({
      errorCode: (unknown.data as { errorCode: string }).errorCode,
    });
    expect((await foreign.request.get(`${apiUrl}/auth/me`)).status()).toBe(200);
  } finally {
    await foreignContext.close();
  }
});

test("an unsafe request without the CSRF token is refused and changes nothing, and a stale token is recovered once", async ({
  page,
}) => {
  await signIn(page, realUsers.admin);

  // Rejections: no header and a wrong header both answer 403 and the session stays valid.
  const csrfHeaders: Record<string, string>[] = [
    {},
    { "X-CSRF-Token": "not-the-token" },
  ];
  for (const csrfHeader of csrfHeaders) {
    const refused = await page.request.post(`${apiUrl}/auth/logout`, {
      headers: { "Idempotency-Key": randomUUID(), ...csrfHeader },
    });
    expect(refused.status()).toBe(403);
    expect(await refused.json()).toMatchObject({
      errorCode: "security.csrf_validation_failed",
    });
    expect((await page.request.get(`${apiUrl}/auth/me`)).status()).toBe(200);
  }

  // Main flow with a stale token: the page's client holds the token it was given at
  // sign-in, so replacing the cookie makes it stale. The sign-out write is refused
  // once, the client fetches a new token, retries once, and the write lands once.
  const [csrfCookie] = (await page.context().cookies()).filter(
    (cookie) => cookie.name === "csrf_token",
  );
  expect(csrfCookie, "the server set the csrf_token cookie").toBeDefined();
  await page.context().addCookies([{ ...csrfCookie, value: "stale-token" }]);

  const logoutStatuses: number[] = [];
  let tokenFetches = 0;
  page.on("response", (response) => {
    const method = response.request().method();
    if (method === "POST" && response.url().endsWith("/auth/logout")) {
      logoutStatuses.push(response.status());
    }
    if (method === "GET" && response.url().endsWith("/auth/csrf")) {
      tokenFetches += 1;
    }
  });

  await page.getByRole("button", { name: /Account menu/ }).click();
  await page.getByRole("menuitem", { name: /Sign out/ }).click();
  await expect(page).toHaveURL(/\/sign-in/);

  expect(logoutStatuses).toEqual([403, 200]);
  expect(tokenFetches).toBe(1);
  expect((await page.request.get(`${apiUrl}/auth/me`)).status()).toBe(401);
});
