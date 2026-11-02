import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";
import { expect, test } from "@playwright/test";
import { apiRead, apiWrite, realUsers, signIn } from "./support/real-session";

interface Summary {
  readonly hasPasswordCredential: boolean;
  readonly linkedProviders: readonly string[];
  readonly mfa: { readonly isEnabled: boolean };
}

/** No provider runs on a developer machine: the development database stands in for a sign-up with Google. */
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

test("a person with only a provider is offered the provider, not a password, and a refusal brings them back with a message", async ({
  page,
}) => {
  await signIn(page, user);
  const workspaceId = /\/workspaces\/([^/]+)/.exec(page.url())![1];
  sql(
    `update identity.users set has_password_credential = false where email = '${user.email}'; insert into identity.oauth_accounts (id, user_id, provider, provider_id, raw_profile) values ('${randomUUID()}', ${ofUser}, 'Google', 'real-e2e-${randomUUID()}', '{}')`,
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

  // The browser leaves for the backend; no provider is set up here, so it is sent straight back to
  // this page (not to a JSON body), the marker is read once and the page says what happened.
  await prompt.getByRole("button", { name: "Confirm with Google" }).click();
  await expect(page).toHaveURL(new RegExp(`${security}$`));
  await expect(page.getByRole("alert")).toContainText(
    "We could not confirm you with Google",
  );
  await expect(
    page.getByRole("button", { name: "Confirm with Google" }),
  ).toBeVisible();

  // Nothing was left to claim: the proof cookie only exists after a real provider login.
  const claim = await apiWrite(
    page,
    "POST",
    "/auth/security/step-up/claim-provider",
    {},
  );
  expect(claim.status).toBe(400);
  expect(
    (await apiRead<Summary>(page, "/auth/security/summary")).mfa.isEnabled,
  ).toBe(false);
});
