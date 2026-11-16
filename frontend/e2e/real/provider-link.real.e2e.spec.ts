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
interface Activity {
  readonly items: readonly { readonly type: string }[];
}

// Not the people the two-factor and provider step-up specs use: step-up calls share a per-person limit of ten a minute.
const user = realUsers.workspaceAdmin;
const other = realUsers.guest;
const ofUser = (email: string) =>
  `(select id from identity.users where email = '${email}')`;
const summaryOf = (page: Parameters<typeof apiRead>[0]) =>
  apiRead<Summary>(page, "/auth/security/summary");
const activityTypes = async (page: Parameters<typeof apiRead>[0]) =>
  (await apiRead<Activity>(page, "/auth/security/activity")).items.map(
    (item) => item.type,
  );

// The seeded people get their provider rows back to none, whatever the run did.
test.afterEach(() => {
  sqlValue(
    `delete from identity.oauth_accounts where user_id in (${ofUser(user.email)}, ${ofUser(other.email)}, ${ofUser(realUsers.admin.email)}); update identity.users set has_password_credential = true where email in ('${user.email}', '${other.email}', '${realUsers.admin.email}')`,
  );
});

test("connecting a provider asks for a proof, refuses an identity another person owns and links a free one", async ({
  page,
}) => {
  test.setTimeout(180_000);
  const taken = `real-e2e-taken-${randomUUID()}`;
  const free = `real-e2e-free-${randomUUID()}`;
  await signIn(page, user);
  const workspaceId = (await findWorkspace(page, "Workspace 3")).id;
  const security = `/workspaces/${workspaceId}/account/security`;
  // Another person already owns this Google identity.
  sqlValue(
    `insert into identity.oauth_accounts (id, user_id, provider, provider_id, raw_profile) values ('${randomUUID()}', ${ofUser(other.email)}, 'Google', '${taken}', '{}')`,
  );
  await page.goto(security);
  expect((await summaryOf(page)).linkedProviders).toEqual([]);

  // Rejection: a wrong password is refused in words and nothing leaves for the provider.
  await page.getByRole("button", { name: "Connect Google" }).click();
  const prompt = page.getByRole("region", {
    name: "Confirm to connect Google",
  });
  await expect(prompt).toBeVisible();
  await prompt.getByLabel("Current password").fill("not-the-password");
  await prompt.getByRole("button", { name: "Confirm", exact: true }).click();
  await expect(prompt.getByRole("alert")).toHaveText(
    "That password is not right.",
  );

  // Collision: the provider confirms the person, but the identity is another person's: no link, no name.
  await prompt.getByLabel("Current password").fill(user.password);
  await prompt.getByRole("button", { name: "Confirm", exact: true }).click();
  await signInAtProvider(page, taken, {
    email: "taken@example.test",
    email_verified: true,
  });
  await expect(page).toHaveURL(new RegExp(`${security}$`));
  await expect(
    page.getByText("already used with another Notrelix account"),
  ).toBeVisible();
  await expect(page.getByText(other.email)).toHaveCount(0);
  expect((await summaryOf(page)).linkedProviders).toEqual([]);

  // A free identity links; the summary lists it and the activity says so.
  await page.getByRole("button", { name: "Connect Google" }).click();
  const again = page.getByRole("region", { name: "Confirm to connect Google" });
  await again.getByLabel("Current password").fill(user.password);
  await again.getByRole("button", { name: "Confirm", exact: true }).click();
  await signInAtProvider(page, free, {
    email: "free@example.test",
    email_verified: true,
  });
  await expect(page).toHaveURL(new RegExp(`${security}$`));
  await expect(page.getByText("Google is connected.")).toBeVisible();
  const linked = await summaryOf(page);
  expect(linked.linkedProviders).toEqual(["Google"]);
  expect(linked.hasPasswordCredential).toBe(true);
  expect(await activityTypes(page)).toContain("ProviderLinked");
});

// Step-up calls share a per-person limit of ten a minute: disconnecting is another person's walk.
test("disconnecting asks for a proof, a proof never issued unlinks nothing, and the password still signs in", async ({
  page,
}) => {
  test.setTimeout(120_000);
  const person = realUsers.admin;
  await signIn(page, person);
  const workspaceId = (await findWorkspace(page, "Workspace 2")).id;
  const security = `/workspaces/${workspaceId}/account/security`;
  sqlValue(
    `insert into identity.oauth_accounts (id, user_id, provider, provider_id, raw_profile) values ('${randomUUID()}', ${ofUser(person.email)}, 'Google', 'real-e2e-linked-${randomUUID()}', '{}')`,
  );
  await page.goto(security);
  expect((await summaryOf(page)).linkedProviders).toEqual(["Google"]);

  // A proof that was never issued unlinks nothing.
  const refused = await apiWrite(page, "POST", "/auth/oauth/google/unlink", {
    stepUpToken: randomUUID(),
  });
  expect(refused.status).toBeGreaterThanOrEqual(400);
  expect((await summaryOf(page)).linkedProviders).toEqual(["Google"]);
  // The write above rotated the shared CSRF token under the open page: a fresh page reads the new one.
  await page.goto(security);

  // Disconnecting asks again; the password still signs in.
  await page.getByRole("button", { name: "Disconnect Google" }).click();
  const off = page.getByRole("region", {
    name: "Confirm to disconnect Google",
  });
  await expect(off).toBeVisible();
  await off.getByLabel("Current password").fill(person.password);
  await off.getByRole("button", { name: "Confirm", exact: true }).click();
  await expect(page.getByText("Google is disconnected.")).toBeVisible();
  const unlinked = await summaryOf(page);
  expect(unlinked.linkedProviders).toEqual([]);
  expect(unlinked.hasPasswordCredential).toBe(true);
  expect(await activityTypes(page)).toContain("ProviderUnlinked");
});

test("the only way to sign in cannot be disconnected", async ({ page }) => {
  const subject = `real-e2e-only-${randomUUID()}`;
  await signIn(page, user);
  const workspaceId = (await findWorkspace(page, "Workspace 3")).id;
  sqlValue(
    `update identity.users set has_password_credential = false where email = '${user.email}'; insert into identity.oauth_accounts (id, user_id, provider, provider_id, raw_profile) values ('${randomUUID()}', ${ofUser(user.email)}, 'Google', '${subject}', '{}')`,
  );
  await page.goto(`/workspaces/${workspaceId}/account/security`);
  await expect(
    page.getByText("This is your only way to sign in"),
  ).toBeVisible();
  await expect(
    page.getByRole("button", { name: "Disconnect Google" }),
  ).toHaveCount(0);
  expect((await summaryOf(page)).linkedProviders).toEqual(["Google"]);
});
