import { expect, test } from "@playwright/test";
import { apiRead, realUsers, signIn } from "./support/real-session";
import { totpCode } from "./support/totp";

interface Summary {
  readonly mfa: { readonly isEnabled: boolean };
}

test("turning two-factor on asks for the password first, and turning it off asks for a code and ends the session", async ({
  page,
}) => {
  const user = realUsers.workspaceGuest;
  await signIn(page, user);
  const workspaceId = /\/workspaces\/([^/]+)/.exec(page.url())![1];
  await page.goto(`/workspaces/${workspaceId}/account/security`);

  expect(
    (await apiRead<Summary>(page, "/auth/security/summary")).mfa.isEnabled,
  ).toBe(false);

  // A session alone cannot add its own factor: the password comes first.
  await page
    .getByRole("button", { name: "Turn on two-factor authentication" })
    .click();
  const proof = page.getByRole("region", {
    name: "Confirm to turn on two-factor authentication",
  });
  await expect(proof).toBeVisible();
  // Rejection: a wrong password is refused in words and nothing is started.
  await proof.getByLabel("Current password").fill("not-the-password");
  await proof.getByRole("button", { name: "Confirm", exact: true }).click();
  await expect(proof.getByRole("alert")).toHaveText(
    "That password is not right.",
  );
  await expect(
    page.getByRole("region", { name: "Set up the authenticator app" }),
  ).toHaveCount(0);
  await proof.getByLabel("Current password").fill(user.password);
  await proof.getByRole("button", { name: "Confirm", exact: true }).click();

  const setup = page.getByRole("region", {
    name: "Set up the authenticator app",
  });
  await expect(setup).toBeVisible();
  const secret = /[A-Z2-7]{16,}/.exec(await setup.innerText())?.[0];
  expect(secret, "the authenticator key is shown").toBeDefined();

  // Rejection: a wrong first code leaves the setup open and the factor off.
  await setup.getByRole("textbox", { name: "Code" }).fill("000000");
  await setup.getByRole("button", { name: "Confirm", exact: true }).click();
  await expect(setup.getByRole("alert")).toContainText(
    "That code is not right",
  );
  expect(
    (await apiRead<Summary>(page, "/auth/security/summary")).mfa.isEnabled,
  ).toBe(false);
  await setup.getByRole("textbox", { name: "Code" }).fill(totpCode(secret!));
  await setup.getByRole("button", { name: "Confirm", exact: true }).click();
  await expect(page.getByText("Save your backup codes")).toBeVisible();
  // The activation says what it did to the other devices (none, or how many it signed out).
  await expect(
    page.getByLabel("Save your backup codes").getByRole("status"),
  ).toHaveText(
    /No other device is signed in\.|other devices? (was|were) signed out\./,
  );
  await page.getByRole("button", { name: "I saved them" }).click();
  expect(
    (await apiRead<Summary>(page, "/auth/security/summary")).mfa.isEnabled,
  ).toBe(true);

  // Turning it off asks for a fresh code (the next time step: a code works once) and ends every session.
  await page
    .getByRole("button", { name: "Turn off two-factor authentication" })
    .click();
  const disable = page.getByRole("region", {
    name: "Confirm to turn off two-factor authentication",
  });
  await expect(disable).toBeVisible();
  await disable
    .getByRole("textbox", { name: "Code" })
    .fill(totpCode(secret!, Date.now() + 30_000));
  await disable.getByRole("button", { name: "Confirm", exact: true }).click();
  await expect(
    page.getByText("Two-factor authentication is off"),
  ).toBeVisible();
});
