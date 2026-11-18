import { expect, test } from "@playwright/test";
import { hook, openDm, sendMessage, startCall } from "./support/chat-calls";

/**
 * On a phone, going back to the conversation list during a call: the stage unmounts, the call must stay connected (the media
 * session lives in the controller, not in the stage), the bar must show on the list, and the bar must lead back to the call.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
test.skip(persona !== "owner", "Needs the default owner world");
test.use({ viewport: { width: 390, height: 844 } });

test("a call survives the conversation list on a phone and the bar leads back", async ({
  page,
}) => {
  const { space, peer } = await openDm(page);
  await sendMessage(page, "Gọi nhé");
  await startCall(page, "voice");
  const id = (await hook(page, "live", space)) as string;
  await hook(page, "act", id, peer, "answer");
  const stage = page.getByTestId("chat-call-stage");
  await expect(stage).toHaveAttribute("data-phase", "connected");
  await page.getByRole("button", { name: "Quay lại cuộc trò chuyện" }).click();
  // The phone's list is its own route: history back from the conversation lands on it (client-side, the store stays).
  await page.goBack();
  await expect(page).toHaveURL(/\/chat$/);
  // The list is showing: the conversation composer is gone, the bar is there.
  await expect(page.locator("main form textarea")).toHaveCount(0);
  const strip = page.getByTestId("chat-call-strip");
  await expect(strip).toBeVisible();
  await expect(strip).toHaveAttribute("data-phase", "connected");
  // Still connected after a few seconds on the list: the media plane is held by the controller, not by the stage.
  await page.waitForTimeout(3000);
  await expect(strip).toHaveAttribute("data-phase", "connected");
  expect(await hook(page, "live", space)).toBe(id);
  const sb = await strip.boundingBox();
  expect(sb && sb.x >= 0 && sb.x + sb.width <= 390).toBe(true);
  // The bar leads back to the call.
  await strip.getByRole("button", { name: "Quay lại cuộc gọi" }).click();
  await expect(page).toHaveURL(/\/chat\/dm\/[^/]+$/);
  await expect(page.getByTestId("chat-call-stage")).toBeVisible();
  await expect(page.getByTestId("chat-call-stage")).toHaveAttribute(
    "data-phase",
    "connected",
  );
});
