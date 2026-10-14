import type {
  IdentityTone,
  WorkspaceDestination,
} from "../../core/model/navigation";

/** Someone the member is waiting on (the rail's third card; no producer yet). */
export interface MyWorkWaitingView {
  readonly id: string;
  readonly title: string;
  readonly key: string;
  readonly destination: WorkspaceDestination;
  readonly ownerName: string;
  readonly ownerTone: IdentityTone;
  /** "Chờ Phúc gửi kết quả". */
  readonly note: string;
  /** "hạn hôm nay". */
  readonly dueNote: string;
}

/** A Board the member can add an item to, in the "Thêm việc" popover. */
export interface MyWorkCreateBoard {
  readonly id: string;
  readonly name: string;
  readonly departmentName: string | null;
  readonly detail: string | null;
}
