import type { MyWorkItemView, WorkStatusBucket } from "./home-models";
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

/** What the member can do with their work; every write is the host's. */
export interface MyWorkActions {
  /** Items (or waiting entries) with a write in flight. */
  readonly pendingIds: ReadonlySet<string>;
  readonly onApprove: (item: MyWorkItemView) => void;
  readonly onRequestChanges: (item: MyWorkItemView) => void;
  readonly onReopen: (item: MyWorkItemView) => void;
  readonly onRemind: (entry: MyWorkWaitingView) => void;
  /** `null` clears the due date. */
  readonly onSetDue: (
    items: readonly MyWorkItemView[],
    dueAt: string | null,
  ) => void;
  readonly onSetStatus: (
    items: readonly MyWorkItemView[],
    bucket: WorkStatusBucket,
  ) => void;
  readonly onUnfollow: (items: readonly MyWorkItemView[]) => void;
}
