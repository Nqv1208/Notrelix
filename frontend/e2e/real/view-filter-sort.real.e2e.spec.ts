import { expect, test } from "@playwright/test";
import { apiRead, apiWrite, realUsers, signIn } from "./support/real-session";
import { createOwnedWorkspace } from "./support/real-workspace";

test.use({ viewport: { width: 1440, height: 900 } });
test.setTimeout(240_000);

/**
 * The filter and the sort of a view are answered by the server on a paged board: the paged read orders a lane by the
 * title (empty last ascending, first descending), keeps the items a title condition matches and counts the lane before
 * and after the filter; the table asks for exactly that when the address names a filter and when the member adds a
 * sort level; and a condition the server cannot evaluate ("overdue") is refused with a 400 and the table reads the
 * whole board and filters there, so the person still sees an answer.
 */
interface Full {
  readonly lists: readonly {
    readonly boardItems: readonly { readonly name: string }[];
  }[];
  readonly lanes?: readonly {
    readonly itemCount: number;
    readonly filteredCount: number;
  }[];
}
const idOf = (data: string | { id: string } | null) =>
  typeof data === "string" ? data : data!.id;
const names = (read: Full) =>
  read.lists.flatMap((list) => list.boardItems.map((item) => item.name));
const sum = (read: Full, key: "itemCount" | "filteredCount") =>
  (read.lanes ?? []).reduce((total, lane) => total + lane[key], 0);

test("the server filters and sorts a paged board, the table asks for it, and a refused condition falls back to the whole board", async ({
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Paged criteria");
  const board = idOf(
    (
      await apiWrite<string | { id: string }>(
        page,
        "POST",
        `/workspaces/${workspace.id}/boards`,
        { title: `Order ${Date.now().toString(36)}`, visibility: "Workspace" },
      )
    ).data,
  );
  const start = await apiRead<{ lists: readonly { id: string }[] }>(
    page,
    `/boards/${board}/full`,
  );
  // A lane is read 50 items at a time: 52 fillers after the three titles put the board past its first page, where
  // only the server can answer a filter or a sort.
  const titles = [
    "Gamma",
    "Alpha",
    "Beta",
    ...Array.from({ length: 52 }, (_, index) => `Filler ${index}`),
  ];
  for (const title of titles) {
    const made = await apiWrite<{ id: string }>(
      page,
      "POST",
      `/boards/${board}/items`,
      { groupId: start.lists[0]!.id, title },
    );
    expect(made.status, title).toBeLessThan(300);
  }

  const read = (query: string) =>
    apiRead<Full>(page, `/boards/${board}/full?pageSize=50&${query}`);
  const ascending = await read("sort=%24title.asc");
  expect(names(ascending)).toHaveLength(50);
  expect(names(ascending).slice(0, 2)).toEqual(["Alpha", "Beta"]);
  const descending = await read("sort=%24title.desc");
  expect(names(descending)[0]).toBe("Gamma");
  expect(names(descending)).not.toContain("Alpha");
  const kept = await read(
    `filter=${encodeURIComponent("c2:and~%24title.contains.alp")}`,
  );
  expect(names(kept)).toEqual(["Alpha"]);
  expect(sum(kept, "filteredCount")).toBe(1);
  expect(sum(kept, "itemCount")).toBe(55);

  // The member's own filter (a Title condition) is the server's to evaluate: the table asks for it and shows what it kept.
  await page.goto(`/workspaces/${workspace.id}/boards/${board}?view=table`);
  await expect(page.locator("[data-row-id]")).toHaveCount(50);
  const asked = page.waitForRequest(
    (request) =>
      request.url().includes(`/boards/${board}/full`) &&
      (new URL(request.url()).searchParams.get("filter") ?? "").includes(
        "title.contains.alp",
      ),
  );
  await page.getByRole("button", { name: /^Filter/ }).click();
  await page.getByRole("button", { name: "Add condition" }).click();
  await page
    .getByRole("combobox", { name: "Field (Condition 1)" })
    .selectOption({ label: "Title" });
  await page.getByRole("textbox", { name: "Value (Condition 1)" }).fill("alp");
  await asked;
  await page.keyboard.press("Escape");
  await expect(page.locator("[data-row-id]")).toHaveCount(1);
  await expect(page.getByText("Alpha", { exact: true })).toBeVisible();
  await expect(page.getByText("Beta", { exact: true })).toHaveCount(0);

  // A condition the server cannot evaluate is refused with a 400; the table reads the whole board and filters there.
  const refused = page.waitForResponse(
    (response) =>
      response.url().includes(`/boards/${board}/full`) &&
      (new URL(response.url()).searchParams.get("filter") ?? "").includes(
        "overdue",
      ),
  );
  await page.getByRole("button", { name: /^Filter/ }).click();
  await page
    .getByRole("combobox", { name: "Field (Condition 1)" })
    .selectOption({ label: "Overdue" });
  expect((await refused).status()).toBe(400);
  await page.keyboard.press("Escape");
  await expect(
    page.getByText(/No items? match(es)? the filter/i),
  ).toBeVisible();

  // Back to the shared view; a sort level the member adds is a sort the server is asked for.
  await page.getByRole("button", { name: "Back to shared view" }).click();
  await expect(page.locator("[data-row-id]")).toHaveCount(50);
  // A fresh load reads the first page again; the sort level then comes from the server too.
  await page.goto(`/workspaces/${workspace.id}/boards/${board}?view=table`);
  await expect(page.locator("[data-row-id]")).toHaveCount(50);
  const sorted = page.waitForRequest(
    (request) =>
      request.url().includes(`/boards/${board}/full`) &&
      Boolean(new URL(request.url()).searchParams.get("sort")),
  );
  await page.getByRole("button", { name: /^Sort/ }).click();
  await page.getByRole("button", { name: "Add sort" }).click();
  await sorted;
});
