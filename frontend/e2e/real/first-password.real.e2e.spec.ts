import { randomUUID } from "node:crypto";
import { expect, test } from "@playwright/test";
import { signInAtProvider, sqlValue } from "./support/oidc-provider";
import {
  apiRead,
  apiWrite,
  findWorkspace,
  realUsers,
  signIn,
} from "./support/real-session";

interface Summary {
  readonly hasPasswordCredential: boolean;
  readonly linkedProviders: readonly string[];
}

// Not the people the other step-up specs use: step-up calls share a per-person limit of ten a minute.
const user = realUsers.workspaceGuest;
const ofUser = `(select id from identity.users where email = '${user.email}')`;
const newPassword = `First-${randomUUID()}`;
let seededHash = "";

// The seeded person gets their password and provider rows back, whatever the run did.
test.afterEach(() => {
  if (!seededHash) return;
  sqlValue(
    `delete from identity.oauth_accounts where user_id = ${ofUser}; update identity.users set has_password_credential = true, password_hash = '${seededHash}' where email = '${user.email}'`,
  );
});

test("a provider-only person sets a first password through the provider, the password then signs in, and a second set is refused", async ({
  page,
}) => {
  test.setTimeout(180_000);
  const subject = `real-e2e-${randomUUID()}`;
  await signIn(page, user);
  const workspaceId = (await findWorkspace(page, "Workspace 3")).id;
  seededHash = sqlValue(
    `select password_hash from identity.users where email = '${user.email}'`,
  );
  sqlValue(
    `update identity.users set has_password_credential = false where email = '${user.email}'; insert into identity.oauth_accounts (id, user_id, provider, provider_id, raw_profile) values ('${randomUUID()}', ${ofUser}, 'Google', '${subject}', '{}')`,
  );
  const security = `/workspaces/${workspaceId}/account/security`;
  await page.goto(security);
  const before = await apiRead<Summary>(page, "/auth/security/summary");
  expect(before.hasPasswordCredential).toBe(false);

  // No proof, no password: the producer refuses a bare session.
  const bare = await apiWrite(page, "POST", "/auth/set-password", {
    newPassword,
    stepUpToken: "",
  });
  expect(bare.status).toBe(412);
  // The write rotated the shared CSRF token under the open page: a fresh page reads the new one.
  await page.goto(security);

  // The page offers Set a password, not Change; the proof is the provider.
  await expect(
    page.getByRole("button", { name: "Change password" }),
  ).toHaveCount(0);
  await page.getByRole("button", { name: "Set a password" }).click();
  const prompt = page.getByRole("region", {
    name: "Confirm to set a password",
  });
  await expect(prompt).toBeVisible();
  await prompt.getByRole("button", { name: "Confirm with Google" }).click();
  await signInAtProvider(page, subject, {
    email: user.email,
    email_verified: true,
  });
  await expect(page).toHaveURL(new RegExp(`${security}$`));

  // Back from the provider the form opens; a short password is told before anything is sent.
  const form = page.getByRole("region", { name: "Set a password" });
  await expect(form).toBeVisible();
  await form.getByLabel("New password", { exact: true }).fill("short");
  await form.getByLabel("Repeat the new password").fill("short");
  await form.getByRole("button", { name: "Set password" }).click();
  await expect(form.getByRole("alert")).toHaveText(
    "Use at least 8 characters.",
  );
  await form.getByLabel("New password", { exact: true }).fill(newPassword);
  await form.getByLabel("Repeat the new password").fill(newPassword);
  await form.getByRole("button", { name: "Set password" }).click();
  await expect(
    page.getByText(
      "Password set. You can now sign in with your email and password.",
    ),
  ).toBeVisible();
  await expect(
    page.getByRole("button", { name: "Change password" }),
  ).toBeVisible();
  const after = await apiRead<Summary>(page, "/auth/security/summary");
  expect(after.hasPasswordCredential).toBe(true);

  // A person who has a password changes it instead: a second set is refused.
  const again = await apiWrite(page, "POST", "/auth/set-password", {
    newPassword: `${newPassword}-2`,
    stepUpToken: randomUUID(),
  });
  expect(again.status).toBe(409);

  // The new password signs in.
  await page.context().clearCookies();
  await signIn(page, { ...user, password: newPassword });
});
