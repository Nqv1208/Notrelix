import { expect, test, type Page } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  findWorkspace,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });
test.setTimeout(240_000);

type TemplateRow = {
  id: string;
  name: string;
  description: string | null;
  version: number;
  canManage: boolean;
};

const text = (value: string) => ({ v: 1, runs: [{ text: value, marks: [] }] });

async function listTemplates(page: Page, workspaceId: string) {
  return apiRead<TemplateRow[]>(
    page,
    `/workspaces/${workspaceId}/page-templates`,
  );
}

async function openTemplates(page: Page) {
  await page.getByRole("menuitem", { name: "File", exact: true }).click();
  await page.getByRole("menuitem", { name: /Page templates/ }).click();
  await expect(
    page.getByRole("dialog", { name: "Page templates" }),
  ).toBeVisible();
}

test("the creator renames, archives and deletes templates in the gallery; a stale row says so; another member can only read; an Admin may manage", async ({
  page,
}) => {
  const stamp = Date.now();
  const keep = `Weekly notes ${stamp}`;
  const renamed = `Sprint notes ${stamp}`;
  const drop = `Throwaway ${stamp}`;

  await signIn(page, realUsers.workspaceOwner);
  const workspace = await findWorkspace(page, "Workspace 3");
  const created = await apiWrite<string>(
    page,
    "POST",
    `/workspaces/${workspace.id}/pages`,
    { title: `Template source ${stamp}` },
  );
  expect(created.status).toBe(201);
  const source = created.data as string;
  const block = await apiWrite(page, "POST", `/pages/${source}/blocks`, {
    type: "Text",
    content: text(`Agenda ${stamp}`),
  });
  expect(block.status).toBe(201);
  for (const name of [keep, drop]) {
    const saved = await apiWrite(
      page,
      "POST",
      `/workspaces/${workspace.id}/page-templates`,
      { pageId: source, name },
    );
    expect(saved.status).toBe(201);
  }

  await page.goto(`/workspaces/${workspace.id}/docs/${source}`);
  await expect(page.getByText(`Agenda ${stamp}`)).toBeVisible();
  await openTemplates(page);
  const dialog = page.getByRole("dialog", { name: "Page templates" });
  await expect(dialog.getByText(keep)).toBeVisible();
  await expect(dialog.getByText(drop)).toBeVisible();

  // Rename in place: an empty name is refused in the row and nothing is sent.
  await dialog.getByRole("button", { name: `Rename template ${keep}` }).click();
  const name = dialog.getByRole("textbox", { name: "Template name" });
  await name.fill("   ");
  await dialog.getByRole("button", { name: "Save", exact: true }).click();
  await expect(dialog.getByRole("alert")).toHaveText(
    "Enter a template name to save.",
  );
  await name.fill(renamed);
  await dialog.getByRole("button", { name: "Save", exact: true }).click();
  await expect(dialog.getByText(renamed)).toBeVisible();
  await expect(dialog.getByText(keep)).toHaveCount(0);
  expect(
    (await listTemplates(page, workspace.id)).map((t) => t.name),
  ).toContain(renamed);

  // Delete asks first: cancel keeps it, confirm removes it for good.
  await dialog.getByRole("button", { name: `Delete template ${drop}` }).click();
  await expect(dialog.getByText(/This cannot be undone/)).toBeVisible();
  await dialog.getByRole("button", { name: "Cancel" }).click();
  await expect(dialog.getByText(drop)).toBeVisible();
  await dialog.getByRole("button", { name: `Delete template ${drop}` }).click();
  await dialog
    .getByRole("button", { name: "Delete template", exact: true })
    .click();
  await expect(dialog.getByText(drop)).toHaveCount(0);
  expect(
    (await listTemplates(page, workspace.id)).map((t) => t.name),
  ).not.toContain(drop);

  // A stale row: someone else renames the template, so archiving with the version this list holds is refused
  // (412), nothing is archived, and the list is read again.
  const current = (await listTemplates(page, workspace.id)).find(
    (t) => t.name === renamed,
  )!;
  const bump = await apiWrite(
    page,
    "PATCH",
    `/workspaces/${workspace.id}/page-templates/${current.id}`,
    {
      name: renamed,
      description: `Changed ${stamp}`,
      expectedVersion: current.version,
    },
  );
  expect(bump.status).toBe(200);
  await dialog
    .getByRole("button", { name: `Archive template ${renamed}` })
    .click();
  await dialog
    .getByRole("button", { name: "Archive template", exact: true })
    .click();
  await expect(dialog.getByRole("alert")).toContainText(
    "Someone just changed this template",
  );
  await expect(dialog.getByText(`Changed ${stamp}`)).toBeVisible();
  expect((await listTemplates(page, workspace.id)).map((t) => t.id)).toContain(
    current.id,
  );

  // A Guest of the workspace cannot manage it: the list says so and the write is refused (403).
  const reader = await page.context().browser()!.newContext();
  const guestPage = await reader.newPage();
  await signIn(guestPage, realUsers.workspaceGuest);
  const asGuest = (await listTemplates(guestPage, workspace.id)).find(
    (t) => t.id === current.id,
  );
  expect(asGuest?.canManage).toBe(false);
  const refused = await apiWrite(
    guestPage,
    "DELETE",
    `/workspaces/${workspace.id}/page-templates/${current.id}?expectedVersion=${asGuest!.version}`,
  );
  expect(refused.status).toBe(403);
  expect((await listTemplates(page, workspace.id)).map((t) => t.id)).toContain(
    current.id,
  );

  // A Workspace Admin who did not save it archives it; the owner's list no longer holds it.
  const adminContext = await page.context().browser()!.newContext();
  const adminPage = await adminContext.newPage();
  await signIn(adminPage, realUsers.workspaceAdmin);
  const own = await apiWrite<string>(
    adminPage,
    "POST",
    `/workspaces/${workspace.id}/pages`,
    { title: `Admin page ${stamp}` },
  );
  expect(own.status).toBe(201);
  await adminPage.goto(
    `/workspaces/${workspace.id}/docs/${own.data as string}`,
  );
  await openTemplates(adminPage);
  const adminDialog = adminPage.getByRole("dialog", { name: "Page templates" });
  await adminDialog
    .getByRole("button", { name: `Archive template ${renamed}` })
    .click();
  await adminDialog
    .getByRole("button", { name: "Archive template", exact: true })
    .click();
  await expect(adminDialog.getByText(renamed)).toHaveCount(0);
  expect(
    (await listTemplates(page, workspace.id)).map((t) => t.id),
  ).not.toContain(current.id);
  await reader.close();
  await adminContext.close();
});
