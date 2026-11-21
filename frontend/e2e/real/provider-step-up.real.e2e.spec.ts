import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";
import { expect, test } from "@playwright/test";
import { signInAtProvider } from "./support/oidc-provider";
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
  readonly mfa: { readonly isEnabled: boolean };
}

/** The linked Google account is seeded straight into the development database; the provider test double signs the person in again. */
function sql(statement: string) {
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
    statement,
  ]);
}

// Not the person the two-factor spec uses: step-up calls share a per-person limit of ten a minute.
const user = realUsers.guest;
const ofUser = `(select id from identity.users where email = '${user.email}')`;

// The seeded person gets their password flag back, whatever the run did.
test.afterEach(() => {
  sql(
    `delete from identity.oauth_accounts where user_id = ${ofUser}; update identity.users set has_password_credential = true where email = '${user.email}'`,
  );
});

test("a person with only a provider is offered the provider, not a password; the wrong provider person is refused with a message, the right one is confirmed and the proof is spent once", async ({
  page,
}) => {
  const subject = `real-e2e-${randomUUID()}`;
  await signIn(page, user);
  // The landing page depends on which specs ran before; the seeded workspace does not (a Guest may not create one).
  const workspaceId = (await findWorkspace(page, "Workspace 3")).id;
  sql(
    `update identity.users set has_password_credential = false where email = '${user.email}'; insert into identity.oauth_accounts (id, user_id, provider, provider_id, raw_profile) values ('${randomUUID()}', ${ofUser}, 'Google', '${subject}', '{}')`,
  );
  const security = `/workspaces/${workspaceId}/account/security`;
  await page.goto(security);

  const summary = await apiRead<Summary>(page, "/auth/security/summary");
  expect(summary.hasPasswordCredential).toBe(false);
  expect(summary.linkedProviders).toEqual(["Google"]);
  expect(summary.mfa.isEnabled).toBe(false);

  // The server answers the provider factor: no password, no code.
  const requirement = await apiWrite<{ requiredFactor: string }>(
    page,
    "POST",
    "/auth/security/step-up/requirement",
    { purpose: "EnrollMfa" },
  );
  expect(requirement.status, JSON.stringify(requirement.data)).toBe(200);
  expect(requirement.data?.requiredFactor).toBe("OAuth");

  await page
    .getByRole("button", { name: "Turn on two-factor authentication" })
    .click();
  const prompt = page.getByRole("region", {
    name: "Confirm to turn on two-factor authentication",
  });
  await expect(prompt).toBeVisible();
  await expect(page.locator('input[type="password"]')).toHaveCount(0);
  await expect(prompt.getByRole("textbox")).toHaveCount(0);

  // Someone else at the provider: the subject differs from the linked one, so there is no proof.
  await prompt.getByRole("button", { name: "Confirm with Google" }).click();
  await signInAtProvider(page, `someone-else-${randomUUID()}`, {
    email: "other@example.test",
    email_verified: true,
  });
  await expect(page).toHaveURL(new RegExp(`${security}$`));
  await expect(page.getByRole("alert")).toContainText(
    "We could not confirm you with Google",
  );

  // The linked person at the provider: the proof is handed over once and the action goes on.
  await page
    .getByRole("button", { name: "Turn on two-factor authentication" })
    .click();
  await page
    .getByRole("region", {
      name: "Confirm to turn on two-factor authentication",
    })
    .getByRole("button", { name: "Confirm with Google" })
    .click();
  await signInAtProvider(page, subject, {
    email: user.email,
    email_verified: true,
  });
  await expect(page).toHaveURL(new RegExp(`${security}`));
  await expect(page.getByRole("alert")).toHaveCount(0);
  await expect(
    page.getByRole("region", {
      name: "Confirm to turn on two-factor authentication",
    }),
  ).toHaveCount(0);
  await expect(page.getByRole("textbox").first()).toBeVisible();

  // A proof is single use: whatever the page did with it, a second claim finds nothing.
  const second = await apiWrite(
    page,
    "POST",
    "/auth/security/step-up/claim-provider",
    {},
  );
  expect(second.status).toBe(400);
});
