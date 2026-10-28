/**
 * Board templates of the mock backend: the workspace's saved templates (like the
 * producer, a saved template starts as a draft) and the built-in starters every
 * workspace offers. A template is creation input only: creating a board from it
 * copies nothing live, so the mock keeps just the template's identity and name.
 */

export type MockBoardTemplateStatus = "Draft" | "Published" | "Archived";

export interface MockBoardTemplateRecord {
  readonly id: string;
  readonly workspaceId: string;
  readonly name: string;
  readonly description: string | null;
  status: MockBoardTemplateStatus;
}

export interface MockBuiltInBoardTemplate {
  readonly id: string;
  readonly key: string;
  readonly name: string;
  readonly description: string;
}

/** The ids the producer fixes for its built-in starters. */
export const MOCK_BUILT_IN_BOARD_TEMPLATES: readonly MockBuiltInBoardTemplate[] =
  [
    {
      id: "00000000-0000-0000-0000-00000000b101",
      key: "blank",
      name: "Bảng trống",
      description: "Tiêu đề, trạng thái, người phụ trách và hạn.",
    },
    {
      id: "00000000-0000-0000-0000-00000000b102",
      key: "project",
      name: "Theo dõi dự án",
      description: "Thêm mức ưu tiên và nhãn lỗi / tính năng.",
    },
    {
      id: "00000000-0000-0000-0000-00000000b103",
      key: "content",
      name: "Kế hoạch nội dung",
      description: "Từ ý tưởng đến đã đăng, có ngày đăng.",
    },
  ];

export class MockBoardTemplateState {
  private readonly templates = new Map<string, MockBoardTemplateRecord>();
  private sequence = 0;

  list(workspaceId: string): MockBoardTemplateRecord[] {
    return Array.from(this.templates.values()).filter(
      (template) => template.workspaceId === workspaceId,
    );
  }

  get(id: string): MockBoardTemplateRecord | undefined {
    return this.templates.get(id);
  }

  save(
    workspaceId: string,
    input: { name: string; description: string | null },
  ): MockBoardTemplateRecord {
    this.sequence += 1;
    const record: MockBoardTemplateRecord = {
      id: `00000000-0000-0000-0000-0000000c${String(this.sequence).padStart(4, "0")}`,
      workspaceId,
      name: input.name,
      description: input.description,
      status: "Draft",
    };
    this.templates.set(record.id, record);
    return record;
  }

  remove(id: string): void {
    this.templates.delete(id);
  }

  clear(): void {
    this.templates.clear();
    this.sequence = 0;
  }
}
