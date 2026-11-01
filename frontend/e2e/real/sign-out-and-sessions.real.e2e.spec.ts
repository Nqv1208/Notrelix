import { expect, test, type Browser } from "@playwright/test";
import { freshMember, registerPerson } from "./support/fresh-person";
import { apiRead, apiWrite } from "./support/real-session";

const apiUrl = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";

interface Session {
  id: string;
  isCurrent: boolean;
  status: string;
}

/** Another device of the person: its own browser context signed in through the API. */
async function deviceOf(
  browser: Browser,
  person: { email: string; password: string },
) {
  const context = await browser.newContext();
  const page = await context.newPage();
  const signedIn = await apiWrite(page, "POST", "/auth/login", {
    email: person.email,
    password: person.password,
  });
  expect(signedIn.status, "sign in on another device").toBe(200);
  return { context, page };
}

const refreshCookieOf = async (context: {
  cookies: () => Promise<{ name: string; value: string }[]>;
}) => {
  const cookie = (await context.cookies()).find((entry) =>
    /refresh/i.test(entry.name),
  );
  expect(cookie, "the refresh cookie").toBeDefined();
  return cookie!;
};

test("the session list shows my devices and no one else's, and one step-up signs out every other device", async ({
  page,
  browser,
}) => {
  const member = await freshMember(page, "devices");
  const phone = await deviceOf(browser, member);
  const laptop = await deviceOf(browser, member);

  // UC-IDN-C02: three live sessions, one of them this device.
  const sessions = await apiRead<Session[]>(page, "/auth/sessions");
  const live = sessions.filter((entry) => entry.status === "Active");
  expect(live).toHaveLength(3);
  expect(live.filter((entry) => entry.isCurrent)).toHaveLength(1);
  await page.goto(`/workspaces/${member.workspaceId}/account/security`);
  await expect(page.getByText("3 sessions are active")).toBeVisible();
  await expect(page.getByText("This device")).toBeVisible();

  // Another person's session never appears in my list.
  const stranger = await browser.newContext();
  const strangerPage = await stranger.newPage();
  await registerPerson(strangerPage, "stranger");
  const strangerIds = (
    await apiRead<Session[]>(strangerPage, "/auth/sessions")
  ).map((entry) => entry.id);
  expect(strangerIds.length).toBeGreaterThan(0);
  expect(sessions.some((entry) => strangerIds.includes(entry.id))).toBe(false);
  await stranger.close();

  // UC-IDN-C04: without a proof the call is refused and every device stays signed in.
  const refused = await apiWrite(page, "POST", "/auth/sessions/revoke-others", {
    stepUpToken: "not-a-proof",
  });
  expect(refused.status).toBeGreaterThanOrEqual(400);
  expect((await phone.page.request.get(`${apiUrl}/auth/me`)).status()).toBe(
    200,
  );

  // The page asks for the password first; a wrong one changes nothing.
  await page
    .getByRole("button", { name: "Sign out of all other devices" })
    .click();
  const proof = page.getByRole("region", {
    name: "Confirm to sign out of all other devices",
  });
  await proof.getByLabel("Current password").fill("not-the-password");
  await proof.getByRole("button", { name: "Confirm", exact: true }).click();
  await expect(proof.getByRole("alert")).toHaveText(
    "That password is not right.",
  );
  expect((await laptop.page.request.get(`${apiUrl}/auth/me`)).status()).toBe(
    200,
  );

  await proof.getByLabel("Current password").fill(member.password);
  await proof.getByRole("button", { name: "Confirm", exact: true }).click();
  await expect(page.getByText("Signed out of 2 other devices.")).toBeVisible();
  await expect
    .poll(async () =>
      (await phone.page.request.get(`${apiUrl}/auth/me`)).status(),
    )
    .toBe(401);
  expect((await laptop.page.request.get(`${apiUrl}/auth/me`)).status()).toBe(
    401,
  );
  expect((await page.request.get(`${apiUrl}/auth/me`)).status()).toBe(200);
  const after = await apiRead<Session[]>(page, "/auth/sessions");
  expect(after.filter((entry) => entry.status === "Active")).toHaveLength(1);

  // With no other device left, it is a no-op: the page says so.
  await page.reload();
  await expect(page.getByText("No other device is signed in.")).toBeVisible();
  await phone.context.close();
  await laptop.context.close();
});

test("signing out this device ends its session and its refresh credential; a second sign-out is refused as already gone", async ({
  page,
}) => {
  const member = await freshMember(page, "signout");
  const refresh = await refreshCookieOf(page.context());

  // UC-IDN-C01: the row of this device asks first, then returns to sign-in.
  await page.goto(`/workspaces/${member.workspaceId}/account/security`);
  await page.locator('button[aria-label$="(this device)"]').first().click();
  const confirmation = page.getByRole("region", {
    name: "Sign out of this device?",
  });
  await expect(confirmation).toBeVisible();
  await confirmation.getByRole("button", { name: "Sign out" }).click();
  await expect(page).toHaveURL(/\/sign-in/);

  // The next call with the old cookies is 401, and the old refresh credential no longer refreshes.
  expect((await page.request.get(`${apiUrl}/auth/me`)).status()).toBe(401);
  await page.context().addCookies([refresh]);
  const replay = await apiWrite(page, "POST", "/auth/refresh");
  expect(replay.status).toBe(401);

  // A second sign-out finds nothing to end: refused, never a revived session.
  const second = await apiWrite(page, "POST", "/auth/logout");
  expect(second.status).toBe(401);
});

test("a refresh rotates the credential and keeps the session; the replaced credential is served inside ten seconds, and after them its use ends the session", async ({
  page,
  browser,
}) => {
  const member = await freshMember(page, "rotate");
  const device = await deviceOf(browser, member);
  const sessionBefore = (
    await apiRead<Session[]>(device.page, "/auth/sessions")
  ).find((entry) => entry.isCurrent)!;
  const first = await refreshCookieOf(device.context);

  // UC-IDN-D01: a refresh answers a new credential and the session keeps its id.
  const refreshed = await apiWrite(device.page, "POST", "/auth/refresh");
  expect(refreshed.status).toBe(200);
  const second = await refreshCookieOf(device.context);
  expect(second.value).not.toBe(first.value);
  const sessionAfter = (
    await apiRead<Session[]>(device.page, "/auth/sessions")
  ).find((entry) => entry.isCurrent)!;
  expect(sessionAfter.id).toBe(sessionBefore.id);

  // UC-IDN-D03: inside the window the replaced credential is a retry and is answered with the new one.
  const retry = await browser.newContext();
  await retry.addCookies([first]);
  const retryPage = await retry.newPage();
  const served = await apiWrite(retryPage, "POST", "/auth/refresh");
  expect(served.status).toBe(200);
  expect((await refreshCookieOf(retry)).value).toBe(second.value);

  // After the window the same credential is a replay: refused, and the whole session ends.
  await new Promise((resolve) => setTimeout(resolve, 11_000));
  await retry.addCookies([first]);
  const replay = await apiWrite(retryPage, "POST", "/auth/refresh");
  expect(replay.status).toBe(401);
  expect((await device.page.request.get(`${apiUrl}/auth/me`)).status()).toBe(
    401,
  );
  const owner = await apiRead<Session[]>(page, "/auth/sessions");
  expect(owner.find((entry) => entry.id === sessionBefore.id)?.status).not.toBe(
    "Active",
  );
  await retry.close();
  await device.context.close();
});
