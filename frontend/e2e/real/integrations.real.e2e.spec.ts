import { createHmac } from "node:crypto";
import { createServer, type IncomingHttpHeaders } from "node:http";
import type { AddressInfo } from "node:net";
import { expect, test } from "@playwright/test";
import {
  apiRead,
  findWorkspace,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });

interface Received {
  readonly headers: IncomingHttpHeaders;
  readonly body: string;
}

/** A receiver on this machine: the backend (in Docker) reaches it as host.docker.internal. */
async function startReceiver() {
  const received: Received[] = [];
  const server = createServer((request, response) => {
    const chunks: Buffer[] = [];
    request.on("data", (chunk: Buffer) => chunks.push(chunk));
    request.on("end", () => {
      received.push({
        headers: request.headers,
        body: Buffer.concat(chunks).toString("utf8"),
      });
      response.writeHead(204).end();
    });
  });
  await new Promise<void>((resolve) => server.listen(0, "0.0.0.0", resolve));
  const { port } = server.address() as AddressInfo;
  return {
    received,
    url: `http://host.docker.internal:${port}/notrelix-hook`,
    close: () => new Promise<void>((resolve) => server.close(() => resolve())),
  };
}

interface Overview {
  catalog: { provider: string; configured: boolean }[];
  connections: {
    id: string;
    provider: string;
    status: string;
    lastSuccessAt: string | null;
    lastError: string | null;
  }[];
}

test("an admin connects an outgoing webhook, sends a test that arrives signed, pauses, resumes and disconnects it; Google Calendar and Slack are not configured", async ({
  page,
}) => {
  const receiver = await startReceiver();
  try {
    await signIn(page, realUsers.workspaceAdmin);
    const workspace = await findWorkspace(page, "Workspace 3");
    await page.goto(`/workspaces/${workspace.id}/integrations`);

    await expect(
      page.getByRole("heading", { level: 2, name: "Integrations" }),
    ).toBeVisible();

    // Without a client id and secret in configuration a provider cannot be connected.
    const catalog = page.getByRole("list", {
      name: "Integrations you can add",
    });
    await expect(catalog.getByText("Not configured")).toHaveCount(2);
    await expect(
      catalog.getByRole("button", { name: "Connect Google Calendar" }),
    ).toHaveCount(0);
    await expect(
      catalog.getByRole("button", { name: "Connect Slack" }),
    ).toHaveCount(0);

    // An address the backend may not reach is refused.
    await catalog
      .getByRole("button", { name: "Connect Outgoing webhook" })
      .click();
    const dialog = page.getByRole("dialog", {
      name: "Connect Outgoing webhook",
    });
    await dialog
      .getByLabel("Receiving address")
      .fill("https://169.254.169.254/latest");
    await dialog.getByRole("button", { name: "Connect", exact: true }).click();
    await expect(dialog.getByRole("alert")).toBeVisible();

    // Connect the receiver; the signing secret is shown once.
    await dialog.getByLabel("Receiving address").fill(receiver.url);
    await dialog.getByRole("button", { name: "Connect", exact: true }).click();
    const secretDialog = page.getByRole("dialog", { name: "Signing secret" });
    const secret = (
      await secretDialog.locator('[data-f="signing-secret"]').innerText()
    ).trim();
    expect(secret).toMatch(/^whsec_/);
    await secretDialog.getByRole("button", { name: "Done" }).click();

    const connections = page.getByRole("table", {
      name: /Integrations in use/,
    });
    await expect(connections.getByText("Outgoing webhook")).toBeVisible();

    // The secret never comes back from the list.
    const listed = await apiRead<Overview>(
      page,
      `/workspaces/${workspace.id}/integrations`,
    );
    expect(JSON.stringify(listed)).not.toContain(secret);
    const webhook = listed.connections.find((c) => c.provider === "webhook")!;
    expect(webhook.lastSuccessAt).toBeNull();

    // Test delivery: signed, with a delivery identity, and the connection records the success.
    await page
      .getByRole("link", { name: "Open Outgoing webhook" })
      .first()
      .click();
    await page.getByRole("button", { name: "Send test" }).click();
    await expect
      .poll(() => receiver.received.length, { timeout: 30_000 })
      .toBe(1);

    const hit = receiver.received[0]!;
    const timestamp = String(hit.headers["x-notrelix-timestamp"]);
    const expected = `sha256=${createHmac("sha256", secret)
      .update(`${timestamp}.${hit.body}`)
      .digest("hex")}`;
    expect(hit.headers["x-notrelix-signature"]).toBe(expected);
    expect(hit.headers["x-notrelix-delivery"]).toMatch(/^[0-9a-f-]{36}$/);
    expect(JSON.parse(hit.body)).toMatchObject({ type: "notrelix.test" });

    await expect
      .poll(
        async () =>
          (
            await apiRead<Overview>(
              page,
              `/workspaces/${workspace.id}/integrations`,
            )
          ).connections.find((c) => c.id === webhook.id)?.lastSuccessAt,
        { timeout: 30_000 },
      )
      .not.toBeNull();

    // Pause and resume.
    await page.getByRole("button", { name: "Pause", exact: true }).click();
    await expect(page.getByRole("button", { name: "Resume" })).toBeVisible();
    await page.getByRole("button", { name: "Resume" }).click();
    await expect(
      page.getByRole("button", { name: "Pause", exact: true }),
    ).toBeVisible();

    // Disconnect after the confirmation.
    await page.getByRole("button", { name: "Disconnect", exact: true }).click();
    await page
      .getByRole("alertdialog")
      .getByRole("button", { name: "Disconnect", exact: true })
      .click();
    await expect(
      page
        .getByRole("table", { name: /Integrations in use/ })
        .getByText("Outgoing webhook"),
    ).toHaveCount(0);
  } finally {
    await receiver.close();
  }
});
