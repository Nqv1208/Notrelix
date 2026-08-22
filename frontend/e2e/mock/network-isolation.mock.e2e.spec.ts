import { expect, test } from "@playwright/test";

const scenario = process.env.VITE_MOCK_STATE ?? "default";

test("does not escape to backend HTTP, auth refresh, or realtime", async ({
  page,
}) => {
  const backendTraffic: string[] = [];
  const refreshTraffic: string[] = [];
  const sockets: string[] = [];
  const unhandled: string[] = [];
  page.on("request", (request) => {
    if (request.url().startsWith("http://127.0.0.1:59999"))
      backendTraffic.push(request.url());
    if (request.url().includes("/auth/refresh"))
      refreshTraffic.push(request.url());
  });
  page.on("websocket", (socket) => sockets.push(socket.url()));
  page.on("pageerror", (error) => {
    if (error.message.includes("MockUnhandledOperationError"))
      unhandled.push(error.message);
  });

  await page.goto("/home");

  if (scenario === "default") {
    await page.goto(
      "/workspaces/mock-workspace-primary/boards/mock-board-roadmap",
    );
    await page.goto(
      "/workspaces/mock-workspace-primary/docs/mock-doc-product-spec",
    );
  }

  // Network isolation is required across ALL scenarios
  expect(backendTraffic).toEqual([]);
  expect(refreshTraffic).toEqual([]);
  expect(
    sockets.filter((url) => url.startsWith("ws://127.0.0.1:59998")),
  ).toEqual([]);
  expect(unhandled).toEqual([]);
});
