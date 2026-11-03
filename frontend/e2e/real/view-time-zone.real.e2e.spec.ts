import { expect, test } from "@playwright/test";
import { apiRead, apiWrite, realUsers, signIn } from "./support/real-session";
import { createOwnedWorkspace } from "./support/real-workspace";

// The browser sits 25 hours behind the workspace: a day that matches in the browser's zone never matches in the workspace's.
test.use({
  viewport: { width: 1440, height: 900 },
  timezoneId: "Pacific/Pago_Pago",
});
test.setTimeout(150_000);

/**
 * A date filter reads the workspace time zone for everyone, not the browser's: with the workspace in Pacific/Kiritimati
 * (UTC+14) and the browser in Pacific/Pago_Pago (UTC-11), an item dated 2026-06-10 18:30 UTC is the 11th of June in
 * the workspace, so a filter on the 11th lists it and a filter on the 10th (its day in the browser and in UTC) does not.
 */
interface FullBoard {
  readonly lists: readonly { readonly id: string }[];
}

test("a date filter lists items by the workspace's day, not the browser's", async ({
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Time zone");
  const settings = await apiRead<{ version: number } & Record<string, unknown>>(
    page,
    `/workspaces/${workspace.id}/settings`,
  );
  const zoned = await apiWrite(
    page,
    "PUT",
    `/workspaces/${workspace.id}/settings`,
    {
      ...settings,
      timeZone: "Pacific/Kiritimati",
      expectedVersion: settings.version,
    },
  );
  expect(zoned.status, "set the workspace time zone").toBeLessThan(300);

  const created = await apiWrite<string>(
    page,
    "POST",
    `/workspaces/${workspace.id}/boards`,
    { title: `Zone ${Date.now().toString(36)}`, visibility: "Workspace" },
  );
  const boardId = created.data as unknown as string | { id: string };
  const id = typeof boardId === "string" ? boardId : boardId.id;
  const field = await apiWrite<string | { id: string }>(
    page,
    "POST",
    `/boards/${id}/fields`,
    { name: "Go live", type: "date" },
  );
  const fieldId = typeof field.data === "string" ? field.data : field.data!.id;
  const full = await apiRead<FullBoard>(page, `/boards/${id}/full`);
  const item = await apiWrite<{ id: string }>(
    page,
    "POST",
    `/boards/${id}/items`,
    {
      groupId: full.lists[0]!.id,
      title: "Dated across the day line",
    },
  );
  const itemRead = await apiRead<{ version: number }>(
    page,
    `/board-items/${item.data!.id}`,
  );
  const dated = await apiWrite(
    page,
    "PATCH",
    `/board-items/${item.data!.id}/values/${fieldId}`,
    { value: "2026-06-10T18:30:00Z", expectedVersion: itemRead.version },
  );
  expect(dated.status, "date the item").toBeLessThan(300);

  const rows = page.locator("[data-row-id]");
  const on = (day: string) =>
    page.goto(
      `/workspaces/${workspace.id}/boards/${id}?view=table&filter=${encodeURIComponent(`c1:and~${fieldId}.equals.${day}`)}`,
    );
  await on("2026-06-11");
  await expect(rows).toHaveCount(1);
  await on("2026-06-10");
  await expect(page.getByRole("tab", { name: /Table/ })).toBeVisible();
  await expect(rows).toHaveCount(0);
});
