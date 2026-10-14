import { expect, test } from "@playwright/test";

const scenario = process.env.VITE_MOCK_STATE ?? "default";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const workspace = "/workspaces/mock-workspace-primary";

test.beforeEach(async ({ page }) => {
  test.skip(
    scenario !== "default" || persona !== "owner",
    "Creates top-level Spaces: needs the default world and a workspace owner",
  );
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "en");
    } catch {
      // Private mode: the default locale applies.
    }
  });
});

test("Space org tree: create a Space, add a member, create a Board and a Doc in it and move them (OT-5)", async ({
  page,
}) => {
  await page.goto(workspace);
  const nav = page.getByRole("navigation", { name: "Workspace navigation" });
  // The sidebar names the Space tree "Departments" (Final-Space).
  const tree = nav.getByRole("tree", { name: "Departments" });
  await expect(
    tree.getByRole("treeitem", { name: /Công nghệ/ }),
  ).toBeVisible();

  // Create a top-level department Space (Private by default).
  await nav.getByRole("button", { name: "Create department" }).click();
  const create = page.getByRole("dialog", { name: "New Space" });
  await create.getByLabel("Name").fill("IT");
  await expect(create.getByRole("radio", { name: /Private/ })).toBeChecked();
  await create.getByRole("button", { name: "Create Space" }).click();
  await expect(
    page.getByRole("heading", { level: 1, name: /^IT/ }),
  ).toBeVisible();
  await expect(
    tree.getByRole("treeitem", { name: /^IT\b.*Private/ }),
  ).toHaveAttribute("aria-current", "page");

  // Add a workspace member to the Space (the tab strip is links, not tabs).
  const tabs = page.getByRole("navigation", { name: "IT" });
  await tabs.getByRole("link", { name: /^Members/ }).click();
  await page.getByRole("button", { name: "Add members" }).click();
  const add = page.getByRole("dialog", { name: "Add to IT" });
  await add
    .getByRole("combobox", { name: "Add people or teams" })
    .fill("jordan");
  await page.keyboard.press("Enter");
  await add.getByRole("button", { name: "Add 1 person" }).click();
  await expect(page.getByText("Jordan Lee (Member) was added.")).toBeVisible();
  await expect(
    page
      .getByRole("table", { name: "Members of IT" })
      .getByText("Jordan Lee (Member)"),
  ).toBeVisible();

  // Create a Board in the Space from the Create menu.
  await tabs.getByRole("link", { name: "Overview" }).click();
  await page.getByRole("button", { name: "Create", exact: true }).click();
  await page.getByRole("menuitem", { name: /^Board/ }).click();
  const board = page.getByRole("dialog", { name: "New Board" });
  await expect(board.getByText("It will be placed in IT.")).toBeVisible();
  await board.getByLabel("Board name").fill("Service desk");
  await board.getByRole("button", { name: "Create Board" }).click();
  await expect(page).toHaveURL(/\/boards\//);

  // Back in the department, the Board is listed with its sub-line.
  await tree.getByRole("treeitem", { name: /^IT/ }).click();
  const boards = page.getByRole("region", { name: "Running Boards" });
  await expect(
    boards.getByRole("link", { name: /Service desk/ }),
  ).toBeVisible();

  // The directory moves it to Công nghệ (the department page has no move).
  await nav.getByRole("link", { name: "Docs", exact: true }).click();
  await page
    .getByRole("button", { name: "Move Service desk to a Space" })
    .click();
  const move = page.getByRole("dialog", {
    name: "Move Service desk to a Space",
  });
  await move.getByRole("radio", { name: /^Công nghệ/ }).click();
  await move.getByRole("button", { name: "Move here" }).click();
  await expect(page.getByText("Service desk was moved.")).toBeVisible();

  await tree.getByRole("treeitem", { name: /^IT/ }).click();
  await expect(
    page
      .getByRole("region", { name: "Running Boards" })
      .getByText("No Boards in this department yet."),
  ).toBeVisible();
  await tree.getByRole("treeitem", { name: /Công nghệ/ }).click();
  await expect(
    page.getByRole("region", { name: "Running Boards" }).getByRole("link", {
      name: /Service desk/,
    }),
  ).toBeVisible();

  // Create a Doc in IT from the Create menu, then move it to Công nghệ with
  // its version (Documents.Pages.MoveToSpace reads PageDto.version).
  await tree.getByRole("treeitem", { name: /^IT/ }).click();
  await page.getByRole("button", { name: "Create", exact: true }).click();
  await page.keyboard.press("d");
  const doc = page.getByRole("dialog", { name: "New Doc" });
  await expect(doc.getByText("It will be placed in IT.")).toBeVisible();
  await doc.getByLabel("Doc title").fill("Runbook");
  await doc.getByRole("button", { name: "Create Doc" }).click();
  await expect(page).toHaveURL(/\/docs\//);

  await tree.getByRole("treeitem", { name: /^IT/ }).click();
  const docs = page.getByRole("region", { name: "Recent Docs" });
  await expect(docs.getByRole("link", { name: /Runbook/ })).toBeVisible();
  await nav.getByRole("link", { name: "Docs", exact: true }).click();
  await page.getByRole("button", { name: "Move Runbook to a Space" }).click();
  const moveDoc = page.getByRole("dialog", { name: "Move Runbook to a Space" });
  await moveDoc.getByRole("radio", { name: /^Công nghệ/ }).click();
  await moveDoc.getByRole("button", { name: "Move here" }).click();
  await expect(page.getByText("Runbook was moved.")).toBeVisible();

  await tree.getByRole("treeitem", { name: /^IT/ }).click();
  await expect(
    page
      .getByRole("region", { name: "Recent Docs" })
      .getByText("No Docs in this department yet."),
  ).toBeVisible();
  await tree.getByRole("treeitem", { name: /Công nghệ/ }).click();
  await expect(
    page.getByRole("region", { name: "Recent Docs" }).getByRole("link", {
      name: /Runbook/,
    }),
  ).toBeVisible();
});
