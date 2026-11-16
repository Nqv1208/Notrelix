import { randomUUID } from "node:crypto";
import { expect, test, type Page } from "@playwright/test";
import {
  continueWithGoogle,
  expectSignedIn,
  signInAtProvider,
  sqlValue,
} from "./support/oidc-provider";
import { realUsers } from "./support/real-session";

/**
 * Google sign-in against the real backend and a provider test double that signs its ID tokens. The
 * backend checks them as it would Google's; nothing about its validation is relaxed here.
 */

const oidcPort = process.env.REAL_E2E_OIDC_PORT ?? "58090";
const apiUrl = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";

async function english(page: Page) {
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "en");
    } catch {
      // Private mode: defaults apply.
    }
  });
}

function newPerson() {
  const id = randomUUID();
  return { subject: `real-e2e-${id}`, email: `ext-${id}@example.test` };
}

const usersWith = (email: string) =>
  sqlValue(`select count(*) from identity.users where email = '${email}'`);
const linksOf = (email: string) =>
  sqlValue(
    `select count(*) from identity.oauth_accounts where user_id in (select id from identity.users where email = '${email}')`,
  );

test("starting Google sign-in sends the browser to the provider with a state, a nonce and an S256 challenge, and an outside return address is refused", async ({
  request,
}) => {
  // An outside return address is refused, never followed: no redirect to the provider is made.
  const outside = await request.get(
    `${apiUrl}/auth/oauth/google/start?returnUrl=${encodeURIComponent("https://evil.example/x")}`,
    { maxRedirects: 0 },
  );
  expect(outside.status()).toBe(400);
  expect(outside.headers()["location"]).toBeUndefined();

  const started = await request.get(`${apiUrl}/auth/oauth/google/start`, {
    maxRedirects: 0,
  });
  expect(started.status()).toBe(302);
  const location = new URL(started.headers()["location"]);
  expect(location.origin).toBe(`http://127.0.0.1:${oidcPort}`);
  expect(location.searchParams.get("client_id")).toBe("real-e2e-google-client");
  expect(location.searchParams.get("response_type")).toBe("code");
  expect(location.searchParams.get("state")?.length).toBeGreaterThan(10);
  expect(location.searchParams.get("nonce")?.length).toBeGreaterThan(10);
  expect(location.searchParams.get("code_challenge_method")).toBe("S256");
  expect(location.searchParams.get("code_challenge")?.length).toBeGreaterThan(
    10,
  );

  // Every start holds a new state.
  const again = await request.get(`${apiUrl}/auth/oauth/google/start`, {
    maxRedirects: 0,
  });
  expect(
    new URL(again.headers()["location"]).searchParams.get("state"),
  ).not.toBe(location.searchParams.get("state"));
});

test("a new Google subject with a verified free email gets one account and a session; the same subject later signs in to that account; a replayed callback opens nothing", async ({
  page,
  browser,
}) => {
  test.setTimeout(90_000);
  const person = newPerson();
  await english(page);
  let callbackUrl = "";
  page.on("request", (request) => {
    if (request.url().includes("/auth/oauth/google/callback"))
      callbackUrl = request.url();
  });

  await continueWithGoogle(page);
  await signInAtProvider(page, person.subject, {
    email: person.email,
    email_verified: true,
    name: "Casey Provider",
  });
  await expectSignedIn(page);
  expect(usersWith(person.email)).toBe("1");
  expect(linksOf(person.email)).toBe("1");

  // The state was spent by the first callback: the same URL again is refused and sets no cookie.
  expect(callbackUrl).toContain("code=");
  const replayContext = await browser.newContext();
  const replay = await replayContext.request.get(callbackUrl, {
    maxRedirects: 0,
  });
  expect(replay.status()).toBe(302);
  expect(replay.headers()["location"]).toContain(
    "error=identity.oauth.state-invalid",
  );
  expect(replay.headers()["set-cookie"]).toBeUndefined();
  await replayContext.close();

  // A new browser, the same provider subject: the same person, no second account.
  const second = await browser.newContext();
  const secondPage = await second.newPage();
  await english(secondPage);
  await continueWithGoogle(secondPage);
  await signInAtProvider(secondPage, person.subject, {
    email: person.email,
    email_verified: true,
  });
  await expectSignedIn(secondPage);
  expect(usersWith(person.email)).toBe("1");
  expect(linksOf(person.email)).toBe("1");
  await second.close();
});

test("a callback with a state that was never issued is refused with the expired sentence and no session", async ({
  page,
}) => {
  await english(page);
  const refused = await page.request.get(
    `${apiUrl}/auth/oauth/google/callback?code=abc&state=${randomUUID()}`,
    { maxRedirects: 0 },
  );
  expect(refused.status()).toBe(302);
  expect(refused.headers()["location"]).toContain(
    "error=identity.oauth.state-invalid",
  );
  expect(refused.headers()["set-cookie"]).toBeUndefined();

  await page.goto("/sign-in?error=identity.oauth.state-invalid");
  await expect(page.getByRole("alert")).toContainText(
    "That sign-in took too long or was already used",
  );
});

test("a provider subject whose email belongs to a password account is refused with no merge, and says so", async ({
  page,
}) => {
  await english(page);
  const existing = realUsers.guest.email;
  const before = linksOf(existing);
  await continueWithGoogle(page);
  await signInAtProvider(page, `real-e2e-${randomUUID()}`, {
    email: existing,
    email_verified: true,
  });
  await expect(page).toHaveURL(/error=identity\.oauth\.email-in-use/);
  await expect(page.getByRole("alert")).toContainText(
    "An account with that email already exists",
  );
  expect(linksOf(existing)).toBe(before);
});

test("an email the provider has not verified creates no account", async ({
  page,
}) => {
  await english(page);
  const person = newPerson();
  await continueWithGoogle(page);
  await signInAtProvider(page, person.subject, {
    email: person.email,
    email_verified: false,
  });
  await expect(page).toHaveURL(/error=identity\.oauth\.email-unverified/);
  await expect(page.getByRole("alert")).toContainText(
    "The provider hasn't verified that email yet",
  );
  expect(usersWith(person.email)).toBe("0");
});
