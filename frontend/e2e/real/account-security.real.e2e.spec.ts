import { randomUUID } from "node:crypto";
import { expect, test } from "@playwright/test";
import { freshMember } from "./support/fresh-person";
import { apiRead, apiWrite } from "./support/real-session";

interface Summary {
  hasPasswordCredential: boolean;
  activeSessionCount: number;
  mfa: { isEnabled: boolean };
  linkedProviders: string[];
}
interface Activity {
  items: { type: string; userAgent: string | null }[];
}

test.setTimeout(120_000);
test("the security page shows the person's own summary and activity, and a password change ends every device", async ({
  page,
  browser,
}) => {
  const member = await freshMember(page, "security");
  const otherDevice = await browser.newContext();
  const deviceB = await otherDevice.newPage();
  const signedInB = await apiWrite(deviceB, "POST", "/auth/login", {
    email: member.email,
    password: member.password,
  });
  expect(signedInB.status).toBe(200);

  // Summary: two sessions, a password, no factor, and no secret in the answer.
  const summary = await apiRead<Summary>(page, "/auth/security/summary");
  expect(summary.hasPasswordCredential).toBe(true);
  expect(summary.activeSessionCount).toBeGreaterThanOrEqual(2);
  expect(summary.mfa.isEnabled).toBe(false);
  expect(JSON.stringify(summary)).not.toMatch(/hash|secret|token/i);

  // Activity: the page lists the sign-ins of this person only.
  await page.goto(`/workspaces/${member.workspaceId}/account/security`);
  await expect(
    page.getByRole("heading", { name: "Recent security activity" }),
  ).toBeVisible();
  await expect(
    page.getByText("Signed in", { exact: true }).first(),
  ).toBeVisible();
  const before = await apiRead<Activity>(page, "/auth/security/activity");
  expect(before.items.some((row) => row.type === "SignInSucceeded")).toBe(true);

  // Change password: the same password and a wrong current password are refused.
  await page.getByRole("button", { name: "Change password" }).first().click();
  const flow = page.getByRole("region", { name: "Change your password" });
  await flow.getByLabel("Current password").fill(member.password);
  await flow.getByLabel("New password", { exact: true }).fill(member.password);
  await flow.getByLabel("Repeat the new password").fill(member.password);
  await flow.getByRole("button", { name: "Change password" }).click();
  await expect(flow).toContainText("Choose a password different");
  const next = `Pw-${randomUUID()}`;
  await flow.getByLabel("Current password").fill("not-the-password");
  await flow.getByLabel("New password", { exact: true }).fill(next);
  await flow.getByLabel("Repeat the new password").fill(next);
  await flow.getByRole("button", { name: "Change password" }).click();
  await expect(flow).toContainText("That password is not right.");

  // Success: signed out everywhere, the old password stops working, the new one signs in.
  await flow.getByLabel("Current password").fill(member.password);
  await flow.getByLabel("New password", { exact: true }).fill(next);
  await flow.getByLabel("Repeat the new password").fill(next);
  await flow.getByRole("button", { name: "Change password" }).click();
  await expect(
    page.getByRole("region", { name: "Password changed" }),
  ).toBeVisible();
  const deviceBRead = await deviceB.request.get(
    `${process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1"}/auth/me`,
  );
  expect(deviceBRead.status()).toBe(401);
  const old = await apiWrite(deviceB, "POST", "/auth/login", {
    email: member.email,
    password: member.password,
  });
  expect(old.status).not.toBe(200);
  const fresh = await apiWrite(deviceB, "POST", "/auth/login", {
    email: member.email,
    password: next,
  });
  expect(fresh.status).toBe(200);
  const after = await apiRead<Activity>(deviceB, "/auth/security/activity");
  expect(after.items.some((row) => row.type === "PasswordChanged")).toBe(true);
  await otherDevice.close();
});
