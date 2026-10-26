import { describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import { fireEvent, renderPureUi, screen, waitFor } from "@notrelix/testing";
import {
  WorkManagementTestScope,
  type WorkManagementServices,
} from "@notrelix/work-management-state";
import { erpBoardScenario } from "@notrelix/work-management-testing";
import { BoardItemCreateDialog } from "../board-item-create-dialog";
import { itemCreateCatalog } from "../item-create.catalog";

function setup(assigneeIndex = 0) {
  const scenario = erpBoardScenario();
  const member = scenario.board.members[assigneeIndex]!;
  const cards = {
    createCard: vi.fn(
      async (_boardId: string, _payload: unknown) => "item-new",
    ),
    updateStatus: vi.fn(async () => undefined),
    updateFieldValue: vi.fn(async () => undefined),
    setAssignees: vi.fn(async () => undefined),
  };
  const services = {
    boards: { getFullBoard: vi.fn(async () => scenario) },
    cards,
    labels: {
      addLabelToCard: vi.fn(async () => undefined),
      getBoardLabels: vi.fn(async () => []),
    },
  } as unknown as WorkManagementServices;
  const onCreated = vi.fn();
  renderPureUi(
    <WorkManagementTestScope services={services}>
      <I18nProvider catalogs={[itemCreateCatalog]} initialLocale="vi">
        <BoardItemCreateDialog
          open
          onOpenChange={() => undefined}
          workspaceId={scenario.board.workspaceId}
          boards={[
            { id: scenario.board.id, label: "ERP · Vận hành" },
            { id: "other-board", label: "Board khác" },
          ]}
          initialTitle="Soạn kế hoạch quý"
          assigneeId={member.userId}
          onCreated={onCreated}
        />
      </I18nProvider>
    </WorkManagementTestScope>,
  );
  return { cards, onCreated, scenario, member, services };
}

describe("BoardItemCreateDialog (Home and My work)", () => {
  it("opens the full form on the first board with the typed name and the member ticked, and creates with them", async () => {
    const { cards, onCreated, scenario, member } = setup();
    const name = (await screen.findByRole("textbox", {
      name: /^Tên công việc/,
    })) as HTMLInputElement;
    expect(name.value).toBe("Soạn kế hoạch quý");
    expect(screen.getByLabelText("Thêm vào board")).toBeTruthy();
    // The member shows as a chip with a remove button.
    await waitFor(() =>
      expect(
        screen.getByRole("button", { name: `Bỏ ${member.name}` }),
      ).toBeTruthy(),
    );
    fireEvent.click(screen.getByRole("button", { name: /^Tạo công việc/ }));
    await waitFor(() => expect(cards.createCard).toHaveBeenCalledTimes(1));
    expect(cards.createCard.mock.calls[0]?.[0]).toBe(scenario.board.id);
    expect(cards.createCard.mock.calls[0]?.[1]).toMatchObject({
      title: "Soạn kế hoạch quý",
    });
    await waitFor(() =>
      expect(cards.setAssignees).toHaveBeenCalledWith(
        "item-new",
        [member.userId],
        [],
      ),
    );
    await waitFor(() =>
      expect(onCreated).toHaveBeenCalledWith({
        boardId: scenario.board.id,
        itemId: "item-new",
      }),
    );
  });
});
