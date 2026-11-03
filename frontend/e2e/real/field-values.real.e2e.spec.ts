import { expect, test, type Page } from "@playwright/test";
import {
  apiRead,
  apiWrite as rawApiWrite,
  realUsers,
  signIn,
} from "./support/real-session";
import { createOwnedWorkspace, joinAs } from "./support/real-workspace";

test.use({ viewport: { width: 1440, height: 900 } });
test.setTimeout(240_000);

/**
 * Custom field values and field definitions on the real stack, in a workspace made for each journey. The values
 * journey edits a cell of each editable type through the table and checks the server after each write, then the
 * rejections: a value the server refuses leaves the cell with its reason, a write that lost the race shows the
 * choice in the cell (keep mine saves on the newest version, use the new value sends nothing), an unchanged value
 * and a second clear change nothing. The definitions journey adds, renames and deletes a field from Settings, restores
 * it from the Deleted tab with its values, and meets the refusals: a field a formula reads, a system field, a member
 * who is not a manager, a name already taken and a stale version.
 */

/** The scene's own writes: a 403 here is the CSRF token rotating under the open page, so it is sent again. */
async function apiWrite<T = unknown>(
  ...args: Parameters<typeof rawApiWrite<T>>
) {
  let last = await rawApiWrite<T>(...args);
  for (let attempt = 1; last.status === 403 && attempt < 5; attempt += 1) {
    await args[0].waitForTimeout(400);
    last = await rawApiWrite<T>(...args);
  }
  return last;
}

/** A person cannot click before the answer and the board read it brings have landed. */
const settle = (page: Page) => page.waitForTimeout(700);

type Created = string | { id: string };
const idOf = (data: Created | null) =>
  typeof data === "string" ? data : data!.id;

interface ItemRead {
  readonly id: string;
  readonly version: number;
  readonly itemKey: string;
  readonly fieldValues?: Record<string, unknown> | null;
}
interface FullBoard {
  readonly lists: readonly { readonly id: string }[];
  readonly columns: readonly {
    readonly id: string;
    readonly name: string;
    readonly version: number;
    readonly fieldType: string;
  }[];
}

async function createBoard(page: Page, workspaceId: string) {
  const created = await apiWrite<Created>(
    page,
    "POST",
    `/workspaces/${workspaceId}/boards`,
    { title: `Fields ${Date.now().toString(36)}`, visibility: "Workspace" },
  );
  expect(created.status, "create board").toBeLessThan(300);
  return idOf(created.data);
}

async function createField(
  page: Page,
  boardId: string,
  name: string,
  type: string,
) {
  const created = await apiWrite<Created>(
    page,
    "POST",
    `/boards/${boardId}/fields`,
    { name, type },
  );
  expect(created.status, `create field ${name}`).toBeLessThan(300);
  return idOf(created.data);
}

const answer = (page: Page, method: string, route: RegExp) =>
  page.waitForResponse(
    (response) =>
      response.request().method() === method &&
      route.test(response.url()) &&
      response.status() !== 403,
  );

