import { expect, test } from "@playwright/test";
import { CONFIRMATION_LINK, devMails } from "./support/dev-mail";
import { freshMember, registerPerson } from "./support/fresh-person";
import { sqlValue } from "./support/oidc-provider";
import { apiRead } from "./support/real-session";
import { uniqueEmail } from "./support/real-workspace";

/**
 * Changing the sign-in email (UC-IDN-K09) against the real backend. The people are new, so the
 * step-up limit (ten a minute per person) is theirs alone. The mail sink holds the links.
 */
const pendingOf = (email: string) =>
  sqlValue(
    `select coalesce(pending_email, '-') from identity.users where normalized_email = '${email.toLowerCase()}'`,
  );

test("a person asks for a new sign-in email, the old one keeps working until the new inbox's link is opened, then every device ends and only the new one signs in", async ({
  page,
  browser,
}) => {
  test.setTimeout(180_000);
  const member = await freshMember(page, "k09");
  const security = `/workspaces/${member.workspaceId}/account/security`;
  const newEmail = uniqueEmail("k09new");

  // Somebody else already holds this address: it must answer exactly like a free one.
  const other = await browser.newPage();
  const taken = await registerPerson(other, "k09taken");
  await other.close();

  await page.goto(security);
  const ask = async (address: string) => {
    await page.getByRole("button", { name: "Change email" }).click();
    const proof = page.getByRole("region", {
      name: "Confirm to change your sign-in email",
    });
    await proof.getByLabel("Current password").fill(member.password);
    await proof.getByRole("button", { name: "Confirm", exact: true }).click();
    const form = page.getByRole("region", {
      name: "Change your sign-in email",
    });
    await form.getByLabel("New email").fill(address);
    await form.getByRole("button", { name: "Send link" }).click();
    const sent = page.getByRole("region", { name: "Check the new inbox" });
    await expect(sent).toContainText(address);
    return (await sent.innerText()).replace(address, "<address>");
  };

  // A taken address: the same words as a free one, and nothing is parked.
  const takenAnswer = await ask(taken.email);
  expect(pendingOf(member.email)).toBe("-");
  await page.getByRole("button", { name: "Done" }).click();

  const freeAnswer = await ask(newEmail);
  expect(freeAnswer).toBe(takenAnswer);
  expect(pendingOf(member.email)).toBe(newEmail.toLowerCase());
  await page.getByRole("button", { name: "Done" }).click();

  // The old address was told, and keeps being the sign-in email until the link is opened.
  await devMails(member.email, "(Your current address keeps working)");
  const before = await apiRead<{ email: string }>(page, "/auth/me");
  expect(before.email.toLowerCase()).toBe(member.email.toLowerCase());

  // The link in the new inbox finishes it; the link is single use.
  const link = (await devMails(newEmail, CONFIRMATION_LINK)).at(-1)!;
  const fresh = await browser.newPage();
  await fresh.goto(new URL(link).pathname + new URL(link).hash);
  await expect(
    fresh.getByRole("heading", { name: "Your email is confirmed" }),
  ).toBeVisible();
  await fresh.goto(new URL(link).pathname + new URL(link).hash);
  await fresh.reload();
  await expect(
    fresh.getByRole("heading", { name: "This link no longer works" }),
  ).toBeVisible();
  await fresh.close();

  // The old address was told it changed; every session ended; the id stayed.
  await devMails(member.email, "(Sign in again with the new address)");
  expect(pendingOf(newEmail)).toBe("-");
  const ended = await page.request.get(
    `${process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1"}/auth/me`,
  );
  expect(ended.status()).toBe(401);

  // Only the new address signs in.
  await page.context().clearCookies();
  const signIn = async (email: string) => {
    await page.goto("/sign-in");
    await page.waitForLoadState("networkidle");
    await page.getByLabel("Email").fill(email);
    await page.getByRole("button", { name: "Continue", exact: true }).click();
    await page.locator("#auth-password").fill(member.password);
    await page.getByRole("button", { name: "Sign in", exact: true }).click();
  };
  await signIn(member.email);
  await expect(page).toHaveURL(/\/sign-in/);
  await page.waitForTimeout(3_000);
  await expect(page).toHaveURL(/\/sign-in/);
  await signIn(newEmail);
  await expect(page).toHaveURL(/\/(home|workspaces\/[^/]+)/, {
    timeout: 20_000,
  });
});
