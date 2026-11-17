import { expect, request, test } from "@playwright/test";
import { freshMember } from "./support/fresh-person";
import { apiRead } from "./support/real-session";

/**
 * API tokens against the real backend (UC-IDN-L01, L02, L03, L04): an owner issues a token behind
 * a password proof, the secret shows once, the list keeps only its last four characters, a call
 * with the token is answered 200, and after the revoke the same call is answered 401.
 */
const apiUrl = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";

test("an owner issues a token, calls the API with it, revokes it, and the same call is refused", async ({
  page,
  context,
}) => {
  test.setTimeout(180_000);
  await context.grantPermissions(["clipboard-read", "clipboard-write"]);
  const owner = await freshMember(page, "l01");
  await page.goto(`/workspaces/${owner.workspaceId}/account/tokens`);
  await expect(
    page.getByRole("heading", { level: 2, name: "API tokens" }),
  ).toBeVisible();
  await expect(page.getByText("You have no tokens yet")).toBeVisible();

  // Issue: a name, a life, the proof, then the secret once.
  await page.getByRole("button", { name: "New token" }).first().click();
  const form = page.getByRole("dialog", { name: "New API token" });
  await form.getByLabel("Token name").fill("CI deploy");
  await form.getByRole("button", { name: "Create token" }).click();
  const proof = page.getByRole("dialog", { name: "Confirm it's you" });
  await proof.getByLabel("Current password").fill(owner.password);
  await proof.getByRole("button", { name: "Confirm", exact: true }).click();
  const reveal = page.getByRole("dialog", { name: "Copy your token" });
  const secret = await reveal.getByTestId("token-secret").innerText();
  expect(secret).toMatch(/^ntk_v1\./);
  await reveal.getByRole("button", { name: "Copy and close" }).click();
  await expect(reveal).toBeHidden();
  expect(await page.evaluate(() => navigator.clipboard.readText())).toBe(
    secret,
  );

  // The list holds the last four characters and never the secret.
  const row = page.getByRole("row").filter({ hasText: "CI deploy" });
  await expect(row).toContainText(`••••••${secret.slice(-4)}`);
  await expect(page.locator("body")).not.toContainText(secret);
  const listed = await apiRead<{ items: { suffix: string | null }[] }>(
    page,
    `/workspaces/${owner.workspaceId}/api-tokens`,
  );
  expect(listed.items.map((item) => item.suffix)).toEqual([secret.slice(-4)]);
  expect(JSON.stringify(listed)).not.toContain(secret);

  // L04: a client with no session, holding only the token, is let in.
  const client = await request.newContext();
  const call = () =>
    client.get(`${apiUrl}/workspaces/${owner.workspaceId}`, {
      headers: { Authorization: `Bearer ${secret}` },
    });
  expect((await call()).status()).toBe(200);

  // Revoke: it asks first, and the next call is refused at once.
  await page.getByRole("button", { name: "Revoke CI deploy" }).click();
  await page
    .getByRole("alertdialog", { name: "Revoke this token?" })
    .getByRole("button", { name: "Revoke token" })
    .click();
  await expect(row).toContainText("Revoked");
  expect((await call()).status()).toBe(401);
  await client.dispose();
});
