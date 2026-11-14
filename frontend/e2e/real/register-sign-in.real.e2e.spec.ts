import { randomUUID } from "node:crypto";
import { expect, test, type Page } from "@playwright/test";
import { CONFIRMATION_LINK, devMails } from "./support/dev-mail";
import { apiRead } from "./support/real-session";

/**
 * Registering and signing in against the real backend. Each person is new, so the sign-in lock and the
 * per-address counters of one run never touch another. Passwords are made per run.
 */

const NOTICE_BODY = "(account already exists)";

async function english(page: Page) {
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "en");
    } catch {
      // Private mode: defaults apply.
    }
  });
}

async function register(
  page: Page,
  person: { email: string; password: string },
) {
  await page.goto("/sign-up");
  await page.waitForLoadState("networkidle");
  await page.getByLabel("First name").fill("Casey");
  await page.getByLabel("Last name").fill("Register");
  await page.getByLabel("Work email").fill(person.email);
  await page.locator("#auth-register-password").fill(person.password);
  await page.getByLabel(/I agree to the/).check();
  await page.getByRole("button", { name: "Create account" }).click();
  await expect(
    page.getByRole("heading", { level: 1, name: "Check your email" }),
  ).toBeVisible();
  return (await page.getByRole("status").innerText()).trim();
}

async function signInThroughThePage(
  page: Page,
  email: string,
  password: string,
) {
  await page.goto("/sign-in");
  await page.waitForLoadState("networkidle");
  await page.getByLabel("Email").fill(email);
  await page.getByRole("button", { name: "Continue", exact: true }).click();
  await page.locator("#auth-password").fill(password);
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
}

const SIGNED_IN = /\/(workspaces\/[^/]+|onboarding)/;

test("a new person registers, sees the neutral step, confirms the email and signs in; registering the same address again changes nothing and says the same", async ({
  page,
}) => {
  // The mail log is read from the backend container after the outbox delivers: allow for it.
  test.setTimeout(90_000);
  await english(page);
  const person = {
    email: `register-${randomUUID().slice(0, 8)}@example.com`,
    password: `Pw-${randomUUID()}`,
  };

  const firstAnswer = await register(page, person);
  // Registering opens no session.
  const noSession = await page.request.get(
    `${process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1"}/auth/me`,
  );
  expect(noSession.status()).toBe(401);

  // The mail carries the link; opening it confirms the address.
  const link = (await devMails(person.email, CONFIRMATION_LINK)).at(-1)!;
  await page.goto(new URL(link).pathname + new URL(link).hash);
  await expect(
    page.getByRole("heading", { name: "Your email is confirmed" }),
  ).toBeVisible();

  await signInThroughThePage(page, person.email, person.password);
  await expect(page).toHaveURL(SIGNED_IN, { timeout: 20_000 });
  const me = await apiRead<{ email: string; emailConfirmed: boolean }>(
    page,
    "/auth/me",
  );
  expect(me.email).toBe(person.email);
  expect(me.emailConfirmed).toBe(true);

  // The registration fact made exactly one personal workspace and sign-in lands inside it (not on onboarding).
  const mine = async () =>
    (await apiRead<readonly { id: string }[]>(page, "/workspaces")).map(
      (workspace) => workspace.id,
    );
  await expect.poll(async () => (await mine()).length).toBe(1);
  const [personal] = await mine();
  await expect(page).toHaveURL(new RegExp(`/workspaces/${personal}`));

  // The same address again, with another password, in a signed-out browser.
  await page.context().clearCookies();
  const intruder = `Pw-${randomUUID()}`;
  const secondAnswer = await register(page, {
    email: person.email,
    password: intruder,
  });
  // Nothing in the page tells an existing address from a new one.
  expect(secondAnswer).toBe(firstAnswer);
  const stillNoSession = await page.request.get(
    `${process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1"}/auth/me`,
  );
  expect(stillNoSession.status()).toBe(401);

  // The owner is told by mail that someone tried.
  await devMails(person.email, NOTICE_BODY);

  // The account is untouched: the second password does not open it, the first still does.
  await signInThroughThePage(page, person.email, intruder);
  await expect(page.getByRole("alert")).toBeVisible();
  await expect(page).toHaveURL(/\/sign-in/);
  await signInThroughThePage(page, person.email, person.password);
  await expect(page).toHaveURL(SIGNED_IN, { timeout: 20_000 });
  // Registering twice and signing in twice made no second workspace.
  expect(await mine()).toEqual([personal]);
});

test("sign-in: a wrong password and an unknown address are refused with the same words, and the right password opens the app", async ({
  page,
}) => {
  await english(page);
  const person = {
    email: `signin-${randomUUID().slice(0, 8)}@example.com`,
    password: `Pw-${randomUUID()}`,
  };
  await register(page, person);

  const refusal = async (email: string, password: string) => {
    await signInThroughThePage(page, email, password);
    const alert = page.getByRole("alert");
    await expect(alert).toBeVisible();
    await expect(page).toHaveURL(/\/sign-in/);
    return (await alert.innerText()).trim();
  };
  const wrongPassword = await refusal(person.email, `Pw-${randomUUID()}`);
  const unknownAddress = await refusal(
    `nobody-${randomUUID().slice(0, 8)}@example.com`,
    `Pw-${randomUUID()}`,
  );
  expect(unknownAddress).toBe(wrongPassword);

  await signInThroughThePage(page, person.email, person.password);
  await expect(page).toHaveURL(SIGNED_IN, { timeout: 20_000 });
  // An unconfirmed address still signs in; the page's session reads as the person.
  const me = await apiRead<{ email: string }>(page, "/auth/me");
  expect(me.email).toBe(person.email);
});
