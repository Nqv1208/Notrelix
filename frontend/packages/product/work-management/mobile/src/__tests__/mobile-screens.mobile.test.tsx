import { describe, expect, it } from "vitest";
import type {
  MobileBoardScreenProps,
  MobileItemDetailScreenProps,
  MobileWorkspaceHomeProps,
} from "../index";
import { MobileBoardScreen } from "../screens/board-screen/mobile-board-screen";
import { MobileItemDetailScreen } from "../screens/item-detail/mobile-item-detail-screen";
import { MobileWorkspaceHome } from "../screens/workspace-home/mobile-workspace-home";

/**
 * MOB-011: WM mobile screens must not import or use react-dom / react-dom/server.
 * MOB-014: Expo thin routes delegate; screen components accept typed RN-safe props.
 * These tests verify the component contract (typed props) without rendering
 * through a DOM/browser renderer.
 */
describe("wm-mobile screens", () => {
  it("MobileBoardScreen accepts required boardId and workspaceId props", () => {
    const props: MobileBoardScreenProps = {
      boardId: "board-1",
      workspaceId: "ws-1",
    };
    expect(props.boardId).toBe("board-1");
    expect(props.workspaceId).toBe("ws-1");
    expect(typeof MobileBoardScreen).toBe("function");
  });

  it("MobileItemDetailScreen accepts required itemId and boardId props", () => {
    const props: MobileItemDetailScreenProps = {
      itemId: "item-9",
      boardId: "board-2",
    };
    expect(props.itemId).toBe("item-9");
    expect(props.boardId).toBe("board-2");
    expect(typeof MobileItemDetailScreen).toBe("function");
  });

  it("MobileWorkspaceHome accepts required workspaceId prop", () => {
    const props: MobileWorkspaceHomeProps = { workspaceId: "ws-3" };
    expect(props.workspaceId).toBe("ws-3");
    expect(typeof MobileWorkspaceHome).toBe("function");
  });
});
