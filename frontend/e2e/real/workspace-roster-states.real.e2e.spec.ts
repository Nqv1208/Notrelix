import AxeBuilder from "@axe-core/playwright";
import { expect, test } from "@playwright/test";
import { realUsers, signIn } from "./support/real-session";
import {
  createOwnedWorkspace,
  invitationsOf,
  joinAs,
  membersOf,
  uniqueEmail,
} from "./support/real-workspace";

test.setTimeout(180_000);

/**
 * UC-WSP-F02 and UC-WSP-F05 on the real stack, in the browser: a write that carries an out-of-date version
 * answers the backend's own 412 and the roster says so and keeps the old role; with the network gone nothing is
 * changed and the roster says that; the row menu works from the keyboard and axe finds nothing serious with it open.
 */
test("the roster reports a version conflict and an offline write, and its row menu works by keyboard", async ({
  browser,
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Roster states");
  const joined = await joinAs(
    browser,
    page,
    workspace.id,
    realUsers.workspaceAdmin,
    "Member",
  );
  const member = (await membersOf(page, workspace.id)).find(
    (row) => row.role === "Member",
  )!;

  await page.goto(`/workspaces/${workspace.id}/members`);
  const roleSelect = page.getByRole("combobox", {
    name: `Role for ${member.name}`,
  });
  await expect(roleSelect).toBeVisible();

  // Conflict: the write carries version 999999, which the row never reached; the backend answers 412 itself.
  const stale = async (route: import("@playwright/test").Route) => {
    const request = route.request();
    if (request.method() !== "PATCH") return route.continue();
    const body = JSON.parse(request.postData() ?? "{}") as object;
    return route.continue({
      postData: JSON.stringify({ ...body, expectedVersion: 999_999 }),
    });
  };
  await page.route(`**/workspaces/${workspace.id}/members/*`, stale);
  const refused = page.waitForResponse(
    (response) => response.request().method() === "PATCH",
  );
  await roleSelect.click();
  await page.getByRole("option", { name: /^Admin/ }).click();
  expect((await refused).status()).toBe(412);
  await expect(
    page.getByText(`${member.name}'s role wasn't changed.`),
  ).toBeVisible();
  await expect(
    page.getByText(/Someone else changed this just now/),
  ).toBeVisible();
  await page.unroute(`**/workspaces/${workspace.id}/members/*`, stale);
  await expect(roleSelect).toContainText("Member");
  expect(
    (await membersOf(page, workspace.id)).find(
      (row) => row.userId === member.userId,
    )?.role,
  ).toBe("Member");

  // Offline: the request never leaves the browser; nothing changes and the roster says the server was not reached.
  await page.context().setOffline(true);
  await roleSelect.click();
  await page.getByRole("option", { name: /^Admin/ }).click();
  await expect(
    page.getByText(/The server couldn't be reached/).first(),
  ).toBeVisible();
  await page.context().setOffline(false);
  await expect(roleSelect).toContainText("Member");

  // Keyboard and axe: the row menu opens from its button, moves with the arrows, closes with Escape and gives
  // focus back; with it open the page has no serious or critical axe violation.
  const menuButton = page.getByRole("button", {
    name: `Actions for ${member.name}`,
  });
  await menuButton.focus();
  await page.keyboard.press("Enter");
  await expect(page.getByRole("menuitem").first()).toBeVisible();
  await page.waitForTimeout(500);
  const open = await new AxeBuilder({ page }).analyze();
  expect(
    open.violations
      .filter((violation) =>
        ["serious", "critical"].includes(violation.impact ?? ""),
      )
      .map((violation) => violation.id),
  ).toEqual([]);
  await page.keyboard.press("ArrowDown");
  await page.keyboard.press("Escape");
  await expect(page.getByRole("menuitem")).toHaveCount(0);
  await expect(menuButton).toBeFocused();

  await joined.context.close();
});

/**
 * UC-WSP-G04, G05 and G06 in the browser on the real stack: a role change that carries an out-of-date version
 * answers the backend's 412 and the list says so; with the network gone a resend and a revoke change nothing and
 * say so; the row's controls reach and run from the keyboard, and axe finds nothing serious on the list.
 */
test("the invitation rows report a version conflict and an offline write, and work by keyboard", async ({
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Invitation states");
  const email = uniqueEmail("states");
  await page.goto(`/workspaces/${workspace.id}/members?tab=invitations`);
  await page.getByLabel("E-mail address").fill(email);
  await page.getByRole("button", { name: "Send invitation" }).click();
  await expect(page.getByText(`Invitation sent to ${email}.`)).toBeVisible();
  const row = page.locator("tbody tr", { hasText: email });
  await expect(row).toBeVisible();

  // G04 conflict: the write carries version 999999, which the invitation never reached.
  const stale = async (route: import("@playwright/test").Route) => {
    const request = route.request();
    if (request.method() !== "PATCH") return route.continue();
    const body = JSON.parse(request.postData() ?? "{}") as object;
    return route.continue({
      postData: JSON.stringify({ ...body, expectedVersion: 999_999 }),
    });
  };
  await page.route(`**/workspaces/${workspace.id}/invitations/*/role`, stale);
  const refused = page.waitForResponse(
    (response) => response.request().method() === "PATCH",
  );
  await row.getByRole("combobox").click();
  await page.getByRole("option", { name: "Guest" }).click();
  expect((await refused).status()).toBe(412);
  await expect(
    page.getByText(`The role in the invitation to ${email} wasn't changed.`),
  ).toBeVisible();
  await page.unroute(`**/workspaces/${workspace.id}/invitations/*/role`, stale);
  expect(
    (await invitationsOf(page, workspace.id)).find((r) => r.email === email)
      ?.role,
  ).toBe("Member");

  // G05 and G06 offline: nothing leaves the browser, nothing changes, and the page says so.
  await page.context().setOffline(true);
  await row
    .getByRole("button", { name: `Resend invitation to ${email}` })
    .click();
  await expect(
    page.getByText(/The server couldn't be reached/).first(),
  ).toBeVisible();
  await row
    .getByRole("button", { name: `Revoke invitation to ${email}` })
    .click();
  await page
    .getByRole("alertdialog")
    .getByRole("button", { name: "Revoke invitation" })
    .click();
  await expect(
    page.getByText(`The invitation to ${email} wasn't revoked.`),
  ).toBeVisible();
  await page.context().setOffline(false);
  expect(
    (await invitationsOf(page, workspace.id)).find((r) => r.email === email)
      ?.status,
  ).toBe("Pending");

  // Keyboard and axe: the revoke button takes focus, opens its confirmation with Enter and Escape closes it.
  const revoke = row.getByRole("button", {
    name: `Revoke invitation to ${email}`,
  });
  await revoke.focus();
  await page.keyboard.press("Enter");
  await expect(page.getByRole("alertdialog")).toBeVisible();
  await page.keyboard.press("Escape");
  await expect(page.getByRole("alertdialog")).toHaveCount(0);
  await expect(revoke).toBeFocused();
  await page.waitForTimeout(500);
  const results = await new AxeBuilder({ page }).analyze();
  expect(
    results.violations
      .filter((violation) =>
        ["serious", "critical"].includes(violation.impact ?? ""),
      )
      .map((violation) => violation.id),
  ).toEqual([]);
});
