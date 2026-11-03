import { expect, test } from "@playwright/test";
import { apiRead, apiWrite, realUsers, signIn } from "./support/real-session";
import { createOwnedWorkspace } from "./support/real-workspace";

test.use({ viewport: { width: 1440, height: 900 } });
test.setTimeout(150_000);

/**
 * A label condition filters on the server: "has any of" lists only the items holding a label, the paged read counts
 * them as the filtered size, the table shows them after a reload, and a label no item holds (another board's)
 * matches nothing.
 */
interface Full {
  readonly lists: readonly { readonly id: string }[];
  readonly lanes?: readonly { readonly filteredCount: number }[];
}
const idOf = (data: string | { id: string } | null) =>
  typeof data === "string" ? data : data!.id;

test("a label filter keeps only the items holding the label, on the server and in the table", async ({
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Label filter");
  const board = idOf(
    (
      await apiWrite<string | { id: string }>(
        page,
        "POST",
        `/workspaces/${workspace.id}/boards`,
        { title: `Labels ${Date.now().toString(36)}`, visibility: "Workspace" },
      )
    ).data,
  );
  const label = idOf(
    (
      await apiWrite<string | { id: string }>(
        page,
        "POST",
        `/boards/${board}/labels`,
        { color: "#E53935", name: "Urgent" },
      )
    ).data,
  );
  const full = await apiRead<Full>(page, `/boards/${board}/full`);
  const items: string[] = [];
  for (const title of ["Tagged", "Plain one", "Plain two"]) {
    const made = await apiWrite<{ id: string }>(
      page,
      "POST",
      `/boards/${board}/items`,
      { groupId: full.lists[0]!.id, title },
    );
    expect(made.status, title).toBeLessThan(300);
    items.push(made.data!.id);
  }
  const tagged = await apiWrite(
    page,
    "POST",
    `/board-items/${items[0]}/labels`,
    { labelId: label },
  );
  expect(tagged.status).toBeLessThan(300);

  const filtered = (id: string) =>
    apiRead<Full>(
      page,
      `/boards/${board}/full?filter=${encodeURIComponent(`c2:and~%24label.has_any.${id}`)}`,
    );
  const sizeOf = (read: Full) =>
    (read.lanes ?? []).reduce((sum, lane) => sum + lane.filteredCount, 0);
  expect(sizeOf(await filtered(label))).toBe(1);
  expect(sizeOf(await filtered(crypto.randomUUID()))).toBe(0);

  await page.goto(
    `/workspaces/${workspace.id}/boards/${board}?view=table&filter=${encodeURIComponent(`c2:and~%24label.has_any.${label}`)}`,
  );
  await expect(page.locator("[data-row-id]")).toHaveCount(1);
  await expect(page.getByText("Tagged", { exact: true })).toBeVisible();
  await expect(page.getByText("Plain one")).toHaveCount(0);
});