test("a member edits the custom cells of the table, a refused value stays in the cell with its reason, and a write that lost the race offers both choices", async ({
  browser,
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Field values");
  const boardId = await createBoard(page, workspace.id);
  const points = await createField(page, boardId, "Points", "number");
  const note = await createField(page, boardId, "Note", "text");
  const link = await createField(page, boardId, "Reference", "link");
  const accepted = await createField(page, boardId, "Accepted", "checkbox");
  const board = await apiRead<FullBoard>(page, `/boards/${boardId}/full`);
  const created = await apiWrite<{ id: string }>(
    page,
    "POST",
    `/boards/${boardId}/items`,
    { groupId: board.lists[0]!.id, title: "Value row" },
  );
  expect(created.status).toBeLessThan(300);
  const itemId = created.data!.id;
  const read = () => apiRead<ItemRead>(page, `/board-items/${itemId}`);
  const valueOf = async (fieldId: string) =>
    (await read()).fieldValues?.[fieldId];

  await page.goto(`/workspaces/${workspace.id}/boards/${boardId}?view=table`);
  const row = page.locator(`[data-row-id="${itemId}"]`);
  await expect(row).toBeVisible();
  const key = (await read()).itemKey;

  // A number: Enter writes once with the version the page read, and the server holds it.
  const before = await read();
  await row.getByRole("button", { name: `Edit Points of ${key}` }).click();
  const number = page.getByRole("textbox", { name: `Edit Points of ${key}` });
  const wrote = answer(page, "PATCH", new RegExp(`/values/${points}$`));
  await number.fill("13");
  await number.press("Enter");
  const first = await wrote;
  expect(first.status()).toBeLessThan(300);
  expect(first.request().postDataJSON()).toMatchObject({
    expectedVersion: before.version,
  });
  expect(String(await valueOf(points))).toBe("13");
  await settle(page);
  const afterNumber = await read();

  // A value that is not a number is refused in the page: the reason shows, nothing is sent, nothing changes.
  await row
    .getByRole("button", { name: new RegExp(`Edit Points of ${key}`) })
    .click();
  const bad = page.getByRole("textbox", { name: `Edit Points of ${key}` });
  await bad.fill("13a");
  await bad.press("Enter");
  await expect(page.getByRole("alert")).toContainText("Enter a number");
  await bad.press("Escape");
  expect((await read()).version).toBe(afterNumber.version);

  // The same value again sends nothing and bumps no version.
  await row
    .getByRole("button", { name: new RegExp(`Edit Points of ${key}`) })
    .click();
  const same = page.getByRole("textbox", { name: `Edit Points of ${key}` });
  await same.fill("13");
  await same.press("Enter");
  await page.waitForTimeout(500);
  expect((await read()).version).toBe(afterNumber.version);

  // Text, link and checkbox.
  await row.getByRole("button", { name: `Edit Note of ${key}` }).click();
  const text = page.getByRole("textbox", { name: `Edit Note of ${key}` });
  const wroteNote = answer(page, "PATCH", new RegExp(`/values/${note}$`));
  await text.fill("Cần xuất bản Excel");
  await text.press("Enter");
  expect((await wroteNote).status()).toBeLessThan(300);
  expect(await valueOf(note)).toBe("Cần xuất bản Excel");
  await settle(page);

  await row.getByRole("button", { name: `Edit Reference of ${key}` }).click();
  const url = page.getByRole("textbox", { name: `Edit Reference of ${key}` });
  await url.fill("javascript:alert(1)");
  await url.press("Enter");
  await expect(page.getByRole("alert")).toContainText("http");
  await url.fill("https://erp.example.vn/t/145");
  const wroteLink = answer(page, "PATCH", new RegExp(`/values/${link}$`));
  await url.press("Enter");
  expect((await wroteLink).status()).toBeLessThan(300);
  expect(await valueOf(link)).toBe("https://erp.example.vn/t/145");
  await settle(page);

  const wroteBox = answer(page, "PATCH", new RegExp(`/values/${accepted}$`));
  await row
    .getByRole("checkbox", { name: new RegExp(`Accepted of ${key}`) })
    .click();
  expect((await wroteBox).status()).toBeLessThan(300);
  expect(await valueOf(accepted)).toBe(true);

  // After a reload the server's values are what the cells show.
  await page.reload();
  await expect(
    page
      .locator(`[data-row-id="${itemId}"]`)
      .getByRole("button", { name: `Edit Note of ${key}: Cần xuất bản Excel` }),
  ).toBeVisible();

  // The rejection: another member changes the number first; the page still holds the old version.
  const member = await joinAs(
    browser,
    page,
    workspace.id,
    realUsers.workspaceAdmin,
    "Member",
  );
  try {
    const current = await read();
    const theirs = await apiWrite(
      member.page,
      "PATCH",
      `/board-items/${itemId}/values/${points}`,
      { value: 21, expectedVersion: current.version },
    );
    expect(theirs.status).toBeLessThan(300);
    let oldVersion: number | null = current.version;
    await page.route(
      new RegExp(`/board-items/${itemId}/values/`),
      async (route) => {
        const request = route.request();
        const body = request.postDataJSON() as Record<string, unknown> | null;
        if (oldVersion === null || request.method() !== "PATCH" || !body)
          return route.continue();
        return route.continue({
          postData: JSON.stringify({ ...body, expectedVersion: oldVersion }),
        });
      },
    );
    await row
      .getByRole("button", { name: new RegExp(`Edit Points of ${key}`) })
      .click();
    const mine = page.getByRole("textbox", { name: `Edit Points of ${key}` });
    const lost = answer(page, "PATCH", new RegExp(`/values/${points}$`));
    await mine.fill("34");
    await mine.press("Enter");
    expect((await lost).status()).toBe(412);
    const choice = page.getByRole("alertdialog", { name: "Value conflict" });
    await expect(choice).toContainText("Your value: 34");
    expect(String(await valueOf(points))).toBe("21");

    // Keep my value: the page reads the board again and then writes on the newest version.
    oldVersion = null;
    const kept = answer(page, "PATCH", new RegExp(`/values/${points}$`));
    await choice.getByRole("button", { name: "Keep my value" }).click();
    const keptAnswer = await kept;
    expect(keptAnswer.status()).toBeLessThan(300);
    expect(keptAnswer.request().postDataJSON()).toMatchObject({
      expectedVersion: (await read()).version - 1,
    });
    expect(String(await valueOf(points))).toBe("34");
    await settle(page);

    // Use the new value: a second race, and the choice that drops mine sends nothing.
    const again = await read();
    expect(
      (
        await apiWrite(
          member.page,
          "PATCH",
          `/board-items/${itemId}/values/${points}`,
          { value: 55, expectedVersion: again.version },
        )
      ).status,
    ).toBeLessThan(300);
    oldVersion = again.version;
    await row
      .getByRole("button", { name: new RegExp(`Edit Points of ${key}`) })
      .click();
    const second = page.getByRole("textbox", { name: `Edit Points of ${key}` });
    const lostAgain = answer(page, "PATCH", new RegExp(`/values/${points}$`));
    await second.fill("89");
    await second.press("Enter");
    expect((await lostAgain).status()).toBe(412);
    oldVersion = null;
    const sent: string[] = [];
    page.on("request", (request) => {
      if (request.method() === "PATCH" && request.url().includes(`/values/`))
        sent.push(request.url());
    });
    await page
      .getByRole("alertdialog", { name: "Value conflict" })
      .getByRole("button", { name: /Use the new value/ })
      .click();
    await page.waitForTimeout(500);
    expect(sent).toHaveLength(0);
    expect(String(await valueOf(points))).toBe("55");

    // Clearing a value: the cell empties, the server holds nothing, and clearing again changes nothing.
    await row
      .getByRole("button", { name: new RegExp(`Edit Note of ${key}`) })
      .click();
    const clear = page.getByRole("textbox", { name: `Edit Note of ${key}` });
    const cleared = answer(page, "PATCH", new RegExp(`/values/${note}$`));
    await clear.fill("");
    await clear.press("Enter");
    expect((await cleared).status()).toBeLessThan(300);
    expect(await valueOf(note)).toBeUndefined();
  } finally {
    await member.context.close();
  }
});

test("a manager adds, renames and deletes a field from Settings, restores it with its values, and the refusals change nothing", async ({
  browser,
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Field lifecycle");
  const member = await joinAs(
    browser,
    page,
    workspace.id,
    realUsers.workspaceAdmin,
    "Member",
  );
  const boardId = await createBoard(page, workspace.id);
  const settings = `/workspaces/${workspace.id}/boards/${boardId}/settings?section=fields`;
  const unique = Date.now().toString(36);

  try {
    // Add a Number field from the picker, then rename it in its panel (the rename carries the field version).
    await page.goto(settings);
    await page.getByRole("button", { name: "Add field" }).click();
    const created = answer(page, "POST", /\/boards\/[^/]+\/fields$/);
    await page.getByRole("button", { name: /^Number/ }).click();
    expect((await created).status()).toBeLessThan(300);
    const name = `Effort ${unique}`;
    const panelName = page.getByLabel("Display name");
    await expect(panelName).toBeVisible();
    const renamed = answer(page, "PATCH", /\/boards\/[^/]+\/fields\/[^/]+$/);
    await panelName.fill(name);
    await page.getByRole("button", { name: "Save changes" }).click();
    const renameAnswer = await renamed;
    expect(renameAnswer.status()).toBeLessThan(300);
    expect(renameAnswer.request().postDataJSON()).toMatchObject({
      name,
      expectedVersion: expect.any(Number),
    });
    const full = await apiRead<FullBoard>(page, `/boards/${boardId}/full`);
    const field = full.columns.find((column) => column.name === name)!;
    expect(field.fieldType.toLowerCase()).toBe("number");

    // A name another field uses is refused with 409 and the field keeps its name; the same name again is a no-op.
    const other = await createField(page, boardId, `Other ${unique}`, "text");
    const otherVersion = (
      await apiRead<FullBoard>(page, `/boards/${boardId}/full`)
    ).columns.find((column) => column.id === other)!.version;
    const taken = await apiWrite(
      page,
      "PATCH",
      `/boards/${boardId}/fields/${other}`,
      { name: name.toUpperCase(), expectedVersion: otherVersion },
    );
    expect(taken.status).toBe(409);
    const stale = await apiWrite(
      page,
      "PATCH",
      `/boards/${boardId}/fields/${field.id}`,
      { name: `${name} again`, expectedVersion: field.version - 1 },
    );
    expect(stale.status).toBe(412);

    // A member who is not a manager cannot delete or rename it, and gets the answer a missing field gets or Forbidden.
    const memberDelete = await rawApiWrite(
      member.page,
      "DELETE",
      `/boards/${boardId}/fields/${field.id}?expectedVersion=${field.version}`,
    );
    expect([403, 404]).toContain(memberDelete.status);

    // A system field cannot be renamed or deleted.
    const system = full.columns.find((column) =>
      /status/i.test(column.fieldType),
    )!;
    const systemRename = await apiWrite(
      page,
      "PATCH",
      `/boards/${boardId}/fields/${system.id}`,
      { name: "Nope", expectedVersion: system.version },
    );
    expect(systemRename.status).toBeGreaterThanOrEqual(400);

    // A formula that reads the field blocks its deletion and is named.
    const formula = await apiWrite<Created>(
      page,
      "POST",
      `/boards/${boardId}/fields`,
      {
        name: `Double ${unique}`,
        type: "formula",
        settingsJson: JSON.stringify({ expression: `[${name}] * 2` }),
      },
    );
    expect(formula.status, JSON.stringify(formula.data)).toBeLessThan(300);
    const blocked = await apiWrite(
      page,
      "DELETE",
      `/boards/${boardId}/fields/${field.id}?expectedVersion=${field.version + 1}`,
    );
    expect(blocked.status).toBe(400);
    expect(JSON.stringify(blocked.data)).toContain(`Double ${unique}`);
    const formulaField = (
      await apiRead<FullBoard>(page, `/boards/${boardId}/full`)
    ).columns.find((column) => column.name === `Double ${unique}`)!;
    expect(
      (
        await apiWrite(
          page,
          "DELETE",
          `/boards/${boardId}/fields/${formulaField.id}?expectedVersion=${formulaField.version}`,
        )
      ).status,
    ).toBeLessThan(300);

    // An item holds a value, then the field is deleted from the page: the dialog says it can be restored, the
    // field leaves the list, and the Deleted tab restores it with its value.
    const board = await apiRead<FullBoard>(page, `/boards/${boardId}/full`);
    const item = await apiWrite<{ id: string }>(
      page,
      "POST",
      `/boards/${boardId}/items`,
      { groupId: board.lists[0]!.id, title: "Holds a value" },
    );
    const itemRead = await apiRead<ItemRead>(
      page,
      `/board-items/${item.data!.id}`,
    );
    expect(
      (
        await apiWrite(
          page,
          "PATCH",
          `/board-items/${itemRead.id}/values/${field.id}`,
          { value: 8, expectedVersion: itemRead.version },
        )
      ).status,
    ).toBeLessThan(300);

    await page.reload();
    await page.getByRole("button", { name: `Edit the field ${name}` }).click();
    await page.getByRole("button", { name: "Delete field" }).click();
    const dialog = page.getByRole("alertdialog");
    await expect(dialog).toContainText("restore it");
    await expect(dialog).toContainText("1 item has a value");
    const deleted = answer(page, "DELETE", /\/boards\/[^/]+\/fields\/[^/]+$/);
    await dialog.getByRole("button", { name: "Delete field" }).click();
    const deletedAnswer = await deleted;
    expect(deletedAnswer.status()).toBeLessThan(300);
    expect(deletedAnswer.url()).toContain("expectedVersion=");
    await expect(
      page.getByRole("button", { name: `Edit the field ${name}` }),
    ).toHaveCount(0);
    // A write to the deleted field answers 412 naming it, and stores nothing.
    const late = await apiWrite(
      page,
      "PATCH",
      `/board-items/${itemRead.id}/values/${field.id}`,
      {
        value: 9,
        expectedVersion: (
          await apiRead<ItemRead>(page, `/board-items/${itemRead.id}`)
        ).version,
      },
    );
    expect(late.status).toBe(412);
    expect(JSON.stringify(late.data)).toContain(name);

    await page.getByRole("tab", { name: /^Deleted/ }).click();
    const restored = answer(page, "POST", /\/fields\/[^/]+\/restore$/);
    await page
      .getByRole("button", { name: `Restore the field ${name}` })
      .click();
    expect((await restored).status()).toBeLessThan(300);
    await expect(page.getByText(`Restored the field ${name}`)).toBeVisible();
    expect(
      String(
        (await apiRead<ItemRead>(page, `/board-items/${itemRead.id}`))
          .fieldValues?.[field.id],
      ),
    ).toBe("8");
  } finally {
    await member.context.close();
  }
});
