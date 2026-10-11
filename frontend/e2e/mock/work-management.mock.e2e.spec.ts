import { expect, test } from "@playwright/test";

const scenario = process.env.VITE_MOCK_STATE ?? "default";

test("renders a production Work Management route through mock transport", async ({
  page,
}) => {
  const errors: string[] = [];
  page.on("pageerror", (error) => errors.push(error.message));

  if (scenario !== "default") {
    // Work management board requires default world data.
    // For other scenarios, just verify home loads without mock errors.
    await page.goto("/home");
    expect(
      errors.filter((message) =>
        message.includes("MockUnhandledOperationError"),
      ),
    ).toEqual([]);
    return;
  }

  await page.goto(
    "/workspaces/mock-workspace-primary/boards/mock-board-roadmap",
  );
  await expect(page.getByText("Product Roadmap").first()).toBeVisible();
  await expect(page.getByText("Ship mock runtime").first()).toBeVisible();
  expect(
    errors.filter((message) => message.includes("MockUnhandledOperationError")),
  ).toEqual([]);
});

test.describe("Kanban item detail through the item search parameter", () => {
  test.skip(scenario !== "default", "Assertions need the default world data");

  const board = "/workspaces/mock-workspace-primary/boards/mock-board-roadmap";

  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      try {
        window.localStorage.setItem("notrelix-locale", "en");
      } catch {
        // Private mode: the default locale applies.
      }
    });
  });

  test("opening a card writes item, a reload reopens it and Escape removes it", async ({
    page,
  }) => {
    const errors: string[] = [];
    page.on("pageerror", (error) => errors.push(error.message));

    await page.goto(`${board}?view=kanban&filter=mine&sort=due&groupBy=status`);
    await page
      .getByRole("button", { name: "Ship mock runtime", exact: true })
      .click();

    const dialog = page.getByRole("dialog", { name: "Ship mock runtime" });
    await expect(dialog).toBeVisible();
    await expect(page).toHaveURL(/[?&]item=/);
    const opened = new URL(page.url());
    const item = opened.searchParams.get("item");
    expect(item).toBeTruthy();
    expect(opened.searchParams.get("view")).toBe("kanban");
    expect(opened.searchParams.get("filter")).toBe("mine");
    expect(opened.searchParams.get("sort")).toBe("due");
    expect(opened.searchParams.get("groupBy")).toBe("status");

    await page.reload();
    await expect(
      page.getByRole("dialog", { name: "Ship mock runtime" }),
    ).toBeVisible();
    expect(new URL(page.url()).searchParams.get("item")).toBe(item);

    await page.keyboard.press("Escape");
    await expect(page.getByRole("dialog")).toHaveCount(0);
    const closed = new URL(page.url());
    expect(closed.searchParams.has("item")).toBe(false);
    expect(closed.searchParams.get("view")).toBe("kanban");
    expect(closed.searchParams.get("filter")).toBe("mine");
    expect(closed.searchParams.get("sort")).toBe("due");
    expect(closed.searchParams.get("groupBy")).toBe("status");
    // Focus returns to the card that opened the sheet.
    await expect(
      page.getByRole("button", { name: "Ship mock runtime", exact: true }),
    ).toBeFocused();

    // Closing replaced the history entry, so Back does not reopen the sheet.
    await page.goBack();
    await expect(page.getByRole("dialog")).toHaveCount(0);
    expect(errors).toEqual([]);
  });

  test("an unknown item id shows no detail and does not throw", async ({
    page,
  }) => {
    const errors: string[] = [];
    page.on("pageerror", (error) => errors.push(error.message));

    await page.goto(`${board}?view=kanban&item=no-such-card`);
    await expect(
      page.getByRole("button", { name: "Ship mock runtime", exact: true }),
    ).toBeVisible();
    await expect(page.getByRole("dialog")).toHaveCount(0);
    // The stale id is cleared from the URL once the lookup fails.
    await expect
      .poll(() => new URL(page.url()).searchParams.has("item"), {
        timeout: 15_000,
      })
      .toBe(false);
    expect(new URL(page.url()).searchParams.get("view")).toBe("kanban");
    expect(errors).toEqual([]);
  });
});
