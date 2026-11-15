import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  findWorkspace,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });
test.setTimeout(150_000);

interface Channel {
  id: string;
  name: string;
  topic: string | null;
  kind: string;
  version: number;
}

const english = () => {
  try {
    window.localStorage.setItem("notrelix-locale", "en");
  } catch {
    // Private mode: the default language applies.
  }
};

async function serious(page: Page) {
  const axe = await new AxeBuilder({ page })
    .disableRules(["color-contrast", "scrollable-region-focusable"])
    .analyze();
  return axe.violations.filter((v) =>
    ["serious", "critical"].includes(v.impact ?? ""),
  );
}

test("a channel is created, renamed and given a topic, a private channel stays hidden, and an outsider is told NotFound", async ({
  page,
  browser,
}) => {
  const unique = Date.now().toString(36);
  const publicName = `ch-${unique}`;
  const taken = `taken-${unique}`;
  const secretName = `secret-${unique}`;
  await page.addInitScript(english);
  await signIn(page, realUsers.workspaceOwner);
  const workspace = await findWorkspace(page, "Workspace 3");
  const base = `/workspaces/${workspace.id}`;
  const channelsPath = `/workspaces/${workspace.id}/chat/channels`;
  await apiWrite(page, "POST", channelsPath, {
    name: taken,
    isPrivate: false,
  });
  await page.goto(`${base}/chat`);

  // The create dialog: empty it cannot be sent; filled, it creates and opens the channel; axe finds nothing serious.
  const nav = page.getByRole("navigation", { name: "Channels" });
  await nav.getByRole("button", { name: "New channel" }).click();
  const dialog = page.getByRole("dialog");
  await expect(
    dialog.getByRole("button", { name: "Create channel" }),
  ).toBeDisabled();
  expect(await serious(page)).toEqual([]);
  await dialog.getByRole("textbox", { name: "Name", exact: true }).fill(taken);
  await dialog.getByRole("button", { name: "Create channel" }).click();
  await expect(dialog.getByRole("alert")).toHaveText(
    "A channel with this name already exists.",
  );
  await dialog
    .getByRole("textbox", { name: "Name", exact: true })
    .fill(publicName);
  await dialog.getByRole("button", { name: "Create channel" }).click();
  await expect(
    page.getByRole("log").getByText("created the channel"),
  ).toBeVisible({
    timeout: 20_000,
  });
  const created = (await apiRead<Channel[]>(page, channelsPath)).find(
    (c) => c.name === publicName,
  )!;
  expect(created.kind).toBe("channel_public");

  // Rename and topic: a taken name is refused and changes nothing; a free name and a topic are saved and announced.
  const openEdit = async () => {
    await page.getByRole("button", { name: "Channel settings" }).click();
    await page.getByRole("menuitem", { name: "Rename channel" }).click();
    return page.getByRole("dialog");
  };
  let edit = await openEdit();
  await edit.getByRole("textbox", { name: "Name", exact: true }).fill(taken);
  await edit.getByRole("button", { name: "Save changes" }).click();
  await expect(edit.getByRole("alert")).toHaveText(
    "A channel with this name already exists.",
  );
  expect(
    (await apiRead<Channel[]>(page, channelsPath)).find(
      (c) => c.id === created.id,
    )!.name,
  ).toBe(publicName);
  const renamed = `${publicName}-new`;
  await edit.getByRole("textbox", { name: "Name", exact: true }).fill(renamed);
  await edit.getByRole("textbox", { name: "Topic" }).fill("Launch plans");
  expect(await serious(page)).toEqual([]);
  await edit.getByRole("button", { name: "Save changes" }).click();
  await expect(page.getByRole("dialog")).toHaveCount(0);
  await expect(
    page.getByRole("heading", { level: 1, name: `Public channel: ${renamed}` }),
  ).toBeVisible();
  await expect(page.getByText("Launch plans").first()).toBeVisible();
  await expect(
    page.getByRole("log").getByText(`renamed the channel to ${renamed}`),
  ).toBeVisible();
  const stored = (await apiRead<Channel[]>(page, channelsPath)).find(
    (c) => c.id === created.id,
  )!;
  expect([stored.name, stored.topic]).toEqual([renamed, "Launch plans"]);
  // A stale write changes nothing.
  const stale = await apiWrite(page, "PATCH", `${channelsPath}/${created.id}`, {
    name: "stale-name",
    expectedVersion: stored.version - 1,
  });
  expect(stale.status).toBe(412);
  // An unchanged save bumps no version.
  edit = await openEdit();
  await edit.getByRole("button", { name: "Save changes" }).click();
  await expect(page.getByRole("dialog")).toHaveCount(0);
  expect(
    (await apiRead<Channel[]>(page, channelsPath)).find(
      (c) => c.id === created.id,
    )!.version,
  ).toBe(stored.version);

  // A private channel made through the dialog is not offered in Browse and a stranger is told NotFound.
  await nav.getByRole("button", { name: "New channel" }).click();
  const privateDialog = page.getByRole("dialog");
  await privateDialog
    .getByRole("textbox", { name: "Name", exact: true })
    .fill(secretName);
  await privateDialog.getByRole("switch", { name: "Private channel" }).click();
  await privateDialog.getByRole("button", { name: "Create channel" }).click();
  await expect(
    page.getByRole("heading", {
      level: 1,
      name: `Private channel: ${secretName}`,
    }),
  ).toBeVisible({ timeout: 20_000 });
  const secret = (await apiRead<Channel[]>(page, channelsPath)).find(
    (c) => c.name === secretName,
  )!;
  expect(secret.kind).toBe("channel_private");

  const memberContext = await browser.newContext();
  const memberPage = await memberContext.newPage();
  await signIn(memberPage, realUsers.workspaceAdmin);
  const api = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";
  const asStranger = async (id: string) => {
    const r = await memberPage.request.get(`${api}${channelsPath}/${id}`);
    const body = JSON.parse(await r.text());
    return {
      status: r.status(),
      body: {
        ...body,
        instance: undefined,
        traceId: undefined,
        correlationId: undefined,
        detail: String(body.detail).replace(/\([0-9a-f-]{36}\)/, "(id)"),
      },
    };
  };
  const hidden = await asStranger(secret.id);
  const missing = await asStranger(crypto.randomUUID());
  expect(hidden.status).toBe(404);
  expect(hidden).toEqual(missing);
  const browse = await memberPage.request.get(
    `${api}${channelsPath}?browse=true`,
  );
  expect(((await browse.json()) as Channel[]).map((c) => c.name)).not.toContain(
    secretName,
  );
  await memberContext.close();
});
