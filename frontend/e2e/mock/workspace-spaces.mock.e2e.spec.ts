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
    tree.getByRole("treeitem", { name: /Engineering/ }),
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

  // Add a workspace member to the Space.
  await page.getByRole("tab", { name: "Members" }).click();
  const add = page.getByRole("form", { name: "Add people or a team" });
  await add.getByRole("combobox", { name: "Person" }).click();
  await page.getByRole("option", { name: "Jordan Lee (Member)" }).click();
  await add.getByRole("button", { name: "Add" }).click();
  await expect(page.getByText("Jordan Lee (Member) was added.")).toBeVisible();
  await expect(
    page
      .getByRole("list", { name: "Members" })
      .getByText("Jordan Lee (Member)"),
  ).toBeVisible();

  // Create a Board in the Space.
  await page.getByRole("tab", { name: "Overview" }).click();
  await page.getByRole("button", { name: "New Board" }).click();
  const board = page.getByRole("dialog", { name: "New Board" });
  await expect(board.getByText("It will be placed in IT.")).toBeVisible();
  await board.getByLabel("Board name").fill("Service desk");
  await board.getByRole("button", { name: "Create Board" }).click();
  await expect(page).toHaveURL(/\/boards\//);

  // Back in the Space, move the Board to Engineering.
  await tree.getByRole("treeitem", { name: /^IT/ }).click();
  const boards = page.getByRole("region", { name: /^Boards/ });
  await expect(
    boards.getByRole("link", { name: "Service desk" }),
  ).toBeVisible();
  await page.getByRole("button", { name: "Move Service desk" }).click();
  const move = page.getByRole("dialog", {
    name: "Move Service desk to a Space",
  });
  await move.getByRole("radio", { name: /^Engineering/ }).click();
  await move.getByRole("button", { name: "Move here" }).click();
  await expect(page.getByText("Service desk was moved.")).toBeVisible();
  await expect(boards.getByText("No Boards in this Space yet.")).toBeVisible();

  await tree.getByRole("treeitem", { name: /Engineering/ }).click();
  await expect(
    page.getByRole("region", { name: /^Boards/ }).getByRole("link", {
      name: "Service desk",
    }),
  ).toBeVisible();

  // Create a Doc in IT, then move it to Engineering with its version
  // (Documents.Pages.MoveToSpace reads PageDto.version as expectedVersion).
  await tree.getByRole("treeitem", { name: /^IT/ }).click();
  await page.getByRole("button", { name: "New Doc" }).click();
  const doc = page.getByRole("dialog", { name: "New Doc" });
  await expect(doc.getByText("It will be placed in IT.")).toBeVisible();
  await doc.getByLabel("Doc title").fill("Runbook");
  await doc.getByRole("button", { name: "Create Doc" }).click();
  await expect(page).toHaveURL(/\/docs\//);

  await tree.getByRole("treeitem", { name: /^IT/ }).click();
  const docs = page.getByRole("region", { name: /^Docs/ });
  await expect(docs.getByRole("link", { name: "Runbook" })).toBeVisible();
  await page.getByRole("button", { name: "Move Runbook" }).click();
  const moveDoc = page.getByRole("dialog", { name: "Move Runbook to a Space" });
  await moveDoc.getByRole("radio", { name: /^Engineering/ }).click();
  await moveDoc.getByRole("button", { name: "Move here" }).click();
  await expect(page.getByText("Runbook was moved.")).toBeVisible();
  await expect(docs.getByText("No Docs in this Space yet.")).toBeVisible();

  await tree.getByRole("treeitem", { name: /Engineering/ }).click();
  await expect(
    page.getByRole("region", { name: /^Docs/ }).getByRole("link", {
      name: "Runbook",
    }),
  ).toBeVisible();

  // The directory offers the same move for the root Doc (in-app navigation:
  // a reload would reset the in-page mock world).
  // The sidebar's short label for Boards & Docs is "Docs".
  await nav.getByRole("link", { name: "Docs", exact: true }).click();
  await expect(
    page.getByRole("heading", { level: 1, name: "Boards & Docs" }),
  ).toBeVisible();
  await expect(
    page.getByRole("button", { name: "Move Runbook to a Space" }),
  ).toBeAttached();
});
