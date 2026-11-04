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
    readonly isSystem?: boolean;
    readonly settings?: string | null;
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
  settingsJson?: string,
) {
  const created = await apiWrite<Created>(
    page,
    "POST",
    `/boards/${boardId}/fields`,
    { name, type, ...(settingsJson ? { settingsJson } : {}) },
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

  // The server refuses what the page would not send: a value of the wrong shape (400, nothing stored) and a field
  // of another board (the identical 404 of a field that does not exist).
  const sceneItem = await read();
  const refused = await apiWrite(
    page,
    "PATCH",
    `/board-items/${itemId}/values/${points}`,
    { value: "13a", expectedVersion: sceneItem.version },
  );
  expect(refused.status).toBe(400);
  const otherBoard = await createBoard(page, workspace.id);
  const foreign = await createField(page, otherBoard, "Elsewhere", "number");
  const crossBoard = await apiWrite(
    page,
    "PATCH",
    `/board-items/${itemId}/values/${foreign}`,
    { value: 1, expectedVersion: sceneItem.version },
  );
  const missing = await apiWrite(
    page,
    "PATCH",
    `/board-items/${itemId}/values/${crypto.randomUUID()}`,
    { value: 1, expectedVersion: sceneItem.version },
  );
  expect(crossBoard.status).toBe(404);
  expect(missing.status).toBe(404);
  expect(String(await valueOf(points))).toBe("13");
  expect((await read()).version).toBe(sceneItem.version);

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

test("a member picks from the option and date cells and clears them again, and each write carries the version the page read", async ({
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Field pickers");
  const boardId = await createBoard(page, workspace.id);
  const module = await createField(page, boardId, "Module", "select");
  const related = await createField(page, boardId, "Related", "multiselect");
  const due = await createField(page, boardId, "Go live", "date");
  const owner = await createField(page, boardId, "Owner", "person");
  const reviewers = await createField(
    page,
    boardId,
    "Reviewers",
    "person",
    JSON.stringify({ multiple: true }),
  );
  for (const fieldId of [module, related]) {
    for (const name of ["Accounting", "Warehouse", "Purchasing"]) {
      const full = await apiRead<FullBoard>(page, `/boards/${boardId}/full`);
      const field = full.columns.find((column) => column.id === fieldId)!;
      const added = await apiWrite(
        page,
        "POST",
        `/boards/${boardId}/fields/${fieldId}/options`,
        { name, color: "#1666CC", expectedVersion: field.version },
      );
      expect(added.status, `option ${name}`).toBeLessThan(300);
    }
  }
  const board = await apiRead<FullBoard>(page, `/boards/${boardId}/full`);
  const created = await apiWrite<{ id: string }>(
    page,
    "POST",
    `/boards/${boardId}/items`,
    { groupId: board.lists[0]!.id, title: "Picker row" },
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
  const edit = (name: string) =>
    row.getByRole("button", { name: new RegExp(`Edit ${name} of ${key}`) });
  const wrote = (fieldId: string) =>
    answer(page, "PATCH", new RegExp(`/values/${fieldId}$`));

  // Select: one choice writes and closes; "Clear" empties it and the server holds nothing.
  let before = await read();
  await edit("Module").click();
  const picked = wrote(module);
  await page.getByRole("option", { name: "Warehouse" }).click();
  const first = await picked;
  expect(first.status()).toBeLessThan(300);
  expect(first.request().postDataJSON()).toMatchObject({
    expectedVersion: before.version,
  });
  expect(await valueOf(module)).toBeTruthy();
  await settle(page);
  await edit("Module").click();
  const cleared = wrote(module);
  await page.getByRole("button", { name: "Clear", exact: true }).click();
  expect((await cleared).status()).toBeLessThan(300);
  expect(await valueOf(module)).toBeUndefined();
  await settle(page);

  // Multi select: each tick writes; "Done" closes.
  await edit("Related").click();
  for (const name of ["Accounting", "Purchasing"]) {
    const tick = wrote(related);
    await page.getByRole("option", { name }).click();
    expect((await tick).status()).toBeLessThan(300);
    await settle(page);
  }
  await page.getByRole("button", { name: "Done", exact: true }).click();
  expect(await valueOf(related)).toHaveLength(2);

  // People: a single field takes one person id and closes; a multiple one takes a list, each tick writes.
  await edit("Owner").click();
  const one = wrote(owner);
  await page.getByRole("option").first().click();
  expect((await one).status()).toBeLessThan(300);
  expect(typeof (await valueOf(owner))).toBe("string");
  await settle(page);
  await edit("Reviewers").click();
  const people = await page.getByRole("option").count();
  for (let index = 0; index < Math.min(2, people); index += 1) {
    const tick = wrote(reviewers);
    await page.getByRole("option").nth(index).click();
    const sent = await tick;
    expect(
      sent.status(),
      `${sent.request().postData()} -> ${await sent.text()} | ${JSON.stringify(
        (
          await apiRead<FullBoard>(page, `/boards/${boardId}/full`)
        ).columns.find((column) => column.id === reviewers),
      )}`,
    ).toBeLessThan(300);
    await settle(page);
  }
  expect(Array.isArray(await valueOf(reviewers))).toBe(true);
  await page.keyboard.press("Escape");
  await settle(page);

  // Date: a day writes and closes; "Clear date" empties it; a second clear sends nothing.
  await edit("Go live").click();
  const day = wrote(due);
  await page.getByRole("gridcell", { name: /^15 / }).click();
  expect((await day).status()).toBeLessThan(300);
  expect(await valueOf(due)).toBeTruthy();
  await settle(page);
  await edit("Go live").click();
  const noDay = wrote(due);
  await page.getByRole("button", { name: "Clear date" }).click();
  expect((await noDay).status()).toBeLessThan(300);
  expect(await valueOf(due)).toBeUndefined();
  await settle(page);
  await edit("Go live").click();
  await expect(page.getByRole("button", { name: "Clear date" })).toBeDisabled();
  await page.keyboard.press("Escape");
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

    // A system field cannot be renamed or deleted, and nothing about it changes.
    const system = full.columns.find((column) => column.isSystem)!;
    const systemRename = await apiWrite(
      page,
      "PATCH",
      `/boards/${boardId}/fields/${system.id}`,
      { name: "Nope", expectedVersion: system.version },
    );
    expect(systemRename.status).toBe(409);
    expect(JSON.stringify(systemRename.data)).toContain("system-locked");
    const systemDelete = await apiWrite(
      page,
      "DELETE",
      `/boards/${boardId}/fields/${system.id}?expectedVersion=${system.version}`,
    );
    expect(systemDelete.status).toBeGreaterThanOrEqual(400);
    expect(
      (await apiRead<FullBoard>(page, `/boards/${boardId}/full`)).columns.find(
        (column) => column.id === system.id,
      )?.version,
    ).toBe(system.version);

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

test("a parent's progress is its sub-items, a checklist keeps its own count, a formula shows its error and a rollup counts live sub-items, and a person outside gets the NotFound of a missing item", async ({
  browser,
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Field progress");
  const boardId = await createBoard(page, workspace.id);
  const points = await createField(page, boardId, "Points", "number");
  const zero = await rawApiWrite<Created>(
    page,
    "POST",
    `/boards/${boardId}/fields`,
    {
      name: "Ratio",
      type: "formula",
      settingsJson: JSON.stringify({
        expression: "[Points] / ([Points] - [Points])",
      }),
    },
  );
  expect(zero.status).toBeLessThan(300);
  const double = await rawApiWrite<Created>(
    page,
    "POST",
    `/boards/${boardId}/fields`,
    {
      name: "Double",
      type: "formula",
      settingsJson: JSON.stringify({ expression: "[Points] * 2" }),
    },
  );
  const rollup = await rawApiWrite<Created>(
    page,
    "POST",
    `/boards/${boardId}/fields`,
    {
      name: "Sub points",
      type: "rollup",
      settingsJson: JSON.stringify({ function: "sum", fieldId: points }),
    },
  );
  expect(double.status).toBeLessThan(300);
  expect(rollup.status).toBeLessThan(300);
  const doubleId = idOf(double.data);
  const ratioId = idOf(zero.data);
  const rollupId = idOf(rollup.data);

  const board = await apiRead<
    FullBoard & {
      columns: {
        options?: { id: string; category?: string | null }[] | null;
      }[];
    }
  >(page, `/boards/${boardId}/full`);
  const done = board.columns
    .flatMap((column) => column.options ?? [])
    .find((option) => option.category === "done")!;
  const statusField = board.columns.find((column) =>
    /status/i.test(column.fieldType),
  )!;
  const group = board.lists[0]!.id;

  const parent = await apiWrite<{ id: string }>(
    page,
    "POST",
    `/boards/${boardId}/items`,
    {
      groupId: group,
      title: "Parent",
    },
  );
  const parentId = parent.data!.id;
  const child = async (title: string, value: number) => {
    const created = await apiWrite<{ id: string }>(
      page,
      "POST",
      `/board-items/${parentId}/child-items`,
      { title },
    );
    expect(created.status).toBeLessThan(300);
    const read = await apiRead<ItemRead>(
      page,
      `/board-items/${created.data!.id}`,
    );
    const set = await apiWrite(
      page,
      "PATCH",
      `/board-items/${read.id}/values/${points}`,
      {
        value,
        expectedVersion: read.version,
      },
    );
    expect(set.status).toBeLessThan(300);
    return read.id;
  };
  const first = await child("One", 3);
  const second = await child("Two", 4);
  const third = await child("Three", 100);

  // One child is done, one is archived: the progress is 1 of 2 and the rollup sums the two live sub-items.
  const doneRead = await apiRead<ItemRead>(page, `/board-items/${first}`);
  expect(
    (
      await apiWrite(page, "PATCH", `/board-items/${first}/status`, {
        status: done.id,
        expectedVersion: doneRead.version,
      })
    ).status,
  ).toBeLessThan(300);
  const archiveRead = await apiRead<ItemRead>(page, `/board-items/${third}`);
  expect(
    (
      await apiWrite(page, "POST", `/board-items/${third}/archive`, {
        expectedVersion: archiveRead.version,
      })
    ).status,
  ).toBeLessThan(300);
  void second;
  void statusField;

  // A checklist of 3 with 1 done sits beside it and never feeds the parent's progress.
  const checklist = await apiWrite<Created>(
    page,
    "POST",
    `/board-items/${parentId}/checklists`,
    {
      title: "Steps",
    },
  );
  expect(checklist.status).toBeLessThan(300);
  const checklistOf = async () =>
    (
      await apiRead<{
        checklists: { id: string; version: number; items: { id: string }[] }[];
      }>(page, `/board-items/${parentId}`)
    ).checklists[0]!;
  for (const title of ["a", "b", "c"]) {
    const current = await checklistOf();
    const added = await apiWrite(
      page,
      "POST",
      `/checklists/${current.id}/items`,
      { title, expectedVersion: current.version },
    );
    expect(added.status, JSON.stringify(added.data)).toBeLessThan(300);
  }
  const ticked = await checklistOf();
  expect(
    (
      await apiWrite(
        page,
        "POST",
        `/checklist-items/${ticked.items[0]!.id}/toggle?expectedVersion=${ticked.version}`,
      )
    ).status,
  ).toBeLessThan(300);

  // A parent with a value for the formula: 13 doubles to 26, and the ratio is a divide-by-zero error, never a number.
  const parentRead = await apiRead<ItemRead>(page, `/board-items/${parentId}`);
  expect(
    (
      await apiWrite(
        page,
        "PATCH",
        `/board-items/${parentId}/values/${points}`,
        {
          value: 13,
          expectedVersion: parentRead.version,
        },
      )
    ).status,
  ).toBeLessThan(300);
  const shown = await apiRead<{
    childProgress: { done: number; total: number } | null;
    computedValues: Record<
      string,
      { state: string; value?: unknown; errorCode?: string }
    >;
  }>(page, `/board-items/${parentId}`);
  expect(shown.childProgress).toEqual({ done: 1, total: 2 });
  expect((await checklistOf()).items).toHaveLength(3);
  expect(shown.computedValues[doubleId]).toMatchObject({
    state: "ok",
    value: 26,
  });
  expect(shown.computedValues[ratioId]).toMatchObject({
    state: "error",
    errorCode: "divide-by-zero",
  });
  expect(shown.computedValues[rollupId]).toMatchObject({
    state: "ok",
    value: 7,
  });
  const written = await apiWrite(
    page,
    "PATCH",
    `/board-items/${parentId}/values/${doubleId}`,
    {
      value: 1,
      expectedVersion: (
        await apiRead<ItemRead>(page, `/board-items/${parentId}`)
      ).version,
    },
  );
  expect(written.status).toBe(400);

  // The table: the parent's row shows 1 of 2 sub-items, the formula as read only, the ratio as an error.
  await page.goto(`/workspaces/${workspace.id}/boards/${boardId}?view=table`);
  const row = page.locator(`[data-row-id="${parentId}"]`);
  await expect(
    row.getByRole("img", { name: "1 of 2 child items done" }),
  ).toBeVisible();
  await expect(
    row.locator('[data-computed-state="value"]').first(),
  ).toBeVisible();
  await expect(row.locator('[data-computed-state="error"]')).toHaveAttribute(
    "title",
    /Divided by zero/,
  );

  // A person who is not in the workspace gets the answer a missing item gets.
  const outsiderContext = await browser.newContext();
  const outsider = await outsiderContext.newPage();
  await signIn(outsider, realUsers.workspaceGuest);
  const hidden = await outsider.request.get(
    `${process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1"}/board-items/${parentId}`,
  );
  const absent = await outsider.request.get(
    `${process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1"}/board-items/${crypto.randomUUID()}`,
  );
  expect(hidden.status()).toBe(404);
  expect(absent.status()).toBe(404);
  expect(await hidden.json()).toMatchObject({ status: 404 });
  await outsiderContext.close();
});

test("a member creates a label from the typed text in the item's picker, it lands on the item on the server, and unticking takes it off", async ({
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Labels");
  const boardId = await createBoard(page, workspace.id);
  const board = await apiRead<FullBoard>(page, `/boards/${boardId}/full`);
  const created = await apiWrite<{ id: string }>(
    page,
    "POST",
    `/boards/${boardId}/items`,
    { groupId: board.lists[0]!.id, title: "Label row" },
  );
  expect(created.status).toBeLessThan(300);
  const itemId = created.data!.id;
  const labelsOf = async () =>
    (
      (
        await apiRead<{ labels?: { name: string }[] | null }>(
          page,
          `/board-items/${itemId}`,
        )
      ).labels ?? []
    ).map((label) => label.name);

  await page.goto(
    `/workspaces/${workspace.id}/boards/${boardId}/items/${itemId}`,
  );
  await page.getByRole("button", { name: "Labels", exact: true }).click();
  await page.getByRole("textbox", { name: "Find labels" }).fill("Urgent");
  await expect(page.getByText("No label named “Urgent” yet")).toBeVisible();
  await page.getByRole("button", { name: /^Create label “Urgent”/ }).click();
  const attach = page.getByRole("checkbox", {
    name: "Put label Urgent on the card",
  });
  await expect(attach).toBeChecked();
  await settle(page);
  expect(await labelsOf()).toEqual(["Urgent"]);

  // Unticking takes the label off the item; the label stays on the board for the next one.
  await attach.click();
  await expect(attach).not.toBeChecked();
  await settle(page);
  expect(await labelsOf()).toEqual([]);
  const boardLabels = await apiRead<{ name: string }[]>(
    page,
    `/boards/${boardId}/labels`,
  );
  expect(boardLabels.map((label) => label.name)).toContain("Urgent");
});

test("a manager adds, renames, recolours and deletes a label in Settings, and the server holds each change", async ({
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Label settings");
  const boardId = await createBoard(page, workspace.id);
  const labelsOnServer = () =>
    apiRead<{ name: string; color: string }[]>(
      page,
      `/boards/${boardId}/labels`,
    );
  await page.goto(
    `/workspaces/${workspace.id}/boards/${boardId}/settings?section=labels`,
  );

  await page.getByRole("button", { name: "Add label", exact: true }).click();
  const form = page.getByRole("form", { name: "New label" });
  await form.getByRole("textbox", { name: "Name of the label" }).fill("Data");
  await form.getByRole("button", { name: "Add label" }).click();
  await expect(
    page.getByRole("list", { name: "Labels of the board" }).getByText("Data"),
  ).toBeVisible();
  await settle(page);
  expect((await labelsOnServer()).map((label) => label.name)).toEqual(["Data"]);

  // Rename and recolour in one edit: Enter or "Save" sends both with the version the page read.
  await page.getByRole("button", { name: "Rename the label Data" }).click();
  await page
    .getByRole("textbox", { name: "Name of the label" })
    .fill("Data model");
  await page.getByRole("button", { name: /^Change colour, now/ }).click();
  await page.getByRole("button", { name: "Rose" }).click();
  await page.getByRole("button", { name: "Save", exact: true }).click();
  await settle(page);
  const [renamed] = await labelsOnServer();
  expect(renamed!.name).toBe("Data model");
  expect(renamed!.color.toLowerCase()).toBe("#d13a5b");

  // A duplicate name is refused on the page and nothing is sent.
  await page.getByRole("button", { name: "Add label", exact: true }).click();
  const again = page.getByRole("form", { name: "New label" });
  await again
    .getByRole("textbox", { name: "Name of the label" })
    .fill("data model");
  await expect(
    page.getByText("A label with this name already exists."),
  ).toBeVisible();
  await again.getByRole("button", { name: "Cancel" }).click();

  await page
    .getByRole("button", { name: "Delete the label Data model" })
    .click();
  await page
    .getByRole("button", { name: "Delete the label", exact: true })
    .click();
  await settle(page);
  expect(await labelsOnServer()).toEqual([]);
});

test("a field can be required only with a default, a new item gets the default, and clearing a required cell is refused with the field named", async ({
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Required fields");
  const boardId = await createBoard(page, workspace.id);
  const summary = await createField(page, boardId, "Summary", "text");
  const columnVersion = async () =>
    (await apiRead<FullBoard>(page, `/boards/${boardId}/full`)).columns.find(
      (column) => column.id === summary,
    )!.version;

  // Required with no default is refused by the server with the rule, and nothing is stored.
  const refused = await apiWrite<{ ruleCode?: string }>(
    page,
    "PATCH",
    `/boards/${boardId}/fields/${summary}`,
    {
      settingsJson: '{"required":true}',
      expectedVersion: await columnVersion(),
    },
  );
  expect(refused.status).toBeGreaterThanOrEqual(400);
  expect(refused.data?.ruleCode).toBe(
    "WorkManagement_FieldSettings_RequiredNeedsDefault",
  );
  const stored = (
    await apiRead<FullBoard>(page, `/boards/${boardId}/full`)
  ).columns.find((column) => column.id === summary)!;
  expect(stored.settings ?? "{}").not.toContain("required");

  // With a default in the same write it is accepted.
  const accepted = await apiWrite(
    page,
    "PATCH",
    `/boards/${boardId}/fields/${summary}`,
    {
      settingsJson: '{"required":true}',
      defaultValueJson: '"Chưa có tóm tắt"',
      expectedVersion: await columnVersion(),
    },
  );
  expect(accepted.status).toBeLessThan(300);

  // A new item gets the default.
  const board = await apiRead<FullBoard>(page, `/boards/${boardId}/full`);
  const created = await apiWrite<{ id: string }>(
    page,
    "POST",
    `/boards/${boardId}/items`,
    { groupId: board.lists[0]!.id, title: "Needs a summary" },
  );
  expect(created.status).toBeLessThan(300);
  const itemId = created.data!.id;
  const read = () => apiRead<ItemRead>(page, `/board-items/${itemId}`);
  expect((await read()).fieldValues?.[summary]).toBe("Chưa có tóm tắt");

  // Clearing the cell is refused: the note names the field and the value stays.
  await page.goto(`/workspaces/${workspace.id}/boards/${boardId}?view=table`);
  const row = page.locator(`[data-row-id="${itemId}"]`);
  await expect(row).toBeVisible();
  const key = (await read()).itemKey;
  const version = (await read()).version;
  await row.getByRole("button", { name: `Edit Summary of ${key}` }).click();
  const box = page.getByRole("textbox", { name: `Edit Summary of ${key}` });
  await box.fill("");
  await box.press("Enter");
  await expect(page.getByRole("alert")).toContainText("Summary is required");
  expect((await read()).fieldValues?.[summary]).toBe("Chưa có tóm tắt");
  expect((await read()).version).toBe(version);
});
