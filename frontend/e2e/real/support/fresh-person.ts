import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";
import { expect, type Page } from "@playwright/test";
import { CONFIRMATION_LINK, devMails, latestMails } from "./dev-mail";
import { apiWrite } from "./real-session";
import { createOwnedWorkspace, uniqueEmail } from "./real-workspace";
import { totpCode } from "./totp";

export interface FreshPerson {
  readonly email: string;
  readonly password: string;
}

/**
 * A new person registered through the API and signed in: registering answers
 * the same neutral 200 for any address and opens no session, so the sign-in is
 * a second call. The page's session is theirs.
 */
export async function registerPerson(
  page: Page,
  label: string,
): Promise<FreshPerson> {
  const email = uniqueEmail(label);
  const password = `Pw-${randomUUID()}`;
  const registered = await apiWrite(page, "POST", "/auth/register", {
    email,
    password,
    name: `Casey ${label}`,
  });
  expect(registered.status, "register").toBeLessThan(300);
  const session = await apiWrite(page, "POST", "/auth/login", {
    email,
    password,
  });
  expect(session.status, "sign in the new person").toBeLessThan(300);
  return { email, password };
}

/** Opens the confirmation link the development backend logged for the address. */
export async function confirmEmailOf(page: Page, email: string, since = "20m") {
  const link = (await devMails(email, CONFIRMATION_LINK, 0, since)).at(-1)!;
  const token = decodeURIComponent(new URL(link).hash.replace("#token=", ""));
  const confirmed = await apiWrite(
    page,
    "POST",
    "/auth/email-verification/confirm",
    { token },
  );
  expect(confirmed.status, "confirm email").toBeLessThan(300);
}

/**
 * The development backend no longer logs the recipient of a message (code scanning removed it), so
 * the address cannot be matched in the log. The harness confirms the address in the stack's own
 * database instead; the emailed-link path is the job of the email-confirmation specs.
 */
export function confirmEmailInDatabase(email: string) {
  const normalized = email.toLowerCase().replace(/'/g, "''");
  execFileSync("docker", [
    "exec",
    "notrelix-fe-real-e2e-postgres",
    "psql",
    "-U",
    "postgres",
    "-d",
    "notrelix_real_e2e",
    "-v",
    "ON_ERROR_STOP=1",
    "-c",
    `UPDATE identity.users SET email_confirmed = true, email_confirmed_at = now() WHERE normalized_email = '${normalized}'`,
  ]);
}

/** Confirms many registered people with one read of the backend log. */
export async function confirmEmailsOf(
  people: readonly { page: Page; email: string }[],
  since = "10m",
) {
  const links = await latestMails(
    people.map((one) => one.email),
    CONFIRMATION_LINK,
    since,
  );
  for (const { page, email } of people) {
    const token = decodeURIComponent(
      new URL(links.get(email)!).hash.replace("#token=", ""),
    );
    const confirmed = await apiWrite(
      page,
      "POST",
      "/auth/email-verification/confirm",
      { token },
    );
    expect(confirmed.status, `confirm ${email}`).toBeLessThan(300);
  }
}

/**
 * A member nobody else uses: registered, confirmed and owning a workspace, so
 * the account pages open. The page stays signed in as them.
 */
export async function freshMember(page: Page, label: string) {
  const person = await registerPerson(page, label);
  confirmEmailInDatabase(person.email);
  const workspace = await createOwnedWorkspace(page, label);
  return { ...person, workspaceId: workspace.id };
}

/**
 * Turns two-factor on through the security page (the password comes first) and
 * returns the authenticator key and the backup codes shown once.
 */
export async function turnOnTwoFactor(
  page: Page,
  workspaceId: string,
  password: string,
) {
  await page.goto(`/workspaces/${workspaceId}/account/security`);
  await page
    .getByRole("button", { name: "Turn on two-factor authentication" })
    .click();
  const proof = page.getByRole("region", {
    name: "Confirm to turn on two-factor authentication",
  });
  await proof.getByLabel("Current password").fill(password);
  await proof.getByRole("button", { name: "Confirm", exact: true }).click();
  const setup = page.getByRole("region", {
    name: "Set up the authenticator app",
  });
  await expect(setup).toBeVisible();
  const secret = /[A-Z2-7]{16,}/.exec(await setup.innerText())?.[0];
  expect(secret, "the authenticator key is shown").toBeDefined();
  await setup.getByRole("textbox", { name: "Code" }).fill(totpCode(secret!));
  await setup.getByRole("button", { name: "Confirm", exact: true }).click();
  const codesRegion = page.getByLabel("Save your backup codes");
  await expect(codesRegion).toBeVisible();
  const codes = (await codesRegion.innerText()).match(
    /\b[A-Z0-9]{4}(?:-[A-Z0-9]{4}){4}\b/g,
  );
  expect(codes?.length, "backup codes are shown").toBeGreaterThan(0);
  await page.getByRole("button", { name: "I saved them" }).click();
  return { secret: secret!, backupCodes: codes! };
}
